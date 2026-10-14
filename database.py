"""
database.py
===========
Local SQLite storage for **U-WIN Smart Assistant**.

Design rules
------------
* One process-wide connection with a lock (Flask's dev server is threaded).
  SQLite is very fast for our workload (hundreds of rows) and this avoids
  "database is locked" errors on Android.
* WAL journal mode so a crash / Termux kill does not corrupt the file.
* **No credentials** ever go into this database: no passwords, no OTP values.
  (There are simply no columns for them.)
* Every helper opens short transactions and commits immediately, so a sudden
  Android restart cannot lose more than the current action.
"""

from __future__ import annotations

import json
import sqlite3
import threading
from datetime import datetime
from typing import Any, Iterable, Optional

import config

# ---------------------------------------------------------------------------
# Low level plumbing
# ---------------------------------------------------------------------------
_lock = threading.RLock()
_conn: Optional[sqlite3.Connection] = None
_SCHEMA_VERSION = 1


def now_iso() -> str:
    """Human friendly local timestamp used everywhere in the app."""
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def json_dumps(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, default=str)


def json_loads(raw: Any, fallback: Any = None) -> Any:
    if raw in (None, "", b""):
        return fallback
    if isinstance(raw, (dict, list)):
        return raw
    try:
        return json.loads(raw)
    except (TypeError, ValueError):
        return fallback


def get_conn() -> sqlite3.Connection:
    """Return the shared connection, creating/upgrading the database on first use."""
    global _conn
    with _lock:
        if _conn is None:
            config.ensure_dirs()
            _conn = sqlite3.connect(
                str(config.DB_PATH),
                timeout=20.0,
                check_same_thread=False,
                isolation_level=None,  # autocommit; we group writes explicitly
            )
            _conn.row_factory = sqlite3.Row
            _conn.execute("PRAGMA journal_mode=WAL")
            _conn.execute("PRAGMA synchronous=NORMAL")
            _conn.execute("PRAGMA foreign_keys=ON")
            _create_schema(_conn)
            _seed_settings(_conn)
        return _conn


def close_conn() -> None:
    """Close the connection (used by tests and on clean shutdown)."""
    global _conn
    with _lock:
        if _conn is not None:
            try:
                _conn.close()
            finally:
                _conn = None


def execute(sql: str, params: Iterable = ()) -> int:
    """Run a write statement, return lastrowid (or rowcount for updates)."""
    with _lock:
        conn = get_conn()
        cur = conn.execute(sql, tuple(params))
        if cur.lastrowid:
            return cur.lastrowid
        return cur.rowcount or 0


def executemany(sql: str, rows: Iterable[Iterable]) -> int:
    with _lock:
        conn = get_conn()
        cur = conn.executemany(sql, [tuple(r) for r in rows])
        return cur.rowcount or 0


def query_all(sql: str, params: Iterable = ()) -> list[sqlite3.Row]:
    with _lock:
        return list(get_conn().execute(sql, tuple(params)).fetchall())


def query_one(sql: str, params: Iterable = ()) -> Optional[sqlite3.Row]:
    with _lock:
        return get_conn().execute(sql, tuple(params)).fetchone()


def query_value(sql: str, params: Iterable = (), default: Any = 0) -> Any:
    row = query_one(sql, params)
    if row is None:
        return default
    value = row[0]
    return default if value is None else value


