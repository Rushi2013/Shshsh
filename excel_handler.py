"""
excel_handler.py
================
Reads ``.xlsx`` / ``.xls`` / ``.csv`` files **on the phone** and turns them into
clean record dictionaries.

Two reading back-ends, chosen automatically:

1. **pandas** (fast, handles most files) - used when pandas is installed.
2. **openpyxl / xlrd native** - used automatically when pandas is missing or
   when pandas cannot open the file. This is the safety net that lets the app
   run on a very small Termux install.

What this module guarantees
---------------------------
* The uploaded file's real content type is sniffed (ZIP / OLE magic bytes),
  the file name alone is never trusted.
* Column names are never assumed. The user always sees what was detected and
  can change the mapping.
* Empty rows, duplicate rows and empty cells are *counted and reported*, never
  silently dropped or filled in.
* Anything that cannot be read raises :class:`ExcelError` with a friendly
  message instead of crashing the dashboard.
"""

from __future__ import annotations

import csv
import io
import re
from datetime import date, datetime
from pathlib import Path
from typing import Any, Optional

import config
import validation

# ---------------------------------------------------------------------------
# Errors
# ---------------------------------------------------------------------------
class ExcelError(Exception):
    """Friendly file problem (never a bare traceback in the UI)."""

    def __init__(self, message: str, code: str = "excel_error", hint: str = ""):
        super().__init__(message)
        self.message = message
        self.code = code
        self.hint = hint


# ---------------------------------------------------------------------------
# Field definitions (used by the mapping UI and by the importer)
# ---------------------------------------------------------------------------
FIELD_DEFINITIONS: list[dict] = [
    {"key": "name", "label": "Beneficiary Name", "required": True,
     "help": "Full name of the child / beneficiary as written in the register."},
    {"key": "beneficiary_id", "label": "Beneficiary ID", "required": False,
     "help": "U-WIN / RCH beneficiary number, if your file has it."},
    {"key": "dob", "label": "Date of Birth", "required": False,
     "help": "Used for matching. Day/Month order must be clear."},
    {"key": "gender", "label": "Gender", "required": False, "help": "Male / Female / Other."},
    {"key": "mobile", "label": "Mobile Number", "required": False, "help": "10 digit mobile of the family."},
    {"key": "mother_name", "label": "Mother's Name", "required": False, "help": "Extra check while matching."},
    {"key": "vaccine_name", "label": "Vaccine Name", "required": True, "help": "e.g. BCG, OPV, Pentavalent."},
    {"key": "dose_number", "label": "Dose Number", "required": False, "help": "1 / 2 / 3 / Booster."},
    {"key": "vaccination_date", "label": "Vaccination Date", "required": True, "help": "Date the dose was given."},
    {"key": "batch_number", "label": "Batch / Lot Number", "required": False, "help": "Optional, from the vial."},
    {"key": "vaccinator_name", "label": "Vaccinator Name", "required": False, "help": "Optional."},
    {"key": "facility", "label": "Facility / Session Site", "required": False, "help": "Optional."},
    {"key": "district", "label": "District", "required": False, "help": "Optional."},
    {"key": "block", "label": "Block / Taluka", "required": False, "help": "Optional."},
    {"key": "address", "label": "Address", "required": False, "help": "Optional."},
    {"key": "remarks", "label": "Remarks", "required": False, "help": "Optional free text."},
]

FIELD_LABELS = {item["key"]: item["label"] for item in FIELD_DEFINITIONS}
FIELD_ORDER = [item["key"] for item in FIELD_DEFINITIONS]

