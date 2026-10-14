"""
app.py
======
U-WIN Smart Assistant - local Flask dashboard.

Run it with::

    python app.py            # starts http://127.0.0.1:5000
    python app.py --check    # verifies the installation and exits

The server binds to **localhost only** by default, so the dashboard is reachable
from Chrome on this phone and from nowhere else. Every route below is written to
keep the app usable on a small phone screen and never to crash because of one
bad Excel row.
"""

from __future__ import annotations

import json
import os
import re
import secrets
import sys
import traceback
from datetime import datetime
from pathlib import Path
from typing import Any, Optional

from flask import (
    Flask,
    abort,
    flash,
    jsonify,
    redirect,
    render_template,
    request,
    send_file,
    session,
    url_for,
)
from werkzeug.utils import secure_filename

import config
import database as db
import excel_handler
import matching_engine
import progress_manager as pm
import report_generator
import samples
import uwin_integration as uwin
import validation

# ---------------------------------------------------------------------------
# App factory
# ---------------------------------------------------------------------------
config.ensure_dirs()

app = Flask(__name__, template_folder=str(config.TEMPLATE_DIR), static_folder=str(config.STATIC_DIR))
app.secret_key = config.get_secret_key()
app.config.update(
    SESSION_COOKIE_HTTPONLY=True,
    SESSION_COOKIE_SAMESITE="Lax",
    SESSION_COOKIE_SECURE=False,  # local http on the phone itself
    PERMANENT_SESSION_LIFETIME=int(os.environ.get("UWIN_SESSION_HOURS", "12")) * 3600,
    MAX_CONTENT_LENGTH=(config.HARD_MAX_UPLOAD_MB + 1) * 1024 * 1024,
    JSON_SORT_KEYS=False,
    TEMPLATES_AUTO_RELOAD=True,
)

#: Routes that must stay reachable even when the PIN lock is on.
PIN_EXEMPT_ENDPOINTS = {
    "static",
    "lock_page",
    "api_pin_unlock",
    "favicon",
}

#: Identity of *this* running instance - lets start.sh confirm that the server
#: answering on the port is really the one it just launched.
INSTANCE_PID = os.getpid()
INSTANCE_STARTED_AT = datetime.now().strftime("%Y-%m-%d %H:%M:%S")

INTERNAL_ERROR_MESSAGE = (
    "Something went wrong inside the app. Your data is safe. "
    "Open Settings -> Diagnostics or restart the app (bash stop.sh then bash start.sh)."
)


# ---------------------------------------------------------------------------
# Security helpers (CSRF, headers, PIN lock)
# ---------------------------------------------------------------------------
def csrf_token() -> str:
    token = session.get("_csrf_token")
    if not token:
        token = secrets.token_urlsafe(32)
        session["_csrf_token"] = token
    return token


app.jinja_env.globals["csrf_token"] = csrf_token


@app.template_filter("slug")
def slug_filter(value) -> str:
    """``"NEEDS REVIEW"`` -> ``"needs-review"`` (badge CSS classes)."""
    return re.sub(r"^-|-$", "", re.sub(r"[^a-z0-9]+", "-", str(value or "none").lower()))


@app.template_filter("nice_date")
def nice_date_filter(value) -> str:
    """Show a stored ISO date the way the worker expects to read it (DD-MM-YYYY)."""
    if not value:
        return ""
    return validation.date_to_display(str(value), db.get_setting("date_format", "auto"))


@app.template_filter("filesize")
def filesize_filter(value) -> str:
    """Bytes -> short human readable text."""
    try:
        size = float(value or 0)
    except (TypeError, ValueError):
        return "0 B"
    for unit in ("B", "KB", "MB", "GB"):
        if size < 1024 or unit == "GB":
            return f"{size:.0f} {unit}" if unit == "B" else f"{size:.1f} {unit}"
        size /= 1024
    return f"{size:.1f} GB"


@app.context_processor
def inject_globals() -> dict:
    return {
        "app_title": config.APP_TITLE,
        "app_subtitle": config.APP_SUBTITLE,
        "app_version": config.APP_VERSION,
        "csrf_token_value": csrf_token(),
        "active_session_id": session.get("active_session_id"),
        "settings": db.get_all_settings(),
        "theme": db.get_setting("theme", "light"),
        "portal_url": config.PORTAL_URL,
    }


def csrf_ok() -> bool:
    sent = request.headers.get("X-CSRF-Token") or request.form.get("_csrf_token") or ""
    expected = session.get("_csrf_token") or ""
    return bool(sent) and bool(expected) and secrets.compare_digest(sent, expected)


def wants_json() -> bool:
    return request.path.startswith("/api/") or request.accept_mimetypes.best == "application/json"


def api_ok(**payload):
    payload.setdefault("ok", True)
    return jsonify(payload)


def api_error(message: str, status: int = 400, **extra):
    payload = {"ok": False, "message": message}
    payload.update(extra)
    return jsonify(payload), status


@app.before_request
def guard_request():
    """CSRF for writes + PIN lock for everything."""
    if request.method in ("POST", "PUT", "PATCH", "DELETE") and request.path.startswith("/api/"):
        if not csrf_ok():
            return api_error(
                "Security check failed (CSRF). Please reload the dashboard and try again.",
                403,
            )

    settings = db.get_all_settings()
    if settings.get("pin_lock_enabled") and not session.get("unlocked"):
        if request.endpoint in PIN_EXEMPT_ENDPOINTS:
            return None
        if wants_json():
            return api_error("Dashboard is locked. Enter your PIN to continue.", 401, locked=True)
        return redirect(url_for("lock_page"))
    return None


@app.after_request
def harden_headers(response):
    response.headers["X-Content-Type-Options"] = "nosniff"
    # Localhost-only use: nothing may frame the dashboard. When the worker
    # deliberately exposes the app on another interface (UWIN_HOST=0.0.0.0),
    # framing is allowed so the pages can be embedded in a preview pane.
    response.headers["X-Frame-Options"] = "ALLOWALL" if config.ALLOW_EMBED else "SAMEORIGIN"
    response.headers["Referrer-Policy"] = "no-referrer"
    response.headers["Cross-Origin-Opener-Policy"] = "same-origin"
    response.headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()"
    response.headers.setdefault(
        "Content-Security-Policy",
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; "
        "script-src 'self'; connect-src 'self'; form-action 'self'; base-uri 'none'; "
        "frame-ancestors " + ("*" if config.ALLOW_EMBED else "'self'"),
    )
    if request.path.startswith("/api/") or request.path.startswith("/reports/download"):
        response.headers["Cache-Control"] = "no-store"
    return response


