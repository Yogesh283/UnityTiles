const fs = require('fs');
const path = require('path');
const {
  withProjectBuildGradle,
  withSettingsGradle,
  withGradleProperties,
  withStringsXml,
  withDangerousMod,
  withAndroidManifest,
  withAppBuildGradle,
  AndroidConfig,
} = require('@expo/config-plugins');

const UNITY_REL = 'unity/builds/android';
const DEFAULT_NDK = '27.0.12077973';

function unityLibraryExists(projectRoot) {
  return fs.existsSync(
    path.join(projectRoot, UNITY_REL, 'unityLibrary', 'build.gradle'),
  );
}

function withUnityFlatDir(config) {
  return withProjectBuildGradle(config, (mod) => {
    if (mod.modResults.contents.includes("project(':unityLibrary')")) {
      return mod;
    }
    const projectRoot = mod.modRequest.projectRoot;
    if (!unityLibraryExists(projectRoot)) {
      return mod;
    }
    const flatDir = `
        flatDir {
            dirs "\${project(':unityLibrary').projectDir}/libs"
        }
`;
    if (mod.modResults.contents.includes("maven { url 'https://www.jitpack.io' }")) {
      mod.modResults.contents = mod.modResults.contents.replace(
        "maven { url 'https://www.jitpack.io' }",
        `maven { url 'https://www.jitpack.io' }\n${flatDir}`,
      );
    } else if (mod.modResults.contents.includes('allprojects')) {
      mod.modResults.contents = mod.modResults.contents.replace(
        /allprojects\s*\{\s*repositories\s*\{/,
        (m) => `${m}\n${flatDir}`,
      );
    }
    return mod;
  });
}

function withUnitySettings(config) {
  return withSettingsGradle(config, (mod) => {
    if (mod.modResults.contents.includes("include ':unityLibrary'")) {
      return mod;
    }
    const projectRoot = mod.modRequest.projectRoot;
    if (!unityLibraryExists(projectRoot)) {
      mod.modResults.contents += `
// Match IQ: unityLibrary not exported yet — skip include.
// Export Unity to ${UNITY_REL} then re-run prebuild.
`;
      return mod;
    }
    mod.modResults.contents += `
include ':unityLibrary'
project(':unityLibrary').projectDir = new File(rootProject.projectDir, '../${UNITY_REL}/unityLibrary')
include ':unityLibrary:GoogleMobileAdsPlugin.androidlib'
`;
    return mod;
  });
}

function upsertGradleProp(modResults, key, value) {
  const existing = modResults.find((p) => p.type === 'property' && p.key === key);
  if (existing) {
    existing.value = value;
  } else {
    modResults.push({ type: 'property', key, value });
  }
}

function withUnityGradleProps(config) {
  return withGradleProperties(config, (mod) => {
    const unityNdk =
      process.env.UNITY_ANDROID_NDK_PATH ||
      'C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK';
    const unitySdk =
      process.env.UNITY_ANDROID_SDK_PATH ||
      'C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK';
    const unityJdk =
      process.env.UNITY_JDK_PATH ||
      'C:/Program Files/Unity/Hub/Editor/6000.3.17f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK';

    upsertGradleProp(mod.modResults, 'unityStreamingAssets', '.unity3d');
    // Il2Cpp build reads these from the RN root gradle.properties
    upsertGradleProp(mod.modResults, 'unity.androidNdkPath', unityNdk);
    upsertGradleProp(mod.modResults, 'unity.androidSdkPath', unitySdk);
    upsertGradleProp(mod.modResults, 'unity.androidNdkVersion', '27.2.12479018');
    upsertGradleProp(mod.modResults, 'unity.jdkPath', unityJdk);
    upsertGradleProp(mod.modResults, 'unity.debugSymbolLevel', 'none');
    upsertGradleProp(mod.modResults, 'reactNativeArchitectures', 'arm64-v8a');
    return mod;
  });
}

function withUnityStrings(config) {
  return withStringsXml(config, (mod) => {
    mod.modResults = AndroidConfig.Strings.setStringItem(
      [
        {
          _: 'Game View',
          $: { name: 'game_view_content_description' },
        },
      ],
      mod.modResults,
    );
    return mod;
  });
}

function withUnityLibraryPatch(config) {
  return withDangerousMod(config, [
    'android',
    async (mod) => {
      const projectRoot = mod.modRequest.projectRoot;
      if (!unityLibraryExists(projectRoot)) {
        console.warn(
          '[withUnityEmbed] Skipping unityLibrary patch — export Unity first to ' +
            UNITY_REL,
        );
        return mod;
      }

      const ndk =
        process.env.UNITY_NDK_VERSION ||
        config?.plugins?.find?.(
          (p) => Array.isArray(p) && p[0] === './plugins/withUnityEmbed.js',
        )?.[1]?.unityNdkVersion ||
        DEFAULT_NDK;

      const gradlePath = path.join(
        projectRoot,
        UNITY_REL,
        'unityLibrary',
        'build.gradle',
      );
      let gradle = fs.readFileSync(gradlePath, 'utf8');
      if (!gradle.includes('ndkVersion')) {
        gradle = gradle.replace(
          /defaultConfig\s*\{/,
          `defaultConfig {\n        ndkVersion "${ndk}"`,
        );
      }
      gradle = gradle.replace(/android\.ndkDirectory(?!\.absolutePath)/g, 'android.ndkDirectory.absolutePath');
      fs.writeFileSync(gradlePath, gradle);

      // Remove LAUNCHER intent-filter so Unity is library-only
      const manifestPath = path.join(
        projectRoot,
        UNITY_REL,
        'unityLibrary',
        'src',
        'main',
        'AndroidManifest.xml',
      );
      if (fs.existsSync(manifestPath)) {
        let xml = fs.readFileSync(manifestPath, 'utf8');
        // Strip MAIN/LAUNCHER regardless of action/category order (Unity export varies)
        xml = xml.replace(
          /<intent-filter>\s*(?:<(?:action|category)[^>]*\/>\s*){2,}<\/intent-filter>/g,
          (block) => {
            if (
              block.includes('android.intent.action.MAIN') &&
              block.includes('android.intent.category.LAUNCHER')
            ) {
              return '';
            }
            return block;
          },
        );
        fs.writeFileSync(manifestPath, xml);
      }

      return mod;
    },
  ]);
}

function withUnityManifestMerge(config) {
  return withAndroidManifest(config, (mod) => {
    const manifest = mod.modResults.manifest;
    if (!manifest.$) manifest.$ = {};
    if (!manifest.$['xmlns:tools']) {
      manifest.$['xmlns:tools'] = 'http://schemas.android.com/tools';
    }
    const app = manifest.application?.[0];
    if (app?.$) {
      app.$['android:allowBackup'] = 'false';
      const replace = app.$['tools:replace'];
      if (!replace) {
        app.$['tools:replace'] = 'android:allowBackup';
      } else if (!String(replace).includes('android:allowBackup')) {
        app.$['tools:replace'] = `${replace},android:allowBackup`;
      }
    }
    return mod;
  });
}

function withUnityMinSdk(config) {
  return withAppBuildGradle(config, (mod) => {
    if (mod.modResults.contents.includes('Math.max(rootProject.ext.minSdkVersion')) {
      return mod;
    }
    mod.modResults.contents = mod.modResults.contents.replace(
      /minSdkVersion\s+rootProject\.ext\.minSdkVersion/,
      'minSdkVersion Math.max(rootProject.ext.minSdkVersion as Integer, 26)',
    );
    return mod;
  });
}

function withUnityEmbed(config) {
  config = withUnityFlatDir(config);
  config = withUnitySettings(config);
  config = withUnityGradleProps(config);
  config = withUnityStrings(config);
  config = withUnityLibraryPatch(config);
  config = withUnityManifestMerge(config);
  config = withUnityMinSdk(config);
  return config;
}

module.exports = withUnityEmbed;
