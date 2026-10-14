"""
report_generator.py
===================
Builds downloadable Excel / CSV reports from the local SQLite database.

Reports contain **only** what a supervisor needs to check the work:

* Original Excel row number
* Beneficiary name
* Record status
* Validation result
* Match result
* Review decision
* Error message
* Processing date and time

Privacy rules enforced here
---------------------------
* The column list is hard-coded - a password or OTP simply has no place to land.
* Free-text notes are length-limited and stripped of anything that looks like a
  credential (defensive, in case a worker ever pastes one by mistake).
* Reports live in ``reports/`` on this device only and can be deleted from the
  dashboard.
"""

from __future__ import annotations

import re
from datetime import datetime
from pathlib import Path
from typing import Any, Optional

import config
import database as db
import excel_handler
import matching_engine
import validation

# ---------------------------------------------------------------------------
# Columns
# ---------------------------------------------------------------------------
REPORT_COLUMNS: list[str] = [
    "Excel Row",
    "Beneficiary Name",
    "Beneficiary ID",
    "Date of Birth",
    "Gender",
    "Mobile",
    "Mother's Name",
    "Vaccine",
    "Dose",
    "Vaccination Date",
    "Record Status",
    "Validation Result",
    "Validation Warnings",
    "Match Result",
    "Match Confidence %",
    "Matched Register ID",
    "Review Decision",
    "Decision Note",
    "Error Message",
    "Processing Date & Time",
]

SHEET_ORDER = [
    ("Completed", config.STATUS_COMPLETED),
    ("Pending", config.STATUS_PENDING),
    ("Matched", config.STATUS_MATCHED),
    ("Needs Review", config.STATUS_NEEDS_REVIEW),
    ("Approved", config.STATUS_APPROVED),
    ("Failed", config.STATUS_FAILED),
    ("Skipped", config.STATUS_SKIPPED),
    ("Save Status Unknown", config.STATUS_SAVE_UNKNOWN),
]

#: Fields that must never appear in an exported file.
FORBIDDEN_PATTERNS = (
    re.compile(r"password", re.I),
    re.compile(r"\botp\b", re.I),
    re.compile(r"\btoken\b", re.I),
    re.compile(r"aadhaar", re.I),
    re.compile(r"\bpin\b", re.I),
)


def _safe_note(text: Any, limit: int = 300) -> str:
    if text is None:
        return ""
    value = str(text).strip()
    if not value:
        return ""
    for pattern in FORBIDDEN_PATTERNS:
        if pattern.search(value):
            return "[removed: sensitive content]"
    return value[:limit]


def _warnings_to_text(record: dict, include_warnings: bool = True) -> str:
    if not include_warnings:
        return f"{len(db.json_loads(record.get('warnings_json'), []) or [])} warning(s)"
    warnings = db.json_loads(record.get("warnings_json"), []) or []
    parts = []
    for item in warnings[:12]:
        code = item.get("code", "INFO")
        field = item.get("field", "")
        message = _safe_note(item.get("message", ""), 160)
        parts.append(f"[{code}] {field}: {message}".strip())
    if len(warnings) > 12:
        parts.append(f"... and {len(warnings) - 12} more")
    return " | ".join(parts)


def _validation_result(record: dict) -> str:
    warnings = db.json_loads(record.get("warnings_json"), []) or []
    errors = [item for item in warnings if item.get("severity") == "error"]
    missing = [item for item in warnings if item.get("code") == validation.MISSING]
    if errors:
        return f"Problems found ({len(errors)})"
    if missing:
        return f"Missing data ({len(missing)})"
    if warnings:
        return f"Warnings ({len(warnings)})"
    return "OK"


def _match_result(record: dict) -> str:
    match = db.json_loads(record.get("match_json"), None)
    if not match:
        return "Not processed"
    label = matching_engine.MATCH_LABELS.get(match.get("status", ""), match.get("summary", ""))
    return _safe_note(label, 160)


def record_row(record: dict, settings: Optional[dict] = None, include_warnings: bool = True) -> list:
    settings = settings or {}
    date_format = settings.get("date_format", "auto")
    return [
        record.get("row_number"),
        _safe_note(record.get("name"), 120),
        _safe_note(record.get("beneficiary_id"), 40),
        validation.date_to_display(record.get("dob") or "", date_format),
        _safe_note(record.get("gender"), 20),
        _safe_note(record.get("mobile"), 20),
        _safe_note(record.get("mother_name"), 120),
        _safe_note(record.get("vaccine_name"), 80),
        _safe_note(record.get("dose_number"), 20),
        validation.date_to_display(record.get("vaccination_date") or "", date_format),
        record.get("status") or "",
        _validation_result(record),
        _warnings_to_text(record, include_warnings),
        _match_result(record),
        int(round((record.get("match_confidence") or 0) * 100)),
        record.get("matched_official_id") or "",
        _safe_note(record.get("decision"), 20),
        _safe_note(record.get("decision_note"), 200),
        _safe_note(record.get("error_message"), 300),
        record.get("updated_at") or record.get("created_at") or "",
    ]