# ---------------------------------------------------------------------------
# Error handling - the dashboard always shows a friendly message
# ---------------------------------------------------------------------------
@app.errorhandler(404)
def handle_404(_error):
    if wants_json():
        return api_error("That page or action does not exist.", 404)
    return render_template("error.html", code=404, title="Page not found",
                           message="The page you tried to open does not exist.",
                           hint="Use the buttons on the dashboard to move around."), 404


@app.errorhandler(413)
def handle_413(_error):
    limit = db.get_setting("max_upload_mb", 5)
    message = f"The file is bigger than the {limit} MB limit."
    if wants_json():
        return api_error(message, 413, hint="Increase the limit in Settings or split the Excel file.")
    return render_template("error.html", code=413, title="File too large", message=message,
                           hint="Open Settings -> Upload size limit and increase it, then upload again."), 413


@app.errorhandler(500)
def handle_500(error):
    app.logger.error("Internal error: %s", error)
    detail = traceback.format_exc(limit=6)
    db.log_event("app_error", f"Internal error: {type(error).__name__}", level="error",
                 detail={"trace": detail[-1200:]})
    if wants_json():
        return api_error(INTERNAL_ERROR_MESSAGE, 500)
    return render_template("error.html", code=500, title="Something went wrong",
                           message=INTERNAL_ERROR_MESSAGE,
                           hint="Nothing was lost. Check the Processing History page for details."), 500


@app.errorhandler(excel_handler.ExcelError)
def handle_excel_error(error):
    if wants_json():
        return api_error(error.message, 400, hint=error.hint, code=error.code)
    return render_template("error.html", code=400, title="Excel problem", message=error.message,
                           hint=error.hint), 400


# ---------------------------------------------------------------------------
# Context helpers
# ---------------------------------------------------------------------------
def current_session_id() -> Optional[int]:
    raw = request.args.get("session_id") or request.form.get("session_id") or session.get("active_session_id")
    try:
        return int(raw) if raw else None
    except (TypeError, ValueError):
        return None


def get_active_session() -> Optional[dict]:
    session_id = current_session_id()
    if session_id:
        found = db.get_session(session_id)
        if found:
            return found
    latest = db.get_latest_session()
    if latest:
        session["active_session_id"] = latest["id"]
    return latest


def dashboard_context() -> dict:
    active = get_active_session()
    session_id = active["id"] if active else None
    counts = pm.statistics(session_id)
    queue = db.get_queue_state()
    recovery = session.pop("recovery_report", None)
    return {
        "active_session": active,
        "counts": counts,
        "queue": queue,
        "sessions": db.list_sessions(limit=20),
        "resumable": db.find_resumable_sessions(),
        "register": db.official_register_info(),
        "recovery": recovery,
        "page": "dashboard",
        "recent_events": db.list_events(limit=8),
    }


def page(title: str, template: str, page_key: str, **extra):
    context = {
        "page_title": title,
        "page": page_key,
        "active_session": get_active_session(),
        "register": db.official_register_info(),
        "counts": pm.statistics((get_active_session() or {}).get("id")),
        "queue": db.get_queue_state(),
    }
    context.update(extra)
    return render_template(template, **context)


# ---------------------------------------------------------------------------
# Pages
# ---------------------------------------------------------------------------
@app.route("/")
def dashboard():
    context = dashboard_context()
    recovery = pm.recover_state() if db.get_setting("auto_recovery", True) else {"sessions": context["resumable"]}
    if recovery.get("messages"):
        context["recovery"] = recovery
    context["resumable"] = recovery.get("sessions", context["resumable"])
    context["page_title"] = "Overview"
    return render_template("dashboard.html", **context)


@app.route("/upload")
def upload_page():
    return page("Upload Excel", "upload.html", "upload",
                fields=excel_handler.FIELD_DEFINITIONS)