# ---------------------------------------------------------------------------
# Schema
# ---------------------------------------------------------------------------
_SCHEMA = """
CREATE TABLE IF NOT EXISTS meta (
    key   TEXT PRIMARY KEY,
    value TEXT
);

CREATE TABLE IF NOT EXISTS settings (
    key        TEXT PRIMARY KEY,
    value      TEXT,
    updated_at TEXT
);

CREATE TABLE IF NOT EXISTS sessions (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    title           TEXT,
    original_filename TEXT,
    stored_filename TEXT,
    file_hash       TEXT,
    file_size       INTEGER DEFAULT 0,
    sheet_name      TEXT,
    header_row      INTEGER DEFAULT 1,
    backend         TEXT,
    total_rows      INTEGER DEFAULT 0,
    empty_rows      INTEGER DEFAULT 0,
    duplicate_rows  INTEGER DEFAULT 0,
    missing_cells   INTEGER DEFAULT 0,
    mappings_json   TEXT,
    columns_json    TEXT,
    warnings_json   TEXT,
    status          TEXT DEFAULT 'active',
    notes           TEXT,
    created_at      TEXT,
    updated_at      TEXT,
    last_opened_at  TEXT,
    completed_at    TEXT
);

CREATE TABLE IF NOT EXISTS records (
    id                INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id        INTEGER NOT NULL REFERENCES sessions(id) ON DELETE CASCADE,
    row_number        INTEGER,
    status            TEXT DEFAULT 'PENDING',
    name              TEXT,
    dob               TEXT,
    dob_raw           TEXT,
    gender            TEXT,
    mobile            TEXT,
    beneficiary_id    TEXT,
    mother_name       TEXT,
    address           TEXT,
    district          TEXT,
    block             TEXT,
    facility          TEXT,
    vaccine_name      TEXT,
    dose_number       TEXT,
    vaccination_date  TEXT,
    vaccination_date_raw TEXT,
    batch_number      TEXT,
    vaccinator_name   TEXT,
    remarks           TEXT,
    extras_json       TEXT,
    raw_json          TEXT,
    edited_json       TEXT,
    warnings_json     TEXT,
    match_json        TEXT,
    match_confidence  REAL DEFAULT 0,
    matched_official_id INTEGER,
    decision          TEXT,
    decision_note     TEXT,
    decided_at        TEXT,
    save_state        TEXT,
    save_reference    TEXT,
    save_confirmed_at TEXT,
    save_attempts     INTEGER DEFAULT 0,
    error_message     TEXT,
    duplicate_of      INTEGER,
    manually_verified    INTEGER DEFAULT 0,
    created_at        TEXT,
    updated_at        TEXT
);

CREATE INDEX IF NOT EXISTS idx_records_session ON records(session_id, id);
CREATE INDEX IF NOT EXISTS idx_records_status  ON records(session_id, status);
CREATE INDEX IF NOT EXISTS idx_records_name    ON records(name);

CREATE TABLE IF NOT EXISTS official_records (
    id             INTEGER PRIMARY KEY AUTOINCREMENT,
    beneficiary_id TEXT,
    name           TEXT,
    dob            TEXT,
    gender         TEXT,
    mobile         TEXT,
    mother_name    TEXT,
    extra_json     TEXT,
    name_key       TEXT,
    dob_key        TEXT,
    source_file    TEXT,
    imported_at    TEXT
);

CREATE INDEX IF NOT EXISTS idx_official_name ON official_records(name_key);
CREATE INDEX IF NOT EXISTS idx_official_dob  ON official_records(dob_key);

CREATE TABLE IF NOT EXISTS queue_state (
    id                INTEGER PRIMARY KEY CHECK (id = 1),
    paused            INTEGER DEFAULT 0,
    stopped           INTEGER DEFAULT 0,
    mode              TEXT DEFAULT 'manual',
    current_record_id INTEGER,
    updated_at        TEXT
);

CREATE TABLE IF NOT EXISTS events (
    id         INTEGER PRIMARY KEY AUTOINCREMENT,
    ts         TEXT,
    level      TEXT DEFAULT 'info',
    event      TEXT,
    session_id INTEGER,
    record_id  INTEGER,
    message    TEXT,
    detail_json TEXT
);

CREATE INDEX IF NOT EXISTS idx_events_ts ON events(ts DESC);

CREATE TABLE IF NOT EXISTS reports (
    id          INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id  INTEGER,
    filename    TEXT,
    kind        TEXT,
    scope       TEXT,
    rows_count  INTEGER DEFAULT 0,
    size_bytes  INTEGER DEFAULT 0,
    created_at  TEXT
);
"""


def _create_schema(conn: sqlite3.Connection) -> None:
    conn.executescript(_SCHEMA)
    current = conn.execute("SELECT value FROM meta WHERE key='schema_version'").fetchone()
    if current is None:
        conn.execute(
            "INSERT INTO meta(key, value) VALUES('schema_version', ?)",
            (str(_SCHEMA_VERSION),),
        )
    conn.execute(
        "INSERT OR IGNORE INTO queue_state(id, paused, stopped, mode, updated_at) "
        "VALUES(1, 0, 0, 'manual', ?)",
        (now_iso(),),
    )
    conn.execute(
        "INSERT OR IGNORE INTO meta(key, value) VALUES('created_at', ?)", (now_iso(),)
    )


