# Security & Privacy — data kaise safe rehta hai

Vaccination records **sensitive personal / medical data** hain. Is app ka poora design
"local-first" hai: data aapke phone se bahar nahi jaata, aur jo cheezein risk banati hain
(password, OTP, uncertain saves) unhein code me hi rok diya gaya hai.

---

## 1. Data kahan rehta hai

| Data | Jagah | Kis format me |
|------|-------|---------------|
| Beneficiary records + status | `data/uwin_assistant.db` | SQLite (local file) |
| Aapki uploaded Excel file | `uploads/` | Aapki original file |
| Reports | `reports/` | .xlsx / .csv |
| Logs (system events) | `logs/` + `events` table | Sirf event names, koi patient detail nahi |
| Approved API token (agar use karo) | `secrets/portal_adapter_token.txt` | 600 permission, `.gitignore` me |
| Flask secret key | `data/.secret_key` | 600 permission |

**Kahin nahi jaata:** koi cloud, koi analytics, koi third-party upload, koi telemetry.
Dashboard ka server sirf `127.0.0.1` par bind hota hai — aapke Wi-Fi par doosre phone
bhi ise nahi khol sakte.

Verify karna ho:

```bash
grep -r "http://" --include=*.py . | grep -v 127.0.0.1     # sirf portal URL dikhega
grep -rn "requests\|urllib" --include=*.py . | head        # network call sirf adapter me
```

---

## 2. Credential handling (password / OTP)

* **Password**:
  * Source code me kahin nahi (`grep -i password *.py` → sirf variable/UI names).
  * Database me column hi nahi hai.
  * Report generator ki column list hard-coded hai — password ka koi jagah nahi.
  * Login form optional hai: form submit hone par value `CredentialBuffer` (RAM) me
    immutable-list ke roop me jaati hai, jo `flush()` par **overwrite** ki jaati hai.
  * 10 minute ka hard TTL — bhool jaane par bhi sach-much mit jaati hai.
  * Login confirm hone par password turant mit jata hai (sirf session state rehta hai).
* **OTP**:
  * App OTP **accept hi nahi karta** (approved adapter ke bina) — endpoint saaf saaf
    kehta hai: "Enter the SMS OTP in the official portal page".
  * Koi SMS permission nahi, koi SMS reading code nahi, koi OTP log/DB save nahi.
  * Adapter enable hone par bhi OTP sirf ek baar forward hota hai aur local variable
    turant `""` kar diya jata hai.
* **Best-effort frame scrub**: `_scrub_frames()` Python frames ke local variables ko
  zero karta hai. Ye guarantee nahi hai (Python ki limitation hai, aur isi liye app in
  values ko **kabhi disk par likhta hi nahi**).

---

## 3. Web security (dashboard)

| Cheez | Kaise laga hai |
|-------|----------------|
| CSRF | Har POST/PUT/DELETE `/api/*` par `X-CSRF-Token` (session-bound) verify; galat → 403 |
| Session cookie | `HttpOnly`, `SameSite=Lax`, 12 hours lifetime |
| Clickjacking | localhost par `X-Frame-Options: SAMEORIGIN`, `frame-ancestors 'self'` |
| MIME sniffing | `X-Content-Type-Options: nosniff` |
| Referrer leak | `Referrer-Policy: no-referrer` |
| CSP | `default-src 'self'` — koi external script/CDN nahi |
| Cache | API aur downloads par `Cache-Control: no-store` |
| Upload validation | Extension + size + **magic bytes** (ZIP `PK\x03\x04` / OLE) check |
| Filenames | `secure_filename` + timestamp + random token; original naam sirf metadata me |
| Path traversal | `safe_report_path()` sirf `reports/` ke andar ki file deta hai |
| Error pages | Friendly message; traceback sirf local log me (7 lines) |
| SQL | Sab queries parameterised (SQLite prepared statements) |

PIN lock (optional) dashboard ko 4–8 digit PIN ke peeche rakh deta hai
(`werkzeug.security` hash, plaintext nahi).

---

## 4. Save-safety (sabse important hissa)

Vaccination register me duplicate entry bahut bura hai. Isliye:

1. **Approval zaroori** — `/api/records/<id>/save` bina approval ke 400 deta hai.
2. **Marker pehle** — network call se **pehle** SQLite me `save_state='saving'` likha jata hai.
3. **Crash hua?** — restart par `recover_state()` us record ko `SAVE STATUS UNKNOWN` karta hai,
   `APPROVED` wapas nahi karta. Automatic retry **band** hai.
4. **Confirmation ke bina COMPLETED nahi** — reference/acknowledgement ke bina
   `mark_completed()` 400 deta hai.
5. **Duplicate warning** — adapter `duplicate` bole to app confirm nahi maanta.

Tests isko verify karte hain: `tests/test_workflow.py::TestPortalSafety` (7 tests).

---

## 5. Data minimisation

* Sirf wahi columns store hote hain jo mapping me chune gaye (aur extra columns bhi,
  kyunki report me kaam aate hain — chaaho to unhe map na karo).
* Upload file privacy: Settings me `keep_original_file` OFF karo → file import ke baad
  rakhne ki zaroorat nahi (re-read nahi kar paoge, lekin data minimise ho jayega).
* `cleanup_days` > 0 karo → purane sessions apne aap delete honge.
* `delete_session_on_complete` ON karo → 100% complete session auto-delete.
* **Delete ALL local data** — Settings → Privacy (confirm me `DELETE`). Ye sessions,
  records, register, reports, uploads, logs sab hata deta hai.
* `bash stop.sh --purge` — orphan upload files saaf.

---

## 6. Threat model — kya cover hai, kya nahi

**Cover hai:**
* Phone kho gaya (data local, PIN lock, auto cleanup, quick wipe).
* Shared phone (PIN lock, logout button, password box tab hide hone par clear).
* Portal par galat/duplicate entry (approval + marker + unknown-save policy).
* Galat data entry (validation, matching, review).
* Net snooping (default me koi network call hi nahi).

**Cover NAHI hai (aapko khud dhyan rakhna hai):**
* Rooted phone ya kisi aur ke haath me phone (SQLite file padhi ja sakti hai).
* Screen recording / shoulder surfing (dashboard PIN se sirf UI lock hota hai).
* Android backup jo `data/` ko cloud par le jaaye — backup me is app ko exclude karo,
  ya kaam khatam hone par data delete kar do.
* Chrome me U-WIN par login karne ke baad browser session (browser ke apne rules lagu hote hain;
  shared phone par browser se logout karo).

---

## 7. Compliance notes

* Ye app **U-WIN / MOHFW ki official app nahi hai**. Ye ek local record-management assistant hai.
* Official portal ki terms, state health department ki SOP aur data-sharing policy follow karna
  aapki zimmedari hai.
* Automation sirf approved API ke saath. OTP/CAPTCHA/login bypass bilkul nahi.
* Aadhaar jaise identifiers app maangta nahi; agar aapki file me hai to usse report/log se
  scrub kar diya jata hai (report generator ke patterns dekho).

---

## 8. Security verify karne ke commands

```bash
# 1) Kahin password likha hai?
grep -rni "password" --include="*.py" --include="*.js" --include="*.html" . | grep -vi "input\|label\|hash\|check_password\|CredentialBuffer\|placeholder\|password field" | head

# 2) Koi external host?
grep -rn "http[s]*://" --include="*.py" --include="*.html" --include="*.js" . | grep -v "127.0.0.1\|mohfw.gov.in\|example.gov.in\|schema\|w3.org\|localhost" | head

# 3) Safety tests
python -m pytest tests/test_workflow.py::TestPortalSafety -v
```
