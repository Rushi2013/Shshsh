"""
progress_manager.py
===================
The "brain" of the review queue.

Responsibilities
----------------
* Move records through the status machine::

      PENDING -> MATCHED -> APPROVED -> COMPLETED
                  \\-> NEEDS REVIEW -/   \\-> SAVE STATUS UNKNOWN
                                        \\-> FAILED / SKIPPED

* Pause / resume / stop the queue and store that state in SQLite.
* **Crash recovery**: if Termux or Android is killed while a portal save was in
  progress, the record is *never* retried automatically. It is marked
  ``SAVE STATUS UNKNOWN`` and the worker is asked to check the portal.
* Recalculate the live statistics after every change.

This module never talks to the network. Saving to the official portal is done
by :mod:`uwin_integration` (assisted mode) or after the worker confirms a real
save - see :func:`mark_completed`.
"""

from __future__ import annotations

from typing import Any, Optional

import config
import database as db
import matching_engine
import validation

# ---------------------------------------------------------------------------
# Index cache for the authorised register
# ---------------------------------------------------------------------------
_index_cache: dict = {"data": None, "count": -1}


def get_register_index(force: bool = False) -> dict:
    """Cached match index of the authorised beneficiary register."""
    count = db.count_official_records()
    if force or _index_cache["data"] is None or _index_cache["count"] != count:
        _index_cache["data"] = matching_engine.build_index(db.official_records_all())
        _index_cache["count"] = count
    return _index_cache["data"]


def invalidate_index() -> None:
    _index_cache["data"] = None
    _index_cache["count"] = -1


# ---------------------------------------------------------------------------
# Queue control (pause / resume / stop)
# ---------------------------------------------------------------------------
def pause_queue(reason: str = "manual") -> dict:
    state = db.set_queue_state(paused=True)
    db.log_event("queue_paused", f"Queue paused ({reason})")
    return state


def resume_queue() -> dict:
    state = db.set_queue_state(paused=False, stopped=False)
    db.log_event("queue_resumed", "Queue resumed")
    return state


def stop_queue() -> dict:
    state = db.set_queue_state(stopped=True, paused=False, current_record_id=None)
    db.log_event("queue_stopped", "Queue stopped by the user")
    return state


def set_mode(mode: str) -> dict:
    if mode not in ("manual", "auto"):
        mode = "manual"
    state = db.set_queue_state(mode=mode)
    db.log_event("queue_mode", f"Queue mode is now {mode}")
    return state


def queue_is_blocked() -> tuple[bool, str]:
    state = db.get_queue_state()
    if state["stopped"]:
        return True, "Processing is stopped. Press Start / Resume to continue."
    if state["paused"]:
        return True, "Processing is paused. Press Resume to continue."
    return False, ""


# ---------------------------------------------------------------------------
# Matching / validation pipeline
# ---------------------------------------------------------------------------
def process_record(record_id: int, force: bool = False) -> dict:
    """Validate + match one record and store the result.

    Records already COMPLETED or with an unknown save status are never touched
    unless the worker explicitly forces it (``force=True`` from the dashboard).
    """
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}

    if record["status"] in config.FINAL_STATUSES and not force:
        return {
            "ok": False,
            "message": f"Record is {record['status']}. It will not be processed again automatically.",
            "record": record,
        }

    settings = db.get_all_settings()
    working = dict(record)
    working["warnings"] = []  # recompute from the stored values

    result = validation.validate_record_fields(working, settings)
    warnings = list(result["warnings"])

    stored_warnings = db.json_loads(record.get("warnings_json"), []) or []
    for item in stored_warnings:
        if item.get("code") == validation.DUPLICATE:
            warnings.append(item)

    working["warnings"] = warnings
    working["status"] = config.STATUS_NEEDS_REVIEW if result["needs_review"] else config.STATUS_PENDING

    index = get_register_index()
    working = matching_engine.apply_match_to_record(working, index)

    # Store the verified ISO date when we have one; otherwise the raw text stays
    # so the warning remains visible instead of being silently "fixed".
    dob_value = working.get("_dob_iso") or validation.clean_text(record.get("dob"))
    vax_value = working.get("_vax_iso") or validation.clean_text(record.get("vaccination_date"))
    if working.get("_dob_iso") and not record.get("dob_raw"):
        dob_value = working["_dob_iso"]

    db.update_record(
        record_id,
        warnings=warnings,
        match=working.get("match"),
        match_confidence=working.get("match_confidence", 0.0),
        matched_official_id=working.get("matched_official_id"),
        status=working["status"],
        dob=dob_value or "",
        vaccination_date=vax_value or "",
        dob_raw=record.get("dob_raw") or validation.clean_text(record.get("dob")),
        vaccination_date_raw=record.get("vaccination_date_raw") or validation.clean_text(record.get("vaccination_date")),
        error_message=None if working["status"] != config.STATUS_FAILED else record.get("error_message"),
    )
    return {"ok": True, "record": db.get_record(record_id), "message": "Record processed."}