@app.route("/records")
def records_page():
    status = request.args.get("status") or ""
    search = request.args.get("q") or ""
    page_number = max(1, int(request.args.get("page", 1) or 1))
    per_page = int(db.get_setting("rows_per_page", 20) or 20)
    active = get_active_session()
    session_id = active["id"] if active else None

    total = db.count_records(session_id=session_id, status=status or None, search=search or None)
    records = db.list_records(
        session_id=session_id, status=status or None, search=search or None,
        limit=per_page, offset=(page_number - 1) * per_page,
    )
    return page("Beneficiary Records", "records.html", "records",
                records=records, total=total, page_number=page_number, per_page=per_page,
                pages=max(1, (total + per_page - 1) // per_page), status=status, search=search,
                statuses=[item for item in config.ALL_STATUSES])


@app.route("/review")
def review_page():
    active = get_active_session()
    if not active:
        flash("Upload an Excel file first, then the review queue will appear here.", "info")
        return redirect(url_for("upload_page"))
    record_id = request.args.get("record_id")
    record = db.get_record(int(record_id)) if record_id and str(record_id).isdigit() else db.next_open_record(active["id"])
    if record is None:
        record = db.query_one("SELECT id FROM records WHERE session_id = ? ORDER BY row_number LIMIT 1", (active["id"],))
        record = db.get_record(record["id"]) if record else None

    match_ui = matching_engine.summary_for_ui(db.json_loads((record or {}).get("match_json"), None))
    warnings = db.json_loads((record or {}).get("warnings_json"), []) or []
    extras = db.json_loads((record or {}).get("extras_json"), {}) or {}
    edited = db.json_loads((record or {}).get("edited_json"), {}) or {}
    neighbors = {}
    if record:
        neighbours = {
            "prev": (db.neighbour_record(active["id"], record["id"], "prev") or {}).get("id"),
            "next": (db.neighbour_record(active["id"], record["id"], "next") or {}).get("id"),
        }
    return page("Review Queue", "review.html", "review",
                record=record, match_ui=match_ui, warnings=warnings, extras=extras, edited=edited,
                neighbours=neighbours, fields=excel_handler.FIELD_DEFINITIONS,
                portal=uwin.login_status())


@app.route("/record/<int:record_id>")
def record_detail(record_id: int):
    record = db.get_record(record_id)
    if not record:
        abort(404)
    match_ui = matching_engine.summary_for_ui(db.json_loads(record.get("match_json"), None))
    return page(f"Record #{record_id}", "review.html", "review",
                record=record, match_ui=match_ui,
                warnings=db.json_loads(record.get("warnings_json"), []) or [],
                extras=db.json_loads(record.get("extras_json"), {}) or {},
                edited=db.json_loads(record.get("edited_json"), {}) or {},
                neighbours={
                    "prev": (db.neighbour_record(record["session_id"], record_id, "prev") or {}).get("id"),
                    "next": (db.neighbour_record(record["session_id"], record_id, "next") or {}).get("id"),
                },
                fields=excel_handler.FIELD_DEFINITIONS,
                portal=uwin.login_status())


@app.route("/history")
def history_page():
    active = get_active_session()
    session_id = active["id"] if active else None
    return page("Processing History", "history.html", "history",
                sessions=db.list_sessions(limit=50),
                events=db.list_events(limit=60),
                status_counts=db.status_counts(session_id),
                reports=db.list_reports(session_id, limit=10),
                resumable=db.find_resumable_sessions())


@app.route("/reports")
def reports_page():
    active = get_active_session()
    session_id = active["id"] if active else None
    return page("Reports", "reports.html", "reports",
                reports=db.list_reports(session_id, limit=50),
                all_reports=db.list_reports(None, limit=50),
                status_counts=db.status_counts(session_id))


@app.route("/settings", methods=["GET"])
def settings_page():
    return page("Settings", "settings.html", "settings",
                fields=excel_handler.FIELD_DEFINITIONS,
                date_formats=config.DATE_FORMAT_CHOICES,
                register=db.official_register_info(),
                adapter=uwin.adapter_status(),
                diagnostics=diagnostics())


@app.route("/portal")
def portal_page():
    return page("U-WIN Portal Login", "portal.html", "portal",
                portal=uwin.login_status(),
                next_record=db.next_open_record((get_active_session() or {}).get("id")))


@app.route("/help")
def help_page():
    return page("Help & Troubleshooting", "help.html", "help",
                url=config.dashboard_url(),
                portal_url=config.PORTAL_URL,
                version=config.APP_VERSION)


@app.route("/lock", methods=["GET"])
def lock_page():
    if not db.get_setting("pin_lock_enabled"):
        session["unlocked"] = True
        return redirect(url_for("dashboard"))
    return render_template("lock.html", page_title="Locked", page="lock",
                           counts=pm.statistics(None), active_session=None, register={})


@app.route("/favicon.ico")
def favicon():
    icon = config.STATIC_DIR / "img" / "favicon.svg"
    if icon.exists():
        return send_file(icon, mimetype="image/svg+xml")
    return ("", 204)


# ---------------------------------------------------------------------------
# API - status, statistics, diagnostics
# ---------------------------------------------------------------------------
def diagnostics() -> dict:
    return {
        "version": config.APP_VERSION,
        "base_dir": str(config.BASE_DIR),
        "host": config.HOST,
        "port": config.PORT,
        "dashboard_url": config.dashboard_url(),
        "database": str(config.DB_PATH),
        "database_size": db.db_size_bytes(),
        "uploads_dir": str(config.UPLOAD_DIR),
        "reports_dir": str(config.REPORT_DIR),
        "logs_dir": str(config.LOG_DIR),
        "python": sys.version.split()[0],
        "instance_pid": INSTANCE_PID,
        "instance_started_at": INSTANCE_STARTED_AT,
        "pandas": _module_version("pandas"),
        "openpyxl": _module_version("openpyxl"),
        "xlrd": _module_version("xlrd"),
        "flask": _module_version("flask"),
        "register": db.official_register_info(),
        "counts": db.status_counts(),
        "csrf_ok": True,
    }


def _module_version(name: str) -> str:
    """Version of an optional dependency, without importing it twice."""
    try:
        import importlib.metadata as metadata

        return metadata.version(name)
    except Exception:
        pass
    try:
        __import__(name)
        return "installed"
    except Exception:
        return "not installed"


@app.get("/api/status")
def api_status():
    active = get_active_session()
    session_id = active["id"] if active else None
    counts = pm.statistics(session_id)
    queue = db.get_queue_state()
    credentials_state = uwin.credentials.status()
    return api_ok(
        counts=counts,
        queue=queue,
        session=active,
        sessions=db.list_sessions(limit=10),
        register=db.official_register_info(),
        credentials={
            "state": credentials_state["state"],
            "message": credentials_state["message"],
            "mobile_masked": credentials_state["mobile_masked"],
            "active": credentials_state["active"],
        },
        server_time=db.now_iso(),
        url=config.dashboard_url(),
        instance={
            "pid": INSTANCE_PID,
            "started_at": INSTANCE_STARTED_AT,
            "host": config.HOST,
            "port": config.PORT,
            "version": config.APP_VERSION,
        },
    )


@app.get("/api/statistics")
def api_statistics():
    session_id = current_session_id()
    counts = pm.statistics(session_id)
    return api_ok(counts=counts, session_id=session_id, queue=db.get_queue_state())


@app.get("/api/diagnostics")
def api_diagnostics():
    return api_ok(diagnostics=diagnostics(), events=db.list_events(limit=25),
                  security=uwin.describe_security())


# ---------------------------------------------------------------------------
# API - upload / preview / mapping / import
# ---------------------------------------------------------------------------
def _save_upload(file_storage) -> Path:
    """Validate and store an uploaded file with a safe, unique name."""
    filename = file_storage.filename or ""
    if not filename:
        raise excel_handler.ExcelError("No file was received.", "no_file",
                                       "Choose the Excel file again from the Files app.")

    extension = Path(filename).suffix.lower()
    if extension not in config.ALLOWED_EXTENSIONS | config.ALLOWED_REGISTER_EXTENSIONS:
        raise excel_handler.ExcelError(
            f"'{extension or 'unknown'}' files are not supported.",
            "bad_extension",
            "Only .xlsx and .xls Excel files can be uploaded (the register also accepts .csv).",
        )

    max_bytes = int(db.get_setting("max_upload_mb", 5)) * 1024 * 1024
    file_storage.stream.seek(0, os.SEEK_END)
    size = file_storage.stream.tell()
    file_storage.stream.seek(0)
    if size == 0:
        raise excel_handler.ExcelError("The file is empty (0 bytes).", "empty_file",
                                       "Export the Excel file again and re-upload.")
    if size > max_bytes:
        raise excel_handler.ExcelError(
            f"The file is {size / (1024 * 1024):.1f} MB, which is above your {max_bytes // (1024 * 1024)} MB limit.",
            "too_large",
            "Increase the limit in Settings -> Upload size limit, or split the file.",
        )

    safe = secure_filename(filename) or "upload.xlsx"
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    stored_name = f"{stamp}-{secrets.token_hex(3)}-{safe}"
    target = config.UPLOAD_DIR / stored_name
    file_storage.save(target)

    # Real content check (a .jpg renamed to .xlsx must be rejected).
    try:
        excel_handler.sniff_format(target)
    except excel_handler.ExcelError:
        target.unlink(missing_ok=True)
        raise
    return target


def _file_hash(path: Path) -> str:
    import hashlib

    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for chunk in iter(lambda: handle.read(65536), b""):
            digest.update(chunk)
    return digest.hexdigest()


@app.post("/api/session/upload")
def api_upload():
    file_storage = request.files.get("file")
    if file_storage is None:
        return api_error("No file received. Please choose an Excel file.", 400)

    target = _save_upload(file_storage)
    settings = db.get_all_settings()
    preview = excel_handler.preview_file(target, settings, limit=20)
    digest = _file_hash(target)

    session_id = db.create_session(
        original_filename=secure_filename(file_storage.filename) or "upload.xlsx",
        stored_filename=target.name,
        file_hash=digest,
        file_size=target.stat().st_size,
        sheet_name=preview["sheet_name"],
        header_row=preview["header_row"],
        backend=preview["backend"],
        total_rows=preview["data_rows"],
        empty_rows=preview["empty_rows"],
        duplicate_rows=preview["duplicate_rows"],
        missing_cells=preview["missing_cells"],
        mappings=preview["mapping"],
        columns=preview["columns"],
        warnings=preview["warnings"],
    )
    session["active_session_id"] = session_id
    db.log_event("file_uploaded", f"File uploaded: {target.name} ({target.stat().st_size} bytes)",
                 session_id=session_id)

    return api_ok(
        session_id=session_id,
        preview=preview,
        message=f"File uploaded. {preview['data_rows']} data row(s) found. Check the column mapping next.",
        size_bytes=target.stat().st_size,
        filename=file_storage.filename,
    )


@app.post("/api/session/<int:session_id>/preview")
def api_session_preview(session_id: int):
    found = db.get_session(session_id)
    if not found:
        return api_error("Session not found.", 404)
    payload = request.get_json(silent=True) or {}
    path = config.UPLOAD_DIR / found["stored_filename"]
    if not path.exists():
        return api_error("The uploaded file is no longer on this device. Please upload it again.", 410)

    preview = excel_handler.preview_file(
        path, db.get_all_settings(), limit=int(payload.get("limit", 20)),
        sheet_name=payload.get("sheet_name") or None,
    )
    db.update_session(session_id, sheet_name=preview["sheet_name"], header_row=preview["header_row"])
    return api_ok(preview=preview, session_id=session_id)


@app.post("/api/session/<int:session_id>/import")
def api_session_import(session_id: int):
    """Save the mapping and import the rows into the local database."""
    found = db.get_session(session_id)
    if not found:
        return api_error("Session not found.", 404)

    payload = request.get_json(silent=True) or {}
    mapping = payload.get("mapping") or db.json_loads(found.get("mappings_json"), {})
    sheet_name = payload.get("sheet_name") or found.get("sheet_name")
    header_row = int(payload.get("header_row") or found.get("header_row") or 1)
    replace = bool(payload.get("replace"))

    path = config.UPLOAD_DIR / found["stored_filename"]
    if not path.exists():
        return api_error("The uploaded file is missing. Please upload it again.", 410)

    existing = db.count_records(session_id=session_id)
    if existing and not replace:
        return api_error(
            f"This session already has {existing} imported record(s).",
            409,
            need_replace=True,
            hint="Press 'Import again' only if you want to re-read the file. Review decisions for these rows will be lost.",
        )

    result = excel_handler.extract_records(
        path, mapping, db.get_all_settings(), sheet_name=sheet_name, header_row=header_row,
    )

    if replace and existing:
        db.execute("DELETE FROM records WHERE session_id = ?", (session_id,))

    db.bulk_insert_records(session_id, result["records"])
    db.update_session(
        session_id,
        mappings=result["mapping"],
        columns=result["columns"],
        sheet_name=result["sheet_name"],
        header_row=result["header_row"],
        backend=result["backend"],
        total_rows=result["stats"]["imported"],
        empty_rows=result["stats"]["empty_rows"],
        duplicate_rows=result["stats"]["duplicate_rows"],
        missing_cells=result["stats"]["missing_cells"],
        warnings=result["warnings"],
        status=config.SESSION_ACTIVE,
    )

    # Auto-run the matching engine so the dashboard has something useful to show.
    processed = pm.process_batch(session_id, limit=1000)
    db.log_event(
        "records_imported",
        f"{result['stats']['imported']} record(s) imported and checked",
        session_id=session_id,
        detail=result["stats"],
    )
    counts = pm.statistics(session_id)
    return api_ok(
        session_id=session_id,
        stats=result["stats"],
        mapping_problems=result["warnings"],
        processed=processed.get("processed", 0),
        counts=counts,
        rows=db.list_records(session_id=session_id, limit=5),
        message=(
            f"{result['stats']['imported']} record(s) imported. "
            f"{counts['NEEDS REVIEW']} need review, {counts['MATCHED']} matched automatically."
        ),
    )


@app.get("/api/session/<int:session_id>")
def api_session_get(session_id: int):
    found = db.get_session(session_id)
    if not found:
        return api_error("Session not found.", 404)
    session["active_session_id"] = session_id
    return api_ok(
        session=found,
        counts=pm.statistics(session_id),
        queue=db.get_queue_state(),
        records=db.list_records(session_id=session_id, limit=10),
    )


@app.post("/api/session/<int:session_id>/activate")
def api_session_activate(session_id: int):
    if not db.get_session(session_id):
        return api_error("Session not found.", 404)
    session["active_session_id"] = session_id
    db.update_session(session_id, last_opened_at=db.now_iso())
    return api_ok(message="Session opened.", session_id=session_id, counts=pm.statistics(session_id))


@app.post("/api/session/<int:session_id>/delete")
def api_session_delete(session_id: int):
    found = db.get_session(session_id)
    if not found:
        return api_error("Session not found.", 404)
    result = db.delete_session(session_id)
    if session.get("active_session_id") == session_id:
        session.pop("active_session_id", None)
    return api_ok(**result)


@app.post("/api/session/<int:session_id>/reprocess")
def api_session_reprocess(session_id: int):
    """Re-validate and re-match every open record of a session."""
    if not db.get_session(session_id):
        return api_error("Session not found.", 404)
    rows = db.query_all(
        "SELECT id FROM records WHERE session_id = ? AND status NOT IN (?, ?) ORDER BY row_number",
        (session_id, config.STATUS_COMPLETED, config.STATUS_SAVE_UNKNOWN),
    )
    done = 0
    for row in rows:
        if pm.process_record(row["id"]).get("ok"):
            done += 1
    return api_ok(message=f"{done} record(s) re-checked.", counts=pm.statistics(session_id))


# ---------------------------------------------------------------------------
# API - queue control
# ---------------------------------------------------------------------------
@app.post("/api/queue/<action>")
def api_queue_action(action: str):
    session_id = current_session_id()
    if action == "pause":
        pm.pause_queue()
        return api_ok(message="Queue paused. Nothing will move until you press Resume.", queue=db.get_queue_state())
    if action == "resume":
        pm.resume_queue()
        return api_ok(message="Queue resumed.", queue=db.get_queue_state())
    if action == "stop":
        pm.stop_queue()
        return api_ok(message="Processing stopped. Records already completed stay as they are.",
                      queue=db.get_queue_state())
    if action == "mode":
        payload = request.get_json(silent=True) or {}
        state = pm.set_mode(payload.get("mode", "manual"))
        return api_ok(message=f"Mode set to {state['mode']}.", queue=state)
    if action == "next":
        if not session_id:
            return api_error("Open a session first.", 400)
        result = pm.process_next(session_id)
        return jsonify(result), (200 if result.get("ok") else 400)
    if action == "batch":
        if not session_id:
            return api_error("Open a session first.", 400)
        payload = request.get_json(silent=True) or {}
        result = pm.process_batch(session_id, limit=int(payload.get("limit", 200)))
        return jsonify(result), (200 if result.get("ok") else 400)
    if action == "retry_failed":
        result = pm.retry_failed(session_id)
        return jsonify(result)
    return api_error("Unknown queue action.", 400)


# ---------------------------------------------------------------------------
# API - record decisions
# ---------------------------------------------------------------------------
def _record_or_404(record_id: int) -> dict:
    record = db.get_record(record_id)
    if not record:
        abort(404)
    return record


@app.post("/api/records/<int:record_id>/process")
def api_record_process(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.process_record(record_id, force=bool(payload.get("force")))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/approve")
def api_record_approve(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.approve_record(record_id, note=payload.get("note", ""))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/edit")
def api_record_edit(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    values = payload.get("values") or {}
    result = pm.save_edited_record(record_id, values, note=payload.get("note", ""))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/reject")
def api_record_reject(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.reject_record(record_id, reason=payload.get("reason", ""))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/skip")
def api_record_skip(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.skip_record(record_id, reason=payload.get("reason", ""))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/reopen")
def api_record_reopen(record_id: int):
    _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.reopen_record(record_id, reason=payload.get("reason", ""))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.get("/api/records/<int:record_id>")
def api_record_get(record_id: int):
    record = _record_or_404(record_id)
    return api_ok(
        record=record,
        warnings=db.json_loads(record.get("warnings_json"), []) or [],
        match=db.json_loads(record.get("match_json"), None),
        extras=db.json_loads(record.get("extras_json"), {}) or {},
        edited=db.json_loads(record.get("edited_json"), {}) or {},
    )


@app.post("/api/records/<int:record_id>/save")
def api_record_save(record_id: int):
    """The single place where a portal save may be triggered.

    Order of operations (this order is the safety feature):

    1. The worker must have approved the record.
    2. If an approved adapter is configured, a "saving" marker is written to
       SQLite **before** the network call, so a crash cannot cause a duplicate.
    3. Anything that is not an explicit confirmation becomes
       ``SAVE STATUS UNKNOWN`` and is never retried automatically.
    """
    record = _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    confirmed_by_worker = bool(payload.get("confirmed"))

    if record["status"] == config.STATUS_COMPLETED:
        return api_error("This record is already completed. Nothing was sent again.", 409)
    if record["status"] == config.STATUS_SAVE_UNKNOWN:
        return api_error(
            "A previous save for this record is unconfirmed. Open the portal, search the beneficiary yourself "
            "and only then decide. The app will not send it again automatically.",
            409,
            save_unknown=True,
        )

    if record["status"] != config.STATUS_APPROVED:
        approve = pm.approve_record(record_id, note="Approved while starting the portal save.")
        if not approve.get("ok"):
            return api_error(approve.get("message", "The record could not be approved."), 400,
                             warnings=approve.get("warnings", []))
        record = db.get_record(record_id)

    result = uwin.save_record(
        record,
        confirmed_by_worker=confirmed_by_worker,
        pre_save_hook=lambda: pm.mark_saving(record_id),
    )
    status = result.get("status")

    if status == "confirmed":
        done = pm.mark_completed(record_id, reference=result.get("reference", ""),
                                 note="Confirmed by the approved integration.")
        return api_ok(message=done.get("message", "Record completed."),
                      reference=result.get("reference", ""), counts=pm.statistics(record["session_id"]))

    if status == "assisted":
        return api_ok(
            message="Assisted workflow ready. Follow the steps below, then save the confirmation.",
            next_steps=result.get("next_steps", []),
            assisted=True,
            portal_url=config.PORTAL_URL,
            counts=pm.statistics(record["session_id"]),
        )

    if status in ("not_logged_in", "session_expired"):
        pm.pause_queue("portal session not confirmed")
        return api_error(result.get("message", "Please log in again."), 409,
                         needs_login=True, portal_url=config.PORTAL_URL)

    if status == "blocked":
        return api_error(result.get("message", "Save blocked for safety."), 409)

    pm.mark_save_unknown(record_id, result.get("message", "Save not confirmed."))
    return api_ok(
        warning=True,
        message=result.get("message", "Save could not be confirmed."),
        save_unknown=True,
        counts=pm.statistics(record["session_id"]),
    )


@app.post("/api/records/<int:record_id>/confirm")
def api_record_confirm(record_id: int):
    """Worker reports what the official portal showed (reference number)."""
    record = _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    reference = (payload.get("reference") or "").strip()
    note = (payload.get("note") or "").strip()
    if not reference:
        return api_error("Please type the confirmation shown by the portal (acknowledgement / reference).",
                         400, need_reference=True)
    result = pm.mark_completed(record_id, reference=reference, note=note)
    result["counts"] = pm.statistics(record["session_id"])
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/records/<int:record_id>/unknown")
def api_record_unknown(record_id: int):
    """Portal did not confirm -> keep it as unknown, never retry automatically."""
    record = _record_or_404(record_id)
    payload = request.get_json(silent=True) or {}
    result = pm.mark_save_unknown(record_id, payload.get("reason", "Worker could not confirm the save."))
    result["counts"] = pm.statistics(record["session_id"])
    return jsonify(result)


# ---------------------------------------------------------------------------
# API - authorised beneficiary register (for matching)
# ---------------------------------------------------------------------------
@app.post("/api/register/import")
def api_register_import():
    file_storage = request.files.get("file")
    if file_storage is None:
        return api_error("No file received for the authorised register.", 400)

    target = _save_upload(file_storage)
    result = excel_handler.extract_register_rows(target)
    if not result["rows"]:
        target.unlink(missing_ok=True)
        return api_error("No usable rows were found in the register file.", 400,
                         hint="Check that the file has headings like Beneficiary Name / Beneficiary ID.")

    count = db.replace_official_records(result["rows"], target.name)
    pm.invalidate_index()
    pm.get_register_index(force=True)
    return api_ok(
        message=f"Authorised register imported: {count} record(s). Matching is now active on this device.",
        count=count,
        skipped=result["skipped"],
        columns=result["columns"],
        warnings=result["warnings"][:20],
        register=db.official_register_info(),
    )


@app.post("/api/register/clear")
def api_register_clear():
    db.clear_official_records()
    pm.invalidate_index()
    return api_ok(message="Authorised register cleared from this device.")


@app.get("/api/register/sample")
def api_register_sample():
    path = samples.ensure_sample_files()["register"]
    return send_file(path, as_attachment=True, download_name=path.name)


# ---------------------------------------------------------------------------
# API - reports
# ---------------------------------------------------------------------------
@app.post("/api/reports/session")
def api_report_session():
    payload = request.get_json(silent=True) or {}
    session_id = int(payload.get("session_id") or current_session_id() or 0)
    if not session_id:
        return api_error("Open a session first.", 400)
    result = report_generator.build_session_report(session_id, scope=payload.get("scope", "all"))
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/reports/overview")
def api_report_overview():
    result = report_generator.build_overview_report()
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/reports/export")
def api_report_export():
    payload = request.get_json(silent=True) or {}
    session_id = int(payload.get("session_id") or current_session_id() or 0)
    if not session_id:
        return api_error("Open a session first.", 400)
    result = report_generator.export_records_csv(
        session_id, status=payload.get("status") or None, search=payload.get("search") or "",
    )
    return jsonify(result), (200 if result.get("ok") else 400)


@app.get("/api/reports")
def api_reports_list():
    return api_ok(reports=db.list_reports(current_session_id(), limit=100))


@app.post("/api/reports/<int:report_id>/delete")
def api_report_delete(report_id: int):
    return jsonify(report_generator.delete_report(report_id))


@app.get("/reports/download/<path:filename>")
def report_download(filename: str):
    path = report_generator.safe_report_path(filename)
    if not path:
        abort(404)
    return send_file(path, as_attachment=True, download_name=path.name)


# ---------------------------------------------------------------------------
# API - sample files
# ---------------------------------------------------------------------------
@app.get("/api/sample-file")
def api_sample_file():
    kind = request.args.get("kind", "beneficiaries")
    files = samples.ensure_sample_files()
    path = files.get("register" if kind == "register" else "beneficiaries")
    if not path or not Path(path).exists():
        return api_error("Sample file could not be created.", 500)
    return send_file(path, as_attachment=True, download_name=Path(path).name)


# ---------------------------------------------------------------------------
# API - settings, privacy, PIN
# ---------------------------------------------------------------------------
@app.post("/api/settings")
def api_settings_save():
    payload = request.get_json(silent=True) or {}
    updates = payload.get("settings") or payload
    if not isinstance(updates, dict):
        return api_error("Settings payload is not valid.", 400)

    changed = {}
    for key, value in updates.items():
        if key not in config.DEFAULT_SETTINGS:
            continue
        if key in ("pin_hash", "pin_salt"):
            continue  # only via the PIN routes
        db.set_setting(key, value)
        changed[key] = db.get_setting(key)

    db.log_event("settings_changed", f"{len(changed)} setting(s) updated", detail=changed)
    return api_ok(message="Settings saved.", settings=db.get_all_settings(), changed=changed)


@app.post("/api/settings/theme")
def api_settings_theme():
    payload = request.get_json(silent=True) or {}
    theme = payload.get("theme", "light")
    db.set_setting("theme", theme)
    return api_ok(message=f"Theme set to {db.get_setting('theme')}.", theme=db.get_setting("theme"))


@app.post("/api/pin/set")
def api_pin_set():
    payload = request.get_json(silent=True) or {}
    pin = str(payload.get("pin") or "")
    current = str(payload.get("current_pin") or "")
    if not (4 <= len(pin) <= 8) or not pin.isdigit():
        return api_error("PIN must be 4 to 8 digits.", 400)

    if db.get_setting("pin_lock_enabled"):
        stored = str(db.get_setting("pin_hash") or "")
        from werkzeug.security import check_password_hash

        if stored and not check_password_hash(stored, current):
            return api_error("Current PIN is wrong.", 403)

    from werkzeug.security import generate_password_hash

    db.set_setting("pin_hash", generate_password_hash(pin))
    db.set_setting("pin_lock_enabled", True)
    session["unlocked"] = True
    db.log_event("pin_enabled", "Dashboard PIN lock enabled")
    return api_ok(message="PIN lock enabled. Keep this PIN safe - it cannot be recovered.")


@app.post("/api/pin/remove")
def api_pin_remove():
    from werkzeug.security import check_password_hash

    payload = request.get_json(silent=True) or {}
    pin = str(payload.get("pin") or "")
    stored = str(db.get_setting("pin_hash") or "")
    if stored and not check_password_hash(stored, pin):
        return api_error("PIN is wrong.", 403)
    db.set_setting("pin_lock_enabled", False)
    db.set_setting("pin_hash", "")
    db.log_event("pin_disabled", "Dashboard PIN lock disabled", level="warn")
    return api_ok(message="PIN lock removed.")


@app.post("/api/pin/unlock")
def api_pin_unlock():
    from werkzeug.security import check_password_hash

    payload = request.get_json(silent=True) or {}
    pin = str(payload.get("pin") or "")
    stored = str(db.get_setting("pin_hash") or "")
    if not stored:
        session["unlocked"] = True
        return api_ok(message="No PIN is set.", redirect=url_for("dashboard"))
    if check_password_hash(stored, pin):
        session["unlocked"] = True
        db.log_event("pin_unlocked", "Dashboard unlocked with PIN")
        return api_ok(message="Unlocked.", redirect=url_for("dashboard"))
    db.log_event("pin_failed", "Wrong PIN entered", level="warn")
    return api_error("Wrong PIN. Try again.", 403)


@app.post("/api/settings/cleanup")
def api_settings_cleanup():
    """Delete finished sessions and temporary files now."""
    settings = db.get_all_settings()
    removed_files = 0
    removed_reports = 0

    for folder in (config.UPLOAD_DIR, config.REPORT_DIR):
        for path in folder.glob("*"):
            if path.is_file() and path.name != ".gitkeep":
                age_days = (datetime.now().timestamp() - path.stat().st_mtime) / 86400
                referenced = any(
                    path.name == (row["filename"] or "") for row in db.list_reports(None, limit=500)
                )
                if folder is config.REPORT_DIR and referenced:
                    continue
                if age_days > 7:
                    try:
                        path.unlink()
                        removed_files += 1
                    except OSError:
                        continue

    if settings.get("delete_session_on_complete"):
        for row in db.query_all("SELECT id FROM sessions WHERE status = ?", (config.SESSION_COMPLETED,)):
            db.delete_session(row["id"])
            removed_reports += 1

    purge = db.purge_expired_sessions(int(settings.get("cleanup_days") or 0))
    db.cleanup_old_events()
    return api_ok(
        message=f"Cleanup finished. {removed_files} old file(s) and {purge.get('deleted', 0)} old session(s) removed.",
        removed_files=removed_files,
        purged_sessions=purge.get("deleted", 0),
    )


@app.post("/api/data/wipe")
def api_data_wipe():
    payload = request.get_json(silent=True) or {}
    if payload.get("confirm") != "DELETE":
        return api_error("Type DELETE to confirm.", 400, need_confirm=True)
    report = db.delete_all_local_data()
    pm.invalidate_index()
    session.pop("active_session_id", None)
    uwin.credentials.flush()
    return api_ok(
        message=(
            f"All local data deleted: {report['sessions']} session(s), {report['records']} record(s), "
            f"{report['files']} file(s). The dashboard is now empty."
        ),
        report=report,
    )


@app.post("/api/session/purge_uploads")
def api_purge_uploads():
    """Delete upload files whose sessions no longer exist (privacy housekeeping)."""
    known = {row["stored_filename"] for row in db.query_all("SELECT stored_filename FROM sessions")}
    removed = 0
    for path in config.UPLOAD_DIR.glob("*"):
        if path.is_file() and path.name not in known and path.name != ".gitkeep":
            try:
                path.unlink()
                removed += 1
            except OSError:
                continue
    return api_ok(message=f"{removed} leftover file(s) removed.", removed=removed)


# ---------------------------------------------------------------------------
# API - U-WIN portal (semi-automated login + assisted workflow)
# ---------------------------------------------------------------------------
@app.get("/api/portal/status")
def api_portal_status():
    return api_ok(portal=uwin.login_status(), adapter=uwin.adapter_status())


@app.post("/api/portal/open")
def api_portal_open():
    payload = request.get_json(silent=True) or {}
    result = uwin.open_in_browser(payload.get("url") or config.PORTAL_URL)
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/portal/credentials")
def api_portal_credentials():
    """Hold mobile + password in RAM for one login attempt (never stored)."""
    payload = request.get_json(silent=True) or {}
    result = uwin.credentials.begin(str(payload.get("mobile") or ""), str(payload.get("password") or ""))
    # Drop references from this request as soon as possible.
    payload.pop("password", None)
    if not result.get("ok"):
        return api_error(result.get("message", "Credentials rejected."), 400)
    return api_ok(
        message=result["message"],
        status=result["status"],
        steps=uwin.guided_login_steps(),
    )


@app.post("/api/portal/otp-stage")
def api_portal_otp_stage():
    result = uwin.credentials.note_otp_stage()
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/portal/otp")
def api_portal_otp():
    """OTP handling.

    Without an approved adapter the OTP is **not accepted** by this app at all -
    the worker types it directly in the official portal page. With an approved
    adapter the value is forwarded once and immediately dropped.
    """
    payload = request.get_json(silent=True) or {}
    otp_value = str(payload.get("otp") or "")
    payload.pop("otp", None)
    result = uwin.AdapterClient().submit_otp(otp_value) if uwin.AdapterClient().enabled else {
        "ok": False,
        "type": "manual_only",
        "message": "OTP Required - Enter the SMS OTP in the official portal page. "
                   "This app never reads, stores or forwards your OTP.",
    }
    return jsonify(result), (200 if result.get("ok") else 400)


@app.post("/api/portal/confirm-login")
def api_portal_confirm_login():
    payload = request.get_json(silent=True) or {}
    verified = False
    adapter = uwin.AdapterClient()
    if adapter.enabled:
        state = adapter.session_state()
        verified = bool(state.get("logged_in"))
        if not verified:
            return api_error(
                "The approved integration says the session is NOT active yet. Finish the login in the portal first.",
                409,
            )
    result = uwin.credentials.mark_logged_in(
        note="Session confirmed by the approved integration." if verified else
             "You confirmed the portal dashboard is open."
    )
    return api_ok(
        message="Login step done. You can continue with the Excel beneficiary workflow.",
        status=result["status"],
        next_url=url_for("records_page"),
        verified=verified,
    )


@app.post("/api/portal/logout")
def api_portal_logout():
    return api_ok(**uwin.logout())


@app.post("/api/portal/test-adapter")
def api_portal_test_adapter():
    payload = request.get_json(silent=True) or {}
    url = str(payload.get("url") or "")
    token = str(payload.get("token") or "")
    if url:
        ok, message = uwin.validate_url(url)
        if not ok:
            return api_error(message, 400)
        db.set_setting("portal_adapter_url", url)
    if token:
        config.save_portal_adapter_token(token)
    db.set_setting("portal_adapter_enabled", True)
    client = uwin.AdapterClient()
    health = client.health()
    if not health.get("ok"):
        db.set_setting("portal_adapter_enabled", False)
        return api_error(health.get("message", "Adapter could not be reached."), 400, adapter=health)
    return api_ok(message=health.get("message", "Adapter reachable."), adapter=uwin.adapter_status(), health=health)


@app.post("/api/portal/disable-adapter")
def api_portal_disable_adapter():
    db.set_setting("portal_adapter_enabled", False)
    return api_ok(message="Approved integration disabled. The assisted workflow will be used.")


@app.get("/api/portal/next-steps/<int:record_id>")
def api_portal_next_steps(record_id: int):
    record = _record_or_404(record_id)
    return api_ok(steps=uwin.assisted_next_steps(record), portal_url=config.PORTAL_URL,
                  record={"id": record_id, "name": record.get("name"), "row": record.get("row_number")})


# ---------------------------------------------------------------------------
# CLI helpers
# ---------------------------------------------------------------------------
def run_self_check() -> int:
    """``python app.py --check`` - quick installation verification."""
    print(f"\n{config.APP_TITLE} v{config.APP_VERSION} - installation check\n" + "-" * 52)
    problems = 0

    print(f"Project folder : {config.BASE_DIR}")
    for folder in config.ALL_DIRS:
        state = "ok" if folder.exists() else "MISSING"
        if not folder.exists():
            problems += 1
        print(f"  {folder.name:<14}: {state}")

    for name in ("flask", "pandas", "openpyxl", "xlrd", "werkzeug"):
        version = _module_version(name)
        if version == "not installed" and name in ("flask", "openpyxl"):
            problems += 1
        print(f"  {name:<14}: {version}")

    try:
        db.get_conn()
        print(f"  database      : ok ({config.DB_PATH.name}, {db.db_size_bytes()} bytes)")
    except Exception as exc:
        problems += 1
        print(f"  database      : PROBLEM - {exc}")

    try:
        files = samples.ensure_sample_files()
        print(f"  sample files  : ok ({', '.join(path.name for path in files.values())})")
    except Exception as exc:
        problems += 1
        print(f"  sample files  : PROBLEM - {exc}")

    print("-" * 52)
    if problems:
        print(f"{problems} problem(s) found. Read docs/TROUBLESHOOTING.md\n")
        return 1
    print("Everything looks good. Start the dashboard with: bash start.sh\n")
    return 0


def run_maintenance() -> int:
    """``python app.py --cleanup`` - privacy housekeeping from the terminal."""
    report = db.delete_all_local_data() if os.environ.get("UWIN_WIPE") == "1" else None
    purge = db.purge_expired_sessions(int(db.get_setting("cleanup_days") or 0))
    db.cleanup_old_events()
    if report:
        print(f"All local data deleted: {report}")
    print(f"Cleanup done. Old sessions removed: {purge.get('deleted', 0)}")
    return 0


def main(argv: Optional[list[str]] = None) -> int:
    argv = argv if argv is not None else sys.argv[1:]
    if "--check" in argv:
        return run_self_check()
    if "--cleanup" in argv:
        return run_maintenance()

    config.ensure_dirs()
    recovery = pm.recover_state()

    banner = f"""
====================================================================
  {config.APP_TITLE}  v{config.APP_VERSION}
  {config.APP_SUBTITLE}
====================================================================
  Dashboard  : {config.dashboard_url()}
  Open it in Chrome on this phone (or run: termux-open-url {config.dashboard_url()})
  Stop       : press Ctrl+C here, or run  bash stop.sh
  Data files : uploads/  reports/  data/uwin_assistant.db  logs/
====================================================================
"""
    print(banner)
    for message in recovery.get("messages", []):
        print(f"  ! {message}")
    if recovery.get("sessions"):
        print(f"  Resume available for {len(recovery['sessions'])} session(s).")
    if config.HOST not in ("127.0.0.1", "localhost"):
        print(f"  ! Binding to {config.HOST}:{config.PORT} - other devices on your network can reach it.")

    app.run(host=config.HOST, port=config.PORT, debug=config.DEBUG, threaded=True, use_reloader=False)
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except KeyboardInterrupt:
        uwin.credentials.flush()
        db.log_event("app_stop", "Dashboard stopped by the user")
        print("\nDashboard stopped. Login data in memory was wiped. Your progress is saved.")
        sys.exit(0)
