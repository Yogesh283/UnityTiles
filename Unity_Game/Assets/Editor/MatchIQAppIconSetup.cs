#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Builds Android launcher icons from AppLogo.png.
/// Icons live in Assets/Mahjong/AppIcons so they are included in player builds.
/// </summary>
public static class MatchIQAppIconSetup
{
    private const string LogoPath = "Assets/Mahjong/Resources/Landing/AppLogo.png";
    private const string GeneratedFolder = "Assets/Mahjong/AppIcons";
    private const string ForegroundPath = GeneratedFolder + "/AndroidAppIconForeground.png";
    private const string BackgroundPath = GeneratedFolder + "/AndroidAppIconBackground.png";
    private const string LegacyPath = GeneratedFolder + "/AndroidAppIconLegacy.png";
    private const string AndroidResRoot = "Assets/Plugins/Android/res";

    private const float IconContentScale = 0.92f;
    private const float AdaptiveForegroundScale = 0.68f;
    private const int IconTextureSize = 1024;

    private static readonly Color BackgroundColor = new Color(0.04f, 0.09f, 0.05f, 1f);

    private static readonly (string folder, int legacySize, int adaptiveSize)[] LauncherDensities =
    {
        ("mipmap-mdpi", 48, 108),
        ("mipmap-hdpi", 72, 162),
        ("mipmap-xhdpi", 96, 216),
        ("mipmap-xxhdpi", 144, 324),
        ("mipmap-xxxhdpi", 192, 432),
    };

    [MenuItem("Match IQ/Apply App Logo As Android Icon")]
    public static void ApplyFromMenu()
    {
        if (ApplyAppLogo())
        {
            Debug.Log(
                "[Match IQ] Android icons updated (Player Settings + Plugins/Android/res).\n" +
                "Next: Build a new APK, uninstall the old app from your phone, then install the new build.");
        }
        else
            Debug.LogError("[Match IQ] Failed to apply AppLogo.png as Android icon.");
    }

