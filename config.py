"""
config.py
=========
Central configuration for **U-WIN Smart Assistant**.

Everything here is intentionally simple:

* All paths are *relative to this file*, so the project works no matter where
  you unpack it on your Android phone.
* Nothing secret is written into the source code. The Flask secret key is
  generated once and stored inside ``data/.secret_key`` (chmod 600).
* User changeable settings (theme, upload limit ...) are stored in the local
  SQLite database and read through :mod:`database`.

The module never imports Flask or pandas, so it can be imported from
``setup.sh`` helpers and from the smoke tests without a full environment.
"""

from __future__ import annotations

import os
import secrets
import stat
from pathlib import Path

# ---------------------------------------------------------------------------
# Identity of the application
# ---------------------------------------------------------------------------
APP_NAME = "U-WIN Smart Assistant"
APP_TITLE = "U-WIN SMART ASSISTANT"
APP_SUBTITLE = "Vaccination Data Management & Review System"
APP_VERSION = "1.0.0"
APP_BUILD_DATE = "2026-10-14"

#: Official, publicly documented U-WIN vaccinator login page.
#: The dashboard only ever *opens* this URL in your browser. It never talks to
#: private/undocumented endpoints.
PORTAL_URL = "https://uwinvaccinator.mohfw.gov.in/auth/login"
PORTAL_INFO_URL = "https://uwin.mohfw.gov.in/"

# ---------------------------------------------------------------------------
# Filesystem layout
# ---------------------------------------------------------------------------
#: Where the program files live (templates, static, Python modules).
BASE_DIR = Path(__file__).resolve().parent

#: Where the *data* lives: SQLite database, uploads, reports, logs, secrets.
#: Advanced users (and the test suite) can move it with the ``UWIN_DATA_ROOT``
#: environment variable, for example to the SD card. Normal Termux use never
#: needs this: everything stays inside the project folder.
DATA_ROOT = Path(os.environ.get("UWIN_DATA_ROOT") or BASE_DIR).resolve()

UPLOAD_DIR = DATA_ROOT / "uploads"
REPORT_DIR = DATA_ROOT / "reports"
LOG_DIR = DATA_ROOT / "logs"
DATA_DIR = DATA_ROOT / "data"
SECRETS_DIR = DATA_ROOT / "secrets"
TEMPLATE_DIR = BASE_DIR / "templates"
STATIC_DIR = BASE_DIR / "static"
SAMPLE_DATA_DIR = BASE_DIR / "sample_data"

DB_PATH = DATA_DIR / "uwin_assistant.db"
SECRET_KEY_FILE = DATA_DIR / ".secret_key"
LOG_FILE = LOG_DIR / "uwin_assistant.log"
PORTAL_TOKEN_FILE = SECRETS_DIR / "portal_adapter_token.txt"

ALL_DIRS = (
    UPLOAD_DIR,
    REPORT_DIR,
    LOG_DIR,
    DATA_DIR,
    SECRETS_DIR,
    SAMPLE_DATA_DIR,
)


def ensure_dirs() -> None:
    """Create every runtime directory (safe to call many times)."""
    for directory in ALL_DIRS:
        directory.mkdir(parents=True, exist_ok=True)


# ---------------------------------------------------------------------------
# Web server
# ---------------------------------------------------------------------------
# Default: localhost only, so nothing is exposed on your Wi-Fi / mobile data.
# You can override with environment variables when you *want* to test from
# another device on your own network:  UWIN_HOST=0.0.0.0 UWIN_PORT=5000
HOST = os.environ.get("UWIN_HOST", "127.0.0.1")
PORT = int(os.environ.get("UWIN_PORT", "5000"))
DEBUG = os.environ.get("UWIN_DEBUG", "0") == "1"

#: Allow the dashboard to be shown inside another page (an embedded preview).
#: Localhost-only use keeps this OFF, so the browser refuses framing.
#: It switches on automatically when you bind the server to another interface
#: (UWIN_HOST=0.0.0.0), which is only ever done on purpose.
ALLOW_EMBED = os.environ.get(
    "UWIN_ALLOW_EMBED", "1" if HOST not in ("127.0.0.1", "localhost", "::1") else "0"
) == "1"

