#!/usr/bin/env bash
# =============================================================================
#  U-WIN Smart Assistant - stop the dashboard
#      bash stop.sh           -> server band karo (data safe rehta hai)
#      bash stop.sh --purge   -> band karke purane temp upload files bhi saaf karo
# =============================================================================
set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

PURGE=0
for arg in "$@"; do
  case "$arg" in
    --purge) PURGE=1 ;;
    -h|--help)
      echo "Usage: bash stop.sh [--purge]"
      echo "  --purge   server band karke unused upload files bhi delete karo"
      exit 0 ;;
  esac
done

PY=""
for candidate in python3 python; do
  if command -v "$candidate" >/dev/null 2>&1; then PY="$candidate"; break; fi
done

PIDFILE="logs/uwin.pid"
STOPPED=0

if [ -f "$PIDFILE" ]; then
  PID="$(cat "$PIDFILE" 2>/dev/null || true)"
  if [ -n "${PID:-}" ] && kill -0 "$PID" >/dev/null 2>&1; then
    kill "$PID" 2>/dev/null || true
    for _ in $(seq 1 20); do
      kill -0 "$PID" >/dev/null 2>&1 || break
      sleep 0.3
    done
    if kill -0 "$PID" >/dev/null 2>&1; then
      kill -9 "$PID" 2>/dev/null || true
    fi
    echo "  ✓ Dashboard band kar diya (PID $PID)."
    STOPPED=1
  fi
  rm -f "$PIDFILE"
fi

# Backup: agar pid file kho gayi ho to process dhoondo. Sirf wahi process band hota hai
# jiska working directory yahi project hai - doosre project ka server nahi chhua jayega.
if command -v pgrep >/dev/null 2>&1; then
  for pid in $(pgrep -f "app\.py" 2>/dev/null || true); do
    [ "$pid" = "$$" ] && continue
    proc_cwd="$(readlink /proc/"$pid"/cwd 2>/dev/null || echo '')"
    if [ -n "$proc_cwd" ] && [ "$proc_cwd" != "$SCRIPT_DIR" ]; then
      continue
    fi
    cmdline="$(tr '\0' ' ' < /proc/"$pid"/cmdline 2>/dev/null || echo '')"
    case "$cmdline" in
      *"app.py"*) ;;
      *) continue ;;
    esac
    kill "$pid" 2>/dev/null && echo "  ✓ Extra dashboard process band kiya (PID $pid)."
    STOPPED=1
  done
fi

if [ "$STOPPED" -eq 0 ]; then
  echo "  ! Koi chal raha dashboard nahi mila. Kuch karne ki zaroorat nahi."
fi

if [ "$PURGE" -eq 1 ] && [ -n "$PY" ]; then
  echo "  → Purani upload files saaf kar raha hoon…"
  "$PY" - <<'PYEOF'
import config
import database as db

known = {row["stored_filename"] for row in db.query_all("SELECT stored_filename FROM sessions")}
removed = 0
for path in config.UPLOAD_DIR.glob("*"):
    if path.is_file() and path.name not in known and path.name != ".gitkeep":
        try:
            path.unlink()
            removed += 1
        except OSError:
            pass
print(f"  ✓ {removed} file(s) delete ki gayi.")
PYEOF
fi

echo
echo "  Aapka data safe hai: data/uwin_assistant.db, uploads/ aur reports/ wahi hain."
echo "  Dubara shuru karne ke liye:  bash start.sh"
