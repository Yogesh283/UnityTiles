# Match IQ — React Native App

Premium dark-fantasy tournament UI shell for **Match IQ**.  
React Native manages menus, economy, social, and results. **Unity handles gameplay only.**

## Stack

- Expo SDK 57 + Dev Client
- TypeScript
- React Navigation (native stack + bottom tabs)
- Reanimated + Gesture Handler
- Zustand + React Query + Axios
- Cinzel + Inter fonts

## Run

```bash
cd MatchIQ_App
npm install
npx expo start
```

Dev client (recommended for Unity Intent testing):

```bash
npx expo start --dev-client
```

## Folder structure

```
src/
  components/   reusable AAA UI kit
  screens/      50 production screens
  navigation/   Auth + MainTabs + App stack
  hooks/        React Query hooks
  api/          Axios client + dummyApi
  services/     UnityBridge
  store/        Zustand (auth, player, ui)
  theme/        colors, typography, spacing
  types/        shared models
  constants/    routes + schemes
```

## Unity embedded (one APK)

React Native UI + Unity gameplay ship in **one** Android APK (`fun.matchiq.app`).

See **[UNITY_EMBED.md](./UNITY_EMBED.md)** for Unity Export Project → `unity/builds/android` → `npx expo run:android`.

Play: Home → **PLAY UNITY GAME** → embedded Unity view (not a second APK).

## Unity bridge contract (standalone / deep link fallback)

### Launch (RN → Unity)

```
matchiqunity://play?matchId=...&mode=tournament|campaign|practice&token=...&tournamentId=...
```

Android also tries a package Intent to `com.matchiq.game` if the scheme alone fails (legacy two-app mode).

### Result (Unity → RN)

```
matchiq://match-result?matchId=...&won=true&score=1400&timeSeconds=120&accuracy=92&coinsEarned=200&xpEarned=80&opponentName=TempleFox&tournamentId=...
```

Embedded mode sends the same fields as JSON via `onUnityMessage`.

## API

Dummy services live in `src/api/dummyApi.ts` and mirror Backend `/api/v1` shapes (`auth`, `wallet`, `tournaments`, `leaderboard`, `notifications`, `payments`).  
Set real API base with:

```
EXPO_PUBLIC_API_URL=https://rmsurveyai.com/api/v1
```

## Design

Premium dark esports UI (aligned with Flutter MATCH IQ):
- Background `#090B18` · Purple `#7B2FF7` · Blue `#2F80ED` · Gold `#F5B700`
- Orbitron + Inter fonts
- Pool cards: 10 / 50 / 100 / 500 / 1000 players
- Create Pool, Wallet, Leaderboard, Mobile OTP login

## Run

```bash
cd MatchIQ_App
npm install
npx expo start --go
```
