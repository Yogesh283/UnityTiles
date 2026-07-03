#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Production Play Store AAB build (api.matchiq.fun, version 1.0.0).
/// </summary>
public static class MatchIQPlayStoreBuild
{
    public const string AppVersion = "1.0.0";
    public const int AndroidVersionCode = 1;
    private const string OutputDir = "Builds/Android";

    [MenuItem("Match IQ/Build Play Store AAB (Production v1.0.0)", false, 50)]
    public static void BuildPlayStoreAabFromMenu()
    {
        BuildPlayStoreAab();
    }

    /// <summary>Called from Unity batchmode: -executeMethod MatchIQPlayStoreBuild.BuildPlayStoreAab</summary>
    public static void BuildPlayStoreAab()
    {
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
                "• Server: https://api.matchiq.fun\n" +
                "• File: " + fullPath);
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError("[Match IQ] AAB build failed: " + report.summary.result);
        EditorApplication.Exit(1);
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
}
#endif