# ---------------------------------------------------------------------------
# Session report
# ---------------------------------------------------------------------------
def build_session_report(session_id: int, scope: str = "all") -> dict:
    """Write an .xlsx (or .csv) report for one uploaded file."""
    session = db.get_session(session_id)
    if not session:
        return {"ok": False, "message": "Session not found."}

    settings = db.get_all_settings()
    include_warnings = bool(settings.get("report_include_warnings", True))
    format_kind = str(settings.get("report_format", "xlsx")).lower()

    records = db.list_records(session_id=session_id, limit=20000)
    if not records:
        return {"ok": False, "message": "There are no records to report yet."}

    counts = db.status_counts(session_id)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    base = f"uwin-report-session{session_id}-{stamp}"

    if format_kind == "csv":
        path = config.REPORT_DIR / f"{base}.csv"
        excel_handler.write_csv(
            path, REPORT_COLUMNS, [record_row(item, settings, include_warnings) for item in records]
        )
        kind = "csv"
    else:
        path = config.REPORT_DIR / f"{base}.xlsx"
        sheets: dict[str, tuple[list[str], list[list]]] = {}
        sheets["Summary"] = _summary_sheet(session, counts, records)
        sheets["All Records"] = (
            REPORT_COLUMNS,
            [record_row(item, settings, include_warnings) for item in records],
        )
        for sheet_name, status in SHEET_ORDER:
            subset = [item for item in records if item.get("status") == status]
            sheets[sheet_name] = (
                REPORT_COLUMNS,
                [record_row(item, settings, include_warnings) for item in subset],
            )
        excel_handler.write_xlsx(path, sheets)
        kind = "xlsx"

    size = path.stat().st_size if path.exists() else 0
    db.add_report(session_id, path.name, kind, scope, len(records), size)
    db.log_event("report_created", f"Report {path.name} created", session_id=session_id)

    return {
        "ok": True,
        "filename": path.name,
        "kind": kind,
        "rows": len(records),
        "size_bytes": size,
        "message": f"Report ready ({len(records)} rows).",
    }


def _summary_sheet(session: dict, counts: dict, records: list[dict]) -> tuple[list[str], list[list]]:
    headers = ["Item", "Value"]
    rows: list[list] = [
        ["Application", f"{config.APP_NAME} v{config.APP_VERSION}"],
        ["Report created", db.now_iso()],
        ["Uploaded file", session.get("original_filename") or ""],
        ["Sheet name", session.get("sheet_name") or ""],
        ["Header row", session.get("header_row") or ""],
        ["Session id", session.get("id")],
        ["Session created", session.get("created_at") or ""],
        ["Last change", session.get("updated_at") or ""],
        ["", ""],
        ["Total records", counts.get("TOTAL", 0)],
        ["Completed", counts.get("COMPLETED", 0)],
        ["Approved (waiting for confirmation)", counts.get("APPROVED", 0)],
        ["Matched", counts.get("MATCHED", 0)],
        ["Needs review", counts.get("NEEDS REVIEW", 0)],
        ["Pending", counts.get("PENDING", 0)],
        ["Skipped", counts.get("SKIPPED", 0)],
        ["Failed", counts.get("FAILED", 0)],
        ["Save status unknown", counts.get("SAVE STATUS UNKNOWN", 0)],
        ["Progress % (completed + skipped)", db.progress_percent(counts)],
        ["", ""],
        ["Match quality: exact ID", _count_match_type(records, matching_engine.MATCH_EXACT_ID)],
        ["Match quality: name + DOB", _count_match_type(records, matching_engine.MATCH_EXACT_NAME_DOB)],
        ["Match quality: name + field", _count_match_type(records, matching_engine.MATCH_EXACT_NAME_FIELD)],
        ["Match quality: fuzzy (suggestion only)", _count_match_type(records, matching_engine.MATCH_FUZZY)],
        ["Match quality: ambiguous", _count_match_type(records, matching_engine.MATCH_AMBIGUOUS)],
        ["Match quality: no match", _count_match_type(records, matching_engine.MATCH_NONE)],
        ["", ""],
        ["Important", "A record is only marked COMPLETED after the official portal confirms the save."],
        ["Important", "Records marked SAVE STATUS UNKNOWN must be checked manually. They are never retried automatically."],
        ["Privacy", "This report was generated on the local device. No password or OTP value is ever included."],
    ]
    return headers, rows


