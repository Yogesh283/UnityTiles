#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// Builds Android launcher icons from AppLogo.png.
/// Icons are stored outside Assets/Editor so they are included in player builds.
/// </summary>
public static class MatchIQAppIconSetup
{
    private const string LogoPath = "Assets/Mahjong/Resources/Landing/AppLogo.png";
    private const string GeneratedFolder = "Assets/Mahjong/AppIcons";
    private const string ForegroundPath = GeneratedFolder + "/AndroidAppIconForeground.png";
    private const string BackgroundPath = GeneratedFolder + "/AndroidAppIconBackground.png";
    private const string LegacyPath = GeneratedFolder + "/AndroidAppIconLegacy.png";

    private const float IconContentScale = 0.88f;
    private const int IconTextureSize = 1024;

    private static readonly Color BackgroundColor = new Color(0.04f, 0.09f, 0.05f, 1f);

    [MenuItem("Match IQ/Apply App Logo As Android Icon")]
    public static void ApplyFromMenu()
    {
        if (ApplyAppLogo())
            Debug.Log("[Match IQ] Android app icons updated from AppLogo.png.");
        else
            Debug.LogError("[Match IQ] Failed to apply AppLogo.png as Android icon.");
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
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        return foreground != null && background != null &&
               textures[0] == foreground && textures[1] == background;
    }

    private static bool ApplyAppLogo()
    {
        Texture2D logo = LoadLogoFromDisk();
        if (logo == null)
        {
            Debug.LogWarning("[Match IQ] AppLogo.png not found at " + LogoPath);
            return false;
        }

        try
        {
            EnsureGeneratedFolder();

            Texture2D background = BuildSolidTexture(BackgroundColor);
            Texture2D foreground = BuildForegroundIcon(logo);
            Texture2D legacy = BuildCompositeIcon(logo);

            SaveTexture(background, BackgroundPath);
            SaveTexture(foreground, ForegroundPath);
            SaveTexture(legacy, LegacyPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var iconBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var iconForeground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
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
        finally
        {
            Object.DestroyImmediate(logo);
        }
    }

    private static Texture2D LoadLogoFromDisk()
    {
        string fullPath = Path.Combine(
            Application.dataPath,
            "Mahjong/Resources/Landing/AppLogo.png");

        if (!File.Exists(fullPath))
            return null;

        byte[] bytes = File.ReadAllBytes(fullPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            Object.DestroyImmediate(tex);
            return null;
        }

        return tex;
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
        if (!AssetDatabase.IsValidFolder("Assets/Mahjong/AppIcons"))
            AssetDatabase.CreateFolder("Assets/Mahjong", "AppIcons");
    }

    private static Texture2D BuildForegroundIcon(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        BlitScaled(source, canvas, IconContentScale, keyDarkBackground: true);
        return canvas;
    }

    private static Texture2D BuildCompositeIcon(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        FillColor(canvas, BackgroundColor);
        BlitScaled(source, canvas, IconContentScale, keyDarkBackground: true);
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

    private static void BlitScaled(
        Texture2D source,
        Texture2D target,
        float scale,
        bool keyDarkBackground)
    {
        int size = target.width;
        int drawW = Mathf.RoundToInt(size * scale);
        int drawH = Mathf.RoundToInt(size * scale);

        float aspect = (float)source.width / Mathf.Max(1, source.height);
        if (aspect > 1f)
            drawH = Mathf.RoundToInt(drawW / aspect);
        else
            drawW = Mathf.RoundToInt(drawH * aspect);

        int offsetX = (size - drawW) / 2;
        int offsetY = (size - drawH) / 2;

        for (int y = 0; y < drawH; y++)
        {
            float v = drawH <= 1 ? 0.5f : y / (float)(drawH - 1);
            for (int x = 0; x < drawW; x++)
            {
                float u = drawW <= 1 ? 0.5f : x / (float)(drawW - 1);
                Color sample = source.GetPixelBilinear(u, v);
                if (keyDarkBackground && IsKeyedBackground(sample))
                    continue;

                int tx = offsetX + x;
                int ty = offsetY + y;
                if (tx < 0 || ty < 0 || tx >= size || ty >= size)
                    continue;

                Color existing = target.GetPixel(tx, ty);
                target.SetPixel(tx, ty, AlphaBlend(existing, sample));
            }
        }

        target.Apply();
    }

    private static bool IsKeyedBackground(Color color) =>
        color.a < 0.05f ||
        (color.r < 0.12f && color.g < 0.12f && color.b < 0.12f);

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

    private static void SaveTexture(Texture2D texture, string assetPath)
    {
        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(assetPath, png);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Default;
        importer.spriteImportMode = SpriteImportMode.None;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = IconTextureSize;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = FilterMode.Bilinear;
        importer.isReadable = false;

        var android = new TextureImporterPlatformSettings
        {
            name = "Android",
            overridden = true,
            maxTextureSize = IconTextureSize,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed,
        };
        importer.SetPlatformTextureSettings(android);

        var standalone = new TextureImporterPlatformSettings
        {
            name = "Standalone",
            overridden = true,
            maxTextureSize = IconTextureSize,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed,
        };
        importer.SetPlatformTextureSettings(standalone);

        importer.SaveAndReimport();
    }
}
#endif
