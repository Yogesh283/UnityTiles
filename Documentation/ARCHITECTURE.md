# TilesClash Architecture

## Unity Client (`Unity_Game/`)

**Contains:** Mahjong gameplay, tournament UI, game UI, animations, audio, assets, APK builds, API/WebSocket client stubs.

**Does NOT contain:** database, admin panel, backend business logic, wallet calculations, ranking engine, tournament matching (server-side).

Current tournament flow runs **locally** (bots + PlayerPrefs). Network layer in `Assets/Mahjong/Scripts/Network/` is ready for future API integration without breaking offline play.

## FastAPI Backend (`Backend/`)

| Module | Responsibility |
|--------|----------------|
| `auth/` | Register, login, guest, Google JWT |
| `wallet/` | Coins, entry fees, prizes, transactions |
| `tournament/` | Catalog, rooms, level seed, ranking, prizes |
| `api/v1/` | REST endpoints |
| `websocket/` | Live tournament room events |

Tournament catalog and prize tables mirror Unity `TournamentCatalog` and `TournamentPrizeTable`.

## Laravel Admin (`AdminPanel/`)

Dashboard, users, wallet management, tournaments, room monitoring, leaderboard, reports, analytics, settings, banners, notifications, support, logs.

Uses the same `game` MySQL database as FastAPI.

## Database (`Database/`)

Single database: **`game`** (XAMPP MySQL)

Tables: users, wallet, wallet_transactions, levels, level_rewards, tournaments, tournament_rooms, room_players, tournament_results, leaderboard, notifications, settings, banners, admins, logs.

## API Endpoints (v1)

| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/v1/auth/register` | Email registration |
| POST | `/api/v1/auth/login` | Email login |
| POST | `/api/v1/auth/guest` | Guest login |
| POST | `/api/v1/auth/google` | Google login |
| GET | `/api/v1/wallet/balance` | Coin balance |
| GET | `/api/v1/tournaments` | Tournament list |
| POST | `/api/v1/tournaments/join` | Join/create room |
| GET | `/api/v1/leaderboard` | Global leaderboard |
| WS | `/ws/tournament/{room_id}` | Room live updates |
