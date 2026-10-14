# Excel Format Guide — file kaisi honi chahiye

App kisi bhi heading naam ko handle kar leta hai, lekin quality import ke liye ye rules
follow karo.

---

## 1. Minimum (itna ho to kaam chal jayega)

| Column (koi bhi naam) | Kyun zaroori |
|-----------------------|--------------|
| Beneficiary Name | Iske bina row pehchana nahi ja sakta (import ruk jayega) |
| Vaccine Name | Iske bina har record review me jayega |
| Vaccination Date | Iske bina har record review me jayega |

Baaki columns optional hain — jo milega wo use karenge, jo nahi mila usko
"missing" bata denge (guess nahi karenge).

---

## 2. Recommended columns (matching aur report ke liye behtar)

| Field | Heading ke common naam | Kya faayda |
|-------|------------------------|------------|
| Beneficiary ID | Beneficiary ID, RCH ID, U-WIN ID, Reg No | Sabse pakka match (100%) |
| Date of Birth | DOB, Date of Birth, Janm tithi | Naam + DOB = 97% match |
| Gender | Sex, Gender | Register se compare |
| Mobile | Mobile No, Contact, Phone | Naam + mobile = 92% match |
| Mother's name | Mother Name, Mata ka naam | Naam + mother = 90% match |
| Dose number | Dose, Dose No, Which dose | Report me dose |
| Batch / lot | Batch No, Lot No, Vial | Report/verification |
| Vaccinator | Vaccinator, ANM, ASHA | Report |
| Session site | Facility, Site, PHC, Sub Centre | Report |
| Remarks | Remarks, Note | Record ke saath dikhta hai |

Jo columns aap **map nahi** karte, wo bhi record ke "Other columns from your Excel file"
section me dikh jaate hain — data gum nahi hota.

---

## 3. Dummy example (copy-paste ke liye)

Filenames: `sample_data/UWIN-sample-beneficiaries.xlsx` (data) aur
`sample_data/UWIN-sample-register.xlsx` (authorised register).

### Beneficiaries sheet

| Sr No | Name of Beneficiary | Sex | Date of Birth | Mobile No | Beneficiary ID | Vaccine Name | Dose No | Date of Vaccination | Batch No | Vaccinator Name | Session Site | Remarks |
|-------|---------------------|-----|---------------|-----------|----------------|--------------|---------|---------------------|----------|-----------------|--------------|---------|
| 1 | Aarav Sharma | M | 12-04-2023 | 9876543210 | 100000000101 | Pentavalent | 1 | 12-06-2023 | PV23A17 | Sister Meena | PHC Wagholi | first dose |
| 2 | Diya Patil | F | 05-01-2023 | 9822001100 | 100000000102 | OPV | 0 | 05-01-2023 | OPV23B01 | Sister Meena | PHC Wagholi | birth dose |
| 3 | Kabir Deshmukh | M | 21-08-2022 | 9730112233 | 100000000103 | Measles Rubella | 1 | 21-08-2023 | MR22C09 | Sister Anita | PHC Wagholi | |
| 4 | Anaya Jadhav | F | 30-11-2023 | 9011223344 | 100000000104 | Pentavalent | 2 | 30-03-2024 | PV24A02 | Sister Anita | Sub Centre Kharadi | follow up |

### Register sheet (matching ke liye)

| Beneficiary ID | Beneficiary Name | Date of Birth | Gender | Mobile Number | Mother Name | Village |
|----------------|------------------|---------------|--------|---------------|-------------|---------|
| 100000000101 | Aarav Sharma | 12-04-2023 | Male | 9876543210 | Sunita Sharma | Wagholi |
| 100000000102 | Diya Patil | 05-01-2023 | Female | 9822001100 | Rekha Patil | Wagholi |

---

## 4. Sheet aur header row

* Kisi bhi sheet me data ho sakta hai — app **sabse sahi sheet khud chunta hai**
  (jo "Read Me" / cover sheet hoti hai wo skip ho jati hai).
