"""
Pytest fixtures for the U-WIN Smart Assistant test suite.

The tests always run against a **temporary data folder**, so your real
database, uploads and reports are never touched::

    python -m pytest -q
"""

from __future__ import annotations

import io
import os
import shutil
import sys
import tempfile
from pathlib import Path

import pytest

# Make the project importable when pytest is started from the project folder.
PROJECT_ROOT = Path(__file__).resolve().parent.parent
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

# Point every runtime folder at a throw-away directory *before* importing config.
_TEMP_ROOT = tempfile.mkdtemp(prefix="uwin-tests-")
os.environ["UWIN_DATA_ROOT"] = _TEMP_ROOT
os.environ["UWIN_SECRET_KEY"] = "test-secret-key-not-used-anywhere-real"

import config  # noqa: E402
import database as db  # noqa: E402
import samples  # noqa: E402


@pytest.fixture(scope="session", autouse=True)
def _cleanup_temp_root():
    yield
    db.close_conn()
    shutil.rmtree(_TEMP_ROOT, ignore_errors=True)


@pytest.fixture()
def fresh_db():
    """Empty database for every test."""
    config.ensure_dirs()
    db.delete_all_local_data()
    yield db
    db.delete_all_local_data()


@pytest.fixture()
def sample_files():
    """The example workbook and register file."""
    return samples.ensure_sample_files(force=True)


@pytest.fixture()
def app_module(fresh_db):
    import app as app_module

    app_module.app.config.update(TESTING=True)
    return app_module


@pytest.fixture()
def client(app_module):
    return app_module.app.test_client()


@pytest.fixture()
def csrf_headers(client):
    """Session-bound CSRF token, exactly as the dashboard sends it."""
    with client.session_transaction() as session:
        session["_csrf_token"] = "test-csrf-token"
    return {"X-CSRF-Token": "test-csrf-token"}


def upload(client, headers, path: Path, field: str = "file"):
    """Helper: POST a file to an upload endpoint."""
    with open(path, "rb") as handle:
        payload = handle.read()
    return client.post(
        "/api/session/upload",
        data={field: (io.BytesIO(payload), path.name)},
        headers=headers,
        content_type="multipart/form-data",
    )


@pytest.fixture()
def imported_session(client, csrf_headers, sample_files):
    """A session with the sample file already imported and matched."""
    response = upload(client, csrf_headers, sample_files["beneficiaries"])
    payload = response.get_json()
    session_id = payload["session_id"]
    mapping = payload["preview"]["mapping"]

    client.post(f"/api/session/{session_id}/import", json={"mapping": mapping}, headers=csrf_headers)

    with open(sample_files["register"], "rb") as handle:
        register_bytes = handle.read()
    client.post(
        "/api/register/import",
        data={"file": (io.BytesIO(register_bytes), "register.xlsx")},
        headers=csrf_headers,
        content_type="multipart/form-data",
    )
    client.post(f"/api/session/{session_id}/reprocess", json={}, headers=csrf_headers)
    return {"session_id": session_id, "preview": payload["preview"]}
