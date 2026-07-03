#if UNITY_EDITOR
using Mkey.NakamaPoc;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor entry point for isolated Nakama Phase 1.5 POC (does not touch production tournament flow).
/// </summary>
public static class NakamaPocMenu
{
    private const string RunnerObjectName = "NakamaPocRunner (Phase 1.5)";

    [MenuItem("Match IQ/Nakama POC/Run Phase 1.5 Test (Play Mode)", false, 200)]
    public static void RunPhase15Test()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning(
                "[NakamaPoc] Already in Play Mode. Stop and run again, or add NakamaPocRunner to scene manually.");
            return;
        }

        EnsureRunnerInScene();
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("Match IQ/Nakama POC/Open Phase 1.5 README", false, 201)]
    public static void OpenReadme()
    {
        string path = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", "..", "Nakama", "PHASE_1_5_README.md"));
        if (System.IO.File.Exists(path))
            EditorUtility.RevealInFinder(path);
        else
            Debug.LogWarning("[NakamaPoc] README not found at " + path);
    }

    [MenuItem("Match IQ/Nakama POC/Print Docker Start Command", false, 202)]
    public static void PrintDockerCommand()
    {
        string repoRoot = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(Application.dataPath, "..", ".."));
        Debug.Log(
            "[NakamaPoc] Start Nakama POC stack:\n" +
            $"  cd \"{repoRoot}\"\n" +
            "  docker compose -f Nakama/docker-compose.nakama.yml up");
    }

    private static void EnsureRunnerInScene()
    {
        NakamaPocRunner existing = Object.FindFirstObjectByType<NakamaPocRunner>();
        if (existing != null)
            return;

        var existingGo = GameObject.Find(RunnerObjectName);
        if (existingGo != null)
            return;

        var go = new GameObject(RunnerObjectName);
        go.AddComponent<NakamaPocRunner>();
        Undo.RegisterCreatedObjectUndo(go, "Create Nakama POC Runner");
    }
}
#endif
