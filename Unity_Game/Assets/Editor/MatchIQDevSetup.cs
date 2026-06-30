#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click Match IQ dev modes: local tournament testing vs production server.
/// </summary>
public static class MatchIQDevSetup
{
    private const string ApiConfigPath = "Assets/Mahjong/Resources/Network/ApiConfig.asset";
    private const string BuildInfoPath = "Assets/Mahjong/Resources/MatchIQBuildInfo.txt";
    private const string TournamentScenePath = "Assets/Mahjong/Scenes/3_Tournaments.unity";
    private const string SplashLogoPath = "Assets/Mahjong/Resources/Landing/AppLogo.png";

    [MenuItem("Match IQ/Local Tournament Testing (PC, no server)", false, 0)]
    public static void EnableLocalTournamentTesting()
    {
        ApplyApiConfig(localSimulation: true, useProductionUrl: false);
        Debug.Log(
            "[Match IQ] Local tournament testing ON.\n" +
            "• No server / no APK needed\n" +
            "• Open scene: Match IQ → Open Tournament Test Scene\n" +
            "• Press Play → Join 1 vs 1 Duel");
    }

    [MenuItem("Match IQ/2 Player Duel Test (2 PCs / ParrelSync)", false, 2)]
    public static void EnableTwoPlayerDuelTest()
    {
        ApplyApiConfig(localSimulation: false, useProductionUrl: true);
        Debug.Log(
            "[Match IQ] 2-player duel test mode ON (real server, 2 real players).\n\n" +
            "OPTION A — ParrelSync (2 Unity windows on same PC):\n" +
            "1. Install ParrelSync from Package Manager → Add from git URL:\n" +
            "   https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync\n" +
            "2. ParrelSync → Clones Manager → Create clone\n" +
            "3. Open ORIGINAL project → Play Tournament Scene → Join 1v1 Duel\n" +
            "4. Open CLONE project → Play Tournament Scene → Join 1v1 Duel\n" +
            "5. Both match in same room on api.matchiq.fun\n\n" +
            "OPTION B — PC + Phone (no ParrelSync):\n" +
            "1. This PC: Production Server Testing + Play → Join 1v1\n" +
            "2. Phone: APK with production server → Join 1v1\n\n" +
            "Note: Local Tournament Testing uses a BOT — not 2 humans.");
    }

    [MenuItem("Match IQ/Production Server Testing (PC + api.matchiq.fun)", false, 11)]
    public static void EnableProductionServerTesting()
    {
        ApplyApiConfig(localSimulation: false, useProductionUrl: true);
        Debug.Log(
            "[Match IQ] Production server testing ON.\n" +
            "• Uses https://api.matchiq.fun\n" +
            "• For final check before APK build");
    }

    [MenuItem("Match IQ/Open Tournament Test Scene", false, 20)]
    public static void OpenTournamentScene()
    {
        if (!System.IO.File.Exists(TournamentScenePath))
        {
            Debug.LogError("[Match IQ] Tournament scene not found: " + TournamentScenePath);
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(TournamentScenePath);
    }

    [MenuItem("Match IQ/Play Tournament Scene Now", false, 21)]
    public static void PlayTournamentScene()
    {
        OpenTournamentScene();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Match IQ/Prepare APK Build (production server)", false, 40)]
    public static void PrepareApkBuild()
    {
        ApplyApiConfig(localSimulation: false, useProductionUrl: true);
        FixAndroidSplashTexture();
        MatchIQAppIconSetup.ApplyFromMenu();
        string commit = WriteBuildInfoFile();
        Debug.Log(
            "[Match IQ] Ready for Release APK build.\n" +
            "• Development Mode OFF\n" +
            "• Production URL ON\n" +
            "• Icons applied\n" +
            "• Build commit embedded: " + commit + "\n" +
            "• Now File → Build Settings → Build\n" +
            "• Verify on phone: adb logcat | findstr \"Match IQ Build\"");
    }

    /// <summary>Embeds git short hash into Resources for runtime logcat verification.</summary>
    public static string WriteBuildInfoFile()
    {
        string repoRoot = Directory.GetParent(Application.dataPath)?.Parent?.FullName;
        string commit = "unknown";
        string subject = "";

        if (!string.IsNullOrEmpty(repoRoot))
        {
            commit = RunGit(repoRoot, "rev-parse --short HEAD") ?? commit;
            subject = RunGit(repoRoot, "log -1 --pretty=%s") ?? "";
        }

        string body = string.IsNullOrEmpty(subject) ? commit : commit + "\n" + subject;
        string fullPath = Path.Combine(Application.dataPath, "Mahjong/Resources/MatchIQBuildInfo.txt");
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Application.dataPath);
        File.WriteAllText(fullPath, body);
        AssetDatabase.ImportAsset(BuildInfoPath);
        AssetDatabase.SaveAssets();
        return commit;
    }

    private static string RunGit(string workingDirectory, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("git", arguments)
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(psi);
            if (process == null)
                return null;
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 && !string.IsNullOrEmpty(output) ? output : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyApiConfig(bool localSimulation, bool useProductionUrl)
    {
        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ApiConfigPath);
        if (!config)
        {
            Debug.LogError("[Match IQ] ApiConfig not found at " + ApiConfigPath);
            return;
        }

        var serialized = new SerializedObject(config);
        serialized.FindProperty("developmentMode").boolValue = localSimulation;
        serialized.FindProperty("useProductionUrl").boolValue = useProductionUrl;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Android splash uses Texture2D; Unity splash logos/backgrounds use Sprite (same image, different sub-asset).
    /// </summary>
    private static void FixAndroidSplashTexture()
    {
        Texture2D splashTexture = LoadTextureAsset(SplashLogoPath);
        Sprite splashSprite = LoadSpriteAsset(SplashLogoPath);
        if (!splashTexture && !splashSprite)
        {
            Debug.LogWarning("[Match IQ] Splash logo not found at " + SplashLogoPath);
            return;
        }

        Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projectSettings == null || projectSettings.Length == 0)
            return;

        var ps = new SerializedObject(projectSettings[0]);
        if (splashTexture)
            SetObjectRef(ps, "androidSplashScreen", splashTexture);

        if (splashSprite)
        {
            SetObjectRef(ps, "splashScreenBackgroundSourceLandscape", splashSprite);
            SetObjectRef(ps, "splashScreenBackgroundSourcePortrait", splashSprite);

            SerializedProperty logos = ps.FindProperty("m_SplashScreenLogos");
            if (logos != null && logos.arraySize > 0)
            {
                SerializedProperty logo = logos.GetArrayElementAtIndex(0).FindPropertyRelative("logo");
                if (logo != null)
                    logo.objectReferenceValue = splashSprite;
            }
        }

        ps.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectRef(SerializedObject ps, string propertyName, Object value)
    {
        SerializedProperty prop = ps.FindProperty(propertyName);
        if (prop != null)
            prop.objectReferenceValue = value;
    }

    private static Texture2D LoadTextureAsset(string assetPath)
    {
        var main = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (main)
            return main;

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Texture2D texture)
                return texture;
        }

        return null;
    }

    private static Sprite LoadSpriteAsset(string assetPath)
    {
        var main = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (main)
            return main;

        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (asset is Sprite sprite)
                return sprite;
        }

        return null;
    }
}
#endif
