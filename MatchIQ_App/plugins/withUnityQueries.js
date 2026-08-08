const { withAndroidManifest } = require('@expo/config-plugins');

/**
 * Lets Match IQ UI discover / open the Unity APK (matchiqunity:// + package).
 * Required on Android 11+ package visibility.
 */
function withUnityQueries(config) {
  return withAndroidManifest(config, (config) => {
    const manifest = config.modResults.manifest;
    if (!manifest.queries) {
      manifest.queries = [];
    }

    const queries = manifest.queries;
    const hasPackage = queries.some(
      (q) => q.package?.some((p) => p.$?.['android:name'] === 'com.matchiq.game'),
    );
    if (!hasPackage) {
      queries.push({
        package: [{ $: { 'android:name': 'com.matchiq.game' } }],
      });
    }

    const hasIntent = queries.some((q) =>
      q.intent?.some((intent) =>
        intent.data?.some((d) => d.$?.['android:scheme'] === 'matchiqunity'),
      ),
    );
    if (!hasIntent) {
      queries.push({
        intent: [
          {
            action: [{ $: { 'android:name': 'android.intent.action.VIEW' } }],
            data: [{ $: { 'android:scheme': 'matchiqunity' } }],
          },
        ],
      });
    }

    return config;
  });
}

module.exports = withUnityQueries;
