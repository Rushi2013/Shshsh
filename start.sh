#!/usr/bin/env bash
# =============================================================================
#  U-WIN Smart Assistant - start the dashboard
#      bash start.sh              -> background me chalega, Chrome khul jayega
#      bash start.sh --foreground -> logs isi window me dikhenge (Ctrl+C se band)
#      UWIN_PORT=5001 bash start.sh -> dusra port
# =============================================================================
set -u

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR" || exit 1

HOST="${UWIN_HOST:-127.0.0.1}"
PORT="${UWIN_PORT:-5000}"
FOREGROUND=0
for arg in "$@"; do
  case "$arg" in
    --foreground|-f) FOREGROUND=1 ;;
    -h|--help)
      echo "Usage: bash start.sh [--foreground]"
      echo "  --foreground   logs screen par dikhao (Ctrl+C se band)"
      exit 0 ;;
  esac
done

PY=""
for candidate in python3 python; do
  if command -v "$candidate" >/dev/null 2>&1; then PY="$candidate"; break; fi
done
if [ -z "$PY" ]; then
  echo "  ✗ Python nahi mila. Termux me chalao:  pkg install python"
  exit 1
fi

mkdir -p uploads reports logs data secrets sample_data

# ---------------------------------------------------------------- already on? --
PIDFILE="logs/uwin.pid"
if [ -f "$PIDFILE" ]; then
  OLD_PID="$(cat "$PIDFILE" 2>/dev/null || true)"
  if [ -n "${OLD_PID:-}" ] && kill -0 "$OLD_PID" >/dev/null 2>&1; then
    echo "  ! Dashboard pehle se chal raha hai (PID $OLD_PID)."
    echo "    URL: http://$HOST:$PORT"
    echo "    Band karna ho to:  bash stop.sh"
    command -v termux-open-url >/dev/null 2>&1 && termux-open-url "http://$HOST:$PORT" >/dev/null 2>&1 || true
    exit 0
  fi
  rm -f "$PIDFILE"
fi

# ------------------------------------------------------------------- check ----
"$PY" app.py --check >/dev/null 2>&1 || echo "  ! Check me problem mili — details: $PY app.py --check"

# --------------------------------------------------------------- foreground ---
if [ "$FOREGROUND" -eq 1 ]; then
  echo "  Dashboard chalu ho raha hai: http://$HOST:$PORT"
  echo "  Band karne ke liye Ctrl+C dabao."
  exec "$PY" app.py
fi

# --------------------------------------------------------------- background ---
LOG="logs/console.log"
: > "$LOG"
nohup "$PY" app.py >>"$LOG" 2>&1 &
NEW_PID=$!
echo "$NEW_PID" > "$PIDFILE"

# ------------------------------------------------------- wait for the server --
# Do not just check "is the port open" - another app may hold that port.
# We wait for *our* process to answer the app's own status endpoint.
READY=0
for _ in $(seq 1 40); do
  # Our own process died -> no point in waiting further.
  if ! kill -0 "$NEW_PID" >/dev/null 2>&1; then
    break
  fi
  if "$PY" - "$HOST" "$PORT" "$NEW_PID" <<'PYEOF' >/dev/null 2>&1
import json, sys, urllib.request
host, port, wanted_pid = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
probe = "127.0.0.1" if host in ("0.0.0.0", "::") else host
try:
    with urllib.request.urlopen(f"http://{probe}:{port}/api/status", timeout=2) as response:
        data = json.load(response)
except Exception:
    raise SystemExit(1)
instance = data.get("instance") or {}
if data.get("ok") is not True:
    raise SystemExit(1)
# The answer must come from the process we just started.
if instance and instance.get("pid") != wanted_pid:
    raise SystemExit(2)
raise SystemExit(0)
PYEOF
  then
    READY=1
    break
  fi
  sleep 0.5
done

if [ "$READY" -eq 1 ]; then
  echo "  ✓ Dashboard chalu hai: http://$HOST:$PORT"
  echo "  ✓ Server PID: $NEW_PID  (logs: $LOG)"
  if command -v termux-open-url >/dev/null 2>&1; then
    echo "  → Chrome khol raha hoon…"
    termux-open-url "http://$HOST:$PORT" >/dev/null 2>&1 || true
  else
    echo "  → Chrome me ye link kholo: http://$HOST:$PORT"
  fi
  echo "  → Band karna ho to: bash stop.sh"
else
  echo "  ✗ Server start nahi ho paya. Log dekho:"
  echo "  ------------------------------------------------------"
  tail -n 25 "$LOG" 2>/dev/null || true
  echo "  ------------------------------------------------------"
  if "$PY" - "$HOST" "$PORT" <<'PYEOF' >/dev/null 2>&1
import socket, sys
host, port = sys.argv[1], int(sys.argv[2])
probe = "127.0.0.1" if host in ("0.0.0.0", "::") else host
with socket.socket() as s:
    s.settimeout(0.6)
    s.connect((probe, port))
PYEOF
  then
    echo "  Port $PORT par pehle se ek dashboard / program chal raha hai."
    echo "  Solution:  bash stop.sh   phir  bash start.sh"
    echo "  Ya dusra port:  UWIN_PORT=5001 bash start.sh"
  else
    echo "  Aam karan: libraries missing. Chalao:  bash setup.sh"
  fi
  rm -f "$PIDFILE"
  exit 1
fi