def _count_match_type(records: list[dict], match_type: str) -> int:
    total = 0
    for record in records:
        match = db.json_loads(record.get("match_json"), None) or {}
        if match.get("match_type") == match_type:
            total += 1
    return total


# ---------------------------------------------------------------------------
# Cross-session reports & filtered CSV exports
# ---------------------------------------------------------------------------
def build_overview_report() -> dict:
    """One workbook with every session and every record (still local-only)."""
    sessions = db.list_sessions(limit=100)
    if not sessions:
        return {"ok": False, "message": "No sessions to report yet."}

    settings = db.get_all_settings()
    include_warnings = bool(settings.get("report_include_warnings", True))
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    path = config.REPORT_DIR / f"uwin-overview-{stamp}.xlsx"

    session_rows = []
    for session in sessions:
        counts = db.status_counts(session["id"])
        session_rows.append(
            [
                session["id"],
                session.get("original_filename") or "",
                session.get("sheet_name") or "",
                session.get("status") or "",
                session.get("created_at") or "",
                session.get("updated_at") or "",
                counts.get("TOTAL", 0),
                counts.get("COMPLETED", 0),
                counts.get("APPROVED", 0),
                counts.get("MATCHED", 0),
                counts.get("NEEDS REVIEW", 0),
                counts.get("PENDING", 0),
                counts.get("SKIPPED", 0),
                counts.get("FAILED", 0),
                counts.get("SAVE STATUS UNKNOWN", 0),
                db.progress_percent(counts),
            ]
        )

    all_rows = []
    for session in sessions:
        for record in db.list_records(session_id=session["id"], limit=20000):
            row = record_row(record, settings, include_warnings)
            all_rows.append([session["id"], session.get("original_filename") or ""] + row)

    sheets = {
        "Sessions": (
            [
                "Session", "File", "Sheet", "Session status", "Created", "Last change",
                "Total", "Completed", "Approved", "Matched", "Needs review", "Pending",
                "Skipped", "Failed", "Save unknown", "Progress %",
            ],
            session_rows,
        ),
        "All Records": (["Session", "File"] + REPORT_COLUMNS, all_rows),
    }
    for sheet_name, status in SHEET_ORDER:
        subset = [row for row in all_rows if row[2 + REPORT_COLUMNS.index("Record Status")] == status]
        sheets[sheet_name] = (["Session", "File"] + REPORT_COLUMNS, subset)

    excel_handler.write_xlsx(path, sheets)
    size = path.stat().st_size if path.exists() else 0
    db.add_report(None, path.name, "xlsx", "overview", len(all_rows), size)
    db.log_event("report_created", f"Overview report {path.name} created")
    return {"ok": True, "filename": path.name, "rows": len(all_rows), "size_bytes": size,
            "message": f"Overview report ready ({len(all_rows)} rows)."}


def export_records_csv(session_id: int, status: Optional[str] = None, search: str = "") -> dict:
    """Small CSV export of the current filter (handy for sharing with a supervisor)."""
    session = db.get_session(session_id)
    if not session:
        return {"ok": False, "message": "Session not found."}

    settings = db.get_all_settings()
    records = db.list_records(session_id=session_id, status=status, search=search, limit=20000)
    if not records:
        return {"ok": False, "message": "Nothing to export for this filter."}

    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    label = (status or "all").lower().replace(" ", "-")
    path = config.REPORT_DIR / f"uwin-export-{label}-{stamp}.csv"
    excel_handler.write_csv(
        path, REPORT_COLUMNS, [record_row(item, settings, bool(settings.get("report_include_warnings", True)))
                               for item in records]
    )
    size = path.stat().st_size if path.exists() else 0
    db.add_report(session_id, path.name, "csv", f"filter:{label}", len(records), size)
    return {"ok": True, "filename": path.name, "rows": len(records), "size_bytes": size,
            "message": f"CSV export ready ({len(records)} rows)."}


def list_reports(session_id: Optional[int] = None) -> list[dict]:
    return db.list_reports(session_id)


def delete_report(report_id: int) -> dict:
    return db.delete_report(report_id)


def safe_report_path(filename: str) -> Optional[Path]:
    """Resolve a report file inside ``reports/`` only (no path traversal)."""
    candidate = (config.REPORT_DIR / Path(filename).name).resolve()
    if config.REPORT_DIR.resolve() != candidate.parent:
        return None
    if not candidate.exists() or not candidate.is_file():
        return None
    return candidate
