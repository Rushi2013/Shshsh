"""
matching_engine.py
==================
Compares an Excel beneficiary row with the **authorised beneficiary register**
that the health worker imported into this device.

Matching priority (exactly as required)
---------------------------------------
1. Exact authorised **Beneficiary ID**                      -> confidence 1.00
2. Exact **Name + Date of Birth**                           -> confidence 0.97
3. Exact **Name + another authorised field** (mobile, mother, gender + DOB year)
                                                            -> confidence 0.88-0.92
4. **Fuzzy name similarity** - a *suggestion only*. Never auto-approved.

Safety rules baked into the code
--------------------------------
* A fuzzy name match can **never** return ``auto_ok = True``.
* If two or more candidates are almost equally good (difference smaller than
  ``config.MATCH_AMBIGUITY_DELTA``) the result is marked ``AMBIGUOUS`` and the
  dashboard shows **every** candidate for manual verification.
* Every result lists the matched fields, the conflicting fields and the fields
  that were missing, so nothing is hidden from the worker.
* If no authorised register was imported, matching is skipped honestly
  (``status = NO_REGISTER``) instead of pretending a match happened.
"""

from __future__ import annotations

import difflib
from typing import Any, Iterable, Optional

import config
import validation

# ---------------------------------------------------------------------------
# Result / match-type vocabulary
# ---------------------------------------------------------------------------
MATCH_EXACT_ID = "EXACT_ID"
MATCH_EXACT_NAME_DOB = "EXACT_NAME_DOB"
MATCH_EXACT_NAME_FIELD = "EXACT_NAME_FIELD"
MATCH_FUZZY = "FUZZY"
MATCH_AMBIGUOUS = "AMBIGUOUS"
MATCH_MULTIPLE = "MULTIPLE"
MATCH_NONE = "NONE"
MATCH_NO_REGISTER = "NO_REGISTER"

MATCH_LABELS = {
    MATCH_EXACT_ID: "Exact beneficiary ID match",
    MATCH_EXACT_NAME_DOB: "Name + date of birth match",
    MATCH_EXACT_NAME_FIELD: "Name + one identifying field match",
    MATCH_FUZZY: "Similar name (suggestion only)",
    MATCH_AMBIGUOUS: "Ambiguous - several possible beneficiaries",
    MATCH_MULTIPLE: "Several possible beneficiaries",
    MATCH_NONE: "No match found in the authorised register",
    MATCH_NO_REGISTER: "Authorised register not imported - matching skipped",
}

#: Match types that may be auto-approved (still needs the worker's OK before
#: anything is saved to the portal).
AUTO_APPROVABLE = (MATCH_EXACT_ID, MATCH_EXACT_NAME_DOB, MATCH_EXACT_NAME_FIELD)


# ---------------------------------------------------------------------------
# Index of the authorised register
# ---------------------------------------------------------------------------
def build_index(official_rows: Iterable[dict]) -> dict:
    """Build lookup tables so matching stays fast with thousands of rows."""
    index = {
        "by_id": {},
        "by_name_dob": {},
        "by_name": {},
        "by_mobile": {},
        "names": [],
        "rows": [],
        "count": 0,
    }
    for row in official_rows:
        item = dict(row)
        item["_name_key"] = row.get("name_key") or validation.name_key(row.get("name"))
        item["_dob_key"] = row.get("dob_key") or (row.get("dob") or "")
        item["_id_key"] = validation.digits_only(row.get("beneficiary_id"))
        item["_mobile_key"] = _tidy_mobile(row.get("mobile"))
        item["_tokens"] = validation.name_tokens(row.get("name"))

        index["rows"].append(item)
        if item["_id_key"]:
            index["by_id"].setdefault(item["_id_key"], []).append(item)
        if item["_name_key"]:
            index["by_name"].setdefault(item["_name_key"], []).append(item)
            if item["_dob_key"]:
                index["by_name_dob"].setdefault((item["_name_key"], item["_dob_key"]), []).append(item)
        if item["_mobile_key"]:
            index["by_mobile"].setdefault(item["_mobile_key"], []).append(item)

    index["names"] = sorted(index["by_name"].keys())
    index["count"] = len(index["rows"])
    return index


