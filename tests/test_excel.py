"""Excel reading: sniffing, header detection, mapping, extraction and statistics."""

from __future__ import annotations

from pathlib import Path

import pytest

import config
import excel_handler
import samples
import validation


@pytest.fixture()
def sample_workbook(sample_files) -> Path:
    return sample_files["beneficiaries"]


class TestSniffing:
    def test_real_xlsx_is_recognised(self, sample_workbook):
        assert excel_handler.sniff_format(sample_workbook) == "xlsx"

    def test_fake_excel_is_rejected(self, tmp_path):
        fake = tmp_path / "fake.xlsx"
        fake.write_text("this is not a spreadsheet", encoding="utf-8")
        with pytest.raises(excel_handler.ExcelError):
            excel_handler.sniff_format(fake)

    def test_missing_file_is_reported(self, tmp_path):
        with pytest.raises(excel_handler.ExcelError):
            excel_handler.sniff_format(tmp_path / "nope.xlsx")


class TestPreview:
    def test_preview_details(self, sample_workbook):
        preview = excel_handler.preview_file(sample_workbook, {"date_format": "auto"}, limit=20)
        assert preview["sheet_name"] == "Beneficiaries"
        assert preview["header_row"] == 1
        assert preview["data_rows"] == len(samples.BENEFICIARY_ROWS)
        assert len(preview["rows"]) == 20
        assert preview["columns"][0] == "Sr No"
        assert preview["mapping"]["name"] == "Name of Beneficiary"
        assert preview["date_order"] == "DD-MM-YYYY"

    def test_cover_sheet_is_skipped_automatically(self, tmp_path, sample_workbook):
        # The sample workbook has a "Read Me" sheet as well; the picker must
        # choose the data sheet, not the instructions.
        preview = excel_handler.preview_file(sample_workbook, {}, limit=5)
        assert preview["sheet_name"] == "Beneficiaries"
        assert "Beneficiaries" in preview["sheets"]

    def test_empty_sheet_is_reported(self, tmp_path):
        from openpyxl import Workbook

        path = tmp_path / "empty.xlsx"
        workbook = Workbook()
        workbook.save(path)
        with pytest.raises(excel_handler.ExcelError):
            excel_handler.preview_file(path, {})


class TestHeaderAndMapping:
    def test_header_row_detection_with_title_row(self, tmp_path):
        from openpyxl import Workbook

        path = tmp_path / "titled.xlsx"
        workbook = Workbook()
        sheet = workbook.active
        sheet.append(["U-WIN vaccination register for August"])
        sheet.append([])
        sheet.append(["Sr No", "Child Name", "Vaccine", "Dose", "Date of Vaccination"])
        sheet.append([1, "Test Child", "BCG", 1, "01-08-2025"])
        workbook.save(path)

        preview = excel_handler.preview_file(path, {}, limit=5)
        assert preview["header_row"] == 3
        assert preview["mapping"]["name"] == "Child Name"
        assert preview["mapping"]["vaccination_date"] == "Date of Vaccination"

    def test_auto_mapping_handles_unusual_headings(self):
        columns = ["S.No", "Mata ka naam", "Bachche ka naam", "Sex", "DOB", "Teeka", "Dose No.", "Date"]
        mapping = excel_handler.auto_mapping(columns)
        assert mapping["name"] == "Bachche ka naam"
        assert mapping["mother_name"] == "Mata ka naam"
        assert mapping["gender"] == "Sex"

    def test_duplicate_headers_get_unique_names(self, tmp_path):
        from openpyxl import Workbook

        path = tmp_path / "dupes.xlsx"
        workbook = Workbook()
        sheet = workbook.active
        sheet.append(["Name", "Name", "Vaccine"])
        sheet.append(["A", "B", "BCG"])
        workbook.save(path)
        preview = excel_handler.preview_file(path, {}, limit=5)
        assert len(preview["columns"]) == len(set(preview["columns"]))