#: Header synonyms -> internal field. Deliberately generous but never silent:
#: the detected mapping is always shown to the user for confirmation.
SYNONYMS: dict[str, list[str]] = {
    "name": ["beneficiary name", "name of beneficiary", "child name", "baby name", "name",
             "patient name", "beneficiary", "child", "infant name", "ladka ladki ka naam", "naam"],
    "beneficiary_id": ["beneficiary id", "beneficiaryid", "rch id", "rchid", "uwin id", "u-win id",
                       "beneficiary code", "registration number", "reg no", "id", "card no",
                       "mother id", "case id", "uid"],
    "dob": ["date of birth", "dob", "birth date", "d.o.b", "birthdate", "date of birth (dd-mm-yyyy)"],
    "gender": ["gender", "sex", "male female", "ling"],
    "mobile": ["mobile", "mobile number", "mobile no", "phone", "phone number", "contact",
               "contact number", "whatsapp", "mo no", "mob"],
    "mother_name": ["mother name", "mother's name", "mothers name", "maternal name", "parent name",
                    "guardian name", "father name", "mata ka naam"],
    "vaccine_name": ["vaccine", "vaccine name", "name of vaccine", "antigen", "dose name",
                     "vaccine given", "immunization", "immunisation", "teeka"],
    "dose_number": ["dose", "dose number", "dose no", "dose#", "dose no.", "which dose", "dose count"],
    "vaccination_date": ["vaccination date", "date of vaccination", "date given", "vaccine date",
                         "immunization date", "immunisation date", "date of dose", "administered on",
                         "given date", "date"],
    "batch_number": ["batch", "batch no", "batch number", "lot", "lot no", "lot number", "vial"],
    "vaccinator_name": ["vaccinator", "vaccinator name", "administered by", "anm", "asha", "staff name"],
    "facility": ["facility", "session site", "site", "health facility", "phc", "chc", "sub center",
                 "subcentre", "hospital", "center", "centre"],
    "district": ["district", "zilla", "dist"],
    "block": ["block", "taluka", "tehsil", "ward", "panchayat"],
    "address": ["address", "residence", "village", "ward no", "pata", "house"],
    "remarks": ["remark", "remarks", "note", "notes", "comment", "comments"],
}

#: Columns that are useful for the authorised register import.
REGISTER_SYNONYMS = {
    "beneficiary_id": SYNONYMS["beneficiary_id"],
    "name": SYNONYMS["name"],
    "dob": SYNONYMS["dob"],
    "gender": SYNONYMS["gender"],
    "mobile": SYNONYMS["mobile"],
    "mother_name": SYNONYMS["mother_name"],
}


def _normalise_header(value: Any) -> str:
    text = validation.clean_text(value).lower()
    text = re.sub(r"[^a-z0-9\s]", " ", text)
    return re.sub(r"\s+", " ", text).strip()


# ---------------------------------------------------------------------------
# File type sniffing & sheet helpers
# ---------------------------------------------------------------------------
def sniff_format(path: Path) -> str:
    """Return ``xlsx`` / ``xls`` / ``csv`` using the file *content*."""
    try:
        with open(path, "rb") as handle:
            head = handle.read(8)
    except OSError as exc:
        raise ExcelError(f"File cannot be opened: {exc}", "unreadable",
                         "Re-upload the file from the Files app.") from exc

    if head.startswith(config.ZIP_MAGIC):
        return "xlsx"
    if head.startswith(config.OLE_MAGIC):
        return "xls"
    suffix = path.suffix.lower()
    if suffix == ".csv":
        return "csv"
    # Some phones rename files; fall back to a text sniff for CSV.
    try:
        with open(path, "rb") as handle:
            sample = handle.read(2048)
        sample.decode("utf-8")
        if b"," in sample or b";" in sample:
            return "csv"
    except (OSError, UnicodeDecodeError):
        pass
    raise ExcelError(
        "This file is not a real Excel file (its content does not match .xlsx or .xls).",
        "bad_format",
        "Open the file on the phone and use 'Save as' / 'Export' to create a proper .xlsx file.",
    )


def list_sheets(path: Path) -> list[str]:
    """Sheet names inside a workbook (empty list when unavailable)."""
    fmt = sniff_format(path)
    try:
        if fmt == "xlsx":
            from openpyxl import load_workbook

            workbook = load_workbook(filename=str(path), read_only=True, data_only=True)
            names = list(workbook.sheetnames)
            workbook.close()
            return names
        if fmt == "xls":
            import xlrd

            book = xlrd.open_workbook(str(path))
            return list(book.sheet_names())
    except Exception:
        return []
    return ["Sheet1"] if fmt == "csv" else []