def _seed_settings(conn: sqlite3.Connection) -> None:
    for key, value in config.DEFAULT_SETTINGS.items():
        conn.execute(
            "INSERT OR IGNORE INTO settings(key, value, updated_at) VALUES(?,?,?)",
            (key, json_dumps(value), now_iso()),
        )


# ---------------------------------------------------------------------------
# Settings
# ---------------------------------------------------------------------------
def get_setting(key: str, default: Any = None):
    row = query_one("SELECT value FROM settings WHERE key = ?", (key,))
    if row is None:
        return config.DEFAULT_SETTINGS.get(key, default)
    return json_loads(row["value"], config.DEFAULT_SETTINGS.get(key, default))


def get_all_settings() -> dict:
    settings = dict(config.DEFAULT_SETTINGS)
    for row in query_all("SELECT key, value FROM settings"):
        settings[row["key"]] = json_loads(row["value"], settings.get(row["key"]))
    return settings


def set_setting(key: str, value: Any) -> None:
    safe = config.coerce_setting(key, value)
    execute(
        "INSERT INTO settings(key, value, updated_at) VALUES(?,?,?) "
        "ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=excluded.updated_at",
        (key, json_dumps(safe), now_iso()),
    )


def set_settings(values: dict) -> dict:
    for key, value in values.items():
        set_setting(key, value)
    return get_all_settings()


# ---------------------------------------------------------------------------
# Event log (never contains patient identifiers beyond row numbers)
# ---------------------------------------------------------------------------
def log_event(
    event: str,
    message: str = "",
    level: str = "info",
    session_id: Optional[int] = None,
    record_id: Optional[int] = None,
    detail: Any = None,
) -> None:
    # Defensive scrub: no password / OTP values can ever reach the log table.
    text = message or ""
    for forbidden in ("password", "otp", "token"):
        if forbidden in text.lower():
            text = "[redacted]"
            break
    execute(
        "INSERT INTO events(ts, level, event, session_id, record_id, message, detail_json) "
        "VALUES(?,?,?,?,?,?,?)",
        (now_iso(), level, event, session_id, record_id, text, json_dumps(detail) if detail else None),
    )


def list_events(limit: int = 40, offset: int = 0) -> list[dict]:
    rows = query_all(
        "SELECT * FROM events ORDER BY id DESC LIMIT ? OFFSET ?", (limit, offset)
    )
    return [dict(row) for row in rows]


def count_events() -> int:
    return int(query_value("SELECT COUNT(*) FROM events"))


# ---------------------------------------------------------------------------
# Sessions (one uploaded Excel file = one session)
# ---------------------------------------------------------------------------
def create_session(
    original_filename: str,
    stored_filename: str,
    file_hash: str,
    file_size: int,
    sheet_name: str = "",
    header_row: int = 1,
    backend: str = "",
    total_rows: int = 0,
    empty_rows: int = 0,
    duplicate_rows: int = 0,
    missing_cells: int = 0,
    mappings: Optional[dict] = None,
    columns: Optional[list] = None,
    warnings: Optional[list] = None,
) -> int:
    stamp = now_iso()
    return execute(
        """INSERT INTO sessions(
               title, original_filename, stored_filename, file_hash, file_size, sheet_name,
               header_row, backend, total_rows, empty_rows, duplicate_rows, missing_cells,
               mappings_json, columns_json, warnings_json, status, created_at, updated_at, last_opened_at)
           VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)""",
        (
            original_filename,
            original_filename,
            stored_filename,
            file_hash,
            file_size,
            sheet_name,
            header_row,
            backend,
            total_rows,
            empty_rows,
            duplicate_rows,
            missing_cells,
            json_dumps(mappings or {}),
            json_dumps(columns or []),
            json_dumps(warnings or []),
            config.SESSION_ACTIVE,
            stamp,
            stamp,
            stamp,
        ),
    )


