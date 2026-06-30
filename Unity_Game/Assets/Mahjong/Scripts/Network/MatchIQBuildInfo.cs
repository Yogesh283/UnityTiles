using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Logs embedded git commit at startup so APK age can be verified via logcat.
    /// Written by Match IQ → Prepare APK Build in the Editor.
    /// </summary>
    public static class MatchIQBuildInfo
    {
        private const string ResourceName = "MatchIQBuildInfo";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LogOnStartup()
        {
            string commit = "unknown";
            var asset = Resources.Load<TextAsset>(ResourceName);
            if (asset && !string.IsNullOrWhiteSpace(asset.text))
                commit = asset.text.Trim().Split('\n')[0].Trim();

            Debug.Log("Match IQ Build\nCommit: " + commit);
        }
    }
}
