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

# --- OpenSubsonic API (/rest) ---
REST="http://localhost:$PORT/rest"
SUB="u=test&p=test&f=json"

# 7. ping: XML is the default response format
curl -s "$REST/ping.view?u=test&p=test" | grep -q 'xmlns="http://subsonic.org/restapi"'
check "subsonic ping returns XML envelope" $?
curl -s "$REST/ping.view?u=test&p=test" | grep -q 'status="ok"'
check "subsonic ping XML status ok" $?

# 8. ping: JSON envelope on f=json
curl -s "$REST/ping?$SUB" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']; assert d['status']=='ok' and d['openSubsonic'] is True, d"
check "subsonic ping JSON envelope" $?

# 9. Auth errors: wrong password -> 40, token auth -> 42
curl -s "$REST/ping?u=test&p=wrong&f=json" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['error']['code']==40"
check "subsonic wrong password returns error 40" $?
curl -s "$REST/ping?u=test&t=deadbeef&s=salt&f=json" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['error']['code']==42"
check "subsonic token auth returns error 42" $?

# 10. ID3 browsing: getArtists -> getArtist -> getAlbum with the fixture song
SUB_ARTIST_ID=$(curl -s "$REST/getArtists?$SUB" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['artists']; print(d['index'][0]['artist'][0]['id'] if d.get('index') else '')")
[ -n "$SUB_ARTIST_ID" ]; check "subsonic getArtists finds fixture artist" $?
SUB_ALBUM_ID=$(curl -s "$REST/getArtist?$SUB&id=$SUB_ARTIST_ID" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['artist']; print(d['album'][0]['id'] if d.get('album') else '')")
[ -n "$SUB_ALBUM_ID" ]; check "subsonic getArtist lists fixture album" $?
curl -s "$REST/getAlbum?$SUB&id=$SUB_ALBUM_ID" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['album']; s=d['song'][0]; assert s['title']=='Test Song' and s['duration']>0 and isinstance(s['id'],str), s"
check "subsonic getAlbum returns fixture song" $?

# 11. search3 finds the fixture
curl -s "$REST/search3?$SUB&query=Test" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['searchResult3']; assert d.get('song') and d.get('album') and d.get('artist'), d"
check "subsonic search3 finds fixture" $?

# 12. getAlbumList2 newest
curl -s "$REST/getAlbumList2?$SUB&type=newest" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['albumList2']; assert len(d['album'])>0, d"
check "subsonic getAlbumList2 newest" $?

# 12b. getAlbumList (folder flavor) entries must be traversable directories in the folder tree
LIST_DIR_ID=$(curl -s "$REST/getAlbumList?$SUB&type=newest" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['albumList']; print(d['album'][0]['id'] if d.get('album') else '')")
curl -s "$REST/getMusicDirectory?$SUB&id=$LIST_DIR_ID" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['directory']; assert any(c['title']=='Test Song' for c in d['child']), d"
check "subsonic getAlbumList entries browse as folders" $?

# 13. Raw stream with Range -> 206
STATUS=$(curl -s -o /dev/null -w "%{http_code}" -H "Range: bytes=100-199" "$REST/stream?u=test&p=test&id=$SONG_ID&format=raw")
[ "$STATUS" = "206" ]; check "subsonic stream range returns 206" $? "(got $STATUS)"

# 14. Transcoded stream (only when ffmpeg is available)
if command -v ffmpeg >/dev/null 2>&1; then
    SIZE=$(curl -s -o /dev/null -w "%{size_download}" "$REST/stream?u=test&p=test&id=$SONG_ID&maxBitRate=32&format=mp3")
    [ "$SIZE" -gt 0 ]; check "subsonic transcoded stream returns audio" $? "(got $SIZE bytes)"
else
    echo "SKIP: subsonic transcoded stream (no ffmpeg)"
fi

# 15. Playlists: duplicate songId keys must both apply; update remove/add keeps counts right
curl -s "$REST/createPlaylist?$SUB&name=SmokeList&songId=$SONG_ID&songId=$SONG_ID" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['playlist']; assert d['songCount']==2 and len(d['entry'])==2, d"
check "subsonic createPlaylist with duplicate ids" $?
SUB_PL_ID=$(curl -s "$REST/getPlaylists?$SUB" | python3 -c "import json,sys; pl=[p for p in json.load(sys.stdin)['subsonic-response']['playlists']['playlist'] if p['name']=='SmokeList']; print(pl[0]['id'] if pl else '')")
curl -s "$REST/updatePlaylist?$SUB&playlistId=$SUB_PL_ID&songIndexToRemove=0&songIdToAdd=$SONG_ID" > /dev/null
curl -s "$REST/getPlaylist?$SUB&id=$SUB_PL_ID" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['playlist']; assert d['songCount']==2 and len(d['entry'])==2, d"
check "subsonic updatePlaylist remove+add" $?
curl -s "$REST/deletePlaylist?$SUB&id=$SUB_PL_ID" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['status']=='ok'"
check "subsonic deletePlaylist" $?

# 16. Star / getStarred2 round-trip
curl -s "$REST/star?$SUB&id=$SONG_ID" > /dev/null
curl -s "$REST/getStarred2?$SUB" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['starred2']; assert len(d.get('song',[]))==1 and d['song'][0]['starred'], d"
check "subsonic star/getStarred2 round-trip" $?
curl -s "$REST/unstar?$SUB&id=$SONG_ID" > /dev/null

# 17. Scrobble -> getNowPlaying + recent album list
curl -s "$REST/scrobble?$SUB&id=$SONG_ID&time=$(($(date +%s) * 1000))" > /dev/null
curl -s "$REST/getNowPlaying?$SUB" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['nowPlaying']; assert d['entry'][0]['username']=='test', d"
check "subsonic scrobble registers now playing" $?
curl -s "$REST/getAlbumList2?$SUB&type=recent" | python3 -c "import json,sys; d=json.load(sys.stdin)['subsonic-response']['albumList2']; assert len(d['album'])>0, d"
check "subsonic getAlbumList2 recent after scrobble" $?

# 18. API key lifecycle: generate via legacy admin API, then apiKey auth + conflict detection
ADMIN_SESSION=$(curl -s "http://localhost:$PORT/api/login?u=admin&p=admin" | python3 -c "import json,sys; print(json.load(sys.stdin).get('sessionId') or '')")
ADMIN_ID=$(curl -s "http://localhost:$PORT/api/users?s=$ADMIN_SESSION" | python3 -c "import json,sys; us=json.load(sys.stdin)['users']; print([u for u in us if u['userName']=='admin'][0]['userId'])")
APIKEY=$(curl -s "http://localhost:$PORT/api/users/$ADMIN_ID?s=$ADMIN_SESSION&action=generateApiKey" | python3 -c "import json,sys; print(json.load(sys.stdin)['users'][0].get('apiKey') or '')")
[ -n "$APIKEY" ]; check "generateApiKey via legacy API" $?
curl -s "$REST/ping?apiKey=$APIKEY&f=json" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['status']=='ok'"
check "subsonic apiKey auth" $?
curl -s "$REST/tokenInfo?apiKey=$APIKEY&f=json" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['tokenInfo']['username']=='admin'"
check "subsonic tokenInfo" $?
curl -s "$REST/ping?apiKey=$APIKEY&u=test&f=json" | python3 -c "import json,sys; assert json.load(sys.stdin)['subsonic-response']['error']['code']==43"
check "subsonic conflicting auth returns error 43" $?

echo ""
if [ "$FAILURES" -gt 0 ]; then
    echo "$FAILURES smoke test(s) FAILED"
    echo "--- server log tail ---"; tail -40 "$WORK/server2.log"
    exit 1
fi
echo "All smoke tests passed"
