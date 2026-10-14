"""
validation.py
=============
Honest, non-guessing validation for beneficiary rows.

Golden rules implemented here
-----------------------------
1. **Never invent data.** A missing vaccine name, dose, date of birth or
   vaccination date is reported as a problem - it is never filled in with a
   guess. The record is pushed to ``NEEDS REVIEW``.
2. **Never silently "fix" a date.** If ``03/04/2025`` is ambiguous and the
   user did not choose a date format, both interpretations are listed as a
   warning instead of picking one.
3. **Explain every problem in simple language.** Each warning carries a
   ``code`` (for code), a ``message`` (plain English/Hinglish) and the field
   name, so the dashboard can highlight the exact cell.
4. **One bad row must never crash the app.** All public functions catch their
   own exceptions and return a warning instead of raising.
"""

from __future__ import annotations

import re
from datetime import date, datetime, timedelta
from typing import Any, Optional

import config

# ---------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------
_DATE_CLEAN_RE = re.compile(r"[^0-9A-Za-z:/.\-\s]")
_TIME_TRAILING_RE = re.compile(r"\s*(00:00:00|00:00|12:00:00\s*AM)\s*$")
_DIGITS_RE = re.compile(r"\D+")

NONE_TOKENS = {
    "", "na", "n/a", "na.", "nil", "none", "null", "-", "--", "---", "nan", "none given",
    "not available", "notapplicable", "unknown",
}

#: Words that mean "we could not read this cell".
MISSING = "MISSING"
INVALID = "INVALID"
AMBIGUOUS = "AMBIGUOUS"
CONFLICT = "CONFLICT"
DUPLICATE = "DUPLICATE"
INFO = "INFO"


def severity_of(code: str) -> str:
    return {
        MISSING: "warn",
        INVALID: "error",
        AMBIGUOUS: "warn",
        CONFLICT: "error",
        DUPLICATE: "warn",
        INFO: "info",
    }.get(code, "info")


def warning(code: str, field: str, message: str, raw: Any = None, hint: str = "") -> dict:
    """Build a warning dictionary that the UI understands."""
    return {
        "code": code,
        "field": field,
        "message": message,
        "raw": "" if raw is None else str(raw)[:120],
        "hint": hint,
        "severity": severity_of(code),
    }


def clean_text(value: Any) -> str:
    """Turn any Excel cell into a tidy string. Empty/NA becomes ``""``."""
    if value is None:
        return ""
    if isinstance(value, float) and value != value:  # NaN
        return ""
    if isinstance(value, str):
        text = " ".join(value.replace("\u00a0", " ").split())
    elif isinstance(value, (datetime, date)):
        text = value.strftime("%Y-%m-%d")
    elif isinstance(value, float) and float(value).is_integer():
        text = str(int(value))
    else:
        text = str(value).strip()
    if text.strip().lower() in NONE_TOKENS:
        return ""
    return text


def is_blank(value: Any) -> bool:
    return clean_text(value) == ""


def name_key(value: Any) -> str:
    """Normalise a name for comparison (lowercase, no titles, no punctuation)."""
    text = clean_text(value).lower()
    for title in ("mrs.", "mrs", "mr.", "mr", "ms.", "ms", "smt.", "smt", "baby", "master", "dr."):
        if text.startswith(title + " "):
            text = text[len(title) + 1 :]
    text = re.sub(r"[^a-z0-9\s]", " ", text)
    text = re.sub(r"\s+", " ", text).strip()
    return text


NAME_COMPARE_ALIASES = {
    "kumar": "kumar",
    "ku": "kumar",
    "devi": "devi",
    "dev": "devi",
    "bai": "bai",
    "singh": "singh",
    "sing": "singh",
}


def name_tokens(value: Any) -> list[str]:
    tokens = [NAME_COMPARE_ALIASES.get(token, token) for token in name_key(value).split()]
    return [token for token in tokens if token]


def digits_only(value: Any) -> str:
    return _DIGITS_RE.sub("", clean_text(value))