#: Upper bound for an uploaded file. The *user* configurable value lives in the
#: settings table (``max_upload_mb``); this is only the hard safety ceiling.
HARD_MAX_UPLOAD_MB = 25

# ---------------------------------------------------------------------------
# File handling
# ---------------------------------------------------------------------------
ALLOWED_EXTENSIONS = {".xlsx", ".xls"}
ALLOWED_REGISTER_EXTENSIONS = {".xlsx", ".xls", ".csv"}

#: xlsx / xls both are ZIP or OLE files. We sniff the first bytes instead of
#: trusting the file name only.
ZIP_MAGIC = b"PK\x03\x04"
OLE_MAGIC = b"\xd0\xcf\x11\xe0\xa1\xb1\x1a\xe1"

# ---------------------------------------------------------------------------
# Processing / status model
# ---------------------------------------------------------------------------
STATUS_PENDING = "PENDING"
STATUS_MATCHED = "MATCHED"
STATUS_NEEDS_REVIEW = "NEEDS REVIEW"
STATUS_APPROVED = "APPROVED"
STATUS_COMPLETED = "COMPLETED"
STATUS_SKIPPED = "SKIPPED"
STATUS_FAILED = "FAILED"
STATUS_SAVE_UNKNOWN = "SAVE STATUS UNKNOWN"

ALL_STATUSES = (
    STATUS_PENDING,
    STATUS_MATCHED,
    STATUS_NEEDS_REVIEW,
    STATUS_APPROVED,
    STATUS_COMPLETED,
    STATUS_SKIPPED,
    STATUS_FAILED,
    STATUS_SAVE_UNKNOWN,
)

#: Statuses that must never be touched by an automatic re-run.
#: Repeating them would risk duplicate entries in the official portal.
FINAL_STATUSES = (STATUS_COMPLETED, STATUS_SAVE_UNKNOWN)

SESSION_ACTIVE = "active"
SESSION_PARKED = "parked"
SESSION_COMPLETED = "completed"

# ---------------------------------------------------------------------------
# Validation / matching thresholds
# ---------------------------------------------------------------------------
MATCH_EXACT_ID = 1.00
MATCH_EXACT_NAME_DOB = 0.97
MATCH_EXACT_NAME_FIELD = 0.90
MATCH_FUZZY_MAX = 0.75  # fuzzy is only ever a *suggestion*, never an approval

#: Two candidates closer than this are treated as ambiguous.
MATCH_AMBIGUITY_DELTA = 0.04

#: Below this confidence a record always needs a human look.
NEEDS_REVIEW_BELOW = 0.96

DATE_FORMAT_CHOICES = (
    ("auto", "Auto detect (recommended)"),
    ("DD-MM-YYYY", "DD-MM-YYYY (31-12-2025)"),
    ("DD/MM/YYYY", "DD/MM/YYYY (31/12/2025)"),
    ("DD.MM.YYYY", "DD.MM.YYYY (31.12.2025)"),
    ("YYYY-MM-DD", "YYYY-MM-DD (2025-12-31)"),
    ("MM/DD/YYYY", "MM/DD/YYYY (12/31/2025)"),
)

# ---------------------------------------------------------------------------
# Settings (defaults + validation)
# ---------------------------------------------------------------------------
DEFAULT_SETTINGS: dict[str, object] = {
    "theme": "light",
    "max_upload_mb": 5,
    "date_format": "auto",
    "rows_per_page": 20,
    "auto_recovery": True,
    "cleanup_days": 0,
    "delete_session_on_complete": False,
    "report_format": "xlsx",
    "report_include_warnings": True,
    "pin_lock_enabled": False,
    "pin_hash": "",
    "pin_salt": "",
    "portal_mode": "assisted",
    "portal_adapter_url": "",
    "portal_adapter_enabled": False,
    "confirm_before_save": True,
    "keep_original_file": True,
    "last_portal_open": "",
}