def _read_grid_native(path: Path, sheet_name: Optional[str], max_rows: Optional[int] = None) -> tuple[list[list], str, str]:
    """Read raw cells with openpyxl / xlrd / csv (no pandas needed)."""
    fmt = sniff_format(path)
    grid: list[list] = []

    if fmt == "csv":
        text = _read_text(path)
        reader = csv.reader(io.StringIO(text))
        for index, row in enumerate(reader):
            if max_rows is not None and index >= max_rows:
                break
            grid.append(list(row))
        return grid, sheet_name or "CSV", "csv"

    if fmt == "xlsx":
        try:
            from openpyxl import load_workbook
        except ImportError as exc:  # pragma: no cover
            raise ExcelError("openpyxl is not installed.", "missing_library",
                             "Run: pip install openpyxl") from exc
        workbook = load_workbook(filename=str(path), read_only=True, data_only=True)
        sheet = workbook[sheet_name] if sheet_name and sheet_name in workbook.sheetnames else workbook[workbook.sheetnames[0]]
        for index, row in enumerate(sheet.iter_rows(values_only=True)):
            if max_rows is not None and index >= max_rows:
                break
            grid.append(list(row))
        used_name = sheet.title
        workbook.close()
        return grid, used_name, "openpyxl"

    # legacy .xls
    try:
        import xlrd
    except ImportError as exc:  # pragma: no cover
        raise ExcelError("xlrd is not installed (needed for .xls files).", "missing_library",
                         "Run: pip install xlrd") from exc
    try:
        book = xlrd.open_workbook(str(path))
    except Exception as exc:
        raise ExcelError(
            f"This .xls file could not be opened: {exc}", "bad_xls",
            "Open it and re-save as .xlsx, then upload again.",
        ) from exc
    sheet = book.sheet_by_name(sheet_name) if sheet_name and sheet_name in book.sheet_names() else book.sheet_by_index(0)
    for row_index in range(sheet.nrows):
        if max_rows is not None and row_index >= max_rows:
            break
        values = []
        for col_index in range(sheet.ncols):
            cell = sheet.cell(row_index, col_index)
            if cell.ctype == xlrd.XL_CELL_DATE:
                try:
                    values.append(xlrd.xldate.xldate_as_datetime(cell.value, book.datemode))
                    continue
                except Exception:
                    pass
            if cell.ctype == xlrd.XL_CELL_NUMBER and float(cell.value).is_integer():
                values.append(int(cell.value))
                continue
            if cell.ctype in (xlrd.XL_CELL_EMPTY, xlrd.XL_CELL_BLANK):
                values.append(None)
                continue
            values.append(cell.value)
        grid.append(values)
    return grid, sheet.title, "xlrd"


def _read_text(path: Path) -> str:
    for encoding in ("utf-8-sig", "utf-16", "latin-1"):
        try:
            return path.read_text(encoding=encoding)
        except (UnicodeDecodeError, UnicodeError):
            continue
    raise ExcelError("The CSV file encoding could not be read.", "bad_encoding",
                     "Save the file as UTF-8 CSV and try again.")


def _read_grid_pandas(path: Path, sheet_name: Optional[str], max_rows: Optional[int] = None):
    """Read with pandas (keeps native types, which helps date detection)."""
    import pandas as pd

    kwargs = {"header": None, "dtype": object}
    if max_rows:
        kwargs["nrows"] = max_rows
    if path.suffix.lower() == ".xls":
        engine = "xlrd"
    elif path.suffix.lower() == ".csv":
        engine = "python"
    else:
        engine = "openpyxl"

    try:
        if engine == "python":
            frame = pd.read_csv(path, **kwargs)
            return frame.values.tolist(), sheet_name or "CSV", "pandas-csv"
        frame = pd.read_excel(path, sheet_name=sheet_name or 0, engine=engine, **kwargs)
    except Exception as exc:
        raise ExcelError(f"pandas could not read this file: {exc}", "pandas_failed") from exc

    name = sheet_name or getattr(getattr(frame, "name", None), "title", None) or "Sheet1"
    if hasattr(frame, "values"):
        return frame.values.tolist(), name, "pandas"
    raise ExcelError("Unexpected sheet layout.", "pandas_failed")


def read_grid(path: Path, sheet_name: Optional[str] = None, max_rows: Optional[int] = None) -> tuple[list[list], str, str]:
    """Read the whole sheet as a list of rows. pandas first, native fallback.

    When the worker has not chosen a sheet, every sheet is inspected quickly and
    the one that actually looks like a data table is used. That is what makes
    files with a cover / instruction sheet work without any hand-holding.
    """
    if not sheet_name:
        best = pick_best_sheet(path)
        if best:
            sheet_name = best
    try:
        import pandas  # noqa: F401  (import check only)

        return _read_grid_pandas(path, sheet_name, max_rows)
    except ExcelError:
        return _read_grid_native(path, sheet_name, max_rows)
    except ImportError:
        return _read_grid_native(path, sheet_name, max_rows)