# ---------------------------------------------------------------------------
# Date handling
# ---------------------------------------------------------------------------
#: App setting name -> strptime pattern (used when the worker picks a format).
FORMAT_TO_STRPTIME = {
    "DD-MM-YYYY": "%d-%m-%Y",
    "DD/MM/YYYY": "%d/%m/%Y",
    "DD.MM.YYYY": "%d.%m.%Y",
    "YYYY-MM-DD": "%Y-%m-%d",
    "MM/DD/YYYY": "%m/%d/%Y",
    "auto": None,
}

DATE_FORMATS = ("%d-%m-%Y", "%d/%m/%Y", "%d.%m.%Y", "%Y-%m-%d", "%Y/%m/%d", "%m/%d/%Y", "%d-%m-%y", "%d/%m/%y", "%Y%m%d")

MONTH_BY_NAME = {
    "jan": 1, "january": 1, "feb": 2, "february": 2, "mar": 3, "march": 3, "apr": 4, "april": 4,
    "may": 5, "jun": 6, "june": 6, "jul": 7, "july": 7, "aug": 8, "august": 8, "sep": 9, "sept": 9,
    "september": 9, "oct": 10, "october": 10, "nov": 11, "november": 11, "dec": 12, "december": 12,
}

EXCEL_EPOCH_FORMATS = {
    "windows": date(1899, 12, 30),  # the value Excel actually stores
    "mac": date(1904, 1, 1),
}


def _excel_serial_to_date(value: float, system: str = "windows") -> Optional[date]:
    if value <= 0 or value > 60000:  # 1900-01-01 .. 2064
        return None
    base = EXCEL_EPOCH_FORMATS.get(system, EXCEL_EPOCH_FORMATS["windows"])
    try:
        return base + timedelta(days=int(value))
    except (OverflowError, ValueError):
        return None


def parse_date(
    value: Any,
    preferred_format: str = "auto",
    *,
    allow_future_days: int = 0,
    today: Optional[date] = None,
) -> dict:
    """Parse a cell into an ISO date **without guessing**.

    Returns a dictionary::

        {"iso": "2025-12-31" | None, "ambiguous": bool, "options": [...],
         "error": "..." | None, "raw": "<original text>", "kind": "ok|empty|invalid|ambiguous|future"}

    ``preferred_format`` follows the ``date_format`` app setting. ``auto`` means
    "standard ISO/DD-MM-YYYY style is accepted, but a value that may be
    DD/MM **or** MM/DD is reported as ambiguous".
    """
    today = today or date.today()
    raw = clean_text(value)
    result = {"iso": None, "ambiguous": False, "options": [], "error": None, "raw": raw, "kind": "ok"}

    if raw == "":
        result["kind"] = "empty"
        result["error"] = "Date is empty."
        return result

    text = _TIME_TRAILING_RE.sub("", raw).strip()

    # 1) Real datetime objects coming from pandas / openpyxl.
    if isinstance(value, datetime):
        parsed = value.date()
        return _finalise_date(parsed, result, allow_future_days, today)
    if isinstance(value, date):
        return _finalise_date(value, result, allow_future_days, today)

    # 2) Excel serial numbers.
    numeric = text.replace(",", "")
    if re.fullmatch(r"\d{5}(\.\d+)?", numeric):
        parsed = _excel_serial_to_date(float(numeric))
        if parsed:
            return _finalise_date(parsed, result, allow_future_days, today)

    # 3) "15 Jan 2025" / "15-Jan-25" style.
    match = re.fullmatch(r"(\d{1,2})[\s\-/.]([A-Za-z]{3,9})[\s\-/.](\d{2,4})", text)
    if match:
        day, month_name, year = match.groups()
        month = MONTH_BY_NAME.get(month_name.lower())
        if month:
            parsed = _make_date(int(year), month, int(day))
            if parsed:
                return _finalise_date(parsed, result, allow_future_days, today)

    # 4) Numeric formats.
    match = re.fullmatch(r"(\d{1,4})[\-/.](\d{1,2})[\-/.](\d{1,4})", text)
    if match:
        first, second, third = (int(part) for part in match.groups())
        candidates: list[tuple[str, Optional[date]]] = []

        if preferred_format not in ("auto", None):
            parsed = _try_strptime(text, preferred_format)
            if parsed:
                candidates.append((preferred_format, parsed))
        else:
            # Unambiguous ISO first.
            if len(str(first)) == 4:
                candidates.append(("YYYY-MM-DD", _make_date(first, second, third)))
            else:
                candidates.append(("DD-MM-YYYY", _make_date(third, second, first)))
                if second <= 12 and first <= 12 and first != second:
                    candidates.append(("MM-DD-YYYY", _make_date(third, first, second)))

        valid = [(fmt, parsed) for fmt, parsed in candidates if parsed]
        if not valid:
            result["kind"] = "invalid"
            result["error"] = f"'{raw}' is not a valid date."
            return result

        distinct = {parsed.isoformat() for _, parsed in valid}
        if len(distinct) > 1:
            result["kind"] = "ambiguous"
            result["ambiguous"] = True
            result["options"] = [
                {"format": fmt, "iso": parsed.isoformat()} for fmt, parsed in valid
            ]
            result["error"] = (
                f"'{raw}' could mean " + " or ".join(parsed.isoformat() for _, parsed in valid)
            )
            return result

        return _finalise_date(valid[0][1], result, allow_future_days, today)

    # 5) Last chance: a plain 8 digit "YYYYMMDD".
    if re.fullmatch(r"\d{8}", text):
        parsed = _try_strptime(text, "%Y%m%d")
        if parsed:
            return _finalise_date(parsed, result, allow_future_days, today)

    result["kind"] = "invalid"
    result["error"] = f"'{raw}' is not a recognised date."
    result["hint"] = "Use DD-MM-YYYY (for example 31-12-2025) or set your date format in Settings."
    return result


