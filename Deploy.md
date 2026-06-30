# Match IQ — Git PR & Server Deploy

**Server:** `72.60.102.128` | **Path:** `/var/www/UnityTiles`

---

## 0. Local tournament testing (PC, no APK)

Unity menu:

| Menu | Use |
|------|-----|
| **Match IQ → Local Tournament Testing** | Offline — no server, simulated 1v1 bot |
| **Match IQ → Open Tournament Test Scene** | Opens `3_Tournaments` scene |
| **Match IQ → Play Tournament Scene Now** | Scene + Play in one click |
| **Match IQ → Production Server Testing** | Real API before APK |
| **Match IQ → Prepare APK Build** | Production URL + app icons |

**Local test steps:**
1. `Match IQ → Local Tournament Testing`
2. `Match IQ → Play Tournament Scene Now`
3. **1 vs 1 Duel** → **JOIN** (5000 test coins auto-set)

**Final APK:** `Match IQ → Prepare APK Build` → Build APK → uninstall old app → install.

### 2 real players (1v1 duel) — NOT offline local mode

**Local Tournament Testing = 1 human + bot only.** For 2 real players use server:

`Match IQ → 2 Player Duel Test (2 PCs / ParrelSync)`

**ParrelSync (best on PC):**
1. Package Manager → Add from git URL: `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`
2. **ParrelSync → Clones Manager → Create clone**
3. **Original** Unity → Play → Tournaments → Join **1 vs 1 Duel**
4. **Clone** Unity → Play → Tournaments → Join **1 vs 1 Duel**
5. Dono same room mein match honge (`api.matchiq.fun`)

**PC + Phone:** Editor (Production Server) + phone APK — dono Join 1v1.

---

## 1. Local — Git PR (Windows)

```powershell
cd C:\Users\yogib\TilesClash

git checkout fix/play-store-ui-and-tournament-join

git add Backend/tournament/level_selector.py Deploy.md .gitignore
git add Unity_Game/Assets/Mahjong/Scripts/MKUtils/GUI/WarningMessController.cs
git add Unity_Game/Assets/Mahjong/Scripts/Network/TournamentJoinCoordinator.cs

git -c user.name="Yogesh Kumar" -c user.email="Yogesh283@users.noreply.github.com" commit -m "Fix tournament join level_seed overflow and popup errors"

git push -u origin fix/play-store-ui-and-tournament-join
```

PR create: https://github.com/Yogesh283/UnityTiles/compare/main...fix/play-store-ui-and-tournament-join

GitHub par **Merge pull request** karo.

**Note:** `git add .` mat use karo — APK/build files commit ho jayengi.

---

## 2. Server — Tournament deploy (NOT `origin/main`)

**Do not use `git pull origin main`** — tournament realtime code is only on:

`feat/realtime-tournament-and-ui-fixes` (required commit: `1bc9567`)

```bash
ssh root@72.60.102.128

cd /var/www/UnityTiles
bash Deployment/deploy-tournament.sh
bash Deployment/verify-production.sh
```

Verify from anywhere:

```bash
curl -s https://api.matchiq.fun/health
# must include: "commit":"1bc9567"
```

Watch WebSocket during phone test:

```bash
journalctl -u matchiq-api -f | grep -Ei 'WebSocket|accepted|join|match_start|match_finished'
```

### APK (both phones)

1. Unity: **Match IQ → Prepare APK Build** (embeds git commit)
2. Build APK → uninstall old app → install on both phones
3. `adb logcat | findstr "Match IQ Build"` → must show `Commit: 1bc9567`

---

## 2b. Server — Admin / DB only (legacy)

```bash
ssh root@72.60.102.128

cd /var/www/UnityTiles
git pull origin main

grep -v "^USE \`game\`" Database/migrations/003_production_upgrade.sql | mysql -u Game -p'Game@123' Game

systemctl restart matchiq-api
cd AdminPanel && php artisan config:cache
```

Verify:

```bash
curl -s https://api.matchiq.fun/health
```
