# Match IQ (UnityTiles) — Production Monorepo

## Structure

```
TilesClash/
├── Unity_Game/      # Unity client (gameplay, UI, assets, APK builds)
├── Backend/         # FastAPI REST + WebSocket server
├── AdminPanel/      # Laravel admin dashboard
├── Database/        # Shared MySQL schema (database: game)
├── Documentation/
└── Deployment/
```

## Architecture

```
Unity APK  →  api.tilesclash.com  →  FastAPI  →  MySQL (game)  ←  Laravel Admin
                                              ↘ Redis (optional)
```

## Quick Start (XAMPP Local)

### 1. Database
1. Start **Apache + MySQL** in XAMPP
2. Open phpMyAdmin → Import `Database/tilesclash.sql`
3. Optional: run `Database/seeders/levels_and_rewards.sql`

### 2. Backend (FastAPI)
```bash
cd Backend
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
copy .env.example .env
uvicorn main:app --reload --port 8000
```

API docs: http://localhost:8000/docs

### 3. Admin Panel (Laravel)
```bash
cd AdminPanel
composer install
copy .env.example .env
php artisan key:generate
php artisan serve --port=8080
```

### 4. Unity Game
Open `Unity_Game/` in Unity Hub (Unity 6000.3.17f1).

- Gameplay, tournament UI, and local simulation **unchanged**
- Network stubs: `Assets/Mahjong/Scripts/Network/`
- APK output: `Unity_Game/Builds/`

## Responsibility Split

| Component | Owns |
|-----------|------|
| **Unity** | Mahjong gameplay, UI, animations, audio, API/WS client |
| **Backend** | Auth, wallet, tournaments, rooms, ranking, prizes, leaderboard |
| **Admin** | Users, coins, tournaments, rooms, reports, settings, banners |
| **Database** | Single MySQL database `game` shared by Backend + Admin |

## Important

- Unity project was moved from `My project` to `TilesClash/Unity_Game/`
- Re-open Unity Hub and point to the new folder
- Close Unity before moving if Library folder is locked
