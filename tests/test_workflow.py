"""
End-to-end workflow tests through the Flask dashboard, including the safety
rules that protect the official register (never auto-complete, never retry an
uncertain save, CSRF protection, local-only data deletion).
"""

from __future__ import annotations

import io
import json

import config
import database as db
import progress_manager as pm
import uwin_integration as uwin


class TestPages:
    def test_every_page_loads(self, client):
        for page in ("/", "/upload", "/records", "/history", "/reports", "/settings", "/portal", "/help"):
            response = client.get(page)
            assert response.status_code == 200, page

    def test_review_page_asks_for_a_file_when_none_is_loaded(self, client):
        # With no session yet, the review screen redirects to the upload page
        # with a friendly message instead of showing an empty queue.
        response = client.get("/review")
        assert response.status_code == 302
        assert "/upload" in response.headers["Location"]

    def test_unknown_page_shows_friendly_error(self, client):
        response = client.get("/does-not-exist")
        assert response.status_code == 404
        assert b"Page not found" in response.data or b"not exist" in response.data

    def test_security_headers_present(self, client):
        response = client.get("/")
        assert response.headers["X-Content-Type-Options"] == "nosniff"
        assert "default-src 'self'" in response.headers["Content-Security-Policy"]


class TestUploadAndImport:
    def test_upload_then_import(self, client, csrf_headers, sample_files):
        with open(sample_files["beneficiaries"], "rb") as handle:
            payload = handle.read()
        response = client.post(
            "/api/session/upload",
            data={"file": (io.BytesIO(payload), "beneficiaries.xlsx")},
            headers=csrf_headers,
            content_type="multipart/form-data",
        )
        assert response.status_code == 200
        data = response.get_json()
        session_id = data["session_id"]
        assert data["preview"]["data_rows"] > 0
        assert data["preview"]["mapping"]["name"]

        response = client.post(
            f"/api/session/{session_id}/import",
            json={"mapping": data["preview"]["mapping"]},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        result = response.get_json()
        assert result["counts"]["TOTAL"] == data["preview"]["data_rows"]
        assert result["counts"]["NEEDS REVIEW"] >= 3

    def test_wrong_file_type_is_refused(self, client, csrf_headers, tmp_path):
        bad = tmp_path / "notes.txt"
        bad.write_text("not a spreadsheet", encoding="utf-8")
        response = client.post(
            "/api/session/upload",
            data={"file": (io.BytesIO(b"not a spreadsheet"), "notes.txt")},
            headers=csrf_headers,
            content_type="multipart/form-data",
        )
        assert response.status_code == 400
        assert response.get_json()["ok"] is False

    def test_fake_xlsx_inside_is_refused(self, client, csrf_headers):
        response = client.post(
            "/api/session/upload",
            data={"file": (io.BytesIO(b"This is plain text pretending to be Excel"), "trick.xlsx")},
            headers=csrf_headers,
            content_type="multipart/form-data",
        )
        assert response.status_code == 400
        assert "not a real Excel file" in response.get_json()["message"]

    def test_import_without_csrf_is_blocked(self, client, sample_files, imported_session):
        response = client.post(f"/api/session/{imported_session['session_id']}/import", json={})
        assert response.status_code == 403

    def test_double_import_needs_confirmation(self, client, csrf_headers, imported_session):
        response = client.post(
            f"/api/session/{imported_session['session_id']}/import",
            json={"mapping": imported_session["preview"]["mapping"]},
            headers=csrf_headers,
        )
        assert response.status_code == 409
        assert response.get_json()["need_replace"] is True

        response = client.post(
            f"/api/session/{imported_session['session_id']}/import",
            json={"mapping": imported_session["preview"]["mapping"], "replace": True},
            headers=csrf_headers,
        )
        assert response.status_code == 200


class TestMatchingFlow:
    def test_register_import_activates_matching(self, imported_session, client):
        records = db.list_records(session_id=imported_session["session_id"], limit=200)
        statuses = [record["status"] for record in records]
        assert config.STATUS_MATCHED in statuses
        assert config.STATUS_NEEDS_REVIEW in statuses

    def test_high_confidence_match_is_not_flagged(self, imported_session):
        records = db.list_records(session_id=imported_session["session_id"], limit=200)
        matched = [r for r in records if r["status"] == config.STATUS_MATCHED]
        assert matched
        for record in matched:
            assert record["match_confidence"] >= config.NEEDS_REVIEW_BELOW

    def test_fuzzy_only_match_is_never_auto_matched(self, imported_session):
        records = db.list_records(session_id=imported_session["session_id"], limit=200)
        for record in records:
            match = db.json_loads(record["match_json"], {}) or {}
            if match.get("match_type") == "FUZZY":
                assert record["status"] != config.STATUS_MATCHED


class TestReviewDecisions:
    def matched_record(self, session_id):
        return next(
            record for record in db.list_records(session_id=session_id, limit=200)
            if record["status"] == config.STATUS_MATCHED
        )

    def test_approve_skip_reject_and_reopen(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        record = self.matched_record(session_id)

        response = client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)
        assert response.status_code == 200
        assert db.get_record(record["id"])["status"] == config.STATUS_APPROVED

        response = client.post(f"/api/records/{record['id']}/skip", json={"reason": "later"}, headers=csrf_headers)
        assert response.status_code == 200
        assert db.get_record(record["id"])["status"] == config.STATUS_SKIPPED

        response = client.post(f"/api/records/{record['id']}/reopen", json={}, headers=csrf_headers)
        assert response.status_code == 200
        assert db.get_record(record["id"])["status"] in (config.STATUS_PENDING, config.STATUS_NEEDS_REVIEW)

        response = client.post(f"/api/records/{record['id']}/reject", json={"reason": "wrong child"}, headers=csrf_headers)
        assert response.status_code == 200
        assert db.get_record(record["id"])["status"] == config.STATUS_FAILED

    def test_reject_requires_a_reason(self, client, csrf_headers, imported_session):
        record = self.matched_record(imported_session["session_id"])
        response = client.post(f"/api/records/{record['id']}/reject", json={"reason": ""}, headers=csrf_headers)
        assert response.status_code == 400

    def test_edit_revalidates_and_normalises_dates(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        db.set_setting("date_format", "DD-MM-YYYY")   # the worker picked a format
        record = next(
            item for item in db.list_records(session_id=session_id, limit=200)
            if item["status"] == config.STATUS_NEEDS_REVIEW and not item["vaccine_name"]
        )
        response = client.post(
            f"/api/records/{record['id']}/edit",
            json={"values": {"vaccine_name": "OPV", "dose_number": "1", "vaccination_date": "12-06-2023"},
                  "note": "checked in the register"},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        updated = db.get_record(record["id"])
        assert updated["vaccine_name"] == "OPV"
        assert updated["vaccination_date"] == "2023-06-12"      # normalised to ISO
        assert updated["vaccination_date_raw"] == "12-06-2023"  # original kept
        assert db.json_loads(updated["edited_json"], {})["vaccine_name"]["to"] == "OPV"

    def test_ambiguous_date_edit_is_flagged_not_guessed(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        db.set_setting("date_format", "auto")
        record = next(
            item for item in db.list_records(session_id=session_id, limit=200)
            if item["status"] == config.STATUS_NEEDS_REVIEW
        )
        response = client.post(
            f"/api/records/{record['id']}/edit",
            json={"values": {"vaccination_date": "03/04/2025"}, "note": "typed from paper"},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        payload = response.get_json()
        assert payload["date_notes"]                      # the worker is told
        assert db.get_record(record["id"])["status"] == config.STATUS_NEEDS_REVIEW

    def test_approving_without_a_name_is_refused(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        record = next(
            (item for item in db.list_records(session_id=session_id, limit=200) if not item["name"]),
            None,
        )
        if record is None:
            return  # sample file version without an empty-name row
        response = client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)
        assert response.status_code == 400


class TestPortalSafety:
    def test_save_requires_login_confirmation(self, client, csrf_headers, imported_session):
        record = next(
            item for item in db.list_records(session_id=imported_session["session_id"], limit=200)
            if item["status"] == config.STATUS_MATCHED
        )
        client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)
        response = client.post(f"/api/records/{record['id']}/save", json={"confirmed": True}, headers=csrf_headers)
        assert response.status_code == 409
        assert response.get_json()["needs_login"] is True
        assert db.get_record(record["id"])["status"] == config.STATUS_APPROVED

    def test_assisted_workflow_never_marks_complete(self, client, csrf_headers, imported_session):
        record = next(
            item for item in db.list_records(session_id=imported_session["session_id"], limit=200)
            if item["status"] == config.STATUS_MATCHED
        )
        client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)

        # Simulate the worker having logged in on the portal.
        uwin.credentials.begin("9876543210", "ExamplePass123")
        uwin.credentials.mark_logged_in()

        response = client.post(f"/api/records/{record['id']}/save", json={"confirmed": True}, headers=csrf_headers)
        assert response.status_code == 200
        payload = response.get_json()
        assert payload["assisted"] is True
        assert payload["next_steps"]
        assert db.get_record(record["id"])["status"] == config.STATUS_APPROVED  # NOT completed

        uwin.credentials.flush()

    def test_confirmation_reference_is_required_to_complete(self, client, csrf_headers, imported_session):
        record = next(
            item for item in db.list_records(session_id=imported_session["session_id"], limit=200)
            if item["status"] == config.STATUS_MATCHED
        )
        client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)

        response = client.post(f"/api/records/{record['id']}/confirm", json={"reference": ""}, headers=csrf_headers)
        assert response.status_code == 400
        assert db.get_record(record["id"])["status"] != config.STATUS_COMPLETED

        response = client.post(
            f"/api/records/{record['id']}/confirm",
            json={"reference": "UWIN-ACK-12345"},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        completed = db.get_record(record["id"])
        assert completed["status"] == config.STATUS_COMPLETED
        assert completed["save_reference"] == "UWIN-ACK-12345"

    def test_otp_is_never_accepted_without_an_approved_adapter(self, client, csrf_headers):
        response = client.post("/api/portal/otp", json={"otp": "123456"}, headers=csrf_headers)
        assert response.status_code == 400
        assert "official portal page" in response.get_json()["message"]

    def test_credentials_are_never_written_to_the_database(self, client, csrf_headers):
        client.post(
            "/api/portal/credentials",
            json={"mobile": "9876543210", "password": "SuperSecret123"},
            headers=csrf_headers,
        )
        haystack = []
        for table in ("sessions", "records", "events", "settings", "meta", "official_records"):
            for row in db.query_all(f"SELECT * FROM {table}"):
                haystack.append(json.dumps(dict(row), default=str))
        combined = " ".join(haystack)
        assert "SuperSecret123" not in combined
        uwin.credentials.flush()

    def test_uncertain_save_is_never_retried_automatically(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        record = next(
            item for item in db.list_records(session_id=session_id, limit=200)
            if item["status"] == config.STATUS_MATCHED
        )
        client.post(f"/api/records/{record['id']}/approve", json={}, headers=csrf_headers)

        response = client.post(
            f"/api/records/{record['id']}/unknown",
            json={"reason": "portal timed out"},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        assert db.get_record(record["id"])["status"] == config.STATUS_SAVE_UNKNOWN

        # A second save attempt must be refused outright.
        response = client.post(f"/api/records/{record['id']}/save", json={"confirmed": True}, headers=csrf_headers)
        assert response.status_code in (400, 409)
        assert db.get_record(record["id"])["status"] == config.STATUS_SAVE_UNKNOWN

    def test_crash_during_save_becomes_unknown_on_restart(self, imported_session):
        record = next(
            item for item in db.list_records(session_id=imported_session["session_id"], limit=200)
            if item["status"] == config.STATUS_MATCHED
        )
        pm.mark_saving(record["id"])                      # marker written before the network call
        db.update_record(record["id"], status=config.STATUS_APPROVED)
        recovered = pm.recover_state()                    # pretend Termux was killed here
        assert recovered["mid_save"] >= 1
        assert db.get_record(record["id"])["status"] == config.STATUS_SAVE_UNKNOWN


class TestQueue:
    def test_pause_resume_stop(self, client, csrf_headers, imported_session):
        session_id = imported_session["session_id"]
        assert client.post("/api/queue/pause", json={}, headers=csrf_headers).get_json()["queue"]["paused"] is True
        blocked = pm.process_next(session_id)
        assert blocked["blocked"] is True
        client.post("/api/queue/resume", json={}, headers=csrf_headers)
        assert pm.process_next(session_id)["ok"] is True
        client.post("/api/queue/stop", json={}, headers=csrf_headers)
        assert db.get_queue_state()["stopped"] is True
        client.post("/api/queue/resume", json={}, headers=csrf_headers)
        assert db.get_queue_state()["stopped"] is False

    def test_statistics_match_the_records(self, imported_session):
        counts = pm.statistics(imported_session["session_id"])
        assert counts["TOTAL"] == sum(
            counts[key] for key in ("PENDING", "MATCHED", "NEEDS REVIEW", "APPROVED", "COMPLETED",
                                    "SKIPPED", "FAILED", "SAVE STATUS UNKNOWN")
        )


class TestReports:
    def test_session_report_and_download(self, client, csrf_headers, imported_session):
        response = client.post("/api/reports/session", json={}, headers=csrf_headers)
        assert response.status_code == 200
        payload = response.get_json()
        assert payload["rows"] > 0

        download = client.get(f"/reports/download/{payload['filename']}")
        assert download.status_code == 200
        assert len(download.data) > 3000

    def test_report_path_traversal_is_blocked(self, client):
        response = client.get("/reports/download/..%2Fdata%2Fuwin_assistant.db")
        assert response.status_code == 404

    def test_csv_export(self, client, csrf_headers, imported_session):
        response = client.post("/api/reports/export", json={"status": "MATCHED"}, headers=csrf_headers)
        assert response.status_code == 200
        assert response.get_json()["rows"] >= 1

    def test_overview_report(self, client, csrf_headers, imported_session):
        response = client.post("/api/reports/overview", json={}, headers=csrf_headers)
        assert response.status_code == 200


class TestSettingsAndPrivacy:
    def test_settings_are_validated(self, client, csrf_headers):
        response = client.post(
            "/api/settings",
            json={"settings": {"max_upload_mb": 999, "theme": "neon", "rows_per_page": "abc"}},
            headers=csrf_headers,
        )
        assert response.status_code == 200
        assert db.get_setting("max_upload_mb") == config.HARD_MAX_UPLOAD_MB
        assert db.get_setting("theme") == config.DEFAULT_SETTINGS["theme"]
        assert db.get_setting("rows_per_page") == config.DEFAULT_SETTINGS["rows_per_page"]

    def test_pin_lock_flow(self, client, csrf_headers):
        response = client.post("/api/pin/set", json={"pin": "2468"}, headers=csrf_headers)
        assert response.status_code == 200

        with client.session_transaction() as session:
            session.pop("unlocked", None)
        locked = client.get("/records")
        assert locked.status_code in (302, 401)
        if locked.status_code == 302:
            assert "/lock" in locked.headers["Location"]

        response = client.post("/api/pin/unlock", json={"pin": "9999"}, headers=csrf_headers)
        assert response.status_code == 403
        response = client.post("/api/pin/unlock", json={"pin": "2468"}, headers=csrf_headers)
        assert response.status_code == 200

        client.post("/api/pin/remove", json={"pin": "2468"}, headers=csrf_headers)
        assert db.get_setting("pin_lock_enabled") is False

    def test_wipe_requires_the_word_delete(self, client, csrf_headers, imported_session):
        response = client.post("/api/data/wipe", json={"confirm": "yes"}, headers=csrf_headers)
        assert response.status_code == 400
        assert db.status_counts()["TOTAL"] > 0

        response = client.post("/api/data/wipe", json={"confirm": "DELETE"}, headers=csrf_headers)
        assert response.status_code == 200
        assert db.status_counts()["TOTAL"] == 0
        assert db.count_official_records() == 0

    def test_sample_files_download(self, client):
        response = client.get("/api/sample-file")
        assert response.status_code == 200
        assert response.data[:4] == b"PK\x03\x04"
