#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>Embeds git commit into MatchIQBuildInfo.txt on every player build.</summary>
public class MatchIQBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        MatchIQDevSetup.WriteBuildInfoFile();
    }
}
#endif