def _try_strptime(text: str, format_name: str) -> Optional[date]:
    """Parse ``text`` with an app-level format name (``DD-MM-YYYY`` etc.)."""
    pattern = FORMAT_TO_STRPTIME.get(format_name)
    if pattern is None and "%" in format_name:
        pattern = format_name  # already a strptime pattern
    if pattern is None:
        return None

    normalised_text = text.replace(".", "-").replace("/", "-").strip()
    normalised_pattern = pattern.replace(".", "-").replace("/", "-")
    try:
        return datetime.strptime(normalised_text, normalised_pattern).date()
    except ValueError:
        return None


def _make_date(year: int, month: int, day: int) -> Optional[date]:
    if year < 100:
        year += 2000 if year < 70 else 1900
    try:
        return date(year, month, day)
    except ValueError:
        return None


def _finalise_date(parsed: date, result: dict, allow_future_days: int, today: date) -> dict:
    if parsed > today + timedelta(days=allow_future_days):
        result["kind"] = "future"
        result["error"] = f"{parsed.isoformat()} is in the future."
        result["iso"] = parsed.isoformat()
        return result
    if parsed.year < 1900:
        result["kind"] = "invalid"
        result["error"] = f"Year {parsed.year} looks wrong."
        return result
    result["iso"] = parsed.isoformat()
    return result


def date_to_display(iso: str, preferred_format: str = "auto") -> str:
    """Show an ISO date the way the worker expects to read it."""
    if not iso:
        return ""
    try:
        parsed = datetime.strptime(iso, "%Y-%m-%d").date()
    except ValueError:
        return iso
    fmt = {
        "DD-MM-YYYY": "%d-%m-%Y",
        "DD/MM/YYYY": "%d/%m/%Y",
        "DD.MM.YYYY": "%d.%m.%Y",
        "MM/DD/YYYY": "%m/%d/%Y",
        "YYYY-MM-DD": "%Y-%m-%d",
    }.get(preferred_format, "%d-%m-%Y")
    return parsed.strftime(fmt)


# ---------------------------------------------------------------------------
# Mobile / gender / dose
# ---------------------------------------------------------------------------
def validate_mobile(value: Any) -> dict:
    raw = clean_text(value)
    digits = digits_only(raw)
    if raw == "":
        return {"ok": False, "empty": True, "value": "", "error": "", "digits": ""}
    if digits.startswith("91") and len(digits) == 12:
        digits = digits[2:]
    if digits.startswith("0") and len(digits) == 11:
        digits = digits[1:]
    if len(digits) != 10:
        return {
            "ok": False,
            "empty": False,
            "value": raw,
            "digits": digits,
            "error": f"Mobile number has {len(digits)} digits; an Indian mobile number has 10.",
        }
    if digits[0] not in "6789":
        return {
            "ok": False,
            "empty": False,
            "value": raw,
            "digits": digits,
            "error": "Mobile number should start with 6, 7, 8 or 9.",
        }
    return {"ok": True, "empty": False, "value": digits, "digits": digits, "error": ""}


