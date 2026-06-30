#!/usr/bin/env bash
# Deploy tournament realtime fixes — NEVER use origin/main for this flow.
# Run on server: bash Deployment/deploy-tournament.sh

set -euo pipefail

APP_ROOT="/var/www/UnityTiles"
BRANCH="feat/realtime-tournament-and-ui-fixes"
MINIMUM_COMMIT="1bc9567"
NGINX_CONF_SRC="$APP_ROOT/Deployment/nginx/api.matchiq.fun.conf"
NGINX_CONF_DST="/etc/nginx/sites-enabled/api.matchiq.fun.conf"

echo "==> Fetch $BRANCH"
cd "$APP_ROOT"
git fetch origin "$BRANCH"
git checkout "$BRANCH"
git reset --hard "origin/$BRANCH"

COMMIT="$(git rev-parse --short HEAD)"
SUBJECT="$(git log -1 --pretty=%s)"
echo "==> Deployed commit: $COMMIT — $SUBJECT"

if ! git merge-base --is-ancestor "$MINIMUM_COMMIT" HEAD; then
  echo "FAIL: HEAD $COMMIT is not descended from $MINIMUM_COMMIT"
  exit 1
fi

echo "==> Backend dependencies"
cd "$APP_ROOT/Backend"
python3 -m venv venv 2>/dev/null || true
venv/bin/pip install -q -r requirements.txt

echo "==> nginx WebSocket config"
if [[ -f "$NGINX_CONF_SRC" ]]; then
  cp "$NGINX_CONF_SRC" "$NGINX_CONF_DST"
  nginx -t
  systemctl reload nginx
else
  echo "WARN: missing $NGINX_CONF_SRC"
fi

echo "==> Restart matchiq-api"
systemctl restart matchiq-api
sleep 2
systemctl is-active --quiet matchiq-api

echo "==> Verify process cwd and imports"
PID="$(systemctl show -p MainPID --value matchiq-api)"
if [[ -n "$PID" && "$PID" != "0" ]]; then
  readlink -f "/proc/$PID/cwd"
  tr '\0' ' ' < "/proc/$PID/cmdline"
  echo ""
fi
cd "$APP_ROOT/Backend"
venv/bin/python -c "from deploy_info import deployed_git_info; print(deployed_git_info())"

echo "==> Health (must include git commit)"
curl -sf "http://127.0.0.1:8000/health"
echo ""
curl -sf "https://api.matchiq.fun/health" || true
echo ""

echo "PASS: server on $BRANCH @ $COMMIT"
echo "Next: rebuild APK (Match IQ → Prepare APK Build), install on both phones, watch:"
echo "  journalctl -u matchiq-api -f | grep -Ei 'WebSocket|accepted|join|match_start|match_finished'"