def process_next(session_id: int, force: bool = False) -> dict:
    """Process the next open record of the session."""
    blocked, message = queue_is_blocked()
    if blocked:
        return {"ok": False, "message": message, "blocked": True}

    session = db.get_session(session_id)
    if not session:
        return {"ok": False, "message": "Session not found."}

    record = db.next_open_record(session_id)
    if not record:
        return {"ok": False, "done": True, "message": "No open records left. Everything is reviewed."}

    db.set_queue_state(current_record_id=record["id"])
    return process_record(record["id"], force=force)


def process_batch(session_id: int, limit: int = 100, progress_cb=None) -> dict:
    """Process several records in one go (used by "Match All" on the dashboard)."""
    blocked, message = queue_is_blocked()
    if blocked:
        return {"ok": False, "message": message, "blocked": True}

    processed, remaining = 0, 0
    for _ in range(max(1, int(limit))):
        state = db.get_queue_state()
        if state["paused"] or state["stopped"]:
            message = "Paused" if state["paused"] else "Stopped"
            return {"ok": True, "processed": processed, "message": f"{message} after {processed} record(s)."}

        record = db.next_open_record(session_id)
        if not record:
            break
        result = process_record(record["id"])
        if not result.get("ok"):
            db.update_record(record["id"], status=config.STATUS_FAILED,
                             error_message=result.get("message", "Processing failed."))
            remaining += 1
            continue
        processed += 1
        if progress_cb:
            progress_cb(processed)

    counts = db.status_counts(session_id)
    return {
        "ok": True,
        "processed": processed,
        "failed": remaining,
        "counts": counts,
        "message": f"{processed} record(s) processed. {counts['PENDING'] + counts['NEEDS REVIEW'] + counts['MATCHED']} still open.",
    }


def retry_failed(session_id: Optional[int] = None) -> dict:
    """Re-run validation for FAILED records (safe: no portal action involved)."""
    where = "status = ?"
    params: list[Any] = [config.STATUS_FAILED]
    if session_id:
        where += " AND session_id = ?"
        params.append(session_id)
    rows = db.query_all(f"SELECT id FROM records WHERE {where}", params)
    if not rows:
        return {"ok": True, "retried": 0, "message": "No failed records to retry."}

    retried = 0
    for row in rows:
        db.update_record(row["id"], status=config.STATUS_PENDING, error_message=None)
        result = process_record(row["id"])
        if result.get("ok"):
            retried += 1
    db.log_event("retry_failed", f"{retried} failed record(s) retried", session_id=session_id)
    return {"ok": True, "retried": retried, "message": f"{retried} record(s) re-checked."}


# ---------------------------------------------------------------------------
# Review decisions
# ---------------------------------------------------------------------------
EDITABLE_FIELDS = (
    "name", "beneficiary_id", "dob", "gender", "mobile", "mother_name", "vaccine_name",
    "dose_number", "vaccination_date", "batch_number", "vaccinator_name", "facility",
    "district", "block", "address", "remarks",
)


