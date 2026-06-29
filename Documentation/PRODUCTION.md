# Match IQ Production Architecture

## Stack

```
Unity APK (Match IQ)
    ↓ HTTPS
api.matchiq.fun (FastAPI + Redis rate limit + JWT)
    ↓
MySQL (Game) ← Filament Admin (admin.matchiq.fun)
```

## Security Features

- **Player UUID** — public identifier in JWT (`user_uuid`); internal bigint for DB FKs
- **Server-authoritative wallet** — all coin changes via backend; idempotent transaction IDs
- **Anti-cheat** — score/time/move validation on submit; security_events logging
- **Ranking** — server-only: time → score → moves → submitted_at
- **Rate limiting** — Redis-backed (240 req/min/IP), graceful fallback without Redis
- **Device/IP bans** — middleware checks on authenticated requests
- **Audit logs** — login, wallet, tournament join, admin actions

## Database Migration

Run after `tilesclash.sql`:

```bash
mysql -u root game < Database/migrations/003_production_upgrade.sql
```

## Environment (Backend `.env`)

```env
DATABASE_URL=mysql+pymysql://root:@localhost:3306/game
REDIS_URL=redis://localhost:6379/0
JWT_SECRET=change-me-use-long-random-string
CORS_ORIGINS=https://admin.matchiq.fun
ENVIRONMENT=production
REQUIRE_HTTPS=true
GOOGLE_PLAY_PACKAGE_NAME=fun.matchiq.game
GOOGLE_PLAY_SERVICE_ACCOUNT_JSON=/secrets/google-play.json
```

## Unity Online Mode

1. `ApiConfig.asset` → `developmentMode = false`
2. `productionUrl = https://api.matchiq.fun`
3. Coins display synced from server only (`WalletService.SyncToCoinsHolderAsync`)
4. Firebase: add SDK, define `FIREBASE` scripting symbol

## Backups

- Linux: `Deployment/backup/backup-mysql.sh` (cron daily)
- Windows: `Deployment/backup/backup-mysql.bat`

## Admin Panel Modules

Filament at `/admin`: Players, Wallets, Transactions, IAP, Tournaments, Rooms, Banners, Notifications, Settings, Logs.

Add via Filament: Device Bans, IP Bans, Security Events, Player Reports, Leaderboard, Tournament Results.

## API Additions (backward compatible)

- `TokenResponse.user_uuid` — new field
- `GET /auth/me` — includes `user_uuid`
- `POST /presence/heartbeat` — online player tracking
- `POST /notifications/fcm/register` — push token
- Wallet transactions include `transaction_id`, `balance_before`, `reason`

## Not Modified (per requirements)

- Mahjong gameplay / tile matching
- Level system logic
- Tournament UI scenes
- Existing endpoint paths and request shapes