def _sheet_score(grid: list[list]) -> int:
    """How much does this sheet look like a beneficiary table?"""
    if not grid:
        return -1
    header_row = guess_header_row(grid)
    columns = build_columns(grid, header_row)
    if not columns:
        return -1

    mapping = auto_mapping(columns)
    score = min(len(mapping), 10)
    if mapping.get("name"):
        score += 6
    if mapping.get("vaccine_name"):
        score += 3
    if mapping.get("vaccination_date"):
        score += 3
    if mapping.get("beneficiary_id"):
        score += 2
    if len(columns) < 2:
        score -= 4

    body = grid[header_row:]
    filled_rows = sum(1 for row in body[:25] if not validation.row_is_empty(row[: len(columns)]))
    score += min(filled_rows, 6)

    title_only = sum(1 for row in grid[: max(1, header_row)] if len([c for c in row if validation.clean_text(c)]) == 1)
    if title_only:
        score -= 1  # a cover sheet with one long line of text
    return score


def pick_best_sheet(path: Path, probe_rows: int = 20) -> Optional[str]:
    """Return the sheet name that most likely holds real data."""
    try:
        names = list_sheets(path)
    except ExcelError:
        return None
    if not names:
        return None
    if len(names) == 1:
        return names[0]

    best_name, best_score = names[0], -10_000
    for name in names[:12]:
        try:
            grid, _used, _backend = _read_any(path, name, probe_rows)
        except ExcelError:
            continue
        score = _sheet_score(grid)
        if score > best_score:
            best_name, best_score = name, score
    return best_name


def _read_any(path: Path, sheet_name: Optional[str], max_rows: Optional[int]) -> tuple[list[list], str, str]:
    """Internal reader used by the sheet picker (no recursion into read_grid)."""
    try:
        import pandas  # noqa: F401

        return _read_grid_pandas(path, sheet_name, max_rows)
    except ExcelError:
        return _read_grid_native(path, sheet_name, max_rows)
    except ImportError:
        return _read_grid_native(path, sheet_name, max_rows)


# ---------------------------------------------------------------------------
# Header detection & auto mapping
# ---------------------------------------------------------------------------
def guess_header_row(grid: list[list], scan: int = 8) -> int:
    """1-based row number that most likely holds the column titles."""
    best_index, best_score = 0, -1
    for index, row in enumerate(grid[:scan]):
        cells = [validation.clean_text(cell) for cell in row]
        filled = [cell for cell in cells if cell]
        if not filled:
            continue
        text_like = sum(1 for cell in filled if not re.fullmatch(r"[\d\s:/.-]+", cell))
        score = text_like * 2 + len(filled)
        # A title row (one long merged cell) must not win.
        if len(filled) == 1 and len(filled[0]) > 25:
            score -= 10
        if score > best_score:
            best_index, best_score = index, score
    return best_index + 1


def build_columns(grid: list[list], header_row: int) -> list[str]:
    """Column names for a 1-based header row, duplicates made unique."""
    if not grid:
        return []
    index = max(0, min(header_row - 1, len(grid) - 1))
    raw = [validation.clean_text(cell) for cell in grid[index]]

    columns: list[str] = []
    seen: dict[str, int] = {}
    for position, name in enumerate(raw):
        name = name or f"Column {position + 1}"
        if name in seen:
            seen[name] += 1
            name = f"{name} ({seen[name]})"
        else:
            seen[name] = 0
        columns.append(name)

    # Trim trailing unnamed columns that hold no data at all.
    while columns and columns[-1].startswith("Column ") and _column_empty(grid, index, len(columns) - 1):
        columns.pop()
    return columns


def _column_empty(grid: list[list], start_row: int, column_index: int) -> bool:
    for row in grid[start_row + 1:]:
        if column_index < len(row) and validation.clean_text(row[column_index]):
            return False
    return True


def auto_mapping(columns: list[str], synonyms: Optional[dict] = None) -> dict[str, str]:
    """Best-effort mapping. The user always confirms it in the UI."""
    synonyms = synonyms or SYNONYMS
    normalised_columns = {column: _normalise_header(column) for column in columns}
    mapping: dict[str, str] = {}
    taken: set[str] = set()

    # Pass 1: exact header match.
    for field, words in synonyms.items():
        for column, normalised in normalised_columns.items():
            if column in taken:
                continue
            if normalised in words:
                mapping[field] = column
                taken.add(column)
                break

    # Pass 2: header contains a synonym (or the other way round).
    for field, words in synonyms.items():
        if field in mapping:
            continue
        best_column, best_length = None, 0
        for column, normalised in normalised_columns.items():
            if column in taken:
                continue
            for word in words:
                if word and (word in normalised or normalised in word) and len(word) > best_length:
                    best_column, best_length = column, len(word)
        if best_column:
            mapping[field] = best_column
            taken.add(best_column)

    return mapping


