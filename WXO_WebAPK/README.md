# WXO Web APK — Website as Android App

Ye APK **rmsurveyai.com** website ko andar load karti hai.  
Wallet · Games · Tournaments · Referral · Profile — sab **isi APK** me.

Package: `fun.wxo.app`  
Scheme: `wxo://`

## Build (Windows PowerShell)

```powershell
cd D:\UnityProjects\matchiq\WXO_WebAPK

npm install

npx expo prebuild --platform android --clean

npx expo run:android
```

Release APK:

```powershell
cd D:\UnityProjects\matchiq\WXO_WebAPK\android
.\gradlew.bat assembleRelease
```

APK path (typical):

```
android\app\build\outputs\apk\release\app-release.apk
```

Rename / copy to website:

```powershell
Copy-Item ...\app-release.apk D:\UnityProjects\matchiq\Deployment\public\WXO-release.apk
```

Upload:

```bash
scp "/d/UnityProjects/matchiq/Deployment/public/WXO-release.apk" root@72.60.102.128:/home/rmsurveyai/htdocs/rmsurveyai.com/public/WXO-release.apk
```

## Note

- Website pe pehle `wxo-wallet.js` update upload karo (in-app Play support).
- Real Unity 3D board ke liye alag `MatchIQ_App` + Unity embed chahiye; is Web APK me website wala play + win popup chalega.
