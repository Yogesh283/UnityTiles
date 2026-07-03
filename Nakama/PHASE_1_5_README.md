# Match IQ — Phase 1.5 Nakama Proof of Concept

**Isolated from production.** FastAPI, Laravel, and tournament gameplay are unchanged.

## What this proves

| # | Test |
|---|------|
| 1 | Docker Nakama + PostgreSQL healthy |
| 2 | Unity guest (device) login + session |
| 3 | 2-player matchmaker → match created |
| 4 | Hello opcode round-trip |
| 5 | Leave observed by other player |
| 6 | Reconnect + rejoin same match |

## 1. Start Nakama (Docker)

```powershell
cd C:\Users\yogib\TilesClash
docker compose -f Nakama/docker-compose.nakama.yml up --build
```

Wait until logs show: `MatchIQ Phase 1.5 POC Nakama runtime loaded`

- **Console:** http://localhost:7351 (admin / password)
- **gRPC API:** `127.0.0.1:7350`
- **Postgres:** `localhost:5433` (user `nakama`, db `nakama`)

Verify health:

```powershell
docker compose -f Nakama/docker-compose.nakama.yml ps
```

## 2. Unity — install SDK (first time only)

Open `Unity_Game` in Unity 6. Package Manager resolves:

- `com.heroiclabs.nakama-unity` (added to `Packages/manifest.json`)

## 3. Run automated POC (single Editor, 2 in-process clients)

1. Ensure Nakama Docker is running.
2. Unity menu: **Match IQ → Nakama POC → Run Phase 1.5 Test (Play Mode)**
3. Watch Console for tag `[NakamaPoc]`
4. On completion: `=== OVERALL PHASE 1.5: PASS ===` or `FAIL`

## 4. Manual 2-window test (optional)

Use ParrelSync: two Unity instances, each runs **Match IQ → Nakama POC → Run Phase 1.5 Test**.

## 5. PASS / FAIL report

Fill in after each run:

```
Match IQ Phase 1.5 — Nakama POC
Date: __________
Unity build: __________
Nakama image: heroiclabs/nakama:3.16.0

[ ] 1. Docker Nakama healthy          PASS / FAIL
[ ] 2. Unity session created          PASS / FAIL
[ ] 3. 2 tickets matched              PASS / FAIL
[ ] 4. Hello round-trip               PASS / FAIL
[ ] 5. Leave observed                 PASS / FAIL
[ ] 6. Reconnect + rejoin             PASS / FAIL
[ ] 7. Production untouched           PASS / FAIL
[ ] 8. No production files deleted    PASS / FAIL

OVERALL: PASS / FAIL
Notes:
```

## Production safety

- `ApiConfig` / `TournamentService` / FastAPI WS — **not modified**
- POC lives under `Assets/Mahjong/Scripts/NakamaPoc/` only
- Feature flag for real migration comes in **Phase 2** (not this POC)

## Stop Nakama

```powershell
docker compose -f Nakama/docker-compose.nakama.yml down
```

Data volume persists in `nakama_postgres_data`. Remove with `-v` to reset.

## Next step

**STOP after Phase 1.5.** Do not proceed to Phase 2 until you approve in writing.