def _tidy_mobile(value: Any) -> str:
    result = validation.validate_mobile(value)
    return result["digits"] if result.get("ok") else validation.digits_only(value)[-10:]


# ---------------------------------------------------------------------------
# Fuzzy helpers (pure python - no extra dependency needed on Android)
# ---------------------------------------------------------------------------
def ratio(a: str, b: str) -> float:
    return difflib.SequenceMatcher(None, a, b).ratio()


def token_set_ratio(a: str, b: str) -> float:
    """Fuzzy name score that ignores word order and extra middle names."""
    ta, tb = set(validation.name_tokens(a)), set(validation.name_tokens(b))
    if not ta and not tb:
        return 1.0
    if not ta or not tb:
        return 0.0
    intersection = " ".join(sorted(ta & tb))
    only_a = " ".join(sorted(ta - tb))
    only_b = " ".join(sorted(tb - ta))
    if not intersection:
        return 0.0
    combined_a = (intersection + " " + only_a).strip()
    combined_b = (intersection + " " + only_b).strip()
    return max(
        ratio(intersection, combined_a),
        ratio(intersection, combined_b),
        ratio(combined_a, combined_b),
    )


def name_similarity(name_a: Any, name_b: Any) -> float:
    """0..1 similarity for two names (order independent, tolerant of transliteration)."""
    key_a, key_b = validation.name_key(name_a), validation.name_key(name_b)
    if not key_a or not key_b:
        return 0.0
    if key_a == key_b:
        return 1.0

    scores = [ratio(key_a, key_b), token_set_ratio(name_a, name_b)]
    tokens_a, tokens_b = validation.name_tokens(name_a), validation.name_tokens(name_b)
    if tokens_a and tokens_b:
        first_match = ratio(tokens_a[0], tokens_b[0])
        last_match = ratio(tokens_a[-1], tokens_b[-1])
        if first_match > 0.8 and last_match > 0.8:
            scores.append(0.92)  # e.g. "Ram Kumar" vs "Ram Singh Kumar" is common
        # Surname/short-name pairs like "Baby of Sunita" are handled here too.
    return round(max(scores), 4)


# ---------------------------------------------------------------------------
# Candidate comparison
# ---------------------------------------------------------------------------
def compare_pair(record: dict, official: dict) -> dict:
    """Explain exactly how an Excel row and an authorised row agree / differ."""
    matched: list[str] = []
    conflicts: list[str] = []
    missing: list[str] = []

    row_name = record.get("name") or ""
    off_name = official.get("name") or ""
    similarity = name_similarity(row_name, off_name)
    if not row_name or not off_name:
        missing.append("name")
    elif validation.name_key(row_name) == validation.name_key(off_name):
        matched.append("name")
    elif similarity >= 0.80:
        matched.append(f"name (similar {int(similarity * 100)}%)")
    else:
        conflicts.append("name")

    row_id = validation.digits_only(record.get("beneficiary_id"))
    off_id = official.get("_id_key") or ""
    if row_id and off_id:
        if row_id == off_id:
            matched.append("beneficiary_id")
        else:
            conflicts.append("beneficiary_id")
    elif not row_id:
        missing.append("beneficiary_id")

    row_dob = record.get("_dob_iso") or record.get("dob_iso") or _iso_of(record.get("dob"))
    off_dob = official.get("_dob_key") or ""
    if row_dob and off_dob:
        if row_dob == off_dob:
            matched.append("dob")
        else:
            conflicts.append("dob")
    elif not row_dob:
        missing.append("dob")

    row_mobile = _tidy_mobile(record.get("mobile"))
    off_mobile = official.get("_mobile_key") or ""
    if row_mobile and off_mobile:
        if row_mobile == off_mobile:
            matched.append("mobile")
        else:
            conflicts.append("mobile")
    elif not row_mobile:
        missing.append("mobile")

    row_mother = record.get("mother_name") or ""
    off_mother = official.get("mother_name") or ""
    if row_mother and off_mother:
        if validation.name_key(row_mother) == validation.name_key(off_mother):
            matched.append("mother_name")
        elif name_similarity(row_mother, off_mother) < 0.80:
            conflicts.append("mother_name")
    elif not row_mother:
        missing.append("mother_name")

    row_gender = validation.normalise_gender(record.get("gender"))["value"]
    off_gender = validation.normalise_gender(official.get("gender"))["value"]
    if row_gender and off_gender:
        if row_gender == off_gender:
            matched.append("gender")
        else:
            conflicts.append("gender")
    elif not row_gender:
        missing.append("gender")

    return {
        "matched": matched,
        "conflicts": conflicts,
        "missing": missing,
        "name_similarity": similarity,
    }


