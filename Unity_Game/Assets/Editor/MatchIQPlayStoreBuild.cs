#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Production Play Store AAB/APK build (api.matchiq.fun, release-signed).
/// </summary>
public static class MatchIQPlayStoreBuild
{
    public const string AppVersion = "1.0.8";
    public const int AndroidVersionCode = 28;
    public const string AndroidPackageId = "com.matchiq.game";
    public const int AndroidTargetSdk = 35;
    private const string OutputDir = "Builds/Android";
    private const string SigningConfigPath = "Keystore/signing.local.json";

    [MenuItem("Match IQ/Build Production APK (Live api.matchiq.fun v1.0.8)", false, 49)]
    public static void BuildProductionApkFromMenu()
    {
        BuildProductionApk();
    }

    /// <summary>Called from Unity batchmode: -executeMethod MatchIQPlayStoreBuild.BuildProductionApk</summary>
    public static void BuildProductionApk()
    {
        if (!ApplyReleaseSigning())
        {
            EditorApplication.Exit(1);
            return;
        }

        ApplyProductionConfig();
        ApplyVersion();
        MatchIQDevSetup.WriteBuildInfoFile();
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                Debug.LogError("[Match IQ] Failed to switch build target to Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, "MatchIQ-" + AppVersion + ".apk");

        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[Match IQ] No scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            string fullPath = Path.GetFullPath(outputPath);
            Debug.Log(
                "[Match IQ] Production APK ready.\n" +
                "• Version: " + AppVersion + " (" + AndroidVersionCode + ")\n" +
                "• Signed: release keystore\n" +
                "• Server: https://api.matchiq.fun\n" +
                "• File: " + fullPath);
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError("[Match IQ] APK build failed: " + report.summary.result);
        EditorApplication.Exit(1);
    }

    [MenuItem("Match IQ/Build Play Store AAB (Production v1.0.8)", false, 50)]
    public static void BuildPlayStoreAabFromMenu()
    {
        BuildPlayStoreAab();
    }

    /// <summary>Called from Unity batchmode: -executeMethod MatchIQPlayStoreBuild.BuildPlayStoreAab</summary>
    public static void BuildPlayStoreAab()
    {
        if (!ApplyReleaseSigning())
        {
            EditorApplication.Exit(1);
            return;
        }

        ApplyProductionConfig();
        ApplyVersion();
        MatchIQDevSetup.WriteBuildInfoFile();
        AssetDatabase.SaveAssets();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                Debug.LogError("[Match IQ] Failed to switch build target to Android.");
                EditorApplication.Exit(1);
                return;
            }
        }

        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

        Directory.CreateDirectory(OutputDir);
        string outputPath = Path.Combine(OutputDir, "MatchIQ-" + AppVersion + ".aab");

        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[Match IQ] No scenes enabled in Build Settings.");
            EditorApplication.Exit(1);
            return;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.CompressWithLz4HC
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == BuildResult.Succeeded)
        {
            string fullPath = Path.GetFullPath(outputPath);
            Debug.Log(
                "[Match IQ] Play Store AAB ready.\n" +
                "• Version: " + AppVersion + " (" + AndroidVersionCode + ")\n" +
                "• Signed: release keystore\n" +
                "• Server: https://api.matchiq.fun\n" +
                "• File: " + fullPath);
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError("[Match IQ] AAB build failed: " + report.summary.result);
        EditorApplication.Exit(1);
    }

    /// <summary>
    /// Loads Keystore/signing.local.json and configures PlayerSettings for release signing.
    /// Without this, Unity signs with the debug key and Play Console rejects the upload.
    /// </summary>
    public static bool ApplyReleaseSigning()
    {
        string configPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SigningConfigPath));
        if (!File.Exists(configPath))
        {
            Debug.LogError(
                "[Match IQ] Missing release signing config: " + configPath + "\n" +
                "See Unity_Game/Keystore/README.md — Play Store rejects debug-signed AABs.");
            return false;
        }

        string json = File.ReadAllText(configPath);
        var cfg = JsonUtility.FromJson<SigningLocalConfig>(json);
        if (cfg == null
            || string.IsNullOrWhiteSpace(cfg.keystore)
            || string.IsNullOrWhiteSpace(cfg.alias)
            || string.IsNullOrWhiteSpace(cfg.storePassword)
            || string.IsNullOrWhiteSpace(cfg.keyPassword))
        {
            Debug.LogError("[Match IQ] Invalid signing.local.json — need keystore, alias, storePassword, keyPassword.");
            return false;
        }

        string keystorePath = cfg.keystore;
        if (!Path.IsPathRooted(keystorePath))
            keystorePath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", keystorePath));

        if (!File.Exists(keystorePath))
        {
            Debug.LogError("[Match IQ] Keystore file not found: " + keystorePath);
            return false;
        }

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = cfg.storePassword;
        PlayerSettings.Android.keyaliasName = cfg.alias;
        PlayerSettings.Android.keyaliasPass = cfg.keyPassword;

        Debug.Log("[Match IQ] Release signing enabled: " + keystorePath + " (alias: " + cfg.alias + ")");
        return true;
    }

    private static void ApplyProductionConfig()
    {
        const string apiConfigPath = "Assets/Mahjong/Resources/Network/ApiConfig.asset";
        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(apiConfigPath);
        if (!config)
        {
            Debug.LogError("[Match IQ] ApiConfig not found.");
            return;
        }

        var serialized = new SerializedObject(config);
        serialized.FindProperty("developmentMode").boolValue = false;
        serialized.FindProperty("useProductionUrl").boolValue = true;
        serialized.FindProperty("productionUrl").stringValue = "https://api.matchiq.fun";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        SetInsecureHttpOption(false);
        SetAndroidCleartextTraffic(false);
        MatchIQAppIconSetup.ApplyFromMenu();
    }

    private static void ApplyVersion()
    {
        PlayerSettings.bundleVersion = AppVersion;
        PlayerSettings.Android.bundleVersionCode = AndroidVersionCode;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)AndroidTargetSdk;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, AndroidPackageId);
    }

    private static string[] GetEnabledScenes()
    {
        EditorBuildSettingsScene[] settings = EditorBuildSettings.scenes;
        var scenes = new System.Collections.Generic.List<string>();
        for (int i = 0; i < settings.Length; i++)
        {
            if (settings[i].enabled && !string.IsNullOrEmpty(settings[i].path))
                scenes.Add(settings[i].path);
        }

        return scenes.ToArray();
    }

    private static void SetInsecureHttpOption(bool allow)
    {
        Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projectSettings == null || projectSettings.Length == 0)
            return;

        var ps = new SerializedObject(projectSettings[0]);
        SerializedProperty prop = ps.FindProperty("insecureHttpOption");
        if (prop == null)
            return;

        prop.intValue = allow ? 2 : 0;
        ps.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetAndroidCleartextTraffic(bool allow)
    {
        const string manifestPath = "Assets/Plugins/Android/AndroidManifest.xml";
        if (!File.Exists(manifestPath))
            return;

        string text = File.ReadAllText(manifestPath);
        string value = allow ? "true" : "false";
        string updated = System.Text.RegularExpressions.Regex.Replace(
            text,
            "android:usesCleartextTraffic=\"(true|false)\"",
            "android:usesCleartextTraffic=\"" + value + "\"");
        if (updated != text)
        {
            File.WriteAllText(manifestPath, updated);
            AssetDatabase.ImportAsset(manifestPath);
        }
    }

    [System.Serializable]
    private class SigningLocalConfig
    {
        public string keystore;
        public string alias;
        public string storePassword;
        public string keyPassword;
    }
}
#endif
