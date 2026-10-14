"""Validation rules: dates, mobile, gender, dose and the "never guess" policy."""

from __future__ import annotations

from datetime import date, datetime

import config
import validation


class TestDateParsing:
    def test_iso_date(self):
        assert validation.parse_date("2025-12-31")["iso"] == "2025-12-31"

    def test_day_first_when_format_chosen(self):
        result = validation.parse_date("03/04/2025", "DD/MM/YYYY")
        assert result["iso"] == "2025-04-03"
        assert result["ambiguous"] is False

    def test_month_first_when_format_chosen(self):
        assert validation.parse_date("03/04/2025", "MM/DD/YYYY")["iso"] == "2025-03-04"

    def test_ambiguous_in_auto_mode(self):
        result = validation.parse_date("03/04/2025", "auto")
        assert result["kind"] == "ambiguous"
        assert result["ambiguous"] is True
        assert {option["iso"] for option in result["options"]} == {"2025-04-03", "2025-03-04"}

    def test_day_above_twelve_is_unambiguous(self):
        result = validation.parse_date("31-12-2025", "auto")
        assert result["iso"] == "2025-12-31"
        assert result["ambiguous"] is False

    def test_impossible_date_is_invalid(self):
        result = validation.parse_date("31-02-2025", "auto")
        assert result["kind"] == "invalid"
        assert result["iso"] is None

    def test_excel_serial_number(self):
        # 45658 = 2025-01-01 in the Windows/Excel epoch
        assert validation.parse_date(45658)["iso"] == "2025-01-01"

    def test_datetime_object(self):
        assert validation.parse_date(datetime(2024, 6, 15, 0, 0))["iso"] == "2024-06-15"

    def test_future_date_flagged(self):
        result = validation.parse_date("01-01-2999", "auto")
        assert result["kind"] in ("future", "invalid")

    def test_named_month(self):
        assert validation.parse_date("15 Jan 2024")["iso"] == "2024-01-15"

    def test_empty_value(self):
        result = validation.parse_date("")
        assert result["kind"] == "empty"
        assert result["iso"] is None

    def test_na_tokens(self):
        assert validation.parse_date("N/A")["kind"] == "empty"

    def test_date_to_display_respects_setting(self):
        assert validation.date_to_display("2025-12-31", "DD-MM-YYYY") == "31-12-2025"
        assert validation.date_to_display("2025-12-31", "YYYY-MM-DD") == "2025-12-31"


class TestFieldRules:
    def test_mobile_normalisation(self):
        assert validation.validate_mobile("+91 98765 43210")["digits"] == "9876543210"
        assert validation.validate_mobile("09876543210")["digits"] == "9876543210"
        assert validation.validate_mobile("987654321")["ok"] is False
        assert validation.validate_mobile("1234567890")["ok"] is False

    def test_gender_aliases(self):
        assert validation.normalise_gender("M")["value"] == "Male"
        assert validation.normalise_gender("female")["value"] == "Female"
        assert validation.normalise_gender("xyz")["error"]

    def test_dose_keeps_the_meaning_it_was_given(self):
        assert validation.normalise_dose("0")["value"] == "0"
        assert validation.normalise_dose("birth")["value"] == "Birth dose"
        assert validation.normalise_dose("Dose 2")["value"] == "2"
        assert validation.normalise_dose("booster")["value"] == "Booster"
        assert validation.normalise_dose("asdf")["error"]


class TestRecordValidation:
    def base_record(self, **overrides):
        record = {
            "name": "Aarav Sharma",
            "beneficiary_id": "100000000101",
            "dob": "2023-04-12",
            "gender": "Male",
            "mobile": "9876543210",
            "vaccine_name": "Pentavalent",
            "dose_number": "1",
            "vaccination_date": "2023-06-12",
        }
        record.update(overrides)
        return record

    def test_clean_record_needs_no_review(self):
        result = validation.validate_record_fields(self.base_record(), {"date_format": "auto"})
        assert result["needs_review"] is False
        assert result["missing"] == []

    def test_missing_vaccine_is_reported_not_invented(self):
        result = validation.validate_record_fields(self.base_record(vaccine_name=""), {"date_format": "auto"})
        assert result["needs_review"] is True
        assert "vaccine_name" in result["missing"]
        codes = [item["code"] for item in result["warnings"]]
        assert validation.MISSING in codes

    def test_missing_name(self):
        result = validation.validate_record_fields(self.base_record(name=""), {"date_format": "auto"})
        assert "name" in result["missing"]

    def test_vaccination_before_birth_is_a_conflict(self):
        result = validation.validate_record_fields(
            self.base_record(vaccination_date="2023-01-01"), {"date_format": "auto"}
        )
        codes = [item["code"] for item in result["warnings"]]
        assert validation.CONFLICT in codes

    def test_ambiguous_date_inside_a_record(self):
        result = validation.validate_record_fields(self.base_record(dob="03/04/2023"), {"date_format": "auto"})
        assert result["needs_review"] is True

    def test_broken_row_never_raises(self):
        result = validation.validate_record_fields({"name": None, "dob": object()}, {"date_format": "auto"})
        assert result["needs_review"] is True


class TestDuplicates:
    def test_identical_rows_are_clustered(self):
        records = [
            {"name": "A", "beneficiary_id": "1", "vaccine_name": "BCG", "dose_number": "1",
             "_dob_iso": "2023-01-01", "_vax_iso": "2023-02-01"},
            {"name": "A", "beneficiary_id": "1", "vaccine_name": "BCG", "dose_number": "1",
             "_dob_iso": "2023-01-01", "_vax_iso": "2023-02-01"},
            {"name": "B", "beneficiary_id": "2", "vaccine_name": "BCG", "dose_number": "1",
             "_dob_iso": "2023-01-01", "_vax_iso": "2023-02-01"},
        ]
        clusters = validation.duplicate_clusters(records)
        assert len(clusters) == 1
        assert sorted(next(iter(clusters.values()))) == [0, 1]

    def test_same_name_different_child_is_not_a_duplicate(self):
        records = [
            {"name": "Aarav Sharma", "beneficiary_id": "1", "vaccine_name": "BCG", "dose_number": "1",
             "_dob_iso": "2023-01-01", "_vax_iso": "2023-02-01"},
            {"name": "Aarav Sharma", "beneficiary_id": "2", "vaccine_name": "BCG", "dose_number": "1",
             "_dob_iso": "2022-05-05", "_vax_iso": "2023-02-01"},
        ]
        assert validation.duplicate_clusters(records) == {}


class TestMappingValidation:
    def test_missing_required_column_is_reported(self):
        problems = validation.validate_mapping({"name": "Name"}, ["Name", "Vaccine"])
        fields = [item["field"] for item in problems]
        assert "vaccine_name" in fields
        assert "vaccination_date" in fields

    def test_duplicate_column_use_is_reported(self):
        problems = validation.validate_mapping({"name": "Col A", "vaccine_name": "Col A"}, ["Col A"])
        assert any(item["code"] == validation.CONFLICT for item in problems)

    def test_unknown_column_is_reported(self):
        problems = validation.validate_mapping({"name": "Nope"}, ["Name"])
        assert any(item["code"] == validation.INVALID for item in problems)