    [InitializeOnLoadMethod]
    private static void ApplyOnLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!File.Exists(ForegroundPath) || !HasAndroidIconsConfigured())
                ApplyAppLogo();
        };
    }

    public static bool ApplyAppLogo()
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
            Texture2D composite = BuildLegacyIcon(logo);

            SaveTexture(background, BackgroundPath);
            SaveTexture(foreground, ForegroundPath);
            SaveTexture(composite, LegacyPath);

            GenerateAndroidResMipmaps(logo);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var iconBackground = LoadIconTexture(BackgroundPath);
            var iconForeground = LoadIconTexture(ForegroundPath);
            var iconLegacy = LoadIconTexture(LegacyPath);
            if (!iconForeground || !iconBackground || !iconLegacy)
            {
                Debug.LogError("[Match IQ] Generated icon textures are missing or invalid.");
                return false;
            }

            // Use the full opaque composite for adaptive foreground — avoids black-circle launcher bugs.
            ConfigureAndroidIcons(composite, iconBackground, iconLegacy);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!VerifyAndroidIconsConfigured(iconLegacy, iconBackground, iconLegacy))
            {
                Debug.LogError("[Match IQ] PlayerSettings icons were not applied. Re-open Project Settings > Player > Android > Icon.");
                return false;
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(logo);
        }
    }

    private static bool HasAndroidIconsConfigured()
    {
        var foreground = LoadIconTexture(ForegroundPath);
        var background = LoadIconTexture(BackgroundPath);
        var legacy = LoadIconTexture(LegacyPath);
        if (!foreground || !background || !legacy)
            return false;

        return VerifyAndroidIconsConfigured(foreground, background, legacy);
    }

    private static bool VerifyAndroidIconsConfigured(
        Texture2D foreground,
        Texture2D background,
        Texture2D legacy)
    {
        var target = NamedBuildTarget.Android;

        if (!IconsMatch(target, AndroidPlatformIconKind.Adaptive, legacy, background))
            return false;
        if (!IconsMatch(target, AndroidPlatformIconKind.Legacy, legacy, null))
            return false;
        if (!IconsMatch(target, AndroidPlatformIconKind.Round, legacy, null))
            return false;

        return true;
    }

    private static bool IconsMatch(
        NamedBuildTarget target,
        PlatformIconKind kind,
        Texture2D primary,
        Texture2D secondary)
    {
        var icons = PlayerSettings.GetPlatformIcons(target, kind);
        if (icons == null || icons.Length == 0)
            return false;

        var textures = icons[0].GetTextures();
        if (textures == null || textures.Length == 0 || textures[0] != primary)
            return false;

        if (secondary == null)
            return textures.Length == 1;

        return textures.Length >= 2 && textures[1] == secondary;
    }

    private static void ConfigureAndroidIcons(Texture2D adaptiveForeground, Texture2D background, Texture2D legacy)
    {
        var target = NamedBuildTarget.Android;

        SetAllIcons(target, AndroidPlatformIconKind.Adaptive, adaptiveForeground, background);
        SetAllIcons(target, AndroidPlatformIconKind.Round, legacy, null);
        SetAllIcons(target, AndroidPlatformIconKind.Legacy, legacy, null);
    }

    /// <summary>
    /// Writes standard Android mipmap resources so the APK always ships real launcher icons.
    /// </summary>
    private static void GenerateAndroidResMipmaps(Texture2D logo)
    {
        string root = ToAbsoluteAssetPath(AndroidResRoot);
        Directory.CreateDirectory(root);

        foreach ((string folder, int legacySize, int adaptiveSize) in LauncherDensities)
        {
            string densityDir = Path.Combine(root, folder);
            Directory.CreateDirectory(densityDir);

            WritePng(Path.Combine(densityDir, "ic_launcher.png"), BuildLegacyIconAtSize(logo, legacySize));
            WritePng(Path.Combine(densityDir, "ic_launcher_foreground.png"), BuildForegroundIconAtSize(logo, adaptiveSize));
            WritePng(Path.Combine(densityDir, "ic_launcher_background.png"), BuildSolidTextureAtSize(BackgroundColor, adaptiveSize));
        }

        string anyDpiDir = Path.Combine(root, "mipmap-anydpi-v26");
        Directory.CreateDirectory(anyDpiDir);

        const string adaptiveXml =
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<adaptive-icon xmlns:android=\"http://schemas.android.com/apk/res/android\">\n" +
            "    <background android:drawable=\"@mipmap/ic_launcher_background\"/>\n" +
            "    <foreground android:drawable=\"@mipmap/ic_launcher_foreground\"/>\n" +
            "</adaptive-icon>\n";

        File.WriteAllText(Path.Combine(anyDpiDir, "ic_launcher.xml"), adaptiveXml);
        File.WriteAllText(Path.Combine(anyDpiDir, "ic_launcher_round.xml"), adaptiveXml);

        AssetDatabase.ImportAsset(AndroidResRoot, ImportAssetOptions.ImportRecursive);
    }

    private static Texture2D BuildLegacyIconAtSize(Texture2D source, int size)
    {
        var canvas = NewClearTexture(size);
        FillColor(canvas, BackgroundColor);
        BlitScaled(source, canvas, IconContentScale, keyBlackBackground: true);
        return canvas;
    }

    private static Texture2D BuildForegroundIconAtSize(Texture2D source, int size)
    {
        var canvas = NewClearTexture(size);
        BlitScaled(source, canvas, AdaptiveForegroundScale, keyBlackBackground: true);
        return canvas;
    }

    private static Texture2D BuildSolidTextureAtSize(Color color, int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        FillColor(tex, color);
        return tex;
    }

    private static void WritePng(string fullPath, Texture2D texture)
    {
        try
        {
            File.WriteAllBytes(fullPath, texture.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }
    }

    private static Texture2D LoadIconTexture(string assetPath)
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

    /// <summary>Adaptive foreground — logo only, transparent background.</summary>
    private static Texture2D BuildForegroundIcon(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        BlitScaled(source, canvas, IconContentScale, keyBlackBackground: true);
        return canvas;
    }

    /// <summary>Legacy/round launcher icon — logo on branded background.</summary>
    private static Texture2D BuildLegacyIcon(Texture2D source)
    {
        var canvas = NewClearTexture(IconTextureSize);
        FillColor(canvas, BackgroundColor);
        BlitScaled(source, canvas, IconContentScale, keyBlackBackground: true);
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
        bool keyBlackBackground)
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
                if (keyBlackBackground && IsPureBlackBackground(sample))
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

    private static bool IsPureBlackBackground(Color color) =>
        color.a < 0.05f ||
        (color.r < 0.03f && color.g < 0.03f && color.b < 0.03f);

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
        string fullPath = ToAbsoluteAssetPath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? GeneratedFolder);

        byte[] png = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, png);
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
        importer.wrapMode = TextureWrapMode.Clamp;

        ApplyUncompressedPlatform(importer, "Android");
        ApplyUncompressedPlatform(importer, "Standalone");
        ApplyUncompressedPlatform(importer, "DefaultTexturePlatform");

        importer.SaveAndReimport();
    }

    private static string ToAbsoluteAssetPath(string assetPath) =>
        Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));

    private static void ApplyUncompressedPlatform(TextureImporter importer, string platform)
    {
        var settings = new TextureImporterPlatformSettings
        {
            name = platform,
            overridden = true,
            maxTextureSize = IconTextureSize,
            format = TextureImporterFormat.RGBA32,
            textureCompression = TextureImporterCompression.Uncompressed,
            resizeAlgorithm = TextureResizeAlgorithm.Mitchell,
        };
        importer.SetPlatformTextureSettings(settings);
    }
}

/// <summary>
/// Regenerates Android launcher icons from AppLogo.png before every APK/AAB build.
/// </summary>
public class MatchIQAndroidIconBuildProcessor : IPreprocessBuildWithReport
{
    public int callbackOrder => -1000;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        if (!MatchIQAppIconSetup.ApplyAppLogo())
            throw new BuildFailedException("[Match IQ] Failed to configure Android application icons.");
    }
}
#endif
