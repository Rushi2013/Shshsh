# U-WIN SMART ASSISTANT
### Vaccination Data Management & Review System
**Android (Termux) + Python + local web dashboard — sirf aapke phone par chalta hai.**

Ye app aapki **Excel file** ko padhta hai, har beneficiary row ko **local rules se check**
karta hai, authorised register se **match** karta hai, aur aapko ek **professional dashboard**
me ek-ek record review karne deta hai. Portal par entry aap khud karte ho, aur **portal
confirm kare bina app kabhi "COMPLETED" nahi likhta**.

> ⚠️ **Zaroori:** Ye ek *assistant* hai, autopilot nahi. Ye U-WIN portal ke OTP, CAPTCHA ya
> login ko bypass nahi karta, kisi private/undocumented endpoint ko call nahi karta, aur
> bina aapki approval ke kuch bhi save nahi karta. Detail: `docs/UWIN_INTEGRATION.md`.

---

## 1. Ye app kya karta hai (1 minute me samjho)

| Step | Kaam | Kahan hota hai |
|------|------|----------------|
| 1 | Excel file upload (.xlsx / .xls) | Aapke phone par (local) |
| 2 | Column mapping (headings kuch bhi ho) | Dashboard |
| 3 | Validation — galat date, khali naam, duplicate, missing value | Phone par hi |
| 4 | Matching — Exact ID → Naam+DOB → Naam+field → fuzzy (sirf suggestion) | Phone par hi |
| 5 | Review — Approve / Edit / Reject / Skip, ek-ek record | Dashboard |
| 6 | Portal — aap khud official page par entry karte ho, OTP aap dalte ho | Chrome |
| 7 | Confirmation — portal ka reference paste karo → record COMPLETED | Dashboard |
| 8 | Report — Excel report (Completed / Pending / Needs Review / Failed / Skipped) | Phone par hi |

**Kabhi nahi karta:** missing DOB/vaccine/date guess karna, SMS padhna, OTP save karna,
password kahin likhna, unknown save ko dobara bhejna.

---

## 2. Folder structure

```
uwin-smart-assistant/
├── app.py                  # Flask dashboard (pages + JSON API)
├── config.py               # setting, path, portal URL, thresholds
├── database.py             # SQLite storage (sessions, records, register, events)
├── excel_handler.py        # Excel padhna (pandas / openpyxl / xlrd), mapping, date order
├── validation.py           # date/phone/dose rules + "kabhi guess nahi" policy
├── matching_engine.py      # ID → naam+DOB → naam+field → fuzzy matching
├── progress_manager.py     # queue, pause/resume/stop, decisions, recovery
├── report_generator.py     # Excel/CSV reports (multi-sheet)
├── uwin_integration.py     # portal login assistant + approved-API adapter + safety
├── samples.py              # example Excel files banata hai
├── setup.sh                # ek-baar ka installation (Termux)
├── start.sh                # dashboard chalu (Chrome bhi khol dega)
├── stop.sh                 # dashboard band
├── requirements.txt        # Python libraries
├── templates/              # HTML (base, dashboard, upload, records, review, history, reports, settings, portal, help, lock, error)
├── static/css/style.css    # poora design system (light + dark, mobile first)
├── static/js/              # app.js, upload.js, dashboard.js, review.js, settings.js, portal.js
├── static/img/favicon.svg
├── uploads/                # aapki uploaded Excel files (local)
├── reports/                # banayi hui reports (local)
├── logs/                   # console log + pid file
├── data/                   # uwin_assistant.db (SQLite) + .secret_key
├── secrets/                # approved-API token (agar aapne set kiya ho)
├── sample_data/            # example beneficiaries + register Excel
├── docs/                   # poori guide (install, user guide, security, troubleshooting)
└── tests/                  # 91 automated tests (pytest)
```

---

## 3. Installation (Android + Termux, ek baar ka kaam)

```bash
# 1) Termux kholo, phir:
pkg update && pkg upgrade -y
pkg install python git -y

# 2) Project folder me jao (jahan aapne files rakhi hain)
cd ~/uwin-smart-assistant

# 3) Setup chalao (packages + libraries + folders + example file + check)
bash setup.sh
```

Setup ke baad:

```bash
# 4) Dashboard chalu karo
bash start.sh          # server chalu + Chrome khul jayega

# 5) Chrome me (agar khud kholna ho)
#    http://127.0.0.1:5000
```

Band karna ho:

```bash
bash stop.sh
```

Poori step-by-step guide (screenshots-style explanation, Hinglish): **`docs/INSTALL_TERMUX.md`**

---

## 4. Excel file ka format (dummy example)

Sirf ek header row chahiye. Heading ke naam kuch bhi ho sakte hain — app khud detect karta hai
aur aap mapping screen par badal sakte ho.

| Sr No | Name of Beneficiary | Sex | Date of Birth | Mobile No | Beneficiary ID | Vaccine Name | Dose No | Date of Vaccination | Batch No | Vaccinator Name | Session Site | Remarks |
|-------|---------------------|-----|---------------|-----------|----------------|--------------|---------|---------------------|----------|-----------------|--------------|---------|
| 1 | Aarav Sharma | M | 12-04-2023 | 9876543210 | 100000000101 | Pentavalent | 1 | 12-06-2023 | PV23A17 | Sister Meena | PHC Wagholi | first dose |
| 2 | Diya Patil | F | 05-01-2023 | 9822001100 | 100000000102 | OPV | 0 | 05-01-2023 | OPV23B01 | Sister Meena | PHC Wagholi | birth dose |

