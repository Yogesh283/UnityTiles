#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Exports Unity as an Android Library for embedding inside MatchIQ_App (React Native).
/// Output: ../MatchIQ_App/unity/builds/android/unityLibrary
/// </summary>
public static class MatchIQReactNativeExport
{
    private const string RelativeExportPath = "../MatchIQ_App/unity/builds/android";

    [MenuItem("Match IQ/Export Android Library for React Native", false, 60)]
    public static void ExportAndroidLibraryForReactNative()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string exportPath = Path.GetFullPath(Path.Combine(projectRoot, RelativeExportPath));

        if (!Application.isBatchMode)
        {
            if (!EditorUtility.DisplayDialog(
                    "Export for React Native",
                    "This will export Unity as an Android Library into:\n\n" + exportPath +
                    "\n\nThen rebuild MatchIQ_App APK so React Native UI + Unity are in ONE app.",
                    "Export",
                    "Cancel"))
            {
                return;
            }
        }

        bool ok = ExportAndroidLibraryInternal(exportPath);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(ok ? 0 : 1);
            return;
        }

        if (ok)
        {
            EditorUtility.DisplayDialog(
                "Export complete",
                "Unity library exported.\n\nNext (in terminal):\n" +
                "cd MatchIQ_App\n" +
                "npx expo prebuild --platform android --clean\n" +
                "npx expo run:android\n\n" +
                "Result: one APK with React Native UI + Unity gameplay.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Export failed", "See Console for details.", "OK");
        }
    }

    /// <summary>Batchmode: -executeMethod MatchIQReactNativeExport.ExportAndroidLibraryBatch</summary>
    public static void ExportAndroidLibraryBatch()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string exportPath = Path.GetFullPath(Path.Combine(projectRoot, RelativeExportPath));
        bool ok = ExportAndroidLibraryInternal(exportPath);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool ExportAndroidLibraryInternal(string exportPath)
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(
                    BuildTargetGroup.Android,
                    BuildTarget.Android))
            {
                Debug.LogError("[Match IQ] Could not switch build target to Android.");
                return false;
            }
        }

        if (Directory.Exists(exportPath))
        {
            try
            {
                Directory.Delete(exportPath, true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[Match IQ] Could not fully clear export folder: " + ex.Message);
            }
        }

        Directory.CreateDirectory(exportPath);

        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;

        string[] scenes = MatchIQPlayStoreBuildScenes();
        if (scenes.Length == 0)
        {
            Debug.LogError("[Match IQ] No scenes enabled in Build Settings.");
            return false;
        }

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exportPath,
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.AcceptExternalModificationsToPlayer
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError("[Match IQ] Unity export failed: " + report.summary.result);
            return false;
        }

        StripLauncherIntentFilter(exportPath);
        Debug.Log("[Match IQ] Unity Android library exported to " + exportPath);
        return true;
    }

    private static string[] MatchIQPlayStoreBuildScenes()
    {
        var list = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene != null && scene.enabled && !string.IsNullOrEmpty(scene.path))
                list.Add(scene.path);
        }

        return list.ToArray();
    }

    private static void StripLauncherIntentFilter(string exportPath)
    {
        string manifest = Path.Combine(exportPath, "unityLibrary", "src", "main", "AndroidManifest.xml");
        if (!File.Exists(manifest))
        {
            Debug.LogWarning("[Match IQ] unityLibrary AndroidManifest.xml not found after export.");
            return;
        }

        string xml = File.ReadAllText(manifest);
        string stripped = System.Text.RegularExpressions.Regex.Replace(
            xml,
            @"<intent-filter>\s*<action android:name=""android\.intent\.action\.MAIN""\s*/>\s*<category android:name=""android\.intent\.category\.LAUNCHER""\s*/>\s*</intent-filter>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (stripped != xml)
        {
            File.WriteAllText(manifest, stripped);
            Debug.Log("[Match IQ] Removed LAUNCHER intent-filter from unityLibrary (library-only).");
        }
    }
}
#endif
