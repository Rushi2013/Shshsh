# User Guide — roz ka kaam kaise karein

Ye guide maan kar likhi gayi hai ki aapko Python nahi aata. Sirf dashboard ke buttons
dabane hain. Har step me "kya dikhega" aur "kya karna hai" likha hai.

---

## 0. Start karne ka routine (30 second)

```bash
cd ~/uwin-smart-assistant
bash start.sh
```

Chrome me **http://127.0.0.1:5000** khulega. Kaam khatam hone par `bash stop.sh`.

Dashboard ke pehle page (**Overview**) par aapko turant dikhega:
Total records, Completed, Needs review, Pending, Matched, Approved, Failed, Save unknown,
aur ek **progress bar**.

---

## 1. Excel upload karna

**Upload Excel** par jao:

1. Bade box par tap karo → Android Files app khulega.
2. Apni `.xlsx` ya `.xls` file chuno (Downloads / WhatsApp / Drive se).
3. App turant dikha dega:
   * file ka naam aur size,
   * **pehli 20 rows ka preview**,
   * total data rows, **khali rows**, **duplicate rows**, **khali cells** ki ginti,
   * warnings (jaise "31 empty cells mile").
4. Agar headings nahi mili hain ya galat sheet padhi gayi ho:
   * **Sheet** dropdown se doosri sheet chuno,
   * **Header row number** sahi karo (jaise headings 3rd line par hain to `3`),
   * **Reload preview** dabao.
5. **Map Excel columns** section me check karo:
   * App ne khud guess kiya hai (jaise `Name of Beneficiary` → *Beneficiary Name*).
   * Kuch galat ho to dropdown se sahi column chuno.
   * Zaroori: **Beneficiary Name**, **Vaccine Name**, **Vaccination Date**.
   * Ek column do jagah use ho jaye to app warning dikhata hai.
6. **Import & check records** dabao.

App ab har row ko check karega (phone par hi) aur batayega:
`27 record(s) imported. 3 need review, 0 matched automatically.`

> **Kabhi guess nahi:** khali vaccine, galat date, missing naam — inko app bharta nahi,
> sirf `NEEDS REVIEW` mark karta hai.

---

## 2. Authorised register import karna (matching ke liye)

Matching ke liye app ko authorised beneficiary list chahiye (jo aapko officially mili ho).

* Dashboard par **Authorised beneficiary register** card me file chuno → **Import register**.
* Ye file bhi `.xlsx` / `.xls` / `.csv` ho sakti hai; isme kam se kam
  **Beneficiary ID** ya **Beneficiary Name** hona chahiye.
* Import hone ke baad matching chalu ho jati hai.

Matching ka order (code me fix hai):

1. **Exact Beneficiary ID** → confidence 100% (auto-matched)
2. **Exact Naam + DOB** → 97% (auto-matched)
3. **Exact Naam + ek aur field** (mobile / mother ka naam) → 90–92% → **review**
4. **Naam milta-julta (fuzzy)** → **sirf suggestion**, kabhi auto-approve nahi
5. Kuch bhi match na ho → `NEEDS REVIEW`, aap portal par khud search karo

Agar ek jaisa naam do logon ka hai to app **sabhi candidates** dikhata hai
(matched fields, conflicting fields, missing fields ke saath) — faisla aapka.

---

## 3. Records list dekhna (Beneficiary Records)

* **Status** filter aur **Search** (naam / ID / mobile / vaccine).
* Har card me: Excel row number, naam, status badge, DOB, vaccine, dose, date, match %.
* Pagination hai (20 records per page) — phone slow nahi hoga.
* **CSV** button se current filter ka CSV export.

---

## 4. Review Queue (asli kaam yahan hota hai)

**Review Queue** kholo. Ek record screen par dikhega:

* **Validation notes** — kya problem hai (missing vaccine, ambiguous date, duplicate row...)
* **Excel values (source)** — file me kya tha
* **Matching result** — authorised register se comparison (sab candidates + confidence)
* **Portal save & confirmation** — status aur buttons
* **Your decision** — Approve / Edit / Reject / Skip / Previous / Next

### Buttons

| Button | Kab dabana hai | Kya hota hai |
|--------|----------------|--------------|
| **Approve** | Details sahi hain | Status `APPROVED` (portal save baaki) |
| **Edit** | Value galat hai / missing hai | Form kholo, sahi value daalo, **Save changes** (purani + nayi value history me rehti hai, phir record dobara validate hota hai) |
| **Reject** | Record use nahi karna | Reason likhna zaroori hai → status `FAILED` |
| **Skip** | Abhi nahi, baad me | Status `SKIPPED` (baad me **Reopen** kar sakte ho) |
| **Next / Previous beneficiary** | Aage-peeche jaana hai | Agla/pichla record |
| **Copy for portal search** | Portal par search karna hai | Naam + DOB + ID clipboard me |