def approve_record(record_id: int, note: str = "") -> dict:
    """Worker confirmed this record is correct (pending the final portal save)."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    if record["status"] == config.STATUS_COMPLETED:
        return {"ok": False, "message": "This record is already completed."}
    if record["status"] == config.STATUS_SAVE_UNKNOWN:
        return {
            "ok": False,
            "message": "This record has an unconfirmed save. Check it on the official portal first, "
                       "then decide (the app will not send it again by itself).",
            "save_unknown": True,
        }

    settings = db.get_all_settings()
    check = validation.validate_record_fields(dict(record), settings)
    blocking = [item for item in check["warnings"] if item["code"] in (validation.MISSING, validation.INVALID, validation.CONFLICT)]
    critical = [item for item in blocking if item["field"] in ("name", "vaccine_name", "vaccination_date")]
    if critical and not note:
        return {
            "ok": False,
            "message": "Cannot approve yet: " + "; ".join(item["message"] for item in critical[:3]),
            "warnings": check["warnings"],
        }

    db.update_record(
        record_id,
        status=config.STATUS_APPROVED,
        decision="APPROVED",
        decision_note=note,
        decided_at=db.now_iso(),
        manually_verified=1,
    )
    db.log_event("record_approved", f"Record #{record_id} approved", record_id=record_id, session_id=record["session_id"])
    return {"ok": True, "message": "Record approved.", "record": db.get_record(record_id)}


def save_edited_record(record_id: int, values: dict, note: str = "") -> dict:
    """Store the worker's correction, re-validate, and keep it in review."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}

    settings = db.get_all_settings()
    updates: dict[str, Any] = {}
    edited = db.json_loads(record.get("edited_json"), {}) or {}

    date_notes: list[str] = []
    for field in EDITABLE_FIELDS:
        if field not in values:
            continue
        new_value = validation.clean_text(values.get(field))
        old_value = validation.clean_text(record.get(field))

        # A date typed by the worker is parsed once, here. Ambiguous input is
        # kept as text and flagged instead of being guessed.
        if field in ("dob", "vaccination_date") and new_value and new_value != old_value:
            parsed = validation.parse_date(new_value, settings.get("date_format", "auto"))
            if parsed.get("iso") and not parsed.get("ambiguous"):
                new_value = parsed["iso"]
            else:
                reason = parsed.get("error") or "could not be read"
                date_notes.append(f"{field.replace('_', ' ')}: {reason}")
            updates[f"{field}_raw"] = validation.clean_text(values.get(field))

        if new_value != old_value:
            updates[field] = new_value
            edited[field] = {"from": old_value, "to": new_value, "at": db.now_iso()}

    if not updates:
        return {"ok": True, "message": "Nothing changed.", "record": record, "unchanged": True}

    updates["edited"] = edited
    updates["decision"] = "EDITED"
    updates["decision_note"] = note
    updates["decided_at"] = db.now_iso()
    db.update_record(record_id, **updates)

    result = process_record(record_id, force=True)
    changed = {key: value for key, value in updates.items() if key in EDITABLE_FIELDS}
    message = f"Saved {len(changed)} change(s) and re-checked the record."
    if date_notes:
        message += " Please check the date: " + "; ".join(date_notes) + "."
    return {
        "ok": True,
        "message": message,
        "record": result.get("record") or db.get_record(record_id),
        "changed": changed,
        "date_notes": date_notes,
    }


def reject_record(record_id: int, reason: str) -> dict:
    """Mark a record as not to be used (kept in history, never deleted)."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    if not reason.strip():
        return {"ok": False, "message": "Please write a short reason for rejecting."}
    db.update_record(
        record_id,
        status=config.STATUS_FAILED,
        decision="REJECTED",
        decision_note=reason.strip(),
        decided_at=db.now_iso(),
        error_message=f"Rejected by worker: {reason.strip()}",
    )
    db.log_event("record_rejected", f"Record #{record_id} rejected", level="warn",
                 record_id=record_id, session_id=record["session_id"])
    return {"ok": True, "message": "Record rejected and moved to Failed."}


def skip_record(record_id: int, reason: str = "") -> dict:
    """Skip for now - the record can come back later."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    db.update_record(
        record_id,
        status=config.STATUS_SKIPPED,
        decision="SKIPPED",
        decision_note=reason.strip(),
        decided_at=db.now_iso(),
    )
    db.log_event("record_skipped", f"Record #{record_id} skipped", record_id=record_id, session_id=record["session_id"])
    return {"ok": True, "message": "Record skipped. You can reopen it any time."}


