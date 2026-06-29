# Match IQ — Live Deploy Guide

**Domains:** `api.matchiq.fun` | `admin.matchiq.fun`  
**Server:** `72.60.102.128` | **Path:** `/var/www/UnityTiles`

---

## 1. DNS (done)

```
A   @      → 72.60.102.128
A   api    → 72.60.102.128
A   admin  → 72.60.102.128
```

Verify:
```bash
nslookup api.matchiq.fun
curl https://api.matchiq.fun/health
```

---

## 2. CloudPanel sites

| Site | Type | Setting |
|------|------|---------|
| `api.matchiq.fun` | Reverse Proxy | `http://127.0.0.1:8000` + SSL |
| `admin.matchiq.fun` | PHP | Root: `/var/www/UnityTiles/AdminPanel/public` + SSL |

Nginx reference configs: `Deployment/nginx/api.matchiq.fun.conf`, `admin.matchiq.fun.conf`

---

## 3. Database (first time only)

```bash
cd /var/www/UnityTiles
grep -v "CREATE DATABASE" Database/tilesclash.sql | grep -v "^USE \`game\`" | mysql -u Game -p'Game@123' Game
grep -v "^USE \`game\`" Database/migrations/003_production_upgrade.sql | mysql -u Game -p'Game@123' Game
```

---

## 4. Backend `.env`

`/var/www/UnityTiles/Backend/.env`

```env
DATABASE_URL=mysql+pymysql://Game:Game%40123@localhost:3306/Game
REDIS_URL=redis://localhost:6379/0
API_HOST=0.0.0.0
API_PORT=8000
API_BASE_URL=https://api.matchiq.fun
JWT_SECRET=matchiq-live-jwt-secret-change-this-long-random
JWT_ALGORITHM=HS256
JWT_EXPIRE_MINUTES=10080
ENVIRONMENT=production
REQUIRE_HTTPS=true
CORS_ORIGINS=https://admin.matchiq.fun
GOOGLE_PLAY_PACKAGE_NAME=com.YogeshKumar.Myproject
GOOGLE_PLAY_SERVICE_ACCOUNT_JSON=secrets/google-play-service-account.json
```

---

## 5. Admin `.env`

`/var/www/UnityTiles/AdminPanel/.env`

```env
APP_NAME="Match IQ"
APP_ENV=production
APP_DEBUG=false
APP_URL=https://admin.matchiq.fun
DB_DATABASE=Game
DB_USERNAME=Game
DB_PASSWORD=Game@123
API_BASE_URL=https://api.matchiq.fun
```

---

## 6. Backend setup + systemd

```bash
apt install -y python3.12-venv python3-pip
cd /var/www/UnityTiles/Backend
python3 -m venv venv
venv/bin/pip install -r requirements.txt

cat > /etc/systemd/system/matchiq-api.service << 'EOF'
[Unit]
Description=Match IQ FastAPI Backend
After=network.target mysql.service

[Service]
User=root
WorkingDirectory=/var/www/UnityTiles/Backend
ExecStart=/var/www/UnityTiles/Backend/venv/bin/python -m uvicorn main:app --host 0.0.0.0 --port 8000
Restart=always
RestartSec=5

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable matchiq-api
systemctl restart matchiq-api
```

---

## 7. Google Play payment verify

1. Google Cloud → enable **Google Play Android Developer API**
2. Create service account → download JSON
3. Play Console → Users & permissions → add service account (Financial data)
4. Upload JSON:

```bash
scp google-play-service-account.json root@72.60.102.128:/var/www/UnityTiles/Backend/secrets/
chmod 600 /var/www/UnityTiles/Backend/secrets/google-play-service-account.json
systemctl restart matchiq-api
curl https://api.matchiq.fun/api/v1/payments/google/status
```

Play Console in-app products: `coins_100`, `coins_500`, `coins_1000`, `coins_2500`, `coins_5000`

---

## 8. Future updates

```bash
cd /var/www/UnityTiles
bash Deployment/deploy-live.sh
```

Or manually:
```bash
git pull origin main
systemctl restart matchiq-api
cd AdminPanel && php artisan config:cache
```

---

## 9. Unity Play Store build

1. Open `Unity_Game/` in Unity Hub
2. **ApiConfig** → `https://api.matchiq.fun` (already set in repo)
3. **Player → Android → Publishing** → create keystore + sign
4. **File → Build Settings → Android → App Bundle (AAB)**
5. Upload AAB to Play Console → Internal testing

---

## 10. Final tests

```bash
curl https://api.matchiq.fun/health
curl -X POST https://api.matchiq.fun/api/v1/auth/guest \
  -H "Content-Type: application/json" \
  -d '{"guest_id":"live1","display_name":"Test"}'
```

Browser: `https://admin.matchiq.fun/admin`