def _iso_of(value: Any) -> str:
    if not value:
        return ""
    result = validation.parse_date(value, "auto")
    return result.get("iso") or ""


def score_candidate(record: dict, official: dict) -> dict:
    """Confidence + reason for one candidate."""
    detail = compare_pair(record, official)
    reasons: list[str] = []
    confidence = 0.0
    match_type = MATCH_FUZZY

    row_id = validation.digits_only(record.get("beneficiary_id"))
    off_id = official.get("_id_key") or ""
    row_dob = record.get("_dob_iso") or _iso_of(record.get("dob"))
    off_dob = official.get("_dob_key") or ""
    row_mobile = _tidy_mobile(record.get("mobile"))
    off_mobile = official.get("_mobile_key") or ""
    row_mother = validation.name_key(record.get("mother_name"))
    off_mother = validation.name_key(official.get("mother_name"))
    name_exact = "name" in detail["matched"] and detail["name_similarity"] >= 0.999

    if row_id and off_id and row_id == off_id:
        match_type, confidence = MATCH_EXACT_ID, config.MATCH_EXACT_ID
        reasons.append("Beneficiary ID matches the authorised ID exactly.")
    elif name_exact and row_dob and off_dob and row_dob == off_dob:
        match_type, confidence = MATCH_EXACT_NAME_DOB, config.MATCH_EXACT_NAME_DOB
        reasons.append("Full name and date of birth both match exactly.")
    elif name_exact and row_mobile and off_mobile and row_mobile == off_mobile:
        match_type, confidence = MATCH_EXACT_NAME_FIELD, 0.92
        reasons.append("Name matches and the mobile number is the same.")
    elif name_exact and row_mother and off_mother and row_mother == off_mother:
        match_type, confidence = MATCH_EXACT_NAME_FIELD, 0.90
        reasons.append("Name matches and the mother's name is the same.")
    elif name_exact:
        match_type, confidence = MATCH_FUZZY, 0.72
        reasons.append("Name matches exactly but no second field could confirm it.")
    else:
        similarity = detail["name_similarity"]
        dob_ok = bool(row_dob and off_dob and row_dob == off_dob)
        mobile_ok = bool(row_mobile and off_mobile and row_mobile == off_mobile)
        if similarity >= 0.90 and dob_ok:
            match_type, confidence = MATCH_FUZZY, 0.70
            reasons.append(f"Name is {int(similarity * 100)}% similar and the date of birth matches.")
        elif similarity >= 0.90 and mobile_ok:
            match_type, confidence = MATCH_FUZZY, 0.66
            reasons.append(f"Name is {int(similarity * 100)}% similar and the mobile number matches.")
        elif similarity >= 0.80:
            match_type, confidence = MATCH_FUZZY, round(0.35 + similarity * 0.25, 3)
            reasons.append(f"Only the name looks {int(similarity * 100)}% similar - spelling may differ.")
        else:
            match_type, confidence = MATCH_NONE, 0.0
            reasons.append("No identifying field matched.")

    # Conflicting hard identifiers must lower the confidence.
    for field in detail["conflicts"]:
        if field in ("beneficiary_id", "dob", "mobile"):
            confidence = max(0.0, round(confidence - 0.25, 3))
            reasons.append(f"Conflict found in {field.replace('_', ' ')} - please verify manually.")

    return {
        "official_id": official.get("id"),
        "official": {
            "id": official.get("id"),
            "beneficiary_id": official.get("beneficiary_id") or "",
            "name": official.get("name") or "",
            "dob": official.get("dob") or "",
            "gender": official.get("gender") or "",
            "mobile": official.get("mobile") or "",
            "mother_name": official.get("mother_name") or "",
            "extra": official.get("extra_json") or official.get("extra") or {},
        },
        "match_type": match_type,
        "confidence": round(confidence, 4),
        "matched_fields": detail["matched"],
        "conflicting_fields": detail["conflicts"],
        "missing_fields": detail["missing"],
        "name_similarity": detail["name_similarity"],
        "reasons": reasons,
    }


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------
def match_record(record: dict, index: Optional[dict] = None, limit: int = 5) -> dict:
    """Find the best authorised candidate(s) for one Excel row.

    Returns a JSON-serialisable dictionary - see :func:`explain_match` for a
    plain-language summary string.
    """
    index = index or {"count": 0, "by_id": {}, "by_name": {}, "by_name_dob": {}, "by_mobile": {}}
    if not index.get("count"):
        return {
            "status": MATCH_NO_REGISTER,
            "match_type": MATCH_NO_REGISTER,
            "confidence": 0.0,
            "auto_ok": False,
            "primary": None,
            "candidates": [],
            "summary": MATCH_LABELS[MATCH_NO_REGISTER],
            "note": "Import the authorised beneficiary register (Settings -> Beneficiary Register) to enable automatic matching.",
        }

    candidates: list[dict] = []
    seen_ids: set = set()

    def _collect(rows: Iterable[dict]) -> None:
        for row in rows:
            key = row.get("id")
            if key in seen_ids:
                continue
            seen_ids.add(key)
            scored = score_candidate(record, row)
            if scored["confidence"] > 0 or scored["match_type"] != MATCH_NONE:
                candidates.append(scored)

    row_id = validation.digits_only(record.get("beneficiary_id"))
    if row_id and row_id in index["by_id"]:
        _collect(index["by_id"][row_id])

    name_key = validation.name_key(record.get("name"))
    dob_key = record.get("_dob_iso") or _iso_of(record.get("dob"))

    if name_key and dob_key and (name_key, dob_key) in index["by_name_dob"]:
        _collect(index["by_name_dob"][(name_key, dob_key)])

    if name_key and name_key in index["by_name"]:
        _collect(index["by_name"][name_key])

    mobile_key = _tidy_mobile(record.get("mobile"))
    if mobile_key and mobile_key in index["by_mobile"]:
        _collect(index["by_mobile"][mobile_key])

    if not candidates and name_key:
        _collect(_fuzzy_candidates(record, index))

    candidates.sort(key=lambda item: item["confidence"], reverse=True)

    if not candidates:
        return {
            "status": MATCH_NONE,
            "match_type": MATCH_NONE,
            "confidence": 0.0,
            "auto_ok": False,
            "primary": None,
            "candidates": [],
            "summary": MATCH_LABELS[MATCH_NONE],
            "note": "Nobody in the authorised register matched. Verify manually on the portal before doing anything.",
        }

    top = candidates[0]
    near_ties = [item for item in candidates[1:] if top["confidence"] - item["confidence"] < config.MATCH_AMBIGUITY_DELTA]
    same_confidence = [item for item in candidates[1:] if item["confidence"] >= top["confidence"]]

    status = top["match_type"]
    ambiguous = False
    if near_ties and top["confidence"] < config.MATCH_EXACT_ID:
        ambiguous = True
        status = MATCH_AMBIGUOUS
    elif same_confidence:
        ambiguous = True
        status = MATCH_MULTIPLE

    auto_ok = (
        top["match_type"] in AUTO_APPROVABLE
        and top["confidence"] >= config.NEEDS_REVIEW_BELOW
        and not ambiguous
    )

    if ambiguous:
        auto_ok = False

    summary = MATCH_LABELS.get(status, status)
    if ambiguous:
        summary = f"{MATCH_LABELS[MATCH_AMBIGUOUS]} ({len(candidates)} found)"
    elif top["match_type"] == MATCH_FUZZY:
        summary = f"{MATCH_LABELS[MATCH_FUZZY]} - {int(top['confidence'] * 100)}% confidence"

    return {
        "status": status,
        "match_type": top["match_type"],
        "confidence": top["confidence"],
        "auto_ok": bool(auto_ok),
        "ambiguous": ambiguous,
        "primary": top,
        "candidates": candidates[:limit],
        "candidate_count": len(candidates),
        "summary": summary,
        "note": _note_for(status, auto_ok),
    }


