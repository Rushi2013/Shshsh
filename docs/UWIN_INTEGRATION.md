# U-WIN Portal Integration — sach, rules aur approved API ka tarika

Ye document bilkul honest hai. Yahan likha hai:

1. App **kya** karta hai (aur kya jaankari official portal ke saath share hoti hai),
2. Kya **nahi** karta (aur kyun nahi karta),
3. Ek **approved API** milne par integration kaise add karna hai (poora contract + code),
4. Aaj se pehle kis se permission leni hogi.

Official portal (publicly documented vaccinator login page):

```
https://uwinvaccinator.mohfw.gov.in/auth/login
```

---

## 1. App portal ke saath kya karta hai

| Kaam | Kaise | Kya bheja jata hai |
|------|-------|--------------------|
| Login page kholna | Android browser (`webbrowser` / `termux-open-url` / `am start`) | Sirf URL |
| Login karna | **Aap** karte ho (mobile, password, SMS OTP) | Kuch nahi — app ke andar sirf RAM me, 10 min max |
| Session detect karna | (a) aap "I am logged in" confirm karte ho, ya (b) approved adapter ke `/session/state` se | Kuch nahi |
| Record save karna | Sirf **approved API** ke through, aur sirf aapki approval ke baad | Sirf beneficiary + vaccine fields (neeche list) |
| Save confirm karna | Adapter ka clear confirmation, warna manual confirmation | — |
| Saving kya? | Har action SQLite me | — |

Assisted (default) mode me app **kuch bhi** portal ko nahi bhejta. Wo sirf:

* official page browser me kholta hai,
* record ke details screen par dikhata hai (copy buttons ke saath),
* aur aapke confirmation + reference number ka intezaar karta hai.

---

## 2. App kya NAHI karta (code me hi rok laga hai)

| Nahi karta | Kahan rok hai |
|------------|----------------|
| Private / undocumented endpoint call karna | Koi aisa URL code me nahi hai; adapter URL aapko khud dena padta hai aur wahi `validate_url` se guzarta hai |
| OTP bypass / auto-fill / SMS padhna | Koi SMS permission nahi; `/api/portal/otp` bina approved adapter ke OTP **accept hi nahi karta** |
| CAPTCHA bypass | Aisa koi code nahi |
| Login bypass / credential stuffing | Sirf human typing |
| Rate limit todna ya background scraping | Adapter client me minimum 3 second gap, koi loop/retry nahi, koi background worker nahi |
| Hidden database access | Sirf local SQLite |
| Password/OTP save karna | DB schema me aise column hain hi nahi; logs me scrub hota hai; reports me hard-coded column list |
| Uncertain save dobara bhejna | `save_state` marker + `recover_state()` → `SAVE STATUS UNKNOWN`, automatic retry band |

Agar bhavishya me koi aisa feature maanga jaye jo inme se kuch kare, to wo **is app me nahi**
jodna chahiye — pehle U-WIN / MOHFW se likhit permission aur API documentation lena zaroori hai.

---

## 3. Semi-automated login workflow (jo aaj kaam karta hai)

```
[1] Dashboard → U-WIN Portal Login → "Open U-WIN login page"
        ↓  (official page Chrome me khulta hai)
[2] Aap mobile number + password type karte ho
        ↓
[3] U-WIN SMS OTP bhejta hai
        ↓
[4] Dashboard: "OTP Required – Enter the SMS OTP"
        ↓  (app rukta hai — kuch nahi karta)
[5] Aap OTP official page me type karte ho
        ↓
[6] Login ho gaya → dashboard me "I am logged in — continue"
        ↓
[7] Excel beneficiary workflow (records pehle se validate + match hain)
        ↓
[8] Login expire ho gaya? → app queue pause karke "log in again" bolta hai
```

Iski implementation `uwin_integration.py` me hai:

* `open_in_browser()` — sirf allow-listed official host khulta hai
* `CredentialBuffer` — credentials RAM me (locked list), 10 minute TTL, `flush()` par overwrite + frame scrub
* `guided_login_steps()` — 5-step checklist jo dashboard par dikhta hai
* `login_status()` — UI ke liye poora status
* `save_record()` — assist / blocked / confirmed / unknown decide karta hai

---

## 4. Approved API adapter — contract (jab portal allow kare)

Ek adapter ek chhota service ya official gateway hota hai jo **aapke department / NIC / State
health authority** deti hai. App usse HTTPS par baat karta hai, bearer token ke saath.

### 4.1 Setup

```bash
# 1) token ko local file me rakho (chmod 600 ho jata hai)
nano ~/uwin-smart-assistant/secrets/portal_adapter_token.txt
#    → sirf token paste karo, koi space/quote nahi

# 2) URL dashboard me daalo:
#    Settings → U-WIN portal workflow → Approved integration URL
#    → "Test connection" dabao
```

Environment variable se bhi de sakte ho:

```bash
UWIN_ADAPTER_TOKEN=xxxxxxxx bash start.sh
```

### 4.2 Requests the app sends

