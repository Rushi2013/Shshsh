# Termux Installation Guide — bilkul beginner ke liye

Aapko computer ki zaroorat **nahi** hai. Sirf ek Android phone/tablet aur internet chaahiye
(installation ke waqt). Uske baad sab kuch phone par hi chalta hai.

Har command ko **copy karke Termux me paste** karo aur Enter dabao.

---

## Step 0 — Termux install karo

1. Play Store ki purani Termux version **theek se kaam nahi karti**.
   Latest version yahan se lo: **F-Droid** (search: `Termux`) — https://f-droid.org/packages/com.termux/
   ya GitHub releases: https://github.com/termux/termux-app/releases
2. Install karke kholo. Pehli baar thoda time lega (packages set hote hain).
3. (Recommended) Termux ko "unrestricted battery usage" do →
   Android Settings → Apps → Termux → Battery → **Unrestricted**.
   Isse background me server band nahi hoga.

---

## Step 1 — Basic packages

```bash
pkg update && pkg upgrade -y
pkg install python git -y
```

> `pkg update` me agar "Do you want to continue?" aaye to `y` + Enter dabao.
> Ye step 2–5 minute le sakta hai. Termux band mat karo.

Check:

```bash
python --version
```
`Python 3.x.x` dikhna chahiye.

---

## Step 2 — Project files phone me lao (koi ek tarika)

### A) GitHub se (internet chahiye)
```bash
pkg install git -y
cd ~
git clone <aapke-repo-ka-link> uwin-smart-assistant
cd uwin-smart-assistant
```

### B) ZIP se (WhatsApp/Telegram/Drive se bheja hua)
1. ZIP file ko phone me Download karo.
2. Termux me:
```bash
pkg install unzip -y
termux-setup-storage          # ek baar permission dialog aayega → Allow
cd ~/storage/downloads
unzip uwin-smart-assistant.zip -d ~/
cd ~/uwin-smart-assistant
```

### C) Files hand se rakhna ho
```bash
mkdir -p ~/uwin-smart-assistant
cd ~/uwin-smart-assistant
```
Phir saari files `storage/shared` ke through copy karo — lekin sabse aasan hai ZIP wala tarika.

---

## Step 3 — Setup script chalao (ek hi baar)

```bash
bash setup.sh
```

Script khud ye karta hai:

1. Termux packages install (python, git, termux-api)
2. Python libraries — `flask`, `openpyxl` (.xlsx), `xlrd` (.xls), aur `pandas` (optional, tez reading)
3. Folders banana — `uploads/ reports/ logs/ data/ secrets/ sample_data/`
4. Example Excel files banana — `sample_data/`
5. `python app.py --check` chala ke sab verify karna

Agar `pandas` install na ho paye:

```bash
pkg install python-numpy -y
pip install pandas
```
Phir bhi na ho to **koi dikkat nahi** — app `openpyxl` / `xlrd` se chal jata hai (bas bade file thode dheere).

Sirf Python libraries dobara install karni ho:

```bash
bash setup.sh --no-pkg
```

---

## Step 4 — Dashboard chalu karo

```bash
bash start.sh
```

Aapko kuch aisa dikhega:

```
  ✓ Dashboard chalu hai: http://127.0.0.1:5000
  ✓ Server PID: 12345  (logs: logs/console.log)
  → Chrome khol raha hoon…
```

* Chrome khud khul jayega (agar `termux-api` install hua ho).
* Nahi khula to Chrome kholo aur address bar me likho: **http://127.0.0.1:5000**

Chrome ke menu se **"Add to Home screen"** kar lo — roz ek tap me khul jayega.

### Logs khud dekhne ho (foreground mode)
```bash
bash start.sh --foreground
```
Ab logs isi window me dikhenge. Band karne ke liye `Ctrl + C` (Termux me `Ctrl` ke liye
volume-down + `C`, ya Termux keyboard me Ctrl button).

### Dusra port chahiye (5000 busy ho)
```bash
UWIN_PORT=5001 bash start.sh
# Chrome me: http://127.0.0.1:5001
```

---

## Step 5 — Band karna

```bash
bash stop.sh
```
Aapka data safe rehta hai (`data/uwin_assistant.db`, `uploads/`, `reports/`).
Dubara `bash start.sh` chalao — dashboard wahi se shuru hoga, "Resume previous session" bhi milega.

Purani/unused upload files bhi saaf karni ho:

```bash
bash stop.sh --purge
```

---

## Step 6 — Pehli baar kya karein (practice)

1. Dashboard → **Upload Excel** → **Download example file** dabao.
2. Usi example file (beneficiaries) ko upload karo → preview dekho → **Import & check records**.
3. Dashboard → **Authorised beneficiary register** → **Example register** download karo →
   usko import karo (isse matching chalu ho jayega).
4. **Review Queue** kholo → ek-ek record dekho → Approve / Edit / Skip se practice karo.
5. Practice khatam hone par: **Settings → Privacy → Delete ALL local data** (confirm me `DELETE` likho).

---

## Useful commands (ek jagah)

| Kaam | Command |
|------|---------|
| Setup (ek baar) | `bash setup.sh` |
| Dashboard chalu | `bash start.sh` |
| Logs ke saath chalu | `bash start.sh --foreground` |
| Dashboard band | `bash stop.sh` |
| Unused files saaf | `bash stop.sh --purge` |
| Installation check | `python app.py --check` |
| Purani files cleanup | `python app.py --cleanup` |
| Tests chalao | `python -m pytest -q` |
| Chrome me kholna | `termux-open-url http://127.0.0.1:5000` |
| Project folder | `cd ~/uwin-smart-assistant` |

---

## Termux auto-start (optional, advanced)

Roz Termux kholke `bash start.sh` likhna bhool jaate ho? Ek shortcut banao:

```bash
echo "cd ~/uwin-smart-assistant && bash start.sh" >> ~/.bashrc
```

Ab Termux kholte hi dashboard chalu ho jayega. (Server background me chalega.)
Hataana ho to `~/.bashrc` ki wahi line delete kar do.

---

## Android tips (app smooth chale)

* **Chrome** me dashboard kholo — Firefox bhi chalega, lekin Chrome best hai.
* Termux ko **split screen** me Chrome ke saath rakh sakte ho.
* Phone ko **Do Not Disturb** par rakho to OTP SMS notification turant dikhega.
* Termux background me kill ho jaye to sirf `bash start.sh` dobara chalao — data safe hai.
* Charging par rakho jab 500+ rows process kar rahe ho.

---

## Aage kya padhein

* Roz ka kaam samajhne ke liye → `docs/USER_GUIDE.md`
* Excel ka format → `docs/EXCEL_FORMAT.md`
* Kuch kaam nahi kar raha → `docs/TROUBLESHOOTING.md`
* Portal/OTP rules → `docs/UWIN_INTEGRATION.md`
* Data security → `docs/SECURITY_PRIVACY.md`
