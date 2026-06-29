#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Builds padded Android launcher icons from AppLogo.png (in-app logo stays unchanged).
/// </summary>
public static class MatchIQAppIconSetup
{
    private const string LogoPath = "Assets/Mahjong/Resources/Landing/AppLogo.png";
    private const string GeneratedFolder = "Assets/Editor/Generated";
    private const string ForegroundPath = GeneratedFolder + "/AndroidAppIconForeground.png";
    private const string BackgroundPath = GeneratedFolder + "/AndroidAppIconBackground.png";
    private const string LegacyPath = GeneratedFolder + "/AndroidAppIconLegacy.png";

    /// <summary>Fit logo inside Android adaptive icon safe zone (~66%).</summary>
    private const float IconContentScale = 0.68f;
    private const int IconTextureSize = 512;

    private static readonly Color BackgroundColor = new Color(0.04f, 0.09f, 0.05f, 1f);

    [MenuItem("Match IQ/Apply App Logo As Android Icon")]
    public static void ApplyFromMenu()
    {
        if (ApplyAppLogo())
            Debug.Log("[Match IQ] Padded Android app icons generated and applied.");
    }

    [InitializeOnLoadMethod]
    private static void ApplyOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (!File.Exists(ForegroundPath) || !HasAndroidIconsConfigured())
            ApplyAppLogo();
    }

    private static bool HasAndroidIconsConfigured()
    {
        PlatformIconKind kind = AndroidPlatformIconKind.Adaptive;
        var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        if (icons == null || icons.Length == 0)
            return false;

        var textures = icons[0].GetTextures();
        if (textures == null || textures.Length < 2 || textures[0] == null || textures[1] == null)
            return false;

        var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        return foreground != null && textures[0] == foreground;
    }

    private static bool ApplyAppLogo()
    {
        var logo = AssetDatabase.LoadAssetAtPath<Texture2D>(LogoPath);
        if (logo == null)
        {
            Debug.LogWarning("[Match IQ] AppLogo.png not found at " + LogoPath);
            return false;
        }

        EnsureGeneratedFolder();

        Texture2D foreground = BuildScaledForeground(logo);
        Texture2D background = BuildSolidTexture(BackgroundColor);
        Texture2D legacy = BuildCompositeIcon(logo);

        SaveTexture(foreground, ForegroundPath);
        SaveTexture(background, BackgroundPath);
        SaveTexture(legacy, LegacyPath);

        Object.DestroyImmediate(foreground);
        Object.DestroyImmediate(background);
        Object.DestroyImmediate(legacy);

        var iconForeground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        var iconBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        var iconLegacy = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyPath);
        if (!iconForeground || !iconBackground || !iconLegacy)
            return false;

        var target = NamedBuildTarget.Android;

        SetAllIcons(target, AndroidPlatformIconKind.Adaptive, iconForeground, iconBackground);
        SetAllIcons(target, AndroidPlatformIconKind.Round, iconLegacy, null);
        SetAllIcons(target, AndroidPlatformIconKind.Legacy, iconLegacy, null);

        AssetDatabase.SaveAssets();
        return true;
    }

    private static void SetAllIcons(
        NamedBuildTarget target,
        PlatformIconKind kind,
        Texture2D primary,
        Texture2D secondary)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        for (var i = 0; i < icons.Length; i++)
        {
            if (secondary != null)
                icons[i].SetTextures(new[] { primary, secondary });
            else
                icons[i].SetTextures(new[] { primary });
        }

        PlayerSettings.SetPlatformIcons(target, kind, icons);
    }

    private static void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Editor/Generated"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");
            AssetDatabase.CreateFolder("Assets/Editor", "Generated");
        }
    }

    private static Texture2D BuildScaledForeground(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        BlitScaled(source, canvas, IconContentScale);
        return canvas;
    }

    private static Texture2D BuildCompositeIcon(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        FillColor(canvas, BackgroundColor);
        BlitScaled(source, canvas, IconContentScale);
        return canvas;
    }

    private static Texture2D BuildSolidTexture(Color color)
    {
        var tex = new Texture2D(IconTextureSize, IconTextureSize, TextureFormat.RGBA32, false);
        FillColor(tex, color);
        return tex;
    }

    private static Texture2D NewClearTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var clear = new Color(0f, 0f, 0f, 0f);
        var pixels = new Color[size * size];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = clear;
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private static void FillColor(Texture2D tex, Color color)
    {
        var pixels = new Color[tex.width * tex.height];
        for (var i = 0; i < pixels.Length; i++)
            pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
    }

    private static void BlitScaled(Texture2D source, Texture2D target, float scale)
    {
        int size = target.width;
        int drawW = Mathf.RoundToInt(size * scale);
        int drawH = Mathf.RoundToInt(size * scale);
        int offsetX = (size - drawW) / 2;
        int offsetY = (size - drawH) / 2;

        bool restoreReadable = EnsureReadable(source);

        for (int y = 0; y < drawH; y++)
        {
            float v = drawH <= 1 ? 0.5f : y / (float)(drawH - 1);
            for (int x = 0; x < drawW; x++)
            {
                float u = drawW <= 1 ? 0.5f : x / (float)(drawW - 1);
                Color sample = source.GetPixelBilinear(u, v);
                int tx = offsetX + x;
                int ty = offsetY + y;
                Color existing = target.GetPixel(tx, ty);
                target.SetPixel(tx, ty, AlphaBlend(existing, sample));
            }
        }

        target.Apply();
        RestoreReadable(source, restoreReadable);
    }

    private static Color AlphaBlend(Color under, Color over)
    {
        float alpha = over.a + under.a * (1f - over.a);
        if (alpha <= 0f)
            return Color.clear;

        return new Color(
            (over.r * over.a + under.r * under.a * (1f - over.a)) / alpha,
            (over.g * over.a + under.g * under.a * (1f - over.a)) / alpha,
            (over.b * over.a + under.b * under.a * (1f - over.a)) / alpha,
            alpha);
    }

    private static bool EnsureReadable(Texture2D source)
    {
        string path = AssetDatabase.GetAssetPath(source);
        if (string.IsNullOrEmpty(path))
            return false;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || importer.isReadable)
            return false;

        importer.isReadable = true;
        importer.SaveAndReimport();
        return true;
    }

    private static void RestoreReadable(Texture2D source, bool wasChanged)
    {
        if (!wasChanged)
            return;

        string path = AssetDatabase.GetAssetPath(source);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        importer.isReadable = false;
        importer.SaveAndReimport();
    }

    private static void SaveTexture(Texture2D texture, string assetPath)
    {
        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(assetPath, png);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.maxTextureSize = IconTextureSize;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
    }
}
#endif
