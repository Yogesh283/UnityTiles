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

echo "==> Restart matchiq-api (always — do not block on nginx)"
systemctl restart matchiq-api
sleep 2
systemctl is-active --quiet matchiq-api

echo "==> nginx WebSocket config (optional — keeps existing certs on failure)"
if [[ -f "$NGINX_CONF_SRC" && -f "$NGINX_CONF_DST" ]]; then
  NGINX_BACKUP="${NGINX_CONF_DST}.bak.$(date +%s)"
  cp "$NGINX_CONF_DST" "$NGINX_BACKUP"
  # Preserve live SSL paths; only ensure /ws/tournament/ block exists.
  if grep -q 'location /ws/tournament/' "$NGINX_CONF_DST"; then
    echo "PASS: nginx already has /ws/tournament/ — skipped cert overwrite"
  else
    cp "$NGINX_CONF_SRC" "$NGINX_CONF_DST"
    if nginx -t 2>/dev/null; then
      systemctl reload nginx
      echo "PASS: nginx reloaded with WebSocket block"
    else
      cp "$NGINX_BACKUP" "$NGINX_CONF_DST"
      echo "WARN: nginx -t failed (wrong SSL cert paths in repo config). Restored $NGINX_BACKUP"
      echo "WARN: Add /ws/tournament/ block manually — see Deployment/nginx/api.matchiq.fun.conf"
    fi
  fi
else
  echo "WARN: missing nginx config at $NGINX_CONF_DST"
fi

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
