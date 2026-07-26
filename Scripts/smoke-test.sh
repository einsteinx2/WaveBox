#!/usr/bin/env bash
# WaveBox end-to-end smoke test.
#
# Usage:
#   Scripts/smoke-test.sh <path-to-WaveBox.Server-binary>
#   Scripts/smoke-test.sh --dotnet          # run via `dotnet run` from source instead
#
# Starts the server against an isolated data directory, then verifies:
# login, status, media scan of a generated MP3 fixture, songs listing, and a range request.
set -uo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
WORK="$(mktemp -d)"
PORT=6500
FAILURES=0
SERVER_PID=""

cleanup() {
    if [ -n "$SERVER_PID" ]; then
        kill "$SERVER_PID" 2>/dev/null
        wait "$SERVER_PID" 2>/dev/null
    fi
    rm -rf "$WORK"
}
trap cleanup EXIT

check() {
    local name="$1" ok="$2" detail="${3:-}"
    if [ "$ok" = "0" ]; then
        echo "PASS: $name"
    else
        echo "FAIL: $name $detail"
        FAILURES=$((FAILURES + 1))
    fi
}

# Generate media fixture
mkdir -p "$WORK/media"
python3 "$REPO_ROOT/tests/fixtures/make_fixture.py" "$WORK/media/test_song.mp3"

# Launch server with isolated HOME so real user data is untouched
export HOME="$WORK/home"
mkdir -p "$HOME"

if [ "${1:-}" = "--dotnet" ]; then
    (cd "$REPO_ROOT/WaveBox.Server" && dotnet run -c Release) > "$WORK/server.log" 2>&1 &
    SERVER_PID=$!
else
    BINARY="${1:?usage: smoke-test.sh <binary path>|--dotnet}"
    "$BINARY" > "$WORK/server.log" 2>&1 &
    SERVER_PID=$!
fi

# Wait for the port to open (up to 60s to allow for a cold dotnet build)
for i in $(seq 1 120); do
    if curl -s -o /dev/null "http://localhost:$PORT/api/status"; then
        break
    fi
    if ! kill -0 "$SERVER_PID" 2>/dev/null; then
        echo "FAIL: server process exited early"; cat "$WORK/server.log"; exit 1
    fi
    sleep 0.5
done

# Point the config at the media fixture folder and restart to pick it up
CONF="$HOME/Library/Application Support/WaveBox/wavebox.conf"
[ -f "$CONF" ] || CONF="$HOME/.wavebox/wavebox.conf"
if [ ! -f "$CONF" ]; then
    echo "FAIL: wavebox.conf was not created"; exit 1
fi
python3 - "$CONF" "$WORK/media" <<'EOF'
import sys
conf, media = sys.argv[1], sys.argv[2]
s = open(conf).read()
open(conf, "w").write(s.replace('"/srv/your/media/here"', '"%s"' % media))
EOF
kill "$SERVER_PID" 2>/dev/null; wait "$SERVER_PID" 2>/dev/null

if [ "${1:-}" = "--dotnet" ]; then
    (cd "$REPO_ROOT/WaveBox.Server" && dotnet run -c Release --no-build) > "$WORK/server2.log" 2>&1 &
    SERVER_PID=$!
else
    "$1" > "$WORK/server2.log" 2>&1 &
    SERVER_PID=$!
fi
for i in $(seq 1 60); do
    curl -s -o /dev/null "http://localhost:$PORT/api/status" && break
    sleep 0.5
done

# 1. Login
SESSION=$(curl -s "http://localhost:$PORT/api/login?u=test&p=test" | python3 -c "import json,sys; print(json.load(sys.stdin).get('sessionId') or '')")
[ -n "$SESSION" ]; check "login returns session" $?

# 2. Status
curl -s "http://localhost:$PORT/api/status?s=$SESSION" | python3 -c "import json,sys; d=json.load(sys.stdin); assert d['error'] is None and 'version' in d['status']"
check "status endpoint" $?

# 3. Albums endpoint responds
curl -s "http://localhost:$PORT/api/albums?s=$SESSION" | python3 -c "import json,sys; d=json.load(sys.stdin); assert d['error'] is None"
check "albums endpoint" $?

# 4. Wait for the scanner to index the fixture (up to 30s)
SONG_ID=""
for i in $(seq 1 30); do
    SONG_ID=$(curl -s "http://localhost:$PORT/api/songs?s=$SESSION" | python3 -c "import json,sys; d=json.load(sys.stdin); print(d['songs'][0]['itemId'] if d['songs'] else '')")
    [ -n "$SONG_ID" ] && break
    sleep 1
done
[ -n "$SONG_ID" ]; check "media scan indexed fixture song" $?

# 5. Song metadata parsed by TagLib
curl -s "http://localhost:$PORT/api/songs?s=$SESSION" | python3 -c "import json,sys; d=json.load(sys.stdin); s=d['songs'][0]; assert s['songName']=='Test Song' and s['artistName']=='Test Artist', s"
check "tag metadata parsed" $?

# 6. Stream with range request returns 206
if [ -n "$SONG_ID" ]; then
    STATUS=$(curl -s -o /dev/null -w "%{http_code}" -H "Range: bytes=100-199" "http://localhost:$PORT/api/stream/$SONG_ID?s=$SESSION")
    [ "$STATUS" = "206" ]; check "range request returns 206" $? "(got $STATUS)"
else
    check "range request returns 206" 1 "(no song id)"
fi

echo ""
if [ "$FAILURES" -gt 0 ]; then
    echo "$FAILURES smoke test(s) FAILED"
    echo "--- server log tail ---"; tail -40 "$WORK/server2.log"
    exit 1
fi
echo "All smoke tests passed"
