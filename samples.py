"""
samples.py
==========
Creates the example Excel files that ship with the project (and that you can
download from the dashboard's Upload page):

* ``sample_data/UWIN-sample-beneficiaries.xlsx`` - a realistic vaccination
  workbook with deliberately *non-standard* column headings, so you can see how
  the column-mapping screen works. It also contains the kind of problems real
  files have: an empty vaccine cell, an impossible date, a duplicated row, an
  unclear day/month date.
* ``sample_data/UWIN-sample-register.xlsx`` - an example of the **authorised
  beneficiary register** you may import locally to power the matching engine.
  It is fake data. Never put real patient data in a sample file.

Both files are regenerated automatically when missing, so ``setup.sh`` and the
dashboard always have something to demonstrate with.
"""

from __future__ import annotations

from pathlib import Path
from typing import Optional

import config
import excel_handler

BENEFICIARY_FILE = config.SAMPLE_DATA_DIR / "UWIN-sample-beneficiaries.xlsx"
REGISTER_FILE = config.SAMPLE_DATA_DIR / "UWIN-sample-register.xlsx"

#: Column names are intentionally "messy" - real files are like this.
BENEFICIARY_HEADERS = [
    "Sr No", "Name of Beneficiary", "Sex", "Date of Birth", "Mobile No",
    "Beneficiary ID", "Vaccine Name", "Dose No", "Date of Vaccination",
    "Batch No", "Vaccinator Name", "Session Site", "Remarks",
]

# (row_number, name, sex, dob, mobile, beneficiary_id, vaccine, dose, vax_date, batch, vaccinator, site, remarks)
BENEFICIARY_ROWS: list[list] = [
    [1, "Aarav Sharma", "M", "12-04-2023", "9876543210", "100000000101", "Pentavalent", "1", "12-06-2023", "PV23A17", "Sister Meena", "PHC Wagholi", "First dose given"],
    [2, "Diya Patil", "F", "05-01-2023", "9822001100", "100000000102", "OPV", "0", "05-01-2023", "OPV23B01", "Sister Meena", "PHC Wagholi", "Birth dose"],
    [3, "Kabir Deshmukh", "M", "21-08-2022", "9730112233", "100000000103", "Measles Rubella", "1", "21-08-2023", "MR22C09", "Sister Anita", "PHC Wagholi", ""],
    [4, "Anaya Jadhav", "F", "30-11-2023", "9011223344", "100000000104", "Pentavalent", "2", "30-03-2024", "PV24A02", "Sister Anita", "Sub Centre Kharadi", "Follow up done"],
    [5, "Vivaan Kulkarni", "M", "14-02-2023", "8900112233", "100000000105", "Rotavirus", "1", "14-04-2023", "RV23A11", "Sister Meena", "PHC Wagholi", ""],
    [6, "Isha Pawar", "F", "09-06-2023", "9812345678", "", "Pentavalent", "1", "09-08-2023", "PV23A19", "Sister Sharda", "PHC Wagholi", "Beneficiary ID not in file"],
    [7, "Reyansh More", "M", "17-09-2022", "9765009988", "100000000107", "OPV", "3", "17-03-2023", "OPV23B07", "Sister Sharda", "Sub Centre Kharadi", ""],
    [8, "Saanvi Bhosale", "F", "02-12-2023", "9167007788", "100000000108", "PCV", "2", "02-04-2024", "PCV24A05", "Sister Meena", "PHC Wagholi", ""],
    [9, "Aarav Sharma", "M", "12-04-2023", "9876543210", "100000000101", "Pentavalent", "1", "12-06-2023", "PV23A17", "Sister Meena", "PHC Wagholi", "DUPLICATE OF ROW 2 - please check"],
    [10, "Myra Gaikwad", "F", "25-07-2023", "", "100000000110", "Pentavalent", "1", "25-09-2023", "PV23A21", "Sister Anita", "PHC Wagholi", "Mobile missing"],
    [11, "Atharv Chavan", "M", "11-05-2022", "9456001122", "100000000111", "", "2", "11-08-2022", "", "Sister Sharda", "PHC Wagholi", "Vaccine name missing - needs review"],
    [12, "Kiara Shinde", "F", "07-03-2023", "9345011100", "100000000112", "Pentavalent", "1", "31-02-2025", "PV25A01", "Sister Meena", "Sub Centre Kharadi", "Impossible date - needs review"],
    [13, "Advait Salunkhe", "M", "19-10-2022", "9225001133", "100000000113", "Measles Rubella", "2", "19-10-2023", "MR23C12", "Sister Anita", "PHC Wagholi", ""],
    [14, "Ira Nair", "F", "03/04/2023", "9112003344", "100000000114", "OPV", "1", "03/06/2023", "OPV23B09", "Sister Meena", "PHC Wagholi", "Ambiguous date format"],
    [15, "Shaurya Kale", "M", "28-01-2023", "9887004455", "100000000115", "Rotavirus", "2", "28-03-2023", "RV23A14", "Sister Sharda", "Sub Centre Kharadi", ""],
    [16, "Zara Sheikh", "F", "16-06-2023", "9778005566", "100000000116", "PCV", "1", "16-08-2023", "PCV23A03", "Sister Anita", "PHC Wagholi", ""],
    [17, "Ishaan Kadam", "M", "22-12-2022", "9669006677", "100000000117", "Pentavalent", "3", "22-04-2023", "PV23A24", "Sister Meena", "PHC Wagholi", ""],
    [18, "Riya Sawant", "F", "08-09-2023", "9550007788", "100000000118", "OPV", "2", "08-11-2023", "OPV23B12", "Sister Sharda", "Sub Centre Kharadi", ""],
    [19, "Arjun Phadtare", "M", "13-03-2023", "9441008899", "100000000119", "Pentavalent", "2", "13-05-2023", "PV23A26", "Sister Anita", "PHC Wagholi", ""],
    [20, "Siya Mane", "F", "27-09-2023", "9332009900", "100000000120", "Measles Rubella", "1", "27-09-2024", "MR24C03", "Sister Meena", "PHC Wagholi", "Nine month dose"],
    [21, "Dhruv Wagh", "M", "04-02-2023", "9223001010", "100000000121", "Rotavirus", "3", "04-06-2023", "RV23A18", "Sister Sharda", "Sub Centre Kharadi", ""],
    [22, "Mahi Solanki", "F", "18-11-2022", "9114001122", "100000000122", "PCV", "2", "18-05-2023", "PCV23A07", "Sister Anita", "PHC Wagholi", ""],
    [23, "Rudra Jadhav", "M", "06-07-2023", "9005002233", "100000000123", "OPV", "1", "06-09-2023", "OPV23B15", "Sister Meena", "PHC Wagholi", ""],
    [24, "Aarohi Thakur", "F", "29-04-2023", "9896003344", "", "Pentavalent", "1", "29-06-2023", "PV23A29", "Sister Sharda", "Sub Centre Kharadi", "Only name + DOB available"],
    [25, "Kavya Pardeshi", "F", "10-08-2023", "9787004455", "100000000125", "Rotavirus", "2", "10-10-2023", "RV23A21", "Sister Anita", "PHC Wagholi", ""],
    [26, "", "M", "", "9999000011", "", "OPV", "", "", "", "", "", "Empty name row - must be flagged"],
    [27, "Yash Bhagat", "M", "23-05-2023", "9870011223", "100000000127", "PCV", "1", "23-07-2023", "PCV23A09", "Sister Meena", "PHC Wagholi", ""],
]