def update_session(session_id: int, **fields) -> None:
    if not fields:
        return
    fields["updated_at"] = now_iso()
    for key in ("mappings", "columns", "warnings"):
        if key in fields:
            fields[f"{key}_json"] = json_dumps(fields.pop(key))
    assignments = ", ".join(f"{key} = ?" for key in fields)
    execute(
        f"UPDATE sessions SET {assignments} WHERE id = ?",
        list(fields.values()) + [session_id],
    )


def get_session(session_id: int) -> Optional[dict]:
    row = query_one("SELECT * FROM sessions WHERE id = ?", (session_id,))
    return dict(row) if row else None


def get_latest_session(status: Optional[str] = None) -> Optional[dict]:
    if status:
        row = query_one(
            "SELECT * FROM sessions WHERE status = ? ORDER BY id DESC LIMIT 1", (status,)
        )
    else:
        row = query_one("SELECT * FROM sessions ORDER BY id DESC LIMIT 1")
    return dict(row) if row else None


def list_sessions(limit: int = 50) -> list[dict]:
    rows = query_all("SELECT * FROM sessions ORDER BY id DESC LIMIT ?", (limit,))
    sessions = []
    for row in rows:
        item = dict(row)
        item.update(status_counts(row["id"]))
        sessions.append(item)
    return sessions


def find_resumable_sessions() -> list[dict]:
    """Sessions that still have work left -> used by "Resume Previous Session"."""
    rows = query_all(
        """SELECT * FROM sessions
           WHERE status IN ('active','parked')
           ORDER BY updated_at DESC, id DESC LIMIT 10"""
    )
    result = []
    for row in rows:
        counts = status_counts(row["id"])
        outstanding = (
            counts["PENDING"]
            + counts["MATCHED"]
            + counts["NEEDS REVIEW"]
            + counts["APPROVED"]
        )
        if outstanding:
            item = dict(row)
            item.update(counts)
            item["outstanding"] = outstanding
            item["progress"] = progress_percent(counts)
            result.append(item)
    return result


def delete_session(session_id: int, delete_files: bool = True) -> dict:
    session = get_session(session_id)
    if not session:
        return {"ok": False, "message": "Session not found."}
    execute("DELETE FROM records WHERE session_id = ?", (session_id,))
    execute("DELETE FROM reports WHERE session_id = ?", (session_id,))
    execute("DELETE FROM sessions WHERE id = ?", (session_id,))
    removed = []
    if delete_files:
        stored = session.get("stored_filename")
        if stored:
            path = config.UPLOAD_DIR / stored
            if path.exists():
                path.unlink()
                removed.append(stored)
    log_event("session_deleted", f"Session #{session_id} deleted", session_id=session_id)
    return {"ok": True, "removed_files": removed, "message": "Session deleted."}


# ---------------------------------------------------------------------------
# Records
# ---------------------------------------------------------------------------
RECORD_INSERT_SQL = """
INSERT INTO records(
    session_id, row_number, status, name, dob, dob_raw, gender, mobile, beneficiary_id,
    mother_name, address, district, block, facility, vaccine_name, dose_number,
    vaccination_date, vaccination_date_raw, batch_number, vaccinator_name, remarks,
    extras_json, raw_json, warnings_json, match_json, match_confidence,
    matched_official_id, error_message, duplicate_of, created_at, updated_at
) VALUES(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)
"""


def bulk_insert_records(session_id: int, rows: Iterable[dict]) -> int:
    stamp = now_iso()
    payload = []
    for row in rows:
        payload.append(
            (
                session_id,
                row.get("row_number"),
                row.get("status", config.STATUS_PENDING),
                row.get("name"),
                row.get("dob"),
                row.get("dob_raw"),
                row.get("gender"),
                row.get("mobile"),
                row.get("beneficiary_id"),
                row.get("mother_name"),
                row.get("address"),
                row.get("district"),
                row.get("block"),
                row.get("facility"),
                row.get("vaccine_name"),
                row.get("dose_number"),
                row.get("vaccination_date"),
                row.get("vaccination_date_raw"),
                row.get("batch_number"),
                row.get("vaccinator_name"),
                row.get("remarks"),
                json_dumps(row.get("extras", {})),
                json_dumps(row.get("raw", [])),
                json_dumps(row.get("warnings", [])),
                json_dumps(row.get("match")),
                float(row.get("match_confidence") or 0),
                row.get("matched_official_id"),
                row.get("error_message"),
                row.get("duplicate_of"),
                stamp,
                stamp,
            )
        )
    if not payload:
        return 0
    with _lock:
        conn = get_conn()
        conn.execute("BEGIN")
        try:
            conn.executemany(RECORD_INSERT_SQL, payload)
            conn.execute("COMMIT")
        except Exception:
            conn.execute("ROLLBACK")
            raise
    return len(payload)


