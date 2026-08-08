# Unity + React Native — ONE APK

Haan, goal yahi hai:

**React Native UI + Unity game = ek hi app** (`fun.matchiq.app`)

Abhi jo `MatchIQ-UI-debug.apk` hai woh **sirf UI** hai. Unity tab aayegi jab Unity Editor se library export hogi.

---

## Tumhe yeh 2 steps karne hain

### Step 1 — Unity Editor (zaroori)

1. `Unity_Game` project Unity mein kholo  
2. Menu: **Match IQ → Export Android Library for React Native**  
3. Export folder hoga:
   ```
   MatchIQ_App/unity/builds/android
   ```
4. Complete hone tak wait karo (pehli baar 10–30 min)

### Step 2 — React Native APK (Unity ke saath)

PowerShell:

```powershell
cd D:\UnityProjects\matchiq\MatchIQ_App

# Unity native module ON
Remove-Item .\react-native.config.js -ErrorAction SilentlyContinue

npx expo prebuild --platform android --clean
npx expo run:android
```

Phone USB + debugging ON hona chahiye.

---

## Play flow (ek app ke andar)

Home → **▶ PLAY UNITY GAME** → Unity board (same APK) → win/lose → Victory/Defeat screen

---

## Important

| Mode | Result |
|------|--------|
| Expo Go | Unity embed **nahi** chalega |
| UI-only APK (abhi wali) | Sirf React screens |
| Unity export + `run:android` | **RN + Unity ek saath** |

Alag `com.matchiq.game` APK ki zaroorat nahi jab embed ready ho.