REGISTER_HEADERS = [
    "Beneficiary ID", "Beneficiary Name", "Date of Birth", "Gender",
    "Mobile Number", "Mother Name", "Village",
]

# Intentionally includes: exact ID matches, name+DOB matches, one fuzzy-only
# name (spelling difference) and two records that do not exist in the upload.
REGISTER_ROWS: list[list] = [
    ["100000000101", "Aarav Sharma", "12-04-2023", "Male", "9876543210", "Sunita Sharma", "Wagholi"],
    ["100000000102", "Diya Patil", "05-01-2023", "Female", "9822001100", "Rekha Patil", "Wagholi"],
    ["100000000103", "Kabir Deshmukh", "21-08-2022", "Male", "9730112233", "Vaishali Deshmukh", "Kharadi"],
    ["100000000104", "Anaya Jadhav", "30-11-2023", "Female", "9011223344", "Pooja Jadhav", "Kharadi"],
    ["100000000105", "Vivaan Kulkarni", "14-02-2023", "Male", "8900112233", "Sneha Kulkarni", "Wagholi"],
    ["100000000126", "Isha Pawar", "09-06-2023", "Female", "9812345678", "Manisha Pawar", "Wagholi"],
    ["100000000107", "Reyansh More", "17-09-2022", "Male", "9765009988", "Kavita More", "Kharadi"],
    ["100000000108", "Saanvi Bhosale", "02-12-2023", "Female", "9167007788", "Archana Bhosale", "Wagholi"],
    ["100000000110", "Myra Gaikwad", "25-07-2023", "Female", "9700110022", "Sushma Gaikwad", "Wagholi"],
    ["100000000111", "Atharv Chavan", "11-05-2022", "Male", "9456001122", "Nanda Chavan", "Wagholi"],
    ["100000000112", "Kiara Shinde", "07-03-2023", "Female", "9345011100", "Maya Shinde", "Kharadi"],
    ["100000000113", "Advait Salunkhe", "19-10-2022", "Male", "9225001133", "Shalini Salunkhe", "Wagholi"],
    ["100000000114", "Ira Nair", "03-04-2023", "Female", "9112003344", "Latha Nair", "Wagholi"],
    ["100000000115", "Shaurya Kale", "28-01-2023", "Male", "9887004455", "Smita Kale", "Kharadi"],
    ["100000000116", "Zara Sheikh", "16-06-2023", "Female", "9778005566", "Nazia Sheikh", "Wagholi"],
    ["100000000117", "Ishaan Kadam", "22-12-2022", "Male", "9669006677", "Reshma Kadam", "Wagholi"],
    ["100000000118", "Riya Sawant", "08-09-2023", "Female", "9550007788", "Asha Sawant", "Kharadi"],
    ["100000000119", "Arjun Fadtare", "13-03-2023", "Male", "9441008899", "Vandana Phadtare", "Wagholi"],
    ["100000000120", "Siya Mane", "27-09-2023", "Female", "9332009900", "Jyoti Mane", "Wagholi"],
    ["100000000121", "Dhruv Wagh", "04-02-2023", "Male", "9223001010", "Sangita Wagh", "Kharadi"],
    ["100000000122", "Mahi Solanki", "18-11-2022", "Female", "9114001122", "Bhavna Solanki", "Wagholi"],
    ["100000000123", "Rudra Jadhav", "06-07-2023", "Male", "9005002233", "Pallavi Jadhav", "Wagholi"],
    ["100000000128", "Aarohi Thakur", "29-04-2023", "Female", "9896003344", "Kiran Thakur", "Kharadi"],
    ["100000000125", "Kavya Pardeshi", "10-08-2023", "Female", "9787004455", "Meenal Pardeshi", "Wagholi"],
    ["100000000127", "Yash Bhagat", "23-05-2023", "Male", "9870011223", "Seema Bhagat", "Wagholi"],
    ["100000000129", "Tanvi Rane", "01-01-2023", "Female", "9090001122", "Neha Rane", "Wagholi"],
    ["100000000130", "Omkar Shelar", "15-03-2022", "Male", "9192003344", "Ujwala Shelar", "Kharadi"],
]