# ---------------------------------------------------------------------------
# Preview (what the user sees before processing)
# ---------------------------------------------------------------------------
def preview_file(path: Path, settings: Optional[dict] = None, limit: int = 20, sheet_name: Optional[str] = None) -> dict:
    """Read a file and describe it: columns, first rows, and data quality."""
    settings = settings or {}
    grid, used_sheet, backend = read_grid(path, sheet_name=sheet_name, max_rows=None)
    if not grid:
        raise ExcelError("This sheet is empty.", "empty_sheet",
                         "Check that the file has data and that you picked the correct sheet.")

    header_row = guess_header_row(grid)
    columns = build_columns(grid, header_row)
    if not columns:
        raise ExcelError("No column headings were found.", "no_headers",
                         "Add one row of headings (Beneficiary Name, Vaccine, Date ...) and upload again.")

    mapping = auto_mapping(columns)
    body = grid[header_row:]
    body = [row for row in body[:100000]]
    total_rows = len(body)
    empty_rows = sum(1 for row in body if validation.row_is_empty(row[: len(columns)]))
    data_rows = [row for row in body if not validation.row_is_empty(row[: len(columns)])]

    seen: set[tuple] = set()
    duplicate_rows = 0
    missing_cells = 0
    for row in data_rows:
        values = [validation.clean_text(cell) for cell in row[: len(columns)]]
        missing_cells += sum(1 for value in values if value == "")
        key = tuple(values)
        if key in seen:
            duplicate_rows += 1
        else:
            seen.add(key)

    preview_rows = []
    for row in data_rows[:limit]:
        preview_rows.append([_display_cell(cell) for cell in row[: len(columns)]])

    warnings = []
    if total_rows == 0:
        raise ExcelError("The sheet has headings but no data rows.", "empty_sheet",
                         "Add the beneficiary rows and upload again.")
    if total_rows > 5000:
        warnings.append(validation.warning(validation.INFO, "file",
                                          f"This file has {total_rows} rows. Processing may be slow on a phone."))
    if duplicate_rows:
        warnings.append(validation.warning(validation.DUPLICATE, "file",
                                          f"{duplicate_rows} row(s) look exactly duplicated.",
                                          hint="Duplicates are marked and you decide what to do."))
    if empty_rows:
        warnings.append(validation.warning(validation.INFO, "file",
                                          f"{empty_rows} completely empty row(s) were found and skipped."))
    if missing_cells:
        warnings.append(validation.warning(validation.MISSING, "file",
                                          f"{missing_cells} empty cell(s) across the data rows.",
                                          hint="Empty cells are never filled automatically."))

    date_info = detect_date_order(grid, columns, mapping)

    return {
        "sheet_name": used_sheet,
        "sheets": list_sheets(path),
        "backend": backend,
        "header_row": header_row,
        "date_order": date_info["order"],
        "date_evidence": date_info["evidence"],
        "columns": columns,
        "mapping": mapping,
        "fields": FIELD_DEFINITIONS,
        "rows": preview_rows,
        "total_rows": total_rows,
        "data_rows": len(data_rows),
        "empty_rows": empty_rows,
        "duplicate_rows": duplicate_rows,
        "missing_cells": missing_cells,
        "warnings": warnings,
        "sheet_preview_grid": [[_display_cell(cell) for cell in row[: len(columns)]] for row in grid[: min(len(grid), 5)]],
    }


def _display_cell(value: Any) -> str:
    if isinstance(value, datetime):
        return value.strftime("%d-%m-%Y") if (value.hour, value.minute) == (0, 0) else value.strftime("%d-%m-%Y %H:%M")
    if isinstance(value, date):
        return value.strftime("%d-%m-%Y")
    return validation.clean_text(value)



# ---------------------------------------------------------------------------
# Date order detection (whole file, so single rows are never guessed one by one)
# ---------------------------------------------------------------------------
_ISO_LIKE_RE = re.compile(r"^(\d{4})[-/.](\d{1,2})[-/.](\d{1,4})$")
_NUMERIC_DATE_RE = re.compile(r"^(\d{1,4})[-/.](\d{1,2})[-/.](\d{1,4})$")