def _fuzzy_candidates(record: dict, index: dict, threshold: float = 0.62, cap: int = 2000) -> list[dict]:
    """Scan the register for similar names (only when nothing exact was found)."""
    key = validation.name_key(record.get("name"))
    if not key:
        return []
    first_letter = key[0]
    results = []
    checked = 0
    for candidate in index["rows"]:
        candidate_key = candidate.get("_name_key") or ""
        if not candidate_key:
            continue
        # Cheap pre-filter so a phone never has to score thousands of names.
        if candidate_key[0] != first_letter and first_letter not in _first_letters(candidate_key):
            continue
        checked += 1
        if checked > cap:
            break
        similarity = name_similarity(record.get("name"), candidate.get("name"))
        if similarity >= threshold:
            results.append(candidate)
    return results


def _first_letters(name_key: str) -> set[str]:
    return {token[0] for token in name_key.split() if token}


def _note_for(status: str, auto_ok: bool) -> str:
    if status in (MATCH_AMBIGUOUS, MATCH_MULTIPLE):
        return "More than one beneficiary looks similar. Open the record and confirm the correct person yourself."
    if auto_ok:
        return "High confidence match. Please still check the details before approving."
    if status == MATCH_FUZZY:
        return "Similar name only - this is a suggestion. Never approve without verifying on the portal."
    if status == MATCH_NONE:
        return "No match in the authorised register. Search the portal yourself before doing anything."
    return "Review the matched and conflicting fields before approving."


