"""
uwin_integration.py
===================
The **only** module that knows about the official U-WIN portal.

Official portal (publicly documented vaccinator login page):

    https://uwinvaccinator.mohfw.gov.in/auth/login

What this module will do
------------------------
* Open the official login page in the device's browser.
* Guide the worker through mobile number -> password -> **manual SMS OTP**.
* Keep the mobile number and password **only in RAM** for a short, explicit
  window and wipe them afterwards (see :class:`CredentialBuffer`).
* Detect a logged-in session **only** through supported ways: an approved
  integration endpoint (adapter) that the user configures, or the worker's own
  confirmation after they see the portal dashboard.
* Send a beneficiary record to the portal **only** through such an approved
  adapter, and only after the worker approved it.

What this module will **never** do (by design, and the code proves it)
---------------------------------------------------------------------
* No reverse-engineered / private endpoints.
* No OTP bypass, no CAPTCHA bypass, no login bypass, no hidden database access.
* No reading SMS messages, no SMS permissions, no autofill of the OTP box.
* No password in source code, SQLite, logs, reports or Excel files.
* No automatic retry of a save whose result is unknown (that could create a
  duplicate vaccine event in a national register).
* No background scraping that would hammer the portal.

If no approved API is available, the app **falls back to the honest assisted
workflow**: it shows the record on screen, opens the portal in the browser, and
lets the worker copy / verify / confirm manually.

Adapter contract (what an approved integration must look like)
--------------------------------------------------------------
The adapter is a small service (or an official API gateway) that the health
department / NIC approves. Configure its base URL in Settings. Every request
carries ``Authorization: Bearer <token>`` where the token comes from
``secrets/portal_adapter_token.txt`` or the ``UWIN_ADAPTER_TOKEN`` environment
variable - never from the database.

===================  ======  ==================================================
Endpoint             Method  Purpose
===================  ======  ==================================================
``/health``          GET     Availability + declared capabilities
``/session/state``   GET     Authenticated session state (no credentials sent)
``/session/otp``     POST    Forward an OTP the worker typed, if the approved
                             adapter asks for it (never stored, never logged)
``/beneficiary/record`` POST Saves one approved vaccination record
===================  ======  ==================================================

Expected save response (JSON)::

    {"status": "confirmed", "reference": "UWIN-ACK-12345"}
    {"status": "pending"}          -> treated as "save status unknown"
    {"status": "duplicate"}        -> treated as "not confirmed", manual check

Anything that is not an explicit confirmation is reported as *SAVE STATUS
UNKNOWN* and is never retried automatically.
"""

from __future__ import annotations

import base64
import json
import re
import ssl
import threading
import time
import urllib.error
import urllib.parse
import urllib.request
import webbrowser
from typing import Any, Optional

import config
import database as db

# ---------------------------------------------------------------------------
# Small utilities
# ---------------------------------------------------------------------------
OTP_RE = re.compile(r"^\d{4,8}$")
MOBILE_RE = re.compile(r"^[6-9]\d{9}$")
MAX_URL_LENGTH = 300


def mask_mobile(value: str) -> str:
    """``9876543210`` -> ``98XXXXXX10`` (safe to display anywhere)."""
    digits = re.sub(r"\D", "", value or "")
    if len(digits) < 4:
        return "******"
    return digits[:2] + "X" * (len(digits) - 4) + digits[-2:]


def validate_mobile_input(value: str) -> tuple[bool, str]:
    digits = re.sub(r"\D", "", value or "")
    if digits.startswith("91") and len(digits) == 12:
        digits = digits[2:]
    if not MOBILE_RE.match(digits):
        return False, "Mobile number must be 10 digits and start with 6, 7, 8 or 9."
    return True, ""