#: ``key -> (type, min, max, allowed values)`` used to sanitise user input.
SETTING_RULES: dict[str, tuple] = {
    "theme": (str, None, None, ("light", "dark", "system")),
    "max_upload_mb": (int, 1, HARD_MAX_UPLOAD_MB, None),
    "date_format": (str, None, None, tuple(value for value, _ in DATE_FORMAT_CHOICES)),
    "rows_per_page": (int, 10, 100, None),
    "auto_recovery": (bool, None, None, None),
    "cleanup_days": (int, 0, 365, None),
    "delete_session_on_complete": (bool, None, None, None),
    "report_format": (str, None, None, ("xlsx", "csv")),
    "report_include_warnings": (bool, None, None, None),
    "pin_lock_enabled": (bool, None, None, None),
    "pin_hash": (str, None, None, None),
    "pin_salt": (str, None, None, None),
    "portal_mode": (str, None, None, ("assisted", "adapter")),
    "portal_adapter_url": (str, None, None, None),
    "portal_adapter_enabled": (bool, None, None, None),
    "confirm_before_save": (bool, None, None, None),
    "keep_original_file": (bool, None, None, None),
    "last_portal_open": (str, None, None, None),
}


def coerce_setting(key: str, value):
    """Return a safe, correctly typed value for ``key``.

    Bad input never raises: the default from :data:`DEFAULT_SETTINGS` is used
    instead. That keeps the dashboard alive if a user types nonsense.
    """
    default = DEFAULT_SETTINGS.get(key)
    if key not in SETTING_RULES:
        return value if value is not None else default

    kind, low, high, allowed = SETTING_RULES[key]

    try:
        if kind is bool:
            if isinstance(value, str):
                cooked = value.strip().lower() in ("1", "true", "yes", "on", "haan", "h")
            else:
                cooked = bool(value)
        elif kind is int:
            cooked = int(str(value).strip())
        else:
            cooked = str(value).strip()
    except (TypeError, ValueError):
        return default

    if kind is int:
        if low is not None and cooked < low:
            cooked = low
        if high is not None and cooked > high:
            cooked = high
    if allowed and cooked not in allowed:
        # Strings that are simply too long are truncated instead of dropped.
        if kind is str and len(str(value)) <= 400:
            return default
        return default
    return cooked


# ---------------------------------------------------------------------------
# Secret key handling
# ---------------------------------------------------------------------------
def get_secret_key() -> str:
    """Load (or create once) the Flask session signing key.

    The key lives in ``data/.secret_key`` with permission ``600``. It is *not*
    part of the source code and *not* committed to Git.
    """
    env_key = os.environ.get("UWIN_SECRET_KEY")
    if env_key:
        return env_key

    ensure_dirs()
    if SECRET_KEY_FILE.exists():
        stored = SECRET_KEY_FILE.read_text(encoding="utf-8").strip()
        if stored:
            return stored

    key = secrets.token_hex(32)
    SECRET_KEY_FILE.write_text(key, encoding="utf-8")
    try:
        os.chmod(SECRET_KEY_FILE, stat.S_IRUSR | stat.S_IWUSR)
    except OSError:
        # Some Android filesystems ignore chmod. Not fatal: file is outside Git.
        pass
    return key


def portal_adapter_token() -> str:
    """Token for an *approved* portal integration, if the user configured one.

    Priority: environment variable ``UWIN_ADAPTER_TOKEN`` then the local file
    ``secrets/portal_adapter_token.txt`` (chmod 600). The token is never written
    to the database, the log file or a report.
    """
    env_token = os.environ.get("UWIN_ADAPTER_TOKEN")
    if env_token:
        return env_token.strip()
    if PORTAL_TOKEN_FILE.exists():
        return PORTAL_TOKEN_FILE.read_text(encoding="utf-8").strip()
    return ""


def save_portal_adapter_token(token: str) -> None:
    """Store (or clear) the adapter token in the local secrets file."""
    ensure_dirs()
    token = (token or "").strip()
    if not token:
        if PORTAL_TOKEN_FILE.exists():
            PORTAL_TOKEN_FILE.unlink()
        return
    PORTAL_TOKEN_FILE.write_text(token, encoding="utf-8")
    try:
        os.chmod(PORTAL_TOKEN_FILE, stat.S_IRUSR | stat.S_IWUSR)
    except OSError:
        pass


def dashboard_url() -> str:
    """URL the user should open in Chrome."""
    host = "127.0.0.1" if HOST in ("0.0.0.0", "::") else HOST
    return f"http://{host}:{PORT}"
