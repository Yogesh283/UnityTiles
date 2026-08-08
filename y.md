# MatchIQ — Local → GitHub → Server

## 1) Local (PC) — commit + push

```powershell
cd D:\UnityProjects\matchiq

git add -A
git status --short
git commit -m "Your message here"
git push origin feat/realtime-tournament-and-ui-fixes
```

Push se pehle check karo ki koi bhaari build folder stage to nahi ho raha
(`node_modules`, `MatchIQ_App/unity`, `android/app/build` — ye `.gitignore` mein hain):

```powershell
git diff --cached --numstat | Measure-Object | Select-Object Count
```

Sirf push (agar pehle se commit ho chuka ho):

```powershell
cd D:\UnityProjects\matchiq
git push origin feat/realtime-tournament-and-ui-fixes
```

Remote: `https://github.com/Yogesh283/UnityTiles.git`  
Branch: `feat/realtime-tournament-and-ui-fixes`

---

## 2) Server — SSH + pull

```bash
ssh root@72.60.102.128
```

Pehli baar clone (sirf ek baar):

```bash
mkdir -p /home/matchiq/htdocs/UnityTiles && cd /home/matchiq/htdocs/UnityTiles && git clone -b feat/realtime-tournament-and-ui-fixes https://github.com/Yogesh283/UnityTiles.git . && git remote -v && ls
```

Har baar update:

```bash
cd /home/matchiq/htdocs/UnityTiles && git pull origin feat/realtime-tournament-and-ui-fixes
```

---

## 3) Deploy / verify (optional)

```bash
cd /home/matchiq/htdocs/UnityTiles
bash Deployment/deploy-tournament.sh
bash Deployment/verify-production.sh
```

---

## Important

- `rmsurveyai.com` / `RMserveay.git` = alag project — MatchIQ wahan mat pull karo
- MatchIQ path: `/home/matchiq/htdocs/UnityTiles`
- `git push` sirf committed files bhejta hai
- Ye folders jaan-bujh kar `.gitignore` mein hain (kul ~11 GB build junk):
  `MatchIQ_App/node_modules`, `MatchIQ_App/unity`, `MatchIQ_App/android/app/build`,
  `WXO_WebAPK/node_modules`, root ka stray `Library/ Packages/ ProjectSettings/`,
  aur `_tmp_RMserveay/` (alag repo ka nested clone)
- Server pe pull ke baad backend restart karna padta hai (naye referral routes ke liye)