class TestExtraction:
    def test_full_extraction_statistics(self, sample_workbook):
        preview = excel_handler.preview_file(sample_workbook, {})
        result = excel_handler.extract_records(sample_workbook, preview["mapping"], {"date_format": "auto"})
        stats = result["stats"]

        assert stats["imported"] == len(samples.BENEFICIARY_ROWS)
        assert stats["nameless_rows"] == 1          # row 26 has data but no name
        assert stats["duplicate_rows"] == 1          # row 9 repeats row 2
        assert stats["needs_review"] >= 3            # bad date, missing vaccine, empty name
        assert stats["date_order"] == "DD-MM-YYYY"

        # Excel row = data row + 1 (row 1 of the sheet holds the headings).
        by_row = {record["row_number"]: record for record in result["records"]}

        # The impossible date (31-02-2025) is flagged, never silently "fixed".
        bad_date = by_row[13]
        assert bad_date["status"] == config.STATUS_NEEDS_REVIEW
        assert any(item["code"] == validation.INVALID for item in bad_date["warnings"])
        # Nothing was invented: the unreadable text is kept as-is (with the raw
        # copy) so the worker sees exactly what was in the file.
        assert bad_date["vaccination_date"] == "31-02-2025"
        assert bad_date["vaccination_date_raw"] == "31-02-2025"

        # The missing vaccine stays missing.
        assert by_row[12]["vaccine_name"] == ""
        assert any(item["code"] == validation.MISSING for item in by_row[12]["warnings"])

        # The duplicate row points at the first copy of itself.
        assert by_row[10]["duplicate_of"] == 2
        assert any(item["code"] == validation.DUPLICATE for item in by_row[10]["warnings"])

        # A row with data but no name is kept and flagged, not thrown away.
        assert by_row[27]["status"] == config.STATUS_NEEDS_REVIEW
        assert any(item["field"] == "name" for item in by_row[27]["warnings"])

    def test_dates_are_normalised_to_iso(self, sample_workbook):
        preview = excel_handler.preview_file(sample_workbook, {})
        result = excel_handler.extract_records(sample_workbook, preview["mapping"], {"date_format": "auto"})
        first = result["records"][0]
        assert first["dob"] == "2023-04-12"
        assert first["dob_raw"] == "12-04-2023"
        assert first["vaccination_date"] == "2023-06-12"

    def test_month_first_file_is_detected(self, tmp_path):
        from openpyxl import Workbook

        path = tmp_path / "us.xlsx"
        workbook = Workbook()
        sheet = workbook.active
        sheet.append(["Name", "Vaccine", "Dose", "DOB", "Date of Vaccination"])
        sheet.append(["Test Child", "BCG", 1, "04/30/2023", "05/31/2023"])
        workbook.save(path)

        preview = excel_handler.preview_file(path, {})
        result = excel_handler.extract_records(path, preview["mapping"], {"date_format": "auto"})
        assert result["date_order"] == "MM/DD/YYYY"
        assert result["records"][0]["dob"] == "2023-04-30"

    def test_unmapped_name_column_stops_the_import(self, sample_workbook):
        with pytest.raises(excel_handler.ExcelError):
            excel_handler.extract_records(sample_workbook, {"vaccine_name": "Vaccine Name"}, {})


class TestRegisterImport:
    def test_register_rows_are_read(self, sample_files):
        result = excel_handler.extract_register_rows(sample_files["register"])
        assert result["count"] == len(samples.REGISTER_ROWS)
        assert result["mapping"]["beneficiary_id"] == "Beneficiary ID"
        first = result["rows"][0]
        assert first["name"] == "Aarav Sharma"
        assert first["dob"] == "2023-04-12"


class TestNativeFallback:
    """The app must keep working when pandas is not installed (small Termux setups)."""

    def test_extraction_works_without_pandas(self, monkeypatch, sample_workbook):
        import builtins

        real_import = builtins.__import__

        def blocked(name, *args, **kwargs):
            if name == "pandas" or name.startswith("pandas."):
                raise ImportError("pandas deliberately blocked for this test")
            return real_import(name, *args, **kwargs)

        monkeypatch.setattr(builtins, "__import__", blocked)

        preview = excel_handler.preview_file(sample_workbook, {})
        assert preview["backend"] in ("openpyxl", "xlrd", "csv")
        assert preview["data_rows"] == len(samples.BENEFICIARY_ROWS)

        result = excel_handler.extract_records(sample_workbook, preview["mapping"], {})
        assert result["stats"]["imported"] == len(samples.BENEFICIARY_ROWS)
        assert result["records"][0]["name"] == "Aarav Sharma"

    def test_sheet_picker_works_without_pandas(self, monkeypatch, sample_workbook):
        import builtins

        real_import = builtins.__import__

        def blocked(name, *args, **kwargs):
            if name == "pandas" or name.startswith("pandas."):
                raise ImportError("pandas deliberately blocked for this test")
            return real_import(name, *args, **kwargs)

        monkeypatch.setattr(builtins, "__import__", blocked)
        assert excel_handler.pick_best_sheet(sample_workbook) == "Beneficiaries"


class TestWriter:
    def test_write_xlsx_round_trip(self, tmp_path):
        path = tmp_path / "report.xlsx"
        excel_handler.write_xlsx(
            path,
            {
                "Completed": (["Name", "Status"], [["Aarav", "COMPLETED"], ["<script>", "PENDING"]]),
                "Bad/Sheet:Name": (["x"], [[1]]),
            },
        )
        assert path.exists()
        preview = excel_handler.preview_file(path, {}, limit=5)
        assert "Completed" in preview["sheets"]

    def test_excel_safe_escapes_formulas(self, tmp_path):
        path = tmp_path / "formula.xlsx"
        excel_handler.write_xlsx(path, {"S": (["V"], [["=1+1"]])})
        from openpyxl import load_workbook

        workbook = load_workbook(path)
        assert workbook["S"]["A2"].value == "'=1+1"
