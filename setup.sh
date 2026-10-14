#!/usr/bin/env bash
# =============================================================================
#  U-WIN Smart Assistant - setup script for Android (Termux)
#  Ek hi baar chalana hai:   bash setup.sh
#  Optional:  bash setup.sh --no-pkg      (Termux packages skip karke sirf Python)
# =============================================================================
set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

SKIP_PKG=0
for arg in "$@"; do
  case "$arg" in
    --no-pkg|--skip-packages) SKIP_PKG=1 ;;
    -h|--help)
      echo "Usage: bash setup.sh [--no-pkg]"
      echo "  --no-pkg   Termux packages install nahi karega (sirf Python libraries)"
      exit 0 ;;
  esac
done

PY=""
for candidate in python3 python; do
  if command -v "$candidate" >/dev/null 2>&1; then PY="$candidate"; break; fi
done

line() { printf '%s\n' "-------------------------------------------------------------"; }
say()  { printf '\n\033[1;34m==> %s\033[0m\n' "$1"; }
ok()   { printf '\033[0;32m  ✓ %s\033[0m\n' "$1"; }
warn() { printf '\033[0;33m  ! %s\033[0m\n' "$1"; }
bad()  { printf '\033[0;31m  ✗ %s\033[0m\n' "$1"; }

line
echo "  U-WIN SMART ASSISTANT - SETUP"
echo "  Android + Termux friendly installer"
line

# ------------------------------------------------------------------ packages --
if [ "$SKIP_PKG" -eq 0 ]; then
  if command -v pkg >/dev/null 2>&1; then
    say "Step 1/6 : Termux packages install kar raha hoon (python, git, termux-api)"
    warn "Ye step internet chahta hai aur 1-3 minute lag sakta hai. Chhodo mat."
    pkg update -y || warn "pkg update me dikkat aayi - aage badh raha hoon"
    pkg install -y python git termux-api || warn "Kuch packages install nahi hue, phir bhi aage badh raha hoon"
    ok "Termux packages ka kaam pura"
  else
    say "Step 1/6 : Termux nahi mila (ye computer/laptop hai)"
    warn "pkg command nahi hai, isliye sirf Python libraries install karunga"
  fi
else
  say "Step 1/6 : Termux packages skip (--no-pkg)"
fi

# -------------------------------------------------------------------- python --
say "Step 2/6 : Python check"
if [ -z "$PY" ]; then
  bad "Python nahi mila. Termux me chalao:  pkg install python"
  exit 1
fi
ok "Python mila: $($PY --version 2>&1)"

# ----------------------------------------------------------------- pip / venv --
say "Step 3/6 : Python libraries install"
if ! "$PY" -m pip --version >/dev/null 2>&1; then
  warn "pip nahi hai, install kar raha hoon"
  if command -v pkg >/dev/null 2>&1; then
    pkg install -y python-pip || true
  fi
fi
"$PY" -m pip install --upgrade pip >/dev/null 2>&1 || warn "pip upgrade skip kiya"

# Termux ke ready-made packages (agar available hain to build karne ki zaroorat nahi)
if command -v pkg >/dev/null 2>&1; then
  pkg install -y python-numpy >/dev/null 2>&1 && ok "python-numpy mil gaya" || true
fi

say "       Zaroori libraries install ho rahi hain: flask, openpyxl, xlrd"
if "$PY" -m pip install --no-cache-dir "Flask>=3.0,<4.0" "Werkzeug>=3.0" "openpyxl>=3.1" "xlrd>=2.0.1"; then
  ok "Zaroori libraries install ho gayi"
else
  bad "pip install fail hua. Internet check karo aur dobara chalao: bash setup.sh"
  exit 1
fi

say "       Optional library: pandas (bade Excel file jaldi padhne ke liye)"
if "$PY" -m pip install --no-cache-dir "pandas>=2.0"; then
  ok "pandas install ho gaya - bade file tez read honge"
else
  warn "pandas install nahi hua. Koi baat nahi - app openpyxl se chal jayega."
  warn "Baad me try kar sakte ho:  pip install pandas   ya   pkg install python-numpy && pip install pandas"
fi

# ------------------------------------------------------------------ folders --
say "Step 4/6 : Project folders bana raha hoon"
mkdir -p uploads reports logs data secrets static/css static/js static/img templates sample_data docs tests
ok "Folders ready: uploads/ reports/ logs/ data/ secrets/ sample_data/"

# ------------------------------------------------------------- sample excel --
say "Step 5/6 : Example Excel files bana raha hoon (practice ke liye)"
if "$PY" - <<'PYEOF'
import samples
files = samples.ensure_sample_files(force=True)
for name, path in files.items():
    print(f"  -> {path}")
PYEOF
then
  ok "Example files sample_data/ me ban gaye"
else
  warn "Example file nahi ban payi - app phir bhi chalega"
fi

# -------------------------------------------------------------------- check --
say "Step 6/6 : Installation check"
"$PY" app.py --check || warn "Check me kuch problem dikhi - docs/TROUBLESHOOTING.md padho"

line
echo "  SETUP COMPLETE — AB YE KAAM KARO:"
line
cat <<'EOF'

  1) Dashboard chalu karo:
        bash start.sh

  2) Chrome me kholo (ya ye command Termux me chalao):
        termux-open-url http://127.0.0.1:5000
        (Chrome address bar me likho:  http://127.0.0.1:5000)

  3) Dashboard me:
        Upload Excel  ->  file chuno  ->  mapping check karo  ->  Import
        Review Queue  ->  Approve / Edit / Reject / Skip
        Portal par khud login karo (OTP aap type karoge)  ->  reference paste karo

  4) Kaam band karna ho:
        bash stop.sh

  Madad chahiye?  docs/INSTALL_TERMUX.md  aur  docs/TROUBLESHOOTING.md  padho.

EOF
line
