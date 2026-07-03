# Phase 1.5 — Migration Report (POC only)

**Date:** __________  
**Author:** __________  
**Production build affected:** NO  

## Files added (isolated)

| Path | Purpose |
|------|---------|
| `Nakama/docker-compose.nakama.yml` | Nakama + PostgreSQL (dev only) |
| `Nakama/runtime/main.js` | `hello_match` authoritative module |
| `Nakama/PHASE_1_5_README.md` | Run instructions |
| `Nakama/verify_nakama_poc.ps1` | Docker PASS/FAIL script |
| `Unity_Game/Assets/Mahjong/Scripts/NakamaPoc/*` | Automated Unity POC runner |
| `Unity_Game/Assets/Editor/NakamaPocMenu.cs` | Editor menu (POC only) |
| `Unity_Game/Packages/manifest.json` | Added `com.heroiclabs.nakama-unity` via OpenUPM |

## Files modified (production-safe)

| Path | Change |
|------|--------|
| `Unity_Game/Packages/manifest.json` | OpenUPM registry + Nakama SDK only |

## Files NOT modified

- FastAPI (`Backend/`)
- Laravel (`AdminPanel/`)
- `TournamentService`, `TournamentJoinCoordinator`, `TournamentRoomWebSocket`, `TournamentApiBridge`
- `TournamentWaitingRoomPanel`, `TournamentMatchManager`, `GameBoard`, wallet, payments

## Test results

### Docker (`Nakama/verify_nakama_poc.ps1`)

| Step | Result |
|------|--------|
| Docker available | PASS / FAIL |
| Postgres running | PASS / FAIL |
| Nakama running | PASS / FAIL |
| HTTP 7351 | PASS / FAIL |
| Runtime loaded | PASS / FAIL |
| **Docker overall** | **PASS / FAIL** |

### Unity (`Match IQ → Nakama POC → Run Phase 1.5 Test`)

| Step | Result |
|------|--------|
| 1. Nakama reachable + guest login | PASS / FAIL |
| 2. Session + socket | PASS / FAIL |
| 3. 2-player match | PASS / FAIL |
| 4. Hello exchange | PASS / FAIL |
| 5. Leave observed | PASS / FAIL |
| 6. Reconnect + rejoin | PASS / FAIL |
| 7. Production untouched | PASS / FAIL |
| 8. No files deleted | PASS / FAIL |
| **Unity overall** | **PASS / FAIL** |

## Notes

_________________________________________________________________

## Decision

- [ ] **Approve Phase 2** (realtime adapter behind feature flag)
- [ ] **Revise POC** (describe issues)
- [ ] **Stop migration**