def detect_date_order(grid: list[list], columns: list[str], mapping: dict[str, str], sample: int = 250) -> dict:
    """Work out whether the file writes dates day-first or month-first.

    Rules (no guessing):
    * If any value has a first part greater than 12, the file must be day-first.
    * Else if any value has a second part greater than 12, the file is month-first.
    * Else the order cannot be proven; the Indian standard **DD-MM-YYYY** is used
      and the worker is told to change it in Settings if the file is different.
    """
    day_first = month_first = iso_like = day_month_alias = 0
    examples: list[str] = []

    indexes = [
        columns.index(column) for column in {mapping.get("dob"), mapping.get("vaccination_date")} if column in columns
    ]
    body = grid[guess_header_row(grid):] if grid else []

    for row in body[:sample]:
        for position in indexes:
            if position >= len(row):
                continue
            text = validation.clean_text(row[position])
            if not text:
                continue
            match = _NUMERIC_DATE_RE.match(text.split()[0])
            if not match:
                continue
            first, second, _third = (int(part) for part in match.groups())
            if len(match.group(1)) == 4:
                iso_like += 1
                continue
            if first > 12:
                day_first += 1
            elif second > 12:
                month_first += 1
            elif first == second:
                day_month_alias += 1
            if len(examples) < 3:
                examples.append(text)

    if day_first > month_first:
        return {"order": "DD-MM-YYYY", "evidence": "day value above 12 found", "examples": examples}
    if month_first > day_first:
        return {"order": "MM/DD/YYYY", "evidence": "month value above 12 found", "examples": examples}
    if iso_like and not day_first and not month_first:
        return {"order": "YYYY-MM-DD", "evidence": "four digit year first", "examples": examples}
    return {"order": "DD-MM-YYYY", "evidence": "assumed (Indian standard)", "examples": examples}


