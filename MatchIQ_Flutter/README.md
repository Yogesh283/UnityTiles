# MATCH IQ — Flutter

Premium dark esports tournament UI (Android-first).

## Stack

- Flutter + Material 3
- Riverpod
- GoRouter
- Google Fonts (Orbitron + Inter)
- flutter_animate + shimmer

## Run

```bash
cd MatchIQ_Flutter
flutter pub get
flutter run
```

Flutter SDK used on this machine: `D:\flutter\bin`

## Structure

```
lib/
  main.dart
  app.dart
  core/theme|constants|utils
  data/models|repositories
  presentation/
    providers/
    router/
    widgets/
    screens/
```

## Screens

Splash · Login/OTP/Google/Guest · Home · Tournament list/detail · Create Pool · Wallet · Leaderboard · Profile · Bottom nav shell

## Design tokens

| Token | Hex |
|-------|-----|
| Background | `#090B18` |
| Purple | `#7B2FF7` |
| Blue | `#2F80ED` |
| Gold | `#F5B700` |
| Green | `#22C55E` |

Dummy data lives in `lib/data/repositories/match_iq_repository.dart` — swap for real API later.