def create_beneficiary_sample(path: Optional[Path] = None) -> Path:
    """Write the example vaccination workbook and return its path."""
    path = path or BENEFICIARY_FILE
    path.parent.mkdir(parents=True, exist_ok=True)

    instructions = [
        ["U-WIN Smart Assistant - EXAMPLE vaccination file"],
        ["This file is only an example. Replace it with your own authorised Excel export."],
        [],
        ["What to notice:"],
        ["1. Column headings are not standard - the app detects them and lets you map them yourself."],
        ["2. Row 12 has an impossible date (31-02-2025) - it is flagged as NEEDS REVIEW."],
        ["3. Row 11 has an empty vaccine name - it is never guessed, only flagged."],
        ["4. Row 9 is an exact duplicate of row 2 - marked as a duplicate for your decision."],
        ["5. Row 14 uses 03/04/2023 - day/month is unclear, so the app asks you to check."],
        ["6. Row 26 has an empty name - it is flagged and never auto-filled."],
        [],
        ["Never paste real patient data into a template that you share with anyone."],
    ]

    excel_handler.write_xlsx(
        path,
        {
            "Beneficiaries": (BENEFICIARY_HEADERS, BENEFICIARY_ROWS),
            "Read Me": (["Instruction"], instructions),
        },
    )
    return path


def create_register_sample(path: Optional[Path] = None) -> Path:
    """Write the example authorised-register workbook and return its path."""
    path = path or REGISTER_FILE
    path.parent.mkdir(parents=True, exist_ok=True)

    notes = [
        ["Example authorised beneficiary register (FAKE DATA)"],
        ["Import this from Settings -> Beneficiary Register, or from the Upload page."],
        ["The matching engine uses it locally on this device only."],
        ["Rows here intentionally include spelling differences (Arjun Fadtare / Phadtare) to show"],
        ["that fuzzy name matching is only ever a suggestion."],
    ]

    excel_handler.write_xlsx(
        path,
        {
            "Register": (REGISTER_HEADERS, REGISTER_ROWS),
            "Read Me": (["Note"], notes),
        },
    )
    return path


def ensure_sample_files(force: bool = False) -> dict:
    """Create the sample files if they are missing (or when ``force`` is set)."""
    config.ensure_dirs()
    result = {"beneficiaries": BENEFICIARY_FILE, "register": REGISTER_FILE}
    try:
        if force or not BENEFICIARY_FILE.exists():
            result["beneficiaries"] = create_beneficiary_sample()
    except Exception:
        pass
    try:
        if force or not REGISTER_FILE.exists():
            result["register"] = create_register_sample()
    except Exception:
        pass
    return result


def sample_summary() -> dict:
    """Small description used by the Help page."""
    return {
        "beneficiary_file": BENEFICIARY_FILE.name,
        "beneficiary_rows": len(BENEFICIARY_ROWS),
        "beneficiary_columns": BENEFICIARY_HEADERS,
        "register_file": REGISTER_FILE.name,
        "register_rows": len(REGISTER_ROWS),
        "register_columns": REGISTER_HEADERS,
        "problems_shown": [
            "Empty vaccine name (row 11)",
            "Impossible date 31-02-2025 (row 12)",
            "Exact duplicate row (row 9)",
            "Ambiguous date 03/04/2023 (row 14)",
            "Empty beneficiary name (row 26)",
            "Beneficiary ID missing but present in register (row 6)",
        ],
    }