| Endpoint | Method | Body | Expected reply |
|----------|--------|------|----------------|
| `/health` | GET | — | `{"status":"ok","capabilities":["session","save"]}` |
| `/session/state` | GET | — | `{"logged_in":true,"expires_at":"..."}` |
| `/session/otp` | POST | `{"otp":"123456"}` | `{"status":"ok"}` (sirf tab jab adapter maange) |
| `/beneficiary/record` | POST | neeche diya JSON | `{"status":"confirmed","reference":"UWIN-ACK-12345"}` |

Headers:

```
Accept: application/json
Content-Type: application/json
Authorization: Bearer <token>
User-Agent: UWIN-Smart-Assistant/1.0.0 (local device app)
```

Save request (exact fields, aur sirf yahi bheje jaate hain):

```json
{
  "beneficiary_id": "100000000101",
  "name": "Aarav Sharma",
  "dob": "2023-04-12",
  "gender": "Male",
  "mobile": "9876543210",
  "mother_name": "Sunita Sharma",
  "vaccine_name": "Pentavalent",
  "dose_number": "1",
  "vaccination_date": "2023-06-12",
  "batch_number": "PV23A17",
  "vaccinator_name": "Sister Meena",
  "facility": "PHC Wagholi",
  "source_row": 2,
  "local_record_id": 17
}
```

Reply handling (`uwin_integration.AdapterClient.submit_record`):

| Adapter sa reply | App kya karta hai |
|------------------|-------------------|
| `status: "confirmed"` + `reference` | Record `COMPLETED` (reference save hota hai) |
| `status: "pending"` | `SAVE STATUS UNKNOWN` — aap khud check karo |
| `status: "duplicate"` | Confirm nahi, manual check (duplicate warning) |
| Timeout / network error / kuch aur | `SAVE STATUS UNKNOWN`, **koi automatic retry nahi** |
| HTTP 401/403 | Session expire maan kar queue pause + login dobara |

### 4.3 Code me kaha add karna hai

Sab kuch pehle se modular hai. Sirf ek jagah badalna padta hai agar U-WIN ka official
contract alag ho:

```
uwin_integration.py
  ├── class AdapterClient       → _request() me headers / path badlo
  │                             → submit_record() me status mapping badlo
  ├── def _record_payload()     → body ke field names badlo
  └── def adapter_status()      → health check ka format badlo
```

Baaki app (validation, matching, queue, reports) ko farak nahi padta — wo waise hi chalega.

### 4.4 Test karne ka tarika (bina portal ke)

Ek local dummy adapter banao aur usse test karo, real portal par nahi:

```bash
cat > /tmp/dummy_adapter.py <<'PY'
from flask import Flask, jsonify, request
app = Flask(__name__)

@app.get("/health")
def health(): return jsonify(status="ok", capabilities=["session", "save"], name="dummy")

@app.get("/session/state")
def state(): return jsonify(logged_in=True)

@app.get("/session/otp")
def otp(): return jsonify(status="ok")

@app.post("/beneficiary/record")
def save():
    print("SAVE REQUEST:", request.json)          # yahan aap dekh sakte ho kya bheja gaya
    return jsonify(status="confirmed", reference="DUMMY-ACK-1")

app.run(host="127.0.0.1", port=8099)
PY
python /tmp/dummy_adapter.py
```

Phir Settings → Approved integration URL: `http://127.0.0.1:8099` → **Test connection** →
kisi approved record par **Prepare portal save** → app `DUMMY-ACK-1` ke saath record
`COMPLETED` kar dega. Isse poora save-flow bina kisi sarkari server ko chhue test ho jata hai.

---

## 5. Agar portal allow hi nahi karta — assisted workflow hi final hai

Bahut sambhavna hai ki U-WIN public API na de. Us case me ye app
**bilkul use karne layak** hai, kyunki:

* validation + matching + dedupe + review + reporting sab local hai,
* portal me entry ke liye app aapko exact details, copy buttons, checklist deta hai,
* galat entry ka risk kam ho jata hai kyunki pehle hi inconsistent rows pakdi jaati hain,
* poora record trail (kis row ka kya hua) report me mil jata hai.

Iske aage aap ye kar sakte ho (permission ke saath):

1. Apne State immunisation officer / U-WIN state nodal officer se likhit me poocho:
   "kya U-WIN me bulk/API based vaccination entry ka koi supported tarika hai?"
2. Referral/nodal se mile API documentation ko is repo ke `docs/` me rakho.
3. `AdapterClient` ko usi documentation ke hisaab se badlo (upar 4.3 dekho).
4. Change ke baad `python -m pytest -q` chalao — safety tests pass hone chahiye
   (OTP accept na hona, confirm ke bina COMPLETED na hona, unknown save retry na hona).

---

## 6. Compliance checklist (release se pehle)

```
[ ] Automation sirf usi ke liye jo officially allowed hai
[ ] Koi private endpoint guess nahi kiya gaya
[ ] OTP hamesha human (aap) dalte ho — app me koi SMS read code nahi
[ ] Password/OTP kahin store nahi (DB, log, report, Excel, source)
[ ] Uncertain save automatic retry nahi hota
[ ] Rate limiting aur "no background loop" banaya hua hai
[ ] Aapke department ki data-sharing approval likhit me hai
[ ] Patient data minimisation: kaam khatam → delete (Settings → Privacy)
```