GENDER_ALIASES = {
    "m": "Male", "male": "Male", "boy": "Male", "purush": "Male", "पुरुष": "Male",
    "f": "Female", "female": "Female", "girl": "Female", "stree": "Female", "महिला": "Female",
    "o": "Other", "other": "Other", "transgender": "Other", "tg": "Other",
}


def normalise_gender(value: Any) -> dict:
    raw = clean_text(value)
    if raw == "":
        return {"value": "", "error": "", "empty": True}
    key = raw.strip().lower().replace(".", "")
    if key in GENDER_ALIASES:
        return {"value": GENDER_ALIASES[key], "error": "", "empty": False}
    return {"value": raw, "error": f"'{raw}' is not a standard gender value.", "empty": False}


def normalise_dose(value: Any) -> dict:
    raw = clean_text(value)
    if raw == "":
        return {"value": "", "error": "", "empty": True}
    key = raw.lower().replace("dose", "").strip()
    # Only *formatting* is normalised here - never an invented meaning.
    # "0" stays "0" (OPV birth dose), "birth" becomes "Birth dose".
    words = {
        "1": "1", "first": "1", "1st": "1", "one": "1",
        "2": "2", "second": "2", "2nd": "2", "two": "2",
        "3": "3", "third": "3", "3rd": "3", "three": "3",
        "0": "0", "birth": "Birth dose",
        "precaution": "Precaution", "pcv booster": "Booster", "booster": "Booster", "b": "Booster",
    }
    if key in words:
        return {"value": words[key], "error": "", "empty": False}
    if key in ("birth dose", "birthdose"):
        return {"value": "Birth dose", "error": "", "empty": False}
    return {"value": raw, "error": f"'{raw}' is not a clear dose number.", "empty": False}


# ---------------------------------------------------------------------------
# Row level validation
# ---------------------------------------------------------------------------
REQUIRED_FIELDS = ("name", "beneficiary_id", "dob", "vaccine_name", "dose_number", "vaccination_date")
CRITICAL_FIELDS = ("name", "vaccine_name", "vaccination_date")


