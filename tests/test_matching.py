"""Matching engine: priority order, ambiguity handling and the no-auto-fuzzy rule."""

from __future__ import annotations

import config
import matching_engine as me
import validation


def official(beneficiary_id, name, dob, mobile="", mother="", gender="", row_id=None):
    return {
        "id": row_id or beneficiary_id,
        "beneficiary_id": beneficiary_id,
        "name": name,
        "dob": dob,
        "mobile": mobile,
        "mother_name": mother,
        "gender": gender,
        "name_key": validation.name_key(name),
        "dob_key": dob,
        "extra": {},
    }


REGISTER = [
    official("100000000101", "Aarav Sharma", "2023-04-12", "9876543210", "Sunita Sharma", "Male"),
    official("100000000102", "Aarav Sharma", "2022-01-05", "9812345678", "Rekha Sharma", "Male"),
    official("100000000103", "Diya Patil", "2023-01-05", "9822001100", "Rekha Patil", "Female"),
    official("100000000104", "Kabir Deshmukh", "2022-08-21", "9730112233", "Vaishali Deshmukh", "Male"),
    official("100000000105", "Anaya Jadhav", "2023-11-30", "9011223344", "Pooja Jadhav", "Female"),
]


def index():
    return me.build_index(REGISTER)


class TestMatchPriorities:
    def test_exact_beneficiary_id_wins(self):
        record = {"name": "Aarav Shrma", "beneficiary_id": "100000000101", "dob": "2023-04-12", "_dob_iso": "2023-04-12"}
        result = me.match_record(record, index())
        assert result["match_type"] == me.MATCH_EXACT_ID
        assert result["confidence"] == config.MATCH_EXACT_ID
        assert result["auto_ok"] is True

    def test_name_and_dob_match(self):
        record = {"name": "Diya Patil", "beneficiary_id": "", "dob": "2023-01-05", "_dob_iso": "2023-01-05"}
        result = me.match_record(record, index())
        assert result["match_type"] in (me.MATCH_EXACT_NAME_DOB, me.MATCH_EXACT_NAME_FIELD)
        assert result["auto_ok"] is True

    def test_name_plus_mobile(self):
        record = {"name": "Anaya Jadhav", "beneficiary_id": "", "dob": "", "mobile": "9011223344"}
        result = me.match_record(record, index())
        assert result["match_type"] == me.MATCH_EXACT_NAME_FIELD
        assert "mobile" in result["primary"]["matched_fields"]

    def test_name_plus_mother(self):
        record = {"name": "Kabir Deshmukh", "beneficiary_id": "", "dob": "", "mother_name": "Vaishali Deshmukh"}
        result = me.match_record(record, index())
        assert result["match_type"] == me.MATCH_EXACT_NAME_FIELD

    def test_similar_name_is_only_a_suggestion(self):
        record = {"name": "Aarav Shrma", "beneficiary_id": "", "dob": "2023-04-12", "_dob_iso": "2023-04-12"}
        result = me.match_record(record, index())
        assert result["match_type"] in (me.MATCH_FUZZY, me.MATCH_EXACT_NAME_DOB)
        if result["match_type"] == me.MATCH_FUZZY:
            assert result["auto_ok"] is False

    def test_two_identical_names_are_ambiguous(self):
        record = {"name": "Aarav Sharma", "beneficiary_id": "", "dob": "", "mobile": ""}
        result = me.match_record(record, index())
        assert result["ambiguous"] is True
        assert result["auto_ok"] is False
        assert len(result["candidates"]) >= 2

    def test_no_match(self):
        record = {"name": "Zzz Qqq", "beneficiary_id": "999999999999", "dob": "1999-01-01", "_dob_iso": "1999-01-01"}
        result = me.match_record(record, index())
        assert result["status"] in (me.MATCH_NONE, me.MATCH_FUZZY)
        assert result["auto_ok"] is False

    def test_no_register_imported(self):
        result = me.match_record({"name": "Aarav Sharma"}, None)
        assert result["status"] == me.MATCH_NO_REGISTER
        assert result["auto_ok"] is False

    def test_conflicting_dob_lowers_confidence(self):
        record = {"name": "Diya Patil", "beneficiary_id": "100000000103", "dob": "2020-01-01", "_dob_iso": "2020-01-01"}
        result = me.match_record(record, index())
        assert result["confidence"] < config.MATCH_EXACT_ID
        assert "dob" in result["primary"]["conflicting_fields"]


class TestHelpers:
    def test_fuzzy_similarity_order_independent(self):
        assert me.name_similarity("Sharma Aarav", "Aarav Sharma") > 0.95
        assert me.name_similarity("Aarav Sharma", "Aarav Shrma") > 0.85
        assert me.name_similarity("Aarav", "Kabir") < 0.5

    def test_titles_and_punctuation_ignored(self):
        assert me.name_similarity("Mrs. Diya Patil", "diya patil") > 0.95

    def test_apply_match_never_upgrades_a_flagged_record(self):
        record = {"name": "Diya Patil", "beneficiary_id": "100000000103", "dob": "2023-01-05",
                  "_dob_iso": "2023-01-05", "status": config.STATUS_NEEDS_REVIEW,
                  "warnings": [{"code": validation.MISSING, "field": "vaccine_name", "severity": "warn"}]}
        result = me.apply_match_to_record(record, index())
        assert result["status"] == config.STATUS_NEEDS_REVIEW

    def test_apply_match_keeps_final_status(self):
        record = {"name": "Diya Patil", "beneficiary_id": "100000000103", "status": config.STATUS_COMPLETED,
                  "warnings": [], "decision": "APPROVED"}
        result = me.apply_match_to_record(record, index())
        assert result["status"] == config.STATUS_COMPLETED

    def test_ui_summary(self):
        record = {"name": "Diya Patil", "beneficiary_id": "100000000103", "dob": "2023-01-05", "_dob_iso": "2023-01-05"}
        summary = me.summary_for_ui(me.match_record(record, index()))
        assert summary["confidence_percent"] == 100
        assert summary["tone"] == "good"
