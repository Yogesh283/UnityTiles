#if UNITY_EDITOR
using UnityEditor;
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
            Debug.Log("[Match IQ] AppLogo.png applied to all Android icon slots.");
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
        var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
        if (icons == null || icons.Length == 0)
            return false;

        return icons[0].GetTexture() != null;
    }

    private static bool ApplyAppLogo()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
        if (logo == null)
        {
            Debug.LogWarning("[Match IQ] AppLogo.png not found at " + LogoPath);
            return false;
        }

        ApplyKind(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, logo);
        ApplyKind(NamedBuildTarget.Android, AndroidPlatformIconKind.Round, logo);
        ApplyKind(NamedBuildTarget.Android, AndroidPlatformIconKind.Legacy, logo);
        AssetDatabase.SaveAssets();
        return true;
    }

    private static void ApplyKind(NamedBuildTarget target, AndroidPlatformIconKind kind, Texture2D logo)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        for (var i = 0; i < icons.Length; i++)
            icons[i].SetTexture(logo);

        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }
}
#endif