# ---------------------------------------------------------------------------
# Turning a sheet into records
# ---------------------------------------------------------------------------
def extract_records(
    path: Path,
    mapping: dict[str, str],
    settings: Optional[dict] = None,
    sheet_name: Optional[str] = None,
    header_row: Optional[int] = None,
) -> dict:
    """Build validated record dictionaries for one uploaded file.

    Returns ``{"records": [...], "stats": {...}, "warnings": [...], "columns": [...]}``.

    Notes
    -----
    * Blank rows are skipped **and counted**.
    * Nothing is ever invented: a missing value stays missing and produces a
      warning that pushes the record to ``NEEDS REVIEW``.
    * A broken row produces a warning; it never stops the import.
    """
    settings = settings or {}
    grid, used_sheet, backend = read_grid(path, sheet_name=sheet_name, max_rows=None)
    if not grid:
        raise ExcelError("This sheet is empty.", "empty_sheet")

    header_row = header_row or guess_header_row(grid)
    columns = build_columns(grid, header_row)
    mapping = {field: column for field, column in (mapping or {}).items() if column in columns}

    mapping_problems = validation.validate_mapping(mapping, columns)
    if any(item["code"] == validation.MISSING and item["field"] == "name" for item in mapping_problems):
        raise ExcelError(
            "The Beneficiary Name column is not mapped, so rows cannot be identified.",
            "mapping_incomplete",
            "Go back to Upload Excel -> Map Columns and choose the name column.",
        )

    column_index = {column: position for position, column in enumerate(columns)}
    body = grid[header_row:]

    # Work out the day/month order once for the whole file (never row by row).
    date_info = detect_date_order(grid, columns, mapping)
    effective_settings = dict(settings)
    if str(effective_settings.get("date_format", "auto")) == "auto":
        effective_settings["date_format"] = date_info["order"]

    date_warnings = list(mapping_problems)
    if date_info["evidence"].startswith("assumed"):
        date_warnings.append(
            validation.warning(
                validation.INFO,
                "date_format",
                f"Day/month order was not clear in the file, so dates were read as {date_info['order']} (day first)."
                + (f" Examples: {', '.join(date_info['examples'])}." if date_info["examples"] else ""),
                hint="If your file actually uses MM/DD/YYYY, change Settings → Date format and import the file again.",
            )
        )
    else:
        date_warnings.append(
            validation.warning(
                validation.INFO,
                "date_format",
                f"Dates were read as {date_info['order']} ({date_info['evidence']}).",
            )
        )

    records: list[dict] = []
    stats = {
        "total_rows": len(body),
        "empty_rows": 0,
        "duplicate_rows": 0,
        "missing_cells": 0,
        "imported": 0,
        "nameless_rows": 0,
        "needs_review": 0,
        "warnings": 0,
        "sheet_name": used_sheet,
        "backend": backend,
        "header_row": header_row,
        "date_order": date_info["order"],
        "date_evidence": date_info["evidence"],
    }

    for offset, row in enumerate(body):
        excel_row = header_row + 1 + offset
        values = list(row)
        if validation.row_is_empty(values[: len(columns)]):
            stats["empty_rows"] += 1
            continue

        record: dict[str, Any] = {"row_number": excel_row, "raw": [_display_cell(cell) for cell in values[: len(columns)]]}
        for field in FIELD_ORDER:
            column = mapping.get(field, "")
            if column:
                position = column_index[column]
                record[field] = validation.clean_text(values[position]) if position < len(values) else ""
            else:
                record[field] = ""

        extras = {}
        mapped_columns = {column for column in mapping.values() if column}
        for position, column in enumerate(columns):
            if column in mapped_columns:
                continue
            value = _display_cell(values[position]) if position < len(values) else ""
            if value:
                extras[column] = value
        record["extras"] = extras

        result = validation.validate_record_fields(record, effective_settings)
        record["warnings"] = result["warnings"]

        # Keep the original text and store the *verified* ISO date in the main
        # field, so later re-checks (and reports) never have to guess again.
        record["dob_raw"] = record.get("dob") or ""
        record["vaccination_date_raw"] = record.get("vaccination_date") or ""
        if record.get("_dob_iso"):
            record["dob"] = record["_dob_iso"]
        if record.get("_vax_iso"):
            record["vaccination_date"] = record["_vax_iso"]
        record["status"] = config.STATUS_NEEDS_REVIEW if result["needs_review"] else config.STATUS_PENDING
        record["match_confidence"] = 0.0
        record["match"] = None
        record["error_message"] = None

        # A row with data but no name/ID is NOT thrown away - it is flagged for
        # review so the worker can match it manually (it may be a newborn whose
        # name was written in a later column, for example).
        if not record.get("name") and not record.get("beneficiary_id"):
            stats["nameless_rows"] = stats.get("nameless_rows", 0) + 1

        stats["missing_cells"] += sum(1 for field in FIELD_ORDER if not record.get(field))
        stats["warnings"] += len(record["warnings"])
        if result["needs_review"]:
            stats["needs_review"] += 1
        records.append(record)

    # Duplicate detection across the whole file (all identifying fields equal).
    clusters = validation.duplicate_clusters(records)
    duplicate_rows = 0
    for _key, indexes in clusters.items():
        first = records[indexes[0]]
        others = [records[i]["row_number"] for i in indexes[1:]]
        first["warnings"].append(
            validation.warning(
                validation.INFO,
                "row",
                "An identical row also appears at Excel row(s) " + ", ".join(str(n) for n in others) + ".",
                hint="Only one of them should be entered on the portal.",
            )
        )
        for position in indexes[1:]:
            record = records[position]
            record["duplicate_of"] = first["row_number"]
            record["warnings"].append(
                validation.warning(
                    validation.DUPLICATE,
                    "row",
                    f"This row looks identical to row {first['row_number']}.",
                    hint="Keep only one copy unless you are sure the child really received the dose twice.",
                )
            )
            if record["status"] == config.STATUS_PENDING:
                record["status"] = config.STATUS_NEEDS_REVIEW
            duplicate_rows += 1
    stats["duplicate_rows"] = duplicate_rows
    stats["imported"] = len(records)

    if not records:
        raise ExcelError("No usable rows were found in this sheet.", "no_records",
                         "Check the header row setting and the column mapping, then try again.")

    return {
        "records": records,
        "stats": stats,
        "warnings": date_warnings,
        "date_order": date_info["order"],
        "date_evidence": date_info["evidence"],
        "columns": columns,
        "mapping": mapping,
        "sheet_name": used_sheet,
        "header_row": header_row,
        "backend": backend,
    }