Zaroori columns: **Beneficiary Name**, **Vaccine Name**, **Vaccination Date**
(baaki optional — jo na ho, app "missing" batata hai, guess nahi karta).

Example file dashboard se download karo: **Upload Excel → Download example file**
ya `sample_data/UWIN-sample-beneficiaries.xlsx` kholo. Us example me jaan-boojh kar
galtiyan daali gayi hain (31-02-2025 jaisi impossible date, khali vaccine, duplicate row)
taaki aap dekh sako app kaise warn karta hai.

Detail: **`docs/EXCEL_FORMAT.md`**

---

## 5. Roz ka kaam (Hinglish)

1. **Termux** kholo → `cd ~/uwin-smart-assistant` → `bash start.sh`
2. **Chrome** me `http://127.0.0.1:5000` kholo (script khud bhi khol deta hai).
3. **Upload Excel** → file chuno → preview dekho → headings ka mapping check karo → **Import & check records**.
4. **Review Queue** → ek-ek record: **Approve / Edit / Reject / Skip**, phir **Next beneficiary**.
5. **U-WIN Portal Login** page → *Open U-WIN login page* → mobile number + password + **SMS OTP aap khud dalo**.
6. Record approve karne ke baad → **Prepare portal save** → steps follow karo → portal me entry karo →
   **"I confirmed it on the portal"** → portal ka reference number paste karo → record **COMPLETED**.
7. Din ke end me: **Reports → Create Excel report** → supervisor ko bhejo → `bash stop.sh`.

Sab kuch detail me: **`docs/USER_GUIDE.md`**

---

## 6. Status ka matlab

| Status | Matlab |
|--------|--------|
| `PENDING` | Row saaf hai, aapne faisla nahi kiya |
| `MATCHED` | Authorised register me pakka match (ID ya naam+DOB) |
| `NEEDS REVIEW` | Kuch missing / conflicting / match pakka nahi — aapko dekhna hai |
| `APPROVED` | Aapne sahi bola, portal par save karna baaki |
| `COMPLETED` | Portal ne confirm kiya (reference number ke saath) |
| `SKIPPED` | Abhi chhoda, baad me khol sakte ho |
| `FAILED` | Reject kiya ya processing fail hui |
| `SAVE STATUS UNKNOWN` | Portal ne confirm nahi kiya → **khud check karo, automatic dobara nahi jayega** |

---

## 7. Safety rules (code me hi lagu)

* **No guessing:** khali cell bhara nahi jata; galat date ko "theek" karne ki koshish nahi hoti.
* **No auto-approve:** fuzzy naam match kabhi automatic approve nahi hota.
* **No duplicate risk:** save se pehle SQLite me marker likha jata hai; crash hone par
  record `SAVE STATUS UNKNOWN` banta hai aur **dobara automatic nahi bhejta**.
* **No secrets on disk:** password/OTP kabhi database, log, report ya Excel me nahi.
* **No cloud:** koi analytics nahi, koi third-party upload nahi, server sirf `127.0.0.1` par.
* **Delete anytime:** Settings → Privacy → *Delete ALL local data*.

Detail: **`docs/SECURITY_PRIVACY.md`**

---

## 8. U-WIN portal integration — sach kya hai

* Default mode = **Assisted**: app official page browser me kholta hai, data screen par dikhata hai,
  aap entry karte ho, aap confirm karte ho.
* **Approved API** (agar department/NIC deta hai) to `Settings → U-WIN portal workflow` me
  URL + token daal ke adapter chalu kar sakte ho. Contract aur exact JSON documented hai.
* Automation sirf usi case me hota hai jab portal **officially** allow kare.
  OTP hamesha aap daloge — app OTP nahi padhta, nahi bharta, nahi save karta.

Iske aage kya karna hoga, kis se permission leni hogi: **`docs/UWIN_INTEGRATION.md`**

---

## 9. Testing aur maintenance

```bash
python app.py --check       # installation check
python -m pytest -q         # 91 automated tests (91 passed)
python app.py --cleanup     # purani files saaf
bash stop.sh --purge        # band karke unused upload files delete
```

---

## 10. Documentation index

| File | Kya hai |
|------|---------|
| `docs/INSTALL_TERMUX.md` | Termux installation, step by step (bilkul beginner ke liye) |
| `docs/USER_GUIDE.md` | Roz ka workflow, review steps, report banana |
| `docs/EXCEL_FORMAT.md` | Excel ka structure, column mapping, date formats |
| `docs/UWIN_INTEGRATION.md` | Portal integration: permission, assisted workflow, approved API |
| `docs/SECURITY_PRIVACY.md` | Data security, credential handling, compliance limits |
| `docs/TROUBLESHOOTING.md` | Problem → solution (Hinglish) |
| `docs/ARCHITECTURE.md` | Modules, database schema, status machine, API list |

---

**Bana hai healthcare workers ke liye.** Patient data sirf authorised kaam ke liye use karo,
kaam khatam hone par delete kar do, aur phone me PIN/screen lock rakho.