def get_record(record_id: int) -> Optional[dict]:
    row = query_one("SELECT * FROM records WHERE id = ?", (record_id,))
    return dict(row) if row else None


def update_record(record_id: int, **fields) -> None:
    if not fields:
        return
    for key in ("warnings", "match", "extras", "raw", "edited"):
        if key in fields:
            fields[f"{key}_json"] = json_dumps(fields.pop(key))
    fields["updated_at"] = now_iso()
    assignments = ", ".join(f"{key} = ?" for key in fields)
    execute(
        f"UPDATE records SET {assignments} WHERE id = ?",
        list(fields.values()) + [record_id],
    )


def _filters_sql(session_id=None, status=None, search=None, decision=None) -> tuple[str, list]:
    clauses, params = [], []
    if session_id:
        clauses.append("session_id = ?")
        params.append(session_id)
    if status:
        if isinstance(status, (list, tuple, set)):
            placeholders = ",".join("?" for _ in status)
            clauses.append(f"status IN ({placeholders})")
            params.extend(list(status))
        else:
            clauses.append("status = ?")
            params.append(status)
    if decision:
        clauses.append("decision = ?")
        params.append(decision)
    if search:
        clauses.append(
            "(name LIKE ? OR beneficiary_id LIKE ? OR mobile LIKE ? OR vaccine_name LIKE ?)"
        )
        token = f"%{search.strip()}%"
        params.extend([token, token, token, token])
    sql = (" WHERE " + " AND ".join(clauses)) if clauses else ""
    return sql, params


def count_records(session_id=None, status=None, search=None, decision=None) -> int:
    where, params = _filters_sql(session_id, status, search, decision)
    return int(query_value(f"SELECT COUNT(*) FROM records{where}", params))


def list_records(
    session_id=None,
    status=None,
    search=None,
    decision=None,
    limit: int = 20,
    offset: int = 0,
    order: str = "row_number",
) -> list[dict]:
    where, params = _filters_sql(session_id, status, search, decision)
    order_sql = "row_number ASC, id ASC" if order == "row_number" else "id DESC"
    rows = query_all(
        f"SELECT * FROM records{where} ORDER BY {order_sql} LIMIT ? OFFSET ?",
        params + [limit, offset],
    )
    return [dict(row) for row in rows]


def session_record_ids(session_id: int, status=None) -> list[int]:
    where, params = _filters_sql(session_id, status)
    rows = query_all(
        f"SELECT id FROM records{where} ORDER BY row_number ASC, id ASC", params
    )
    return [row["id"] for row in rows]


def neighbour_record(session_id: int, record_id: int, direction: str = "next") -> Optional[dict]:
    """Previous / next record inside the same session (wraps around)."""
    ids = session_record_ids(session_id)
    if not ids:
        return None
    if record_id not in ids:
        return get_record(ids[0])
    index = ids.index(record_id)
    if direction == "prev":
        index = (index - 1) % len(ids)
    else:
        index = (index + 1) % len(ids)
    return get_record(ids[index])


def next_open_record(session_id: int) -> Optional[dict]:
    """First record that still needs a human decision."""
    row = query_one(
        """SELECT * FROM records
           WHERE session_id = ? AND status IN ('PENDING','MATCHED','NEEDS REVIEW')
           ORDER BY CASE status WHEN 'NEEDS REVIEW' THEN 0 WHEN 'MATCHED' THEN 1 ELSE 2 END,
                    row_number ASC, id ASC LIMIT 1""",
        (session_id,),
    )
    return dict(row) if row else None