def reopen_record(record_id: int, reason: str = "") -> dict:
    """Bring a skipped / failed record back into the queue."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    if record["status"] == config.STATUS_COMPLETED:
        return {"ok": False, "message": "Completed records stay completed. Re-open the record only after checking the portal yourself."}
    db.update_record(
        record_id,
        status=config.STATUS_NEEDS_REVIEW if record.get("warnings_json") else config.STATUS_PENDING,
        decision=None,
        decision_note=reason or record.get("decision_note"),
    )
    return {"ok": True, "message": "Record is back in the queue.", "record": db.get_record(record_id)}


# ---------------------------------------------------------------------------
# Save confirmation (portal)
# ---------------------------------------------------------------------------
def mark_saving(record_id: int) -> None:
    """Flush a marker to SQLite *before* any portal action.

    If the phone dies right now, :func:`recover_state` will see ``saving`` and
    mark the record as *SAVE STATUS UNKNOWN* instead of repeating the save.
    """
    record = db.get_record(record_id)
    attempts = (record or {}).get("save_attempts", 0) or 0
    db.update_record(
        record_id,
        save_state="saving",
        save_attempts=attempts + 1,
        status=config.STATUS_APPROVED,
        error_message=None,
    )
    db.log_event("save_started", f"Save attempt #{attempts + 1} for record #{record_id}",
                 record_id=record_id, session_id=(record or {}).get("session_id"))


def mark_completed(record_id: int, reference: str = "", note: str = "") -> dict:
    """Mark COMPLETED **only** after reliable confirmation from the portal.

    ``reference`` is the portal's own confirmation (acknowledgement number,
    screenshot reference, or the adapter's response id). Without it the record
    stays ``SAVE STATUS UNKNOWN``.
    """
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    if not reference.strip():
        return {
            "ok": False,
            "message": "A confirmation reference is required. Without it the record stays 'SAVE STATUS UNKNOWN'.",
        }
    db.update_record(
        record_id,
        status=config.STATUS_COMPLETED,
        save_state="confirmed",
        save_reference=reference.strip(),
        save_confirmed_at=db.now_iso(),
        decision_note=note or record.get("decision_note"),
        error_message=None,
        manually_verified=1,
    )
    db.log_event("record_completed", f"Record #{record_id} completed with reference",
                 record_id=record_id, session_id=record["session_id"])
    return {"ok": True, "message": "Record marked COMPLETED."}


def mark_save_unknown(record_id: int, reason: str) -> dict:
    """The portal did not confirm the save -> never retry automatically."""
    record = db.get_record(record_id)
    if not record:
        return {"ok": False, "message": "Record not found."}
    db.update_record(
        record_id,
        status=config.STATUS_SAVE_UNKNOWN,
        save_state="unknown",
        error_message=(reason or "Save could not be confirmed.")[:400],
    )
    db.log_event("save_unknown", f"Record #{record_id}: save status unknown - manual check needed",
                 level="warn", record_id=record_id, session_id=record["session_id"])
    return {
        "ok": True,
        "warning": True,
        "message": "Marked as SAVE STATUS UNKNOWN. Open the portal, search this beneficiary yourself and confirm.",
        "record": db.get_record(record_id),
    }


def mark_failed(record_id: int, message: str) -> dict:
    db.update_record(record_id, status=config.STATUS_FAILED, error_message=(message or "Failed")[:400])
    return {"ok": True, "message": "Record marked as failed."}


# ---------------------------------------------------------------------------
# Statistics & recovery
# ---------------------------------------------------------------------------
def statistics(session_id: Optional[int] = None) -> dict:
    counts = db.status_counts(session_id)
    counts["PROGRESS"] = db.progress_percent(counts)
    counts["OPEN"] = (
        counts["PENDING"] + counts["MATCHED"] + counts["NEEDS REVIEW"] + counts["APPROVED"]
    )
    counts["ATTENTION"] = counts["FAILED"] + counts["SAVE STATUS UNKNOWN"]
    return counts


def recover_state() -> dict:
    """Called once when the dashboard starts.

    * Any record left in the middle of a portal save becomes
      ``SAVE STATUS UNKNOWN`` (never silently retried).
    * ``APPROVED`` records that already had a save attempt are also flagged,
      because we cannot know whether the portal received them.
    """
    recovered = {"mid_save": 0, "unknown": 0, "messages": []}

    rows = db.query_all("SELECT id FROM records WHERE save_state = 'saving'")
    for row in rows:
        db.update_record(
            row["id"],
            status=config.STATUS_SAVE_UNKNOWN,
            save_state="unknown",
            error_message="The app stopped while saving. Check the portal, then decide manually.",
        )
        recovered["mid_save"] += 1

    rows = db.query_all(
        "SELECT id FROM records WHERE status = ? AND save_attempts > 0 AND (save_state IS NULL OR save_state = '')",
        (config.STATUS_APPROVED,),
    )
    for row in rows:
        db.update_record(
            row["id"],
            status=config.STATUS_SAVE_UNKNOWN,
            save_state="unknown",
            error_message="A save was attempted before the app closed. Verify on the portal - it will not be retried automatically.",
        )
        recovered["unknown"] += 1

    db.cleanup_old_events()

    if recovered["mid_save"] or recovered["unknown"]:
        recovered["messages"].append(
            f"{recovered['mid_save'] + recovered['unknown']} record(s) need a manual check on the portal."
        )
        db.log_event(
            "recovery",
            "Recovered after restart: uncertain saves flagged, nothing retried automatically",
            level="warn",
            detail=recovered,
        )

    settings = db.get_all_settings()
    cleanup = db.purge_expired_sessions(int(settings.get("cleanup_days") or 0))
    recovered["cleanup"] = cleanup

    recovered["sessions"] = db.find_resumable_sessions()
    recovered["counts"] = db.status_counts()
    return recovered
