# Match IQ — Android Release Keystore

Play Store AAB/APK must be signed with a **release** keystore (not Unity debug).

## Local setup

1. Keep these files on this machine (and a secure backup):
   - `matchiq-release.keystore`
   - `signing.local.json` (passwords)
2. Both are gitignored. **If you lose the keystore/passwords, you cannot update the same Play app.**
3. Builds read `signing.local.json` automatically via `Match IQ → Build Play Store AAB`.

## Create / replace keystore (only if you don't have one)

```powershell
$keytool = "C:\Program Files\Unity\Hub\Editor\6000.3.17f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
& $keytool -genkeypair -v `
  -keystore matchiq-release.keystore `
  -alias matchiq `
  -keyalg RSA -keysize 2048 -validity 10000 `
  -storepass YOUR_STORE_PASS -keypass YOUR_KEY_PASS `
  -dname "CN=Match IQ, OU=Mobile, O=Yogesh Kumar, C=IN"
```

Then create `signing.local.json`:

```json
{
  "keystore": "Keystore/matchiq-release.keystore",
  "alias": "matchiq",
  "storePassword": "YOUR_STORE_PASS",
  "keyPassword": "YOUR_KEY_PASS"
}
```
