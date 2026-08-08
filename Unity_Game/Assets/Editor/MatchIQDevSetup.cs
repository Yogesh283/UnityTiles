#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
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
    private const string GameScenePath = "Assets/Mahjong/Scenes/2_Game_Constructor.unity";
    private const string MapScenePath = "Assets/Mahjong/Scenes/1_Map_Simple.unity";
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

    [MenuItem("Match IQ/Local LAN Server Testing (same WiFi, 2 phones)...", false, 12)]
    public static void EnableLocalLanServerTesting()
    {
        string ip = LanIpPrompt.Show(ReadBaseUrl());
        if (string.IsNullOrWhiteSpace(ip))
            return;

        ip = ip.Trim();
        string lanUrl = "http://" + ip + ":8000";
        ApplyApiConfig(localSimulation: false, useProductionUrl: false, baseUrl: lanUrl);
        ConfigureAndroidHttp(true);

        Debug.Log(
            "[Match IQ] Local LAN ON → " + lanUrl + "\n" +
            "Backend: cd Backend && python main.py\n" +
            "Build once: Match IQ → Prepare LAN APK Build");
    }

    [MenuItem("Match IQ/Prepare LAN APK Build (local WiFi server)", false, 41)]
    public static void PrepareLanApkBuild()
    {
        EnableLocalLanServerTesting();
        FixAndroidSplashTexture();
        MatchIQAppIconSetup.ApplyFromMenu();
        string commit = WriteBuildInfoFile();
        Debug.Log("[Match IQ] LAN APK ready. Build once → install on BOTH phones. Commit: " + commit);
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

    [MenuItem("Match IQ/Open Game Scene", false, 22)]
    public static void OpenGameScene()
    {
        if (!System.IO.File.Exists(GameScenePath))
        {
            Debug.LogError("[Match IQ] Game scene not found: " + GameScenePath);
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(GameScenePath);
    }

    /// <summary>Opens 2_Game_Constructor and starts Play Mode (campaign board).</summary>
    [MenuItem("Match IQ/Play Game Scene Now", false, 23)]
    public static void PlayGameScene()
    {
        OpenGameScene();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Match IQ/Open Map Scene", false, 24)]
    public static void OpenMapScene()
    {
        if (!System.IO.File.Exists(MapScenePath))
        {
            Debug.LogError("[Match IQ] Map scene not found: " + MapScenePath);
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(MapScenePath);
    }

    [MenuItem("Match IQ/Play Map Scene Now", false, 25)]
    public static void PlayMapScene()
    {
        OpenMapScene();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Match IQ/Prepare APK Build (production server)", false, 42)]
    public static void PrepareApkBuild()
    {
        if (!MatchIQPlayStoreBuild.ApplyReleaseSigning())
        {
            Debug.LogError("[Match IQ] Release signing not configured — Play Store rejects debug-signed builds.");
            return;
        }

        ApplyApiConfig(localSimulation: false, useProductionUrl: true);
        ConfigureAndroidHttp(false);
        FixAndroidSplashTexture();
        MatchIQAppIconSetup.ApplyFromMenu();
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android,
            MatchIQPlayStoreBuild.AndroidPackageId);
        string commit = WriteBuildInfoFile();
        Debug.Log(
            "[Match IQ] Ready for Release APK build.\n" +
            "• Development Mode OFF\n" +
            "• Production URL ON\n" +
            "• Package: " + MatchIQPlayStoreBuild.AndroidPackageId + "\n" +
            "• Signed: release keystore\n" +
            "• Icons applied\n" +
            "• Build commit embedded: " + commit + "\n" +
            "• Play Store: use Match IQ → Build Play Store AAB\n" +
            "• Phone test APK: File → Build Settings → Build\n" +
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

    private static void ApplyApiConfig(bool localSimulation, bool useProductionUrl, string baseUrl = null)
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
        if (!string.IsNullOrEmpty(baseUrl))
            serialized.FindProperty("baseUrl").stringValue = baseUrl;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private static string ReadBaseUrl()
    {
        var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(ApiConfigPath);
        if (!config)
            return "http://localhost:8000";
        return new SerializedObject(config).FindProperty("baseUrl").stringValue;
    }

    private static void ConfigureAndroidHttp(bool allow)
    {
        SetAndroidCleartextTraffic(allow);
        SetInsecureHttpOption(allow);
    }

    /// <summary>
    /// Unity 6 blocks http:// unless insecureHttpOption is enabled (separate from Android manifest).
    /// </summary>
    private static void SetInsecureHttpOption(bool allow)
    {
        Object[] projectSettings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (projectSettings == null || projectSettings.Length == 0)
            return;

        var ps = new SerializedObject(projectSettings[0]);
        SerializedProperty prop = ps.FindProperty("insecureHttpOption");
        if (prop == null)
            return;

        // 0 = NotAllowed, 1 = DevelopmentOnly, 2 = AlwaysAllowed
        prop.intValue = allow ? 2 : 0;
        ps.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
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

internal class LanIpPrompt : EditorWindow
{
    private string input = "192.168.1.42";
    private bool done;
    private bool ok;

    public static string Show(string currentUrl)
    {
        string defaultIp = "192.168.1.42";
        try
        {
            if (!string.IsNullOrEmpty(currentUrl))
                defaultIp = new System.Uri(currentUrl).Host;
        }
        catch { }

        var window = CreateInstance<LanIpPrompt>();
        window.input = defaultIp;
        window.titleContent = new GUIContent("Local LAN IP");
        window.minSize = new Vector2(380, 120);
        window.maxSize = new Vector2(420, 120);
        window.ShowModalUtility();
        return window.ok ? window.input : null;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("PC LAN IP (same WiFi as phones). Example: 192.168.1.42");
        input = EditorGUILayout.TextField("IP", input);
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel")) { ok = false; Close(); }
        if (GUILayout.Button("Apply")) { ok = true; Close(); }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
