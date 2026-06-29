#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures Unity IAP BillingMode.json exists so Google Play billing can initialize.
/// </summary>
[InitializeOnLoad]
public static class MatchIQBillingModeSetup
{
    private const string BillingModePath = "Assets/Resources/BillingMode.json";
    private const string BillingModeJson = "{\n  \"androidStore\": \"GooglePlay\"\n}\n";

    static MatchIQBillingModeSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureBillingModeFile();
    }

    [MenuItem("Match IQ/Ensure Google Play Billing Mode")]
    public static void EnsureFromMenu()
    {
        if (EnsureBillingModeFile())
            Debug.Log("[Match IQ] BillingMode.json ready for Google Play.");
    }

    private static bool EnsureBillingModeFile()
    {
        if (!Directory.Exists("Assets/Resources"))
            Directory.CreateDirectory("Assets/Resources");

        if (File.Exists(BillingModePath))
            return true;

        File.WriteAllText(BillingModePath, BillingModeJson);
        AssetDatabase.Refresh();
        return true;
    }
}
#endif
