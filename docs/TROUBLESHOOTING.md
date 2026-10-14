# Troubleshooting — problem aur solution (Hinglish)

Sabse aam problems yahan hain. Pehle ye chalane ki koshish karo:

```bash
cd ~/uwin-smart-assistant
python app.py --check
```

Ye batayega kaunsi cheez missing hai.

---

## A. Installation / Termux

### "command not found: python"
```bash
pkg install python -y
python --version
```

### "No module named flask" / "No module named openpyxl"
```bash
pip install --upgrade pip
pip install -r requirements.txt
# ya ek-ek:
pip install Flask Werkzeug openpyxl xlrd
```

### pandas install nahi ho raha (`Failed building wheel for pandas`)
Koi dikkat nahi — app pandas ke bina chal jata hai (openpyxl/xlrd engine use karta hai).
Try kar sakte ho:
```bash
pkg install python-numpy -y
pip install --no-cache-dir pandas
```
Check: `python app.py --check` → pandas me "not installed" likha ho to bhi chalega.

### "bash: setup.sh: No such file or directory"
Aap galat folder me ho. `cd ~/uwin-smart-assistant` karo, phir `ls` — `setup.sh` dikhna chahiye.

### "Permission denied: setup.sh"
```bash
bash setup.sh        # chmod ki zaroorat nahi, bash se chalao
```

### Storage permission (files padhne ke liye)
```bash
termux-setup-storage     # Allow dabao
ls ~/storage/downloads   # aapki Downloads dikhni chahiye
```

---

## B. Dashboard nahi khul raha

### Chrome me "site can't be reached"
1. Termux me server chal raha hai? Wahan `Running on http://127.0.0.1:5000` dikhna chahiye.
   ```bash
   bash start.sh --foreground     # logs screen par dikhenge
   ```
2. Chrome me **exactly** likho: `http://127.0.0.1:5000` (`https` nahi, `http`).
3. Termux background me kill ho gaya? `bash start.sh` dubara.

### "Address already in use" / port busy
```bash
UWIN_PORT=5001 bash start.sh
# Chrome: http://127.0.0.1:5001
```
Ya purana process band karo: `bash stop.sh` phir `bash start.sh`.

### Chrome khud nahi khulta
```bash
pkg install termux-api -y        # termux:api app bhi install karo (F-Droid)
termux-open-url http://127.0.0.1:5000
```
Nahi chala to Chrome kholke link paste karo.

### Dashboard "locked" dikha raha hai
PIN bhool gaye? `data/uwin_assistant.db` ke `settings` table se `pin_lock_enabled` ko
`false` karna padega (advanced), warna app data folder delete karna padega:
```bash
bash stop.sh
mv data data-backup
bash start.sh        # naya data folder banega (purana data-backup me safe hai)
```

---

## C. Excel upload / import

### "This file is not a real Excel file"
File ka content .xlsx jaisa nahi hai (naam badal diya gaya ho sakta hai).
Excel me kholo → **Save As → Excel Workbook (.xlsx)** → dobara upload.

### "The file is bigger than the X MB limit"
Settings → **Upload size limit** 25 tak badhao, ya file ko tod do. Bade file phone par dheeme hain.

### "The Beneficiary Name column is not mapped"
1. Upload page → **Map Excel columns** → *Beneficiary Name* ke dropdown me sahi column chuno.
2. **Import & check records** dabao.

### "No column headings were found"
File me heading row nahi hai, ya pehli row title hai.
Upload page → **Header row number** sahi do (jaise `3`) → **Reload preview**.

### Galat sheet padhi gayi (jaise "Read Me")
Upload page → **Sheet** dropdown se sahi sheet chuno → **Reload preview**.
(App pehle khud best sheet chunta hai; purani file me sheet ka naam different ho sakta hai.)

### "This session already has N imported record(s)" (error 409)
Aap wahi file dobara import kar rahe ho. Confirm dialog aayega:
* **Import again** → purane records aur unke decisions hat jayenge (report pehle bana lo).

### Dates galat aa rahi hain (din-mahina ulta)
* Settings → **Date format** = `DD-MM-YYYY` (India) set karo → file dobara import karo.
* Ya CSV/Excel me dates ko `DD-MM-YYYY` me likh do.
* App khud bata deta hai: "Dates were read as DD-MM-YYYY (…)".

### "31-02-2025 is not a valid date" jaisi warning
App us date ko theek nahi karta (guess nahi karta). Record **Needs review** me hai:
record kholo → **Edit** → sahi date daalo (jaise `28-02-2025`) → **Save changes**.

### Import bahut slow (500+ rows)
* Phone charging par rakho, screen on rakho.
* Termux battery unrestricted karo.
* File ko 2–3 hisson me todo.
* `pandas` install karo (tez hota hai): `pip install pandas`.

---

## D. Matching

