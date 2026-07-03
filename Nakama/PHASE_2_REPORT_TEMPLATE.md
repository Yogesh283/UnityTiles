# Match IQ — Phase 2 Realtime Adapter Report

**Date:** __________  
**Branch/Commit:** __________  
**Feature Flag:** `ApiConfig.useNakamaRealtime = true/false`  

## Milestone A — Adapter Wiring (Code)

| Check | PASS/FAIL | Notes |
|---|---|---|
| `ApiConfig` has Nakama realtime flag |  |  |
| FastAPI websocket path still present |  |  |
| `TournamentRoomWebSocket` delegates by feature flag |  |  |
| `TournamentJoinCoordinator` has Nakama join path |  |  |
| `TournamentApiBridge` still drives same room snapshot pipeline |  |  |
| No gameplay classes modified |  |  |

## Milestone B — Nakama Room Lifecycle

| Check | PASS/FAIL | Notes |
|---|---|---|
| Matchmaker creates 2-player room |  |  |
| Presence join/leave events propagate to Unity |  |  |
| Countdown state messages arrive (`starting`) |  |  |
| Match start message arrives (`active`) |  |  |
| Reconnect rejoins same match |  |  |

## Milestone C — Safety / Backward Compatibility

| Check | PASS/FAIL | Notes |
|---|---|---|
| With flag **OFF**, old FastAPI multiplayer works |  |  |
| With flag **ON**, Nakama realtime path is used |  |  |
| Wallet and payments untouched |  |  |
| FastAPI API endpoints unchanged |  |  |
| Laravel untouched |  |  |

## Risks observed

-  
-  

## Decision

- [ ] Approve next phase
- [ ] Needs fixes
- [ ] Roll back to FastAPI realtime (flag off)