STATUS_COUNT_KEYS = {
    config.STATUS_PENDING: "PENDING",
    config.STATUS_MATCHED: "MATCHED",
    config.STATUS_NEEDS_REVIEW: "NEEDS REVIEW",
    config.STATUS_APPROVED: "APPROVED",
    config.STATUS_COMPLETED: "COMPLETED",
    config.STATUS_SKIPPED: "SKIPPED",
    config.STATUS_FAILED: "FAILED",
    config.STATUS_SAVE_UNKNOWN: "SAVE STATUS UNKNOWN",
}


def status_counts(session_id: Optional[int] = None) -> dict:
    counts = {key: 0 for key in STATUS_COUNT_KEYS}
    if session_id:
        rows = query_all(
            "SELECT status, COUNT(*) AS n FROM records WHERE session_id = ? GROUP BY status",
            (session_id,),
        )
    else:
        rows = query_all("SELECT status, COUNT(*) AS n FROM records GROUP BY status")
    for row in rows:
        counts[STATUS_COUNT_KEYS.get(row["status"], row["status"])] = row["n"]
    counts["TOTAL"] = sum(counts.values())
    return counts


def progress_percent(counts: dict) -> float:
    total = counts.get("TOTAL") or 0
    if not total:
        return 0.0
    done = counts.get("COMPLETED", 0) + counts.get("SKIPPED", 0)
    return round(done * 100.0 / total, 1)


# ---------------------------------------------------------------------------
# Authorised beneficiary register (imported by the worker, stays on device)
# ---------------------------------------------------------------------------
def replace_official_records(rows: Iterable[dict], source_file: str) -> int:
    stamp = now_iso()
    with _lock:
        conn = get_conn()
        conn.execute("BEGIN")
        try:
            conn.execute("DELETE FROM official_records")
            conn.executemany(
                """INSERT INTO official_records(
                       beneficiary_id, name, dob, gender, mobile, mother_name,
                       extra_json, name_key, dob_key, source_file, imported_at)
                   VALUES(?,?,?,?,?,?,?,?,?,?,?)""",
                [
                    (
                        row.get("beneficiary_id"),
                        row.get("name"),
                        row.get("dob"),
                        row.get("gender"),
                        row.get("mobile"),
                        row.get("mother_name"),
                        json_dumps(row.get("extra", {})),
                        row.get("name_key"),
                        row.get("dob_key"),
                        source_file,
                        stamp,
                    )
                    for row in rows
                ],
            )
            conn.execute("COMMIT")
        except Exception:
            conn.execute("ROLLBACK")
            raise
    count = count_official_records()
    log_event("register_imported", f"{count} authorised records imported", detail={"file": source_file})
    return count


def official_records_all() -> list[dict]:
    return [dict(row) for row in query_all("SELECT * FROM official_records ORDER BY id")]


def count_official_records() -> int:
    return int(query_value("SELECT COUNT(*) FROM official_records"))


def official_register_info() -> dict:
    row = query_one(
        "SELECT source_file, imported_at, COUNT(*) AS n FROM official_records "
        "GROUP BY source_file, imported_at ORDER BY id DESC LIMIT 1"
    )
    if not row:
        return {"count": 0, "source_file": "", "imported_at": ""}
    return {"count": row["n"], "source_file": row["source_file"], "imported_at": row["imported_at"]}


def clear_official_records() -> None:
    execute("DELETE FROM official_records")
    log_event("register_cleared", "Authorised register cleared", level="warn")


# ---------------------------------------------------------------------------
# Queue state (pause / resume / stop)
# ---------------------------------------------------------------------------
def get_queue_state() -> dict:
    row = query_one("SELECT * FROM queue_state WHERE id = 1")
    if row is None:
        execute(
            "INSERT INTO queue_state(id, paused, stopped, mode, updated_at) VALUES(1,0,0,'manual',?)",
            (now_iso(),),
        )
        row = query_one("SELECT * FROM queue_state WHERE id = 1")
    state = dict(row)
    state["paused"] = bool(state["paused"])
    state["stopped"] = bool(state["stopped"])
    return state


def set_queue_state(**fields) -> dict:
    fields = {key: value for key, value in fields.items() if key in ("paused", "stopped", "mode", "current_record_id")}
    for key in ("paused", "stopped"):
        if key in fields:
            fields[key] = 1 if fields[key] else 0
    if fields:
        assignments = ", ".join(f"{key} = ?" for key in fields)
        execute(
            f"UPDATE queue_state SET {assignments}, updated_at = ? WHERE id = 1",
            list(fields.values()) + [now_iso()],
        )
    return get_queue_state()


