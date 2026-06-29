# Match IQ — Git PR & Server Deploy

**Server:** `72.60.102.128` | **Path:** `/var/www/UnityTiles`

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

## 2. Server — Git Deploy

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