def validate_record_fields(record: dict, settings: Optional[dict] = None, today: Optional[date] = None) -> dict:
    """Validate one already-normalised record dictionary.

    Used both after Excel import and after a manual edit. Returns::

        {"warnings": [...], "needs_review": bool, "missing": [...], "conflicts": [...]}
    """
    settings = settings or {}
    date_format = settings.get("date_format", "auto")
    warnings: list[dict] = []
    missing: list[str] = []

    try:
        name = clean_text(record.get("name"))
        if not name:
            missing.append("name")
            warnings.append(
                warning(MISSING, "name", "Beneficiary name is empty.", hint="Ask the supervisor for the correct name; do not type a guess.")
            )
        elif len(name_key(name)) < 2:
            warnings.append(warning(INVALID, "name", f"Name '{name}' is too short to verify."))

        # Date of birth ------------------------------------------------------
        dob_result = parse_date(record.get("dob"), date_format, allow_future_days=0, today=today)
        if dob_result["kind"] == "empty":
            missing.append("dob")
            warnings.append(
                warning(MISSING, "dob", "Date of birth is missing.", hint="Auto-fill is not allowed; verify it on the portal.")
            )
        elif dob_result["kind"] == "ambiguous":
            warnings.append(
                warning(AMBIGUOUS, "dob", f"Date of birth is ambiguous: {dob_result['error']}", raw=dob_result["raw"],
                        hint="Open the Excel file and confirm the day/month, or set the date format in Settings.")
            )
        elif dob_result["kind"] in ("invalid", "future"):
            warnings.append(warning(INVALID, "dob", f"Date of birth problem: {dob_result['error']}", raw=dob_result["raw"]))
        else:
            parsed_dob = datetime.strptime(dob_result["iso"], "%Y-%m-%d").date()
            if (today or date.today()) < parsed_dob:
                warnings.append(warning(CONFLICT, "dob", "Date of birth is after today.", raw=dob_result["raw"]))
        record["_dob_iso"] = dob_result.get("iso")

        # Vaccination date ---------------------------------------------------
        vax_result = parse_date(record.get("vaccination_date"), date_format, allow_future_days=0, today=today)
        if vax_result["kind"] == "empty":
            missing.append("vaccination_date")
            warnings.append(
                warning(MISSING, "vaccination_date", "Vaccination date is missing.",
                        hint="The app will not guess a vaccination date. Get it from the portal / register.")
            )
        elif vax_result["kind"] == "ambiguous":
            warnings.append(
                warning(AMBIGUOUS, "vaccination_date", f"Vaccination date is ambiguous: {vax_result['error']}",
                        raw=vax_result["raw"], hint="Confirm the day/month, then edit the record.")
            )
        elif vax_result["kind"] in ("invalid", "future"):
            warnings.append(warning(INVALID, "vaccination_date", f"Vaccination date problem: {vax_result['error']}", raw=vax_result["raw"]))
        else:
            vax_date = datetime.strptime(vax_result["iso"], "%Y-%m-%d").date()
            dob_iso = record.get("_dob_iso")
            if dob_iso:
                try:
                    dob_date = datetime.strptime(dob_iso, "%Y-%m-%d").date()
                    if vax_date < dob_date:
                        warnings.append(
                            warning(CONFLICT, "vaccination_date", "Vaccination date is before the date of birth.", raw=vax_result["raw"])
                        )
                    elif vax_date == dob_date:
                        warnings.append(
                            warning(INFO, "vaccination_date", "Vaccination date is the same as the date of birth (birth dose?).")
                        )
                    elif (vax_date - dob_date).days > 5 * 365:
                        warnings.append(
                            warning(INFO, "vaccination_date", "More than 5 years between birth and this vaccination - please confirm.")
                        )
                except ValueError:
                    pass
        record["_vax_iso"] = vax_result.get("iso")

        # Vaccine / dose -----------------------------------------------------
        vaccine = clean_text(record.get("vaccine_name"))
        if not vaccine:
            missing.append("vaccine_name")
            warnings.append(warning(MISSING, "vaccine_name", "Vaccine name is empty.",
                                    hint="Select the correct vaccine from the authorised list; the app will not guess."))
        elif len(vaccine) < 2:
            warnings.append(warning(INVALID, "vaccine_name", f"'{vaccine}' is not a clear vaccine name."))

        dose = normalise_dose(record.get("dose_number"))
        record["_dose_clean"] = dose["value"]
        if dose["empty"]:
            missing.append("dose_number")
            warnings.append(warning(MISSING, "dose_number", "Dose number is missing."))
        elif dose["error"]:
            warnings.append(warning(INVALID, "dose_number", dose["error"], raw=dose["value"]))

        # Mobile / gender ----------------------------------------------------
        mobile = validate_mobile(record.get("mobile"))
        record["_mobile_clean"] = mobile["digits"] if mobile["ok"] else ""
        if mobile["empty"]:
            warnings.append(warning(INFO, "mobile", "Mobile number is not filled."))
        elif not mobile["ok"]:
            warnings.append(warning(INVALID, "mobile", mobile["error"], raw=mobile["value"]))

        gender = normalise_gender(record.get("gender"))
        record["_gender_clean"] = gender["value"]
        if gender["empty"]:
            warnings.append(warning(INFO, "gender", "Gender is not filled."))
        elif gender["error"]:
            warnings.append(warning(INVALID, "gender", gender["error"], raw=gender["value"]))

        # Beneficiary ID -----------------------------------------------------
        bid = digits_only(record.get("beneficiary_id"))
        record["_bid_clean"] = bid
        if not bid and not clean_text(record.get("name")):
            warnings.append(warning(MISSING, "beneficiary_id", "No beneficiary ID and no name - record cannot be identified."))
        elif bid and len(bid) < 6:
            warnings.append(warning(INVALID, "beneficiary_id", f"Beneficiary ID '{bid}' looks too short.", raw=bid))

    except Exception as exc:  # pragma: no cover - defensive only
        warnings.append(warning(INVALID, "record", f"Unexpected validation problem: {exc}"))

    conflicts = [item for item in warnings if item["code"] in (CONFLICT, INVALID, AMBIGUOUS)]
    needs_review = bool(missing or conflicts)
    return {"warnings": warnings, "needs_review": needs_review, "missing": missing, "conflicts": conflicts}