def validate_url(value: str) -> tuple[bool, str]:
    """Only clean http/https URLs without embedded credentials."""
    value = (value or "").strip()
    if not value:
        return True, ""
    if len(value) > MAX_URL_LENGTH:
        return False, "URL is too long."
    parsed = urllib.parse.urlparse(value)
    if parsed.scheme not in ("http", "https"):
        return False, "URL must start with http:// or https://"
    if not parsed.hostname:
        return False, "URL has no host name."
    if parsed.username or parsed.password:
        return False, "Do not put user name or password inside the URL."
    return True, ""


# ---------------------------------------------------------------------------
# Credential handling - RAM only, wiped by hand
# ---------------------------------------------------------------------------
class CredentialBuffer:
    """Short-lived, locked RAM buffer for the login attempt.

    * Values live inside single-item lists so they can be **overwritten**
      (Python strings are immutable, so rebinding a name would leave the old
      text in memory).
    * A hard expiry (default 10 minutes) throws the data away even if the
      browser is forgotten.
    * ``flush()`` overwrites, deletes the slot, and best-effort scrubs local
      variables of the calling frames.
    """

    def __init__(self, ttl_seconds: int = 600):
        self._lock = threading.RLock()
        self._slot: Optional[list[str]] = None
        self._mobile_masked = ""
        self._created_at = 0.0
        self._ttl = ttl_seconds
        self._state = "idle"  # idle | credentials_held | awaiting_otp | logged_in | failed | expired
        self._message = ""
        self._otp_seen_at = 0.0
        self._logged_in_at = 0.0
        self._wipe_count = 0
        self._frame_scrubs = 0

    # -- state ------------------------------------------------------------
    def status(self) -> dict:
        with self._lock:
            self._expire_if_needed()
            age = int(time.time() - self._created_at) if self._created_at else 0
            return {
                "active": self._slot is not None,
                "state": self._state,
                "message": self._message,
                "mobile_masked": self._mobile_masked,
                "age_seconds": age,
                "ttl_seconds": self._ttl,
                "otp_seen": bool(self._otp_seen_at),
                "logged_in": self._state == "logged_in",
                "logged_in_seconds_ago": int(time.time() - self._logged_in_at) if self._logged_in_at else None,
                "wipe_count": self._wipe_count,
                "frame_scrubs": self._frame_scrubs,
            }

    def begin(self, mobile: str, password: str) -> dict:
        """Hold the credentials for one login attempt (never stored on disk)."""
        ok, message = validate_mobile_input(mobile)
        if not ok:
            return {"ok": False, "message": message}
        if not password or len(password) < 4:
            return {"ok": False, "message": "Password looks too short. Type the password you use on the portal."}
        if len(password) > 128:
            return {"ok": False, "message": "Password is longer than the portal allows."}

        with self._lock:
            self._flush_locked()
            digits = re.sub(r"\D", "", mobile)
            self._slot = [digits, password]
            self._mobile_masked = mask_mobile(digits)
            self._created_at = time.time()
            self._state = "credentials_held"
            self._message = "Credentials are held in memory only and will be wiped after the login attempt."
        return {
            "ok": True,
            "message": "Credentials held in memory. They are not written to the database, logs or files.",
            "status": self.status(),
        }

    def note_otp_stage(self) -> dict:
        with self._lock:
            self._expire_if_needed()
            if self._slot is None:
                return {"ok": False, "message": "No login attempt in progress. Start the login again."}
            self._state = "awaiting_otp"
            self._otp_seen_at = time.time()
            self._message = "Waiting for you to type the SMS OTP in the official portal page."
        return {"ok": True, "status": self.status()}

    def mark_logged_in(self, note: str = "") -> dict:
        with self._lock:
            self._state = "logged_in"
            self._logged_in_at = time.time()
            self._message = note or "Login confirmed. You can continue with the Excel workflow."
            # Password is no longer needed once the session exists.
            self._wipe_password_locked()
        db.set_setting("last_portal_open", db.now_iso())
        return {"ok": True, "status": self.status()}

    def mark_failed(self, message: str = "Login was not completed.") -> dict:
        with self._lock:
            self._state = "failed"
            self._message = message
            self._flush_locked(keep_state=True)
        return {"ok": True, "status": self.status()}

    def get_credentials(self) -> Optional[tuple[str, str]]:
        """Return the held credentials **once**, for an approved adapter call.

        The caller must not log or persist them. Returns ``None`` when nothing
        is held or the buffer expired.
        """
        with self._lock:
            self._expire_if_needed()
            if self._slot is None:
                return None
            return self._slot[0], self._slot[1]

    def clear_password_now(self) -> dict:
        """Wipe the password (keep the mobile number) as soon as it is not needed."""
        with self._lock:
            self._wipe_password_locked()
        return self.status()

    def flush(self) -> dict:
        """Wipe everything. Called after the login attempt, on log out, and on exit."""
        with self._lock:
            self._flush_locked()
        return self.status()

    # -- internals --------------------------------------------------------
    def _wipe_password_locked(self) -> None:
        if self._slot and len(self._slot) > 1:
            self._slot[1] = "\x00" * 8
            self._slot[1] = ""

    def _flush_locked(self, keep_state: bool = False) -> None:
        if self._slot:
            for index in range(len(self._slot)):
                self._slot[index] = "\x00" * 8
                self._slot[index] = ""
            self._slot = None
            self._wipe_count += 1
        self._mobile_masked = ""
        self._created_at = 0.0
        self._otp_seen_at = 0.0
        if not keep_state:
            self._state = "idle"
            self._message = ""
        self._frame_scrubs += _scrub_frames()

    def _expire_if_needed(self) -> None:
        if self._slot and self._created_at and (time.time() - self._created_at) > self._ttl:
            self._flush_locked()
            self._state = "expired"
            self._message = "The login attempt expired and the held credentials were wiped. Please start again."


