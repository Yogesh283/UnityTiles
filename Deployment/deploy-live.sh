#!/usr/bin/env bash
# Match IQ — live server update script
# Run as root on VPS: bash Deployment/deploy-live.sh

set -euo pipefail

APP_ROOT="/var/www/UnityTiles"
DB_USER="Game"
DB_PASS="Game@123"
DB_NAME="Game"

echo "==> Pull tournament feature branch (NOT main)"
cd "$APP_ROOT"
git fetch origin feat/realtime-tournament-and-ui-fixes
git checkout feat/realtime-tournament-and-ui-fixes
git reset --hard origin/feat/realtime-tournament-and-ui-fixes
git log -1 --oneline

echo "==> Backend dependencies"
cd "$APP_ROOT/Backend"
python3 -m venv venv 2>/dev/null || true
venv/bin/pip install -r requirements.txt

echo "==> Database migrations"
# Applies every Database/migrations/*.sql in order. Migrations are written to be
# idempotent (CREATE TABLE IF NOT EXISTS, guarded INSERTs, additive ALTERs), so
# re-running on each deploy is safe.
if command -v mysql >/dev/null 2>&1; then
  for f in $(ls "$APP_ROOT/Database/migrations/"*.sql 2>/dev/null | sort); do
    echo "  -> $(basename "$f")"
    mysql -u"$DB_USER" -p"$DB_PASS" "$DB_NAME" < "$f" || echo "  WARN: $(basename "$f") reported an error (continuing)"
  done
else
  echo "  WARN: mysql client not found; skipping migrations"
fi

echo "==> Admin panel"
cd "$APP_ROOT/AdminPanel"
composer install --no-dev --optimize-autoloader
php artisan config:cache
php artisan route:cache
php artisan view:cache

echo "==> Restart API"
if [[ -f "$APP_ROOT/Deployment/nginx/api.matchiq.fun.conf" ]]; then
  cp "$APP_ROOT/Deployment/nginx/api.matchiq.fun.conf" /etc/nginx/sites-enabled/api.matchiq.fun.conf
  nginx -t && systemctl reload nginx
fi
systemctl restart matchiq-api
sleep 2

echo "==> Health checks"
curl -sf http://127.0.0.1:8000/health | head -c 200
echo ""
curl -sf http://127.0.0.1:8000/api/v1/payments/google/status | head -c 200
echo ""

if command -v curl >/dev/null 2>&1; then
  curl -sf https://api.matchiq.fun/health && echo "" || echo "WARN: api.matchiq.fun not reachable yet"
fi

echo "==> Done"