# ---------------------------------------------------------------------------
# Authorised beneficiary register (imported by the worker)
# ---------------------------------------------------------------------------
def extract_register_rows(path: Path, sheet_name: Optional[str] = None, header_row: Optional[int] = None) -> dict:
    """Read the authorised beneficiary register used by the matching engine."""
    grid, used_sheet, backend = read_grid(path, sheet_name=sheet_name, max_rows=None)
    if not grid:
        raise ExcelError("This sheet is empty.", "empty_sheet")

    header_row = header_row or guess_header_row(grid)
    columns = build_columns(grid, header_row)
    mapping = auto_mapping(columns, REGISTER_SYNONYMS)

    if not mapping.get("name") and not mapping.get("beneficiary_id"):
        raise ExcelError(
            "No name or beneficiary ID column was found in the register file.",
            "mapping_incomplete",
            "The register file needs at least a Beneficiary Name or Beneficiary ID column.",
        )

    column_index = {column: position for position, column in enumerate(columns)}
    date_info = detect_date_order(grid, columns, mapping)
    rows: list[dict] = []
    skipped = 0
    warnings: list[dict] = []

    for offset, raw_row in enumerate(grid[header_row:]):
        row_number = header_row + 1 + offset
        values = list(raw_row)
        if validation.row_is_empty(values[: len(columns)]):
            skipped += 1
            continue

        row = {"row_number": row_number, "extra": {}}
        for field, column in mapping.items():
            position = column_index[column]
            row[field] = validation.clean_text(values[position]) if position < len(values) else ""

        for position, column in enumerate(columns):
            if column in mapping.values():
                continue
            value = _display_cell(values[position]) if position < len(values) else ""
            if value:
                row["extra"][column] = value

        dob = validation.parse_date(row.get("dob"), date_info["order"])
        row["dob"] = dob.get("iso") or ""
        row["name_key"] = validation.name_key(row.get("name"))
        row["dob_key"] = row["dob"]

        problems = validation.validate_register_row(row)
        if len(warnings) < 50:
            warnings.extend(problems)
        rows.append(row)

    return {
        "rows": rows,
        "count": len(rows),
        "skipped": skipped,
        "date_order": date_info["order"],
        "warnings": warnings,
        "columns": columns,
        "mapping": mapping,
        "sheet_name": used_sheet,
        "header_row": header_row,
        "backend": backend,
    }


# ---------------------------------------------------------------------------
# Writing helper used by the report generator and the sample-file builder
# ---------------------------------------------------------------------------
def write_xlsx(path: Path, sheets: dict[str, tuple[list[str], list[list]]], autosize: bool = True) -> Path:
    """Write an .xlsx file with several tables. ``sheets`` -> name: (headers, rows)."""
    try:
        from openpyxl import Workbook
    except ImportError as exc:  # pragma: no cover
        raise ExcelError("openpyxl is required to write Excel reports.", "missing_library",
                         "Run: pip install openpyxl") from exc

    workbook = Workbook()
    default = workbook.active
    first = True

    for name, (headers, rows) in sheets.items():
        safe_name = re.sub(r"[\\/*?:\[\]]", "-", name)[:31] or "Sheet"
        sheet = default if first else workbook.create_sheet()
        sheet.title = safe_name
        first = False

        sheet.append(list(headers))
        for row in rows:
            sheet.append([_excel_safe(value) for value in row])

        for column_index, header in enumerate(headers, start=1):
            width = max(12, min(38, len(str(header)) + 4))
            if autosize:
                for row in rows[:200]:
                    if column_index - 1 < len(row):
                        width = max(width, min(38, len(str(row[column_index - 1])) + 3))
            sheet.column_dimensions[_column_letter(column_index)].width = width

        if headers:
            from openpyxl.styles import Alignment, Font, PatternFill

            fill = PatternFill("solid", fgColor="0B5ED7")
            font = Font(bold=True, color="FFFFFF")
            for cell in sheet[1]:
                cell.fill = fill
                cell.font = font
                cell.alignment = Alignment(vertical="center")
            sheet.freeze_panes = "A2"

    workbook.save(str(path))
    return path


def _column_letter(index: int) -> str:
    letters = ""
    while index:
        index, remainder = divmod(index - 1, 26)
        letters = chr(65 + remainder) + letters
    return letters


def _excel_safe(value: Any) -> Any:
    """Keep Excel happy: no timezone objects, no giant strings, no leading '='."""
    if value is None:
        return ""
    if isinstance(value, (datetime, date)):
        return value.strftime("%Y-%m-%d")
    if isinstance(value, bool):
        return "Yes" if value else "No"
    if isinstance(value, (int, float)):
        return value
    text = str(value)
    if text.startswith(("=", "+", "@")):
        text = "'" + text  # avoid accidental formulas in the report
    return text[:32000]


def write_csv(path: Path, headers: list[str], rows: list[list], delimiter: str = ",") -> Path:
    """Write a UTF-8 CSV (Excel opens it, and it is small)."""
    with open(path, "w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.writer(handle, delimiter=delimiter, quoting=csv.QUOTE_MINIMAL)
        writer.writerow(headers)
        for row in rows:
            writer.writerow([_display_cell(value) if not isinstance(value, (int, float)) else value for value in row])
    return path