def row_is_empty(values: list) -> bool:
    """A row is empty when every mapped cell is blank."""
    return all(is_blank(value) for value in values)


def row_fingerprint(bid: str, name: str, dob_iso: str, vaccine: str, dose: str, vax_date: str) -> str:
    """Stable key used to spot duplicate rows inside one file."""
    return "|".join(
        [
            digits_only(bid),
            name_key(name),
            dob_iso or "",
            name_key(vaccine),
            clean_text(dose).lower(),
            vax_date or "",
        ]
    )


def duplicate_groups(records: list[dict]) -> dict:
    """Map ``index -> (first_index, fingerprint)`` for duplicate rows.

    A row is a duplicate only when **all** identifying fields match, so two
    children of the same family with different names are never merged.
    """
    seen: dict[str, int] = {}
    duplicates: dict[int, tuple[int, str]] = {}
    for index, record in enumerate(records):
        key = row_fingerprint(
            record.get("_bid_clean") or record.get("beneficiary_id") or "",
            record.get("name") or "",
            record.get("_dob_iso") or "",
            record.get("vaccine_name") or "",
            record.get("_dose_clean") or record.get("dose_number") or "",
            record.get("_vax_iso") or "",
        )
        if key.strip("|") == "":
            continue
        if key in seen:
            duplicates[index] = (seen[key], key)
        else:
            seen[key] = index
    return duplicates


def duplicate_clusters(records: list[dict]) -> dict[str, list[int]]:
    """Group identical rows together: ``fingerprint -> [row indexes]``.

    Only clusters with more than one member are returned.
    """
    seen: dict[str, list[int]] = {}
    for index, record in enumerate(records):
        key = row_fingerprint(
            record.get("_bid_clean") or record.get("beneficiary_id") or "",
            record.get("name") or "",
            record.get("_dob_iso") or "",
            record.get("vaccine_name") or "",
            record.get("_dose_clean") or record.get("dose_number") or "",
            record.get("_vax_iso") or "",
        )
        if key.strip("|") == "":
            continue
        seen.setdefault(key, []).append(index)
    return {key: indexes for key, indexes in seen.items() if len(indexes) > 1}


# ---------------------------------------------------------------------------
# Whole workbooks / registers
# ---------------------------------------------------------------------------
def validate_mapping(mapping: dict[str, str], columns: list[str]) -> list[dict]:
    """Check the user's column mapping before we start importing rows."""
    problems: list[dict] = []
    used: dict[str, str] = {}

    for field, column in (mapping or {}).items():
        if not column:
            continue
        if column not in columns:
            problems.append(
                warning(INVALID, field, f"Mapped column '{column}' is not present in the sheet.")
            )
            continue
        if column in used:
            problems.append(
                warning(CONFLICT, field, f"Column '{column}' is already used for '{used[column]}'.")
            )
        used[column] = field

    if not mapping.get("name"):
        problems.append(
            warning(MISSING, "name", "Beneficiary Name column is not mapped.",
                    hint="Open 'Upload Excel' -> Map Columns and choose the name column.")
        )
    if not mapping.get("vaccine_name"):
        problems.append(
            warning(MISSING, "vaccine_name", "Vaccine Name column is not mapped.",
                    hint="Without it, every record would need review.")
        )
    if not mapping.get("vaccination_date"):
        problems.append(
            warning(MISSING, "vaccination_date", "Vaccination Date column is not mapped.")
        )
    return problems


def validate_register_row(row: dict) -> list[dict]:
    """Light validation for the authorised beneficiary register file."""
    problems = []
    if is_blank(row.get("name")) and is_blank(row.get("beneficiary_id")):
        problems.append(
            warning(MISSING, "name", "Register row has neither a name nor a beneficiary ID.",
                    raw=f"row {row.get('row_number')}")
        )
    if not is_blank(row.get("name")) and len(name_key(row.get("name"))) < 2:
        problems.append(warning(INVALID, "name", f"Register name '{row.get('name')}' is too short.",
                                raw=f"row {row.get('row_number')}"))
    return problems
