/**
 * Until Unity is exported to unity/builds/android/unityLibrary (real export),
 * keep native Unity module disabled so UI APK can build.
 *
 * AFTER "Match IQ → Export Android Library for React Native" in Unity:
 * delete this file (or remove the @azesmway block) then:
 *   npx expo prebuild --platform android --clean
 *   npx expo run:android
 */
const fs = require('fs');
const path = require('path');

const realUnityMarker = path.join(
  __dirname,
  'unity',
  'builds',
  'android',
  'unityLibrary',
  'src',
  'main',
  'assets',
);

const hasRealUnityExport = fs.existsSync(realUnityMarker);

module.exports = {
  dependencies: hasRealUnityExport
    ? {}
    : {
        '@azesmway/react-native-unity': {
          platforms: {
            android: null,
            ios: null,
          },
        },
      },
};