* Headings **pehli row** me ho to best. Warna Upload page par **Header row number**
  set karo (jaise headings 3rd row me hain → `3`) aur **Reload preview** dabao.
* Headings ke upar ek title row ho (merged cell) to bhi app samajh jata hai.
* Khali rows aur khali columns app khud hatata hai aur ginti bata deta hai.

---

## 5. Date format — sabse important baat

App **guess nahi karta**, isliye:

* File me poore column me **ek hi format** rakho:
  `12-04-2023` (DD-MM-YYYY) ← India ke liye recommended
* Ye bhi chalta hai: `12/04/2023`, `12.04.2023`, `2023-04-12`, `15 Jan 2023`,
  aur Excel ka real date cell.
* App file me dekhta hai ki din 12 se bada kahin hai (jaise `21-08-2022`) →
  tab **DD-MM-YYYY** pakka ho jata hai aur sab dates wahi padhi jati hain.
* Agar file me kahin bhi 12 se bada din nahi mila (sab `03/04/2025` jaise), tab app
  **DD-MM-YYYY maan leta hai** aur aapko screen par bata deta hai:
  "Day/month order was not clear… dates were read as DD-MM-YYYY".
  Aapki file US-style (MM/DD/YYYY) ho to Settings → **Date format** badal kar
  dobara import karo.
* Jo date padhi nahi ja sakti (jaise `31-02-2025` — 31 February exist nahi karta),
  wo `NEEDS REVIEW` hoti hai aur report me asli text ke saath dikhti hai.

---

## 6. Vaccine aur dose

* Vaccine ka naam waisa hi likho jaisa official me hai: `BCG`, `OPV`, `Pentavalent`,
  `Rotavirus`, `PCV`, `Measles Rubella`, `DPT Booster`, `TT` …
  App naam badal kar "theek" nahi karta — jo likha hai wahi dikhata hai (aap Edit se sudhar sakte ho).
* Dose: `0` / `1` / `2` / `3` / `Booster` / `Birth dose`.
  `Dose 2` → `2`, `first` → `1` (sirf formatting normalise hoti hai, meaning guess nahi hota).

---

## 7. Mobile aur gender

* Mobile: 10 digit, 6/7/8/9 se shuru. `+91 98765 43210` bhi chalega (normalise ho jata hai).
* Gender: `M`, `F`, `Male`, `Female`, `Other` — sab chalte hain.

---

## 8. Duplicate aur missing rows

* **Exact duplicate** (naam + ID + DOB + vaccine + dose + date same) → app dono rows
  ko mark karta hai aur kitna duplicate hai batata hai. Aap faisla karo —
  app khud delete nahi karta.
* **Khali row** → skip + count.
* **Naam khali par baaki data hai** → row rakhi jati hai aur `NEEDS REVIEW` mark hoti hai.

---

## 9. Bahut badi file

* 500+ rows: chal jayega, thoda time lagega. Import ke waqt phone ko charging par rakho.
* File 5 MB se badi ho to Settings → Upload size limit badhao (max 25 MB),
  ya file ko mahine ke hisaab se tod do.
* Dashboard pagination (20 rows/page) use karta hai, isliye browsing slow nahi hota.

---

## 10. `.xls` (purani file) aur `.csv`

* `.xls` chalega (`xlrd` library se).
* `.csv` upload bhi hota hai. UTF-8 me save karo.
* Agar `.xls` kholne me dikkat aaye to Excel me kholke **Save As → .xlsx** karo — sabse safe.

---

## 11. Quick checklist (import se pehle)

```
[ ] Ek header row hai
[ ] Naam, Vaccine, Vaccination Date ke columns hain
[ ] Poori file me ek hi date format hai
[ ] Mobile numbers 10 digit hain (ya khali)
[ ] Dose/dose number clear likha hai (0/1/2/3/Booster)
[ ] Extra title rows ya note rows hata di hain
```
