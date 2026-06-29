#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Applies Assets/Mahjong/Resources/Landing/AppLogo.png as the Android app icon.
/// </summary>
public static class MatchIQAppIconSetup
{
    private const string LogoPath = "Assets/Mahjong/Resources/Landing/AppLogo.png";

    [MenuItem("Match IQ/Apply App Logo As Android Icon")]
    public static void ApplyFromMenu()
    {
        if (ApplyAppLogo())
            Debug.Log("[Match IQ] AppLogo.png applied to Android adaptive icon slots.");
    }

    [InitializeOnLoadMethod]
    private static void ApplyOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!HasAndroidIconsConfigured())
            ApplyAppLogo();
    }

    private static bool HasAndroidIconsConfigured()
    {
        PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
        var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        if (icons == null || icons.Length == 0)
            return false;

        var textures = icons[0].GetTextures();
        return textures != null && textures.Length > 0 && textures[0] != null;
    }

    private static bool ApplyAppLogo()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
        if (logo == null)
        {
            Debug.LogWarning("[Match IQ] AppLogo.png not found at " + LogoPath);
            return false;
        }

        PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
        var target = NamedBuildTarget.Android;
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        for (var i = 0; i < icons.Length; i++)
            icons[i].SetTextures(new[] { logo, logo });

        PlayerSettings.SetPlatformIcons(target, kind, icons);
        AssetDatabase.SaveAssets();
        return true;
    }
}
#endif