def explain_match(match: Optional[dict]) -> str:
    """One-line summary for lists and reports."""
    if not match:
        return "Not matched yet"
    return match.get("summary") or MATCH_LABELS.get(match.get("status", ""), "Not matched yet")


# ---------------------------------------------------------------------------
# Applying the result to a record dictionary
# ---------------------------------------------------------------------------
def apply_match_to_record(record: dict, index: Optional[dict]) -> dict:
    """Fill ``record`` with the match information and choose a queue status.

    * A record with validation problems always stays ``NEEDS REVIEW``.
    * A clean record with a high confidence match becomes ``MATCHED``.
    * A clean record without a reliable match also goes to ``NEEDS REVIEW``
      (the worker decides - the app never decides for them).
    """
    match = match_record(record, index)
    record["match"] = match
    record["match_confidence"] = match.get("confidence", 0.0)
    primary = match.get("primary") or {}
    record["matched_official_id"] = primary.get("official_id")

    validation_flagged = record.get("status") == config.STATUS_NEEDS_REVIEW or bool(
        [item for item in record.get("warnings", []) if item.get("severity") in ("error", "warn")]
    )

    if match.get("status") == MATCH_NO_REGISTER:
        new_status = record.get("status", config.STATUS_PENDING)
    elif match.get("auto_ok") and not validation_flagged:
        new_status = config.STATUS_MATCHED
    else:
        new_status = config.STATUS_NEEDS_REVIEW

    # Never downgrade a record the worker already handled.
    if record.get("status") in config.FINAL_STATUSES or record.get("decision"):
        new_status = record["status"]
    record["status"] = new_status
    return record


def summary_for_ui(match: Optional[dict]) -> dict:
    """Small dictionary used by the record page / review card."""
    if not match:
        return {"label": "Not matched yet", "confidence": 0.0, "tone": "muted", "candidates": []}
    status = match.get("status", MATCH_NONE)
    tone = {
        MATCH_EXACT_ID: "good",
        MATCH_EXACT_NAME_DOB: "good",
        MATCH_EXACT_NAME_FIELD: "ok",
        MATCH_FUZZY: "warn",
        MATCH_AMBIGUOUS: "warn",
        MATCH_MULTIPLE: "warn",
        MATCH_NONE: "bad",
        MATCH_NO_REGISTER: "muted",
    }.get(status, "muted")
    return {
        "label": match.get("summary") or MATCH_LABELS.get(status, status),
        "confidence": match.get("confidence", 0.0),
        "confidence_percent": int(round((match.get("confidence") or 0) * 100)),
        "tone": tone,
        "candidates": match.get("candidates", []),
        "auto_ok": match.get("auto_ok", False),
        "note": match.get("note", ""),
    }
