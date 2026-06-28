# Unity ↔ FastAPI Integration (Phase 2)

## Configuration

Edit `Assets/Mahjong/Resources/Network/ApiConfig.asset`:

| Field | Purpose |
|-------|---------|
| `baseUrl` | Local API root (`http://localhost:8000`) |
| `productionUrl` | Production API (`https://api.tilesclash.com`) |
| `useProductionUrl` | Switch to production |
| `developmentMode` | **ON** = local simulation, no API calls |
| `requestTimeoutSeconds` | HTTP timeout |

## Architecture

```
UI (unchanged)
  ↓
TournamentJoinCoordinator / TournamentPageController
  ↓
AuthService | WalletService | TournamentService | LeaderboardService | ProfileService
  ↓
NetworkManager (singleton)
  ↓
FastAPI /api/v1
```

## Connected endpoints

- `POST /auth/register`, `/auth/login`, `/auth/guest`, `GET /auth/me`
- `GET /wallet/balance`
- `GET /tournaments`, `POST /tournaments/join`
- `GET /tournaments/rooms/{id}`, `POST /tournaments/submit-score`
- `GET /tournaments/history`
- `GET /leaderboard`

## Error handling

- Connection failures show **"Server unavailable."** with retry
- Game never crashes on API errors
- Offline fallback: enable **Development Mode** in ApiConfig

## Testing

1. Start XAMPP MySQL + import `Database/tilesclash.sql`
2. Start FastAPI: `uvicorn main:app --reload --port 8000`
3. Unity: `developmentMode = false`, `baseUrl = http://localhost:8000`
4. Open tournament scene — guest login runs automatically