# ---------------------------------------------------------------------------
# Reports
# ---------------------------------------------------------------------------
def add_report(session_id: Optional[int], filename: str, kind: str, scope: str, rows_count: int, size_bytes: int) -> int:
    return execute(
        "INSERT INTO reports(session_id, filename, kind, scope, rows_count, size_bytes, created_at) "
        "VALUES(?,?,?,?,?,?,?)",
        (session_id, filename, kind, scope, rows_count, size_bytes, now_iso()),
    )


def list_reports(session_id: Optional[int] = None, limit: int = 50) -> list[dict]:
    if session_id:
        rows = query_all(
            "SELECT * FROM reports WHERE session_id = ? ORDER BY id DESC LIMIT ?",
            (session_id, limit),
        )
    else:
        rows = query_all("SELECT * FROM reports ORDER BY id DESC LIMIT ?", (limit,))
    reports = []
    for row in rows:
        item = dict(row)
        path = config.REPORT_DIR / item["filename"]
        item["exists"] = path.exists()
        item["size_bytes"] = path.stat().st_size if path.exists() else item["size_bytes"]
        reports.append(item)
    return reports


def delete_report(report_id: int) -> dict:
    row = query_one("SELECT * FROM reports WHERE id = ?", (report_id,))
    if not row:
        return {"ok": False, "message": "Report not found."}
    path = config.REPORT_DIR / row["filename"]
    if path.exists():
        path.unlink()
    execute("DELETE FROM reports WHERE id = ?", (report_id,))
    return {"ok": True, "message": "Report deleted."}


# ---------------------------------------------------------------------------
# Maintenance / privacy
# ---------------------------------------------------------------------------
def purge_expired_sessions(cleanup_days: int) -> dict:
    """Delete sessions (and their files) older than ``cleanup_days`` days."""
    if cleanup_days <= 0:
        return {"ok": True, "deleted": 0, "message": "Automatic cleanup is off."}
    from datetime import timedelta

    cutoff = (datetime.now() - timedelta(days=int(cleanup_days))).strftime("%Y-%m-%d %H:%M:%S")
    rows = query_all("SELECT id FROM sessions WHERE updated_at < ?", (cutoff,))
    deleted = 0
    for row in rows:
        delete_session(row["id"])
        deleted += 1
    if deleted:
        log_event("cleanup", f"{deleted} old session(s) deleted", level="warn")
    return {"ok": True, "deleted": deleted}


def cleanup_old_events(keep: int = 500) -> int:
    return execute(
        "DELETE FROM events WHERE id NOT IN (SELECT id FROM events ORDER BY id DESC LIMIT ?)",
        (keep,),
    )


def delete_all_local_data() -> dict:
    """The big red button: wipe the database rows and every generated file."""
    report = {"sessions": 0, "records": 0, "official_records": 0, "files": 0}
    report["sessions"] = int(query_value("SELECT COUNT(*) FROM sessions"))
    report["records"] = int(query_value("SELECT COUNT(*) FROM records"))
    report["official_records"] = count_official_records()

    with _lock:
        conn = get_conn()
        conn.execute("BEGIN")
        try:
            for table in ("records", "sessions", "official_records", "events", "reports"):
                conn.execute(f"DELETE FROM {table}")
            conn.execute("UPDATE queue_state SET paused=0, stopped=0, current_record_id=NULL WHERE id=1")
            conn.execute("COMMIT")
        except Exception:
            conn.execute("ROLLBACK")
            raise

    for folder in (config.UPLOAD_DIR, config.REPORT_DIR):
        for path in folder.glob("*"):
            if path.is_file() and path.name != ".gitkeep":
                try:
                    path.unlink()
                    report["files"] += 1
                except OSError:
                    pass
    log_event("data_wiped", "All local data deleted by the user", level="warn")
    return report


def db_size_bytes() -> int:
    total = 0
    for suffix in ("", "-wal", "-shm"):
        path = config.DB_PATH.with_name(config.DB_PATH.name + suffix)
        if path.exists():
            total += path.stat().st_size
    return total