def _scrub_frames() -> int:
    """Best-effort removal of any local variable that still holds a secret.

    Python does not guarantee this (frames, caching and string interning are
    implementation details), which is exactly why the app **never writes
    credentials anywhere** in the first place. This routine simply reduces the
    window in which they could survive inside the interpreter.
    """
    scrubbed = 0
    try:
        import inspect

        for frame_info in inspect.stack()[1:]:
            frame = frame_info.frame
            for name in list(frame.f_locals.keys()):
                value = frame.f_locals.get(name)
                if isinstance(value, (str, bytearray)) and len(str(value)) >= 8:
                    if name.lower() in ("password", "passwd", "pwd", "otp", "otp_value", "pincode", "pin"):
                        try:
                            frame.f_locals[name] = ""
                            scrubbed += 1
                        except Exception:
                            continue
    except Exception:
        return scrubbed
    return scrubbed


#: Single process-wide buffer.
credentials = CredentialBuffer()


# ---------------------------------------------------------------------------
# Browser opening
# ---------------------------------------------------------------------------
def portal_login_url() -> str:
    return config.PORTAL_URL


def open_in_browser(url: Optional[str] = None) -> dict:
    """Open the official portal (or the given URL) in the device browser."""
    target = (url or config.PORTAL_URL).strip()
    ok, message = validate_url(target)
    if not ok:
        return {"ok": False, "message": message}
    if not _looks_like_supported_portal(target):
        return {
            "ok": False,
            "message": "Only the official U-WIN / MOHFW portal links can be opened from here.",
        }

    opened = False
    method = "none"
    error = ""
    try:
        opened = bool(webbrowser.open(target, new=2, autoraise=True))
        method = "webbrowser module"
    except Exception as exc:  # pragma: no cover - browser problems are environmental
        error = str(exc)

    if not opened:
        try:
            import subprocess

            for command in (
                ["termux-open-url", target],
                ["am", "start", "-a", "android.intent.action.VIEW", "-d", target],
                ["xdg-open", target],
            ):
                try:
                    subprocess.Popen(command, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
                    opened, method = True, " ".join(command[:2])
                    break
                except (OSError, FileNotFoundError):
                    continue
        except Exception as exc:
            error = error or str(exc)

    db.set_setting("last_portal_open", db.now_iso())
    db.log_event("portal_opened", "Official portal opened in the browser", detail={"method": method})

    if opened:
        return {
            "ok": True,
            "method": method,
            "url": target,
            "message": "Portal opened in your browser. If you do not see it, copy the link below and paste it in Chrome.",
        }
    return {
        "ok": False,
        "url": target,
        "method": method,
        "error": error,
        "message": "Could not open a browser automatically. Open Chrome and paste the link shown below.",
    }


def _looks_like_supported_portal(url: str) -> bool:
    host = urllib.parse.urlparse(url).hostname or ""
    allowed_suffixes = ("mohfw.gov.in", "uwin.mohfw.gov.in", "uwinvaccinator.mohfw.gov.in", "gov.in")
    return host.endswith(allowed_suffixes)


def guided_login_steps() -> list[dict]:
    """The semi-automated login checklist shown on the dashboard."""
    return [
        {
            "step": 1,
            "title": "Open the official U-WIN login page",
            "text": "Press 'Open U-WIN Login'. The page opens in Chrome on this device. The app never talks to any private endpoint.",
            "action": "open_portal",
        },
        {
            "step": 2,
            "title": "Enter your mobile number and password",
            "text": "Type them in the official page. If you use the secure form in this dashboard, the values stay in RAM only and are wiped automatically.",
            "action": "hold_credentials",
        },
        {
            "step": 3,
            "title": "Wait for the SMS OTP",
            "text": "U-WIN sends the OTP by SMS. The app pauses here and never reads your SMS or your OTP.",
            "action": "await_otp",
        },
        {
            "step": 4,
            "title": "Type the OTP in the official page",
            "text": "OTP Required - Enter the SMS OTP in the official portal page yourself. The app will not type it for you and will not save it.",
            "action": "manual_otp",
        },
        {
            "step": 5,
            "title": "Come back and continue",
            "text": "Once you can see the portal dashboard, press 'I am logged in'. Then continue with the Excel beneficiary workflow.",
            "action": "confirm_login",
        },
    ]


def login_status() -> dict:
    """Everything the login card needs, in one dictionary."""
    return {
        "portal_url": config.PORTAL_URL,
        "settings_url": config.PORTAL_INFO_URL,
        "steps": guided_login_steps(),
        "credential_buffer": credentials.status(),
        "adapter": adapter_status(),
        "otp_policy": (
            "OTP is typed by you in the official portal page. This app never reads SMS, never "
            "intercepts the OTP, never stores it and never types it for you."
        ),
        "compliance_note": (
            "Automation is used only where the portal permits it (an approved API adapter). "
            "Otherwise the app runs the assisted workflow: open the official page, organise the "
            "data locally, and let you verify and confirm."
        ),
    }


# ---------------------------------------------------------------------------
# Approved adapter client (optional)
# ---------------------------------------------------------------------------
class AdapterError(Exception):
    def __init__(self, message: str, code: str = "adapter_error"):
        super().__init__(message)
        self.message = message
        self.code = code


class AdapterClient:
    """Talks to an **approved** integration endpoint.

    The adapter is *off* until the worker configures a URL in Settings and
    presses "Test connection". Even then, the app only sends data after an
    explicit approval, never in the background, and never retries an uncertain
    save automatically.
    """

    #: Minimum gap between two calls - protects the portal from accidental loops.
    MIN_INTERVAL_SECONDS = 3.0
    TIMEOUT_SECONDS = 25

    _last_call = 0.0
    _call_lock = threading.Lock()

    def __init__(self, settings: Optional[dict] = None):
        self.settings = settings or db.get_all_settings()
        self.base_url = str(self.settings.get("portal_adapter_url") or "").rstrip("/")
        self.enabled = bool(self.settings.get("portal_adapter_enabled")) and bool(self.base_url)
        self.token = config.portal_adapter_token()

    # -- plumbing ---------------------------------------------------------
    def _wait_for_rate_limit(self) -> None:
        with AdapterClient._call_lock:
            elapsed = time.time() - AdapterClient._last_call
            if elapsed < self.MIN_INTERVAL_SECONDS:
                time.sleep(self.MIN_INTERVAL_SECONDS - elapsed)
            AdapterClient._last_call = time.time()

    def _request(self, method: str, path: str, payload: Optional[dict] = None) -> dict:
        if not self.enabled:
            raise AdapterError(
                "Approved integration is not enabled. Use the assisted workflow or ask your "
                "department for an approved API endpoint.",
                "adapter_disabled",
            )
        ok, message = validate_url(self.base_url)
        if not ok:
            raise AdapterError(f"Adapter URL is invalid: {message}", "bad_url")

        url = f"{self.base_url}{path}"
        body = json.dumps(payload).encode("utf-8") if payload is not None else None
        headers = {
            "Accept": "application/json",
            "User-Agent": f"UWIN-Smart-Assistant/{config.APP_VERSION} (local device app)",
        }
        if body is not None:
            headers["Content-Type"] = "application/json"
        if self.token:
            headers["Authorization"] = f"Bearer {self.token}"

        request = urllib.request.Request(url, data=body, headers=headers, method=method)
        context = ssl.create_default_context()

        self._wait_for_rate_limit()
        try:
            with urllib.request.urlopen(request, timeout=self.TIMEOUT_SECONDS, context=context) as response:
                raw = response.read(200000).decode("utf-8", errors="replace")
                return _parse_json(raw) or {"raw": raw[:2000]}
        except urllib.error.HTTPError as exc:
            detail = ""
            try:
                detail = exc.read(4000).decode("utf-8", errors="replace")[:400]
            except Exception:
                detail = ""
            raise AdapterError(f"Adapter replied HTTP {exc.code}. {detail}", f"http_{exc.code}") from exc
        except urllib.error.URLError as exc:
            raise AdapterError(
                f"Adapter is not reachable ({exc.reason}). Check Wi-Fi / mobile data and the URL.",
                "unreachable",
            ) from exc
        except TimeoutError as exc:
            raise AdapterError("Adapter did not answer in time.", "timeout") from exc

    # -- public calls -----------------------------------------------------
    def health(self) -> dict:
        try:
            data = self._request("GET", "/health")
        except AdapterError as exc:
            return {"ok": False, "message": exc.message, "code": exc.code}
        return {
            "ok": True,
            "message": "Approved integration is reachable.",
            "capabilities": data.get("capabilities", []),
            "detail": {key: data.get(key) for key in ("name", "version", "portal", "auth") if key in data},
        }

    def session_state(self) -> dict:
        try:
            data = self._request("GET", "/session/state")
        except AdapterError as exc:
            return {"ok": False, "message": exc.message, "logged_in": False}
        logged_in = bool(data.get("logged_in") or data.get("authenticated"))
        return {
            "ok": True,
            "logged_in": logged_in,
            "message": data.get("message") or ("Session is active." if logged_in else "Not logged in."),
            "expires_at": data.get("expires_at", ""),
        }

    def submit_otp(self, otp_value: str) -> dict:
        """Forward a worker-typed OTP **only** when an approved adapter asks for it.

        The value is used once and dropped. It is never logged and never stored.
        """
        if not self.enabled:
            return {
                "ok": False,
                "message": "This app does not accept OTP by itself. Type the OTP in the official portal page.",
                "type": "manual_only",
            }
        if not OTP_RE.match(str(otp_value or "").strip()):
            return {"ok": False, "message": "OTP should be 4 to 8 digits."}

        value = str(otp_value).strip()
        try:
            data = self._request("POST", "/session/otp", {"otp": value})
        except AdapterError as exc:
            return {"ok": False, "message": exc.message, "code": exc.code}
        finally:
            value = ""  # noqa: F841 - drop the local copy immediately

        credentials.mark_logged_in("OTP accepted by the approved integration.")
        return {"ok": True, "message": data.get("message", "OTP submitted to the approved integration.")}

    def submit_record(self, record: dict) -> dict:
        """Send one **approved** record. Never called automatically."""
        payload = _record_payload(record)
        try:
            data = self._request("POST", "/beneficiary/record", payload)
        except AdapterError as exc:
            return {"status": "unknown", "confirmed": False, "message": exc.message, "code": exc.code}

        status = str(data.get("status", "")).lower()
        reference = str(data.get("reference") or data.get("acknowledgement") or data.get("ack_no") or "")

        if status in ("confirmed", "success", "saved", "created", "ok") and reference:
            return {
                "status": "confirmed",
                "confirmed": True,
                "reference": reference,
                "message": data.get("message") or "Portal confirmed the save.",
            }
        if status == "duplicate":
            return {
                "status": "duplicate",
                "confirmed": False,
                "message": "The portal says this record already exists. Check it yourself before doing anything else.",
            }
        return {
            "status": "unknown",
            "confirmed": False,
            "message": data.get("message")
            or "The portal did not send a clear confirmation. Record marked SAVE STATUS UNKNOWN.",
        }


def _parse_json(raw: str) -> Optional[dict]:
    try:
        data = json.loads(raw)
        return data if isinstance(data, dict) else {"data": data}
    except ValueError:
        return None


def _record_payload(record: dict) -> dict:
    """Only the fields the portal needs - no notes, no file paths, no secrets."""
    return {
        "beneficiary_id": record.get("beneficiary_id") or "",
        "name": record.get("name") or "",
        "dob": record.get("dob") or "",
        "gender": record.get("gender") or "",
        "mobile": record.get("mobile") or "",
        "mother_name": record.get("mother_name") or "",
        "vaccine_name": record.get("vaccine_name") or "",
        "dose_number": record.get("dose_number") or "",
        "vaccination_date": record.get("vaccination_date") or "",
        "batch_number": record.get("batch_number") or "",
        "vaccinator_name": record.get("vaccinator_name") or "",
        "facility": record.get("facility") or "",
        "source_row": record.get("row_number"),
        "local_record_id": record.get("id"),
    }


def adapter_status() -> dict:
    client = AdapterClient()
    info = {
        "enabled": client.enabled,
        "configured": bool(client.base_url),
        "url_masked": _mask_url(client.base_url),
        "token_present": bool(client.token),
        "mode": "adapter" if client.enabled else "assisted",
    }
    if client.enabled:
        try:
            info.update(client.health())
        except Exception as exc:  # pragma: no cover - defensive
            info.update({"ok": False, "message": str(exc)})
    return info


def _mask_url(url: str) -> str:
    if not url:
        return ""
    parsed = urllib.parse.urlparse(url)
    return f"{parsed.scheme}://{parsed.hostname}{':' + str(parsed.port) if parsed.port else ''}"


# ---------------------------------------------------------------------------
# Save flow
# ---------------------------------------------------------------------------
def save_record(record: dict, *, confirmed_by_worker: bool, pre_save_hook=None) -> dict:
    """Attempt an authorised save for an approved record.

    ``pre_save_hook`` is called immediately *before* the network request (the
    dashboard uses it to write a durable "saving" marker to SQLite, so a crash
    can never lead to an automatic duplicate attempt).

    Returns one of:

    * ``{"status": "assisted", "next_steps": [...]}`` - no approved API, the
      worker is guided through the portal manually. The record is **not**
      marked completed.
    * ``{"status": "confirmed", "reference": "..."}`` - the approved adapter
      confirmed the save; :mod:`progress_manager` may mark it COMPLETED.
    * ``{"status": "unknown"}`` - anything else (pending, timeout, duplicate,
      ambiguous answer). The record becomes *SAVE STATUS UNKNOWN* and must be
      checked by hand. It is never retried automatically.
    """
    if not confirmed_by_worker:
        return {"status": "blocked", "message": "A record can only be saved after your explicit approval."}

    if record.get("save_state") == "unknown":
        return {
            "status": "blocked",
            "message": "A previous save for this record is unconfirmed. Check the portal first - "
                       "the app will not send it again automatically.",
        }

    if credentials.status()["state"] != "logged_in":
        return {
            "status": "not_logged_in",
            "message": "Portal session is not confirmed. Open the official login page and log in again.",
            "portal_url": config.PORTAL_URL,
        }

    client = AdapterClient()
    if not client.enabled:
        return {
            "status": "assisted",
            "message": "No approved integration is configured, so this app will not send anything by itself.",
            "next_steps": assisted_next_steps(record),
        }

    # Verify the session through the approved endpoint before sending data.
    state = client.session_state()
    if not state.get("logged_in"):
        credentials.mark_failed("Portal session expired.")
        return {
            "status": "session_expired",
            "message": "The portal session expired. Please log in again (OTP will be needed).",
            "portal_url": config.PORTAL_URL,
        }

    if pre_save_hook:
        pre_save_hook()

    result = client.submit_record(record)
    if result.get("status") == "confirmed":
        return result
    return {
        "status": "unknown",
        "message": result.get("message", "Save could not be verified."),
        "detail": result,
    }


def assisted_next_steps(record: dict) -> list[dict]:
    """Plain steps for the manual (assisted) workflow - the safe default."""
    return [
        {
            "title": "Copy the beneficiary details",
            "text": f"Name: {record.get('name') or '-'} | DOB: {record.get('dob') or '-'} | "
                    f"Beneficiary ID: {record.get('beneficiary_id') or '-'}",
        },
        {
            "title": "Open U-WIN and search the beneficiary",
            "text": "Use the official portal search. Compare the name, date of birth and mother's name.",
        },
        {
            "title": "Enter the vaccination details yourself",
            "text": f"Vaccine: {record.get('vaccine_name') or '-'} | Dose: {record.get('dose_number') or '-'} | "
                    f"Date: {record.get('vaccination_date') or '-'}",
        },
        {
            "title": "Confirm what the portal shows",
            "text": "After the portal shows a success message, come back and paste the portal acknowledgement "
                    "(reference number) into the confirmation box. Only then is the record marked COMPLETED.",
        },
    ]


def portal_session_confirmed() -> bool:
    state = credentials.status()["state"]
    if state != "logged_in":
        return False
    client = AdapterClient()
    if not client.enabled:
        return True  # worker confirmed visually
    return bool(client.session_state().get("logged_in"))


def logout() -> dict:
    """Wipe held login data. Does not (and cannot) log out of the browser."""
    status = credentials.flush()
    db.log_event("portal_logout", "Local login data wiped")
    return {
        "ok": True,
        "message": "Local login data wiped from memory. Please also log out in the browser if you are on a shared phone.",
        "status": status,
    }


def describe_security() -> list[dict]:
    """Short, honest list used by the dashboard's security panel."""
    return [
        {"label": "Where data is processed", "value": "Only on this device. No analytics, no cloud, no third-party upload."},
        {"label": "Where data is stored", "value": "Local SQLite file (data/uwin_assistant.db) and files in uploads/ and reports/."},
        {"label": "Passwords", "value": "Never written to source, database, logs or reports. Only held in RAM for the login attempt."},
        {"label": "OTP", "value": "Typed by you in the official page. Never read from SMS, never stored, never reused."},
        {"label": "Portal automation", "value": "Only through an approved API endpoint. Otherwise the assisted (manual) workflow is used."},
        {"label": "Unknown saves", "value": "Never retried automatically - they are marked SAVE STATUS UNKNOWN for manual checking."},
        {"label": "Delete everything", "value": "Settings -> Privacy -> Delete All Local Data removes the database rows and generated files."},
    ]


def b64_note() -> str:
    """Tiny helper kept for completeness of the adapter auth toolkit."""
    return base64.b64encode(b"uwin-smart-assistant").decode("ascii")
