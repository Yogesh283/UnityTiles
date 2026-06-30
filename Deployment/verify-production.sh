#!/usr/bin/env bash
# Production deployment verification — run on server or from CI with curl access.
set -euo pipefail

APP_ROOT="${APP_ROOT:-/var/www/UnityTiles}"
API="${API:-https://api.matchiq.fun}"
REQUIRED_COMMIT="1bc9567"
pass() { echo "PASS: $*"; }
fail() { echo "FAIL: $*"; exit 1; }

echo "=== 1. Git branch and commit ==="
if [[ -d "$APP_ROOT/.git" ]]; then
  cd "$APP_ROOT"
  git branch --show-current
  git log -1 --oneline
  SHORT="$(git rev-parse --short HEAD)"
  if git merge-base --is-ancestor "$REQUIRED_COMMIT" HEAD; then
    pass "server commit $SHORT (includes $REQUIRED_COMMIT)"
  else
    fail "server commit $SHORT — must include ancestor $REQUIRED_COMMIT (run Deployment/deploy-tournament.sh)"
  fi
else
  fail "no git repo at $APP_ROOT"
fi

echo "=== 2. FastAPI service ==="
systemctl is-active --quiet matchiq-api || fail "matchiq-api not active"
PID="$(systemctl show -p MainPID --value matchiq-api)"
CWD="$(readlink -f "/proc/$PID/cwd" 2>/dev/null || echo unknown)"
[[ "$CWD" == *"/Backend" ]] && pass "process cwd $CWD" || fail "process cwd $CWD (expected */Backend)"

echo "=== 3. Health + git commit in API ==="
HEALTH="$(curl -sf "$API/health")"
echo "$HEALTH"
echo "$HEALTH" | grep -q "\"commit\"" || fail "/health missing commit field — deploy latest Backend and restart matchiq-api"
API_COMMIT="$(echo "$HEALTH" | sed -n 's/.*"commit":"\([^"]*\)".*/\1/p')"
if git -C "$APP_ROOT" merge-base --is-ancestor "$REQUIRED_COMMIT" "${API_COMMIT:-deadbeef}" 2>/dev/null; then
  pass "API reports commit $API_COMMIT"
elif [[ "${API_COMMIT:-}" == "$REQUIRED_COMMIT"* ]]; then
  pass "API reports commit $API_COMMIT"
else
  fail "API commit '$API_COMMIT' — run deploy-tournament.sh"
fi

echo "=== 4. nginx WebSocket ==="
CODE="$(curl -s -o /dev/null -w '%{http_code}' \
  -H 'Connection: Upgrade' -H 'Upgrade: websocket' \
  -H 'Sec-WebSocket-Version: 13' -H 'Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==' \
  "$API/ws/tournament/test_room?token=bad")"
[[ "$CODE" == "403" || "$CODE" == "101" ]] && pass "WebSocket upgrade HTTP $CODE" || fail "WebSocket upgrade HTTP $CODE (expected 403)"

echo "=== 5. Duel catalog fingerprint ==="
curl -sf "$API/api/v1/tournaments" | grep -q '"waiting_seconds":300' && pass "duel_1v1 waiting_seconds=300" || fail "catalog not on feature branch"

echo ""
echo "Manual: both phones JOIN → journalctl must show WebSocket [accepted] twice per match."
echo "Manual: adb logcat | grep 'Match IQ Build' → Commit: $REQUIRED_COMMIT"