Record ko **Approve** kiye bina kuch bhi portal par nahi jata.

---

## 5. Portal par entry (assisted workflow)

Ek baar **U-WIN Portal Login** page par jao:

1. **Open U-WIN login page** dabao → Chrome me official page khulta hai.
2. Apna **mobile number** aur **password** type karo (chaho to dashboard ke secure form se
   bhi kar sakte ho — wo sirf memory me rakhta hai, file me nahi).
3. U-WIN **SMS OTP** bhejega → dashboard `OTP Required – Enter the SMS OTP` dikhata hai.
   **OTP aap official page me khud type karo.** App SMS nahi padhta, OTP nahi save karta.
4. Login ho gaya? → **"I am logged in — continue"** dabao.

Ab record par wapas jao:

1. **Prepare portal save** dabao.
   * Agar department ne **approved API** diya hai → app usi se bhejega (confirm hone par hi COMPLETED).
   * Warna app **steps + copy buttons** dikhata hai (assisted).
2. Portal me beneficiary search karke entry karo (vaccine, dose, date).
3. Portal ne success dikhaya? → **"I confirmed it on the portal"** → portal ka
   **acknowledgement / reference number** paste karo → record **COMPLETED**.
4. Confirm nahi dikha / timeout ho gaya? → **"Mark as not confirmed"** dabao →
   record `SAVE STATUS UNKNOWN` ban jata hai. Portal par khud check karo, phir faisla karo.
   **App ise dobara automatic nahi bhejega** (duplicate nahi banega).

---

## 6. Pause / Resume / Stop / Recovery

Dashboard par **Queue controls**:

* **Pause** — ab kuch nahi hoga jab tak Resume na dabao.
* **Resume** — aage badho.
* **Stop** — stop ho jao (completed records waise hi rehte hain).
* **Process next** — agla record check/match karo.
* **Retry failed** — `FAILED` records dobara validate hote hain (portal save nahi retry hota).

**Termux band ho gaya / phone restart hua?**
Dubara `bash start.sh` → dashboard par **Resume previous session** dikhega → wahi se aage.
Jo save confirm nahi hua tha wo `SAVE STATUS UNKNOWN` me milega — usko khud check karo.

---

## 7. Reports banana

**Reports** page:

* **Create Excel report** — sheets: `Summary`, `Completed`, `Pending`, `Matched`,
  `Needs Review`, `Approved`, `Failed`, `Skipped`, `Save Status Unknown`, `All Records`.
  Har row me: Excel row number, naam, status, validation result, match result,
  review decision, error message, processing date-time.
* **CSV export** — current filter ka simple table.
* **Overview report** — saare uploaded files ek saath (+ Sessions summary sheet).
* **Download** / **Delete** buttons neeche list me.

Reports me **password/OTP kabhi nahi** aata (app unhein store hi nahi karta).

---

## 8. Settings me kya-kya hai

| Setting | Kaam |
|---------|------|
| Theme | Light / Dark / Phone setting |
| Date format | `DD-MM-YYYY` (India default), `MM/DD/YYYY`, `YYYY-MM-DD` ... — ambiguous date ka faisla |
| Upload size limit | 1–25 MB (phone par chhota rakho) |
| Rows per page | 10–100 |
| Report format | Excel multi-sheet ya CSV |
| Cleanup days | 0 = kabhi auto-delete nahi |
| Automatic session recovery | restart ke baad "Resume previous session" |
| Always ask before portal save | ON rakho (safety) |
| PIN lock | 4–8 digit PIN, dashboard lock |

**Privacy** section:

* **Run cleanup now** / **Remove leftover upload files**
* **Delete ALL local data** — confirm me `DELETE` type karna padta hai.

---

## 9. Daily routine ka short checklist

```
[ ] bash start.sh
[ ] Chrome → http://127.0.0.1:5000
[ ] Upload Excel → mapping check → Import
[ ] Review Queue → ek-ek record Approve/Edit/Skip
[ ] Portal login (OTP khud dalo)
[ ] Approved records portal me entry → reference paste → COMPLETED
[ ] Uncertain record ko "not confirmed" mark karo (khud check karo)
[ ] Reports → Excel report download
[ ] Sensitive data delete karo (Settings → Privacy) agar aage zaroorat nahi
[ ] bash stop.sh
```

---

## 10. Kya NAHI karna

* ❌ OTP kisi ke saath share nahi karna (app bhi kabhi nahi maangta).
* ❌ Dusre ka U-WIN login use nahi karna.
* ❌ `SAVE STATUS UNKNOWN` record ko "man se" complete mark nahi karna — pehle portal check karo.
* ❌ Practice ke liye real patient data use nahi karna (example file use karo).
* ❌ Kaam khatam hone par patient data phone me chhod dena — delete kar do.