### Sab records "Needs review" aa rahe hain
Possible reasons:
1. **Register import nahi hua** → dashboard → *Authorised beneficiary register* import karo.
2. Register me naam ki spelling alag hai → fuzzy suggestion milega, par auto-approve nahi hoga (by design).
3. `Vaccine Name` / `Vaccination Date` map nahi hui → har row review me jayegi.
4. DOB ka format galat → Settings → Date format set karo.

### "Ambiguous — several possible beneficiaries"
Register me ek jaisa naam waale do log hain. Screen par **sabhi candidates** dikhte hain.
Portal par khud verify karke sahi insaan chuno. (App galat insaan select nahi karega.)

### Match % 92% hai par MATCHED nahi hua
Ye jaan-boojh kar hai: 96% se neeche confidence auto-approve nahi hota.
Aap record dekh kar **Approve** kar sakte ho (aapka faisla, app ka nahi).

---

## E. Portal / login

### Login page nahi khul raha
```bash
termux-open-url https://uwinvaccinator.mohfw.gov.in/auth/login
```
Ya Chrome me yahi link paste karo. App sirf official host open karta hai.

### "OTP Required – Enter the SMS OTP" — ab?
OTP **aap** official page me type karo. App SMS nahi padhta, OTP save nahi karta
(ye safety feature hai, bug nahi).

### OTP nahi aa raha
* Mobile number U-WIN me registered hai?
* Network signal check karo (Wi-Fi calling off karke try karo).
* Thoda intezaar (60s) → **Resend** portal par.
* Baar-baar na aaye to U-WIN helpline / apne block se contact karo.

### "Login needed" / "Portal session expired"
Session khatam ho gaya → **U-WIN Portal Login** page → dubara login → phir
**"I am logged in — continue"**.

### "Prepare portal save" dabaya, screen par steps aa gaye, kuch portal me nahi bheja
Ye **assisted mode** hai (default). App aapki jagah portal par entry nahi karta.
Steps follow karo → portal me entry karo → **"I confirmed it on the portal"** → reference paste karo.

### "SAVE STATUS UNKNOWN"
Portal ne confirmation nahi di (timeout, network, ya page band ho gaya).
1. Portal khud kholo → beneficiary search karo → dekho entry ho gayi kya.
2. **Ho gayi:** record kholo → **"I confirmed it on the portal"** → reference paste karo → COMPLETED.
3. **Nahi hui:** record kholo → dobara edit/verify karke save karo.
   (App dobara automatic nahi bhejta — isi se duplicate bachta hai.)

---

## F. Queue / processing

### "Processing is paused" / "stopped"
Dashboard → **Queue controls** → **Resume** dabao.

### Record edit karne ke baad bhi "Needs review"
Kuch aur missing/conflicting hai (jaise mobile galat, DOB missing). Warning list padho —
har warning ke saath exact field aur hint diya hota hai.

### Termux band ho gaya, kaam gaya?
Nahi gaya. `bash start.sh` → **Resume previous session**.
Har decision SQLite me save hota hai. Jo save confirm nahi hua tha wo
`SAVE STATUS UNKNOWN` me milega — usko khud check karo.

---

## G. Reports

### Report download nahi ho raha
* Reports page → list me file ke saamne **Download** dabao.
* "file missing" likha ho to **Delete** karke naya report banao (purani file manually delete ho gayi thi).

### Report khulti nahi (Excel app)
`.csv` banao (Settings → Report format = CSV) — koi bhi phone CSV khol leta hai.
Ya Google Sheets me import karo.

### Report me password/OTP dikh raha hai?
Aisa nahi ho sakta — app in values ko store hi nahi karta. Report ki column list
`report_generator.REPORT_COLUMNS` me hard-coded hai. Kuch suspicious dikhe to
`docs/SECURITY_PRIVACY.md` ke verify commands chalao.

---

## H. Performance / phone

### Phone garam / slow ho raha hai
* Settings → **Rows per page** = 10–20.
* Bade file ko todo.
* History → **Cleanup now** (purani files hat jayengi).
* Chrome me sirf ek dashboard tab rakho.

### App "hang" lag raha hai
Import ke waqt busy overlay aata hai — wait karo. 500 rows me 10–60 second normal hai.

---

## I. Data delete

### Sirf ek file ka data hatana
History → us file ke saamne **Delete**.

### Sab kuch hatana
Settings → Privacy → **Delete ALL local data** → confirm me `DELETE` likho.

### Terminal se
```bash
bash stop.sh --purge
python app.py --cleanup
```

---

## J. Kuch aur samajh nahi aaya

1. `python app.py --check` chalao aur output dekho.
2. Dashboard → **Help & Troubleshooting** page (same content mobile-friendly form me).
3. `logs/console.log` dekho (background mode me):
   ```bash
   tail -50 ~/uwin-smart-assistant/logs/console.log
   ```
4. Tests chalao — kuch toota hua ho to pata chal jayega:
   ```bash
   python -m pytest -q
   ```
5. Server dubara start karo (sabse aasan "fix"):
   ```bash
   bash stop.sh && bash start.sh
   ```
