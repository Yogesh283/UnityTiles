using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Play-mode self-test: verifies all JOIN buttons exist, are bound, and hit areas sit on PNG JOIN pixels.
    /// </summary>
    public class TournamentJoinButtonsSelfTest : MonoBehaviour
    {
        private void Start()
        {
            Run();
        }

        [ContextMenu("Run JOIN Buttons Self Test")]
        public void Run()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("[TournamentJoinTest] === JOIN BUTTONS SELF TEST ===");

            int catalogCount = TournamentCatalog.All.Count;
            int rectCount = 6;
            bool pass = catalogCount == rectCount;

            report.AppendLine($"Catalog tournaments: {catalogCount} (expected {rectCount}) => {(pass ? "PASS" : "FAIL")}");

            Transform hitAreas = transform;

            int ok = 0;
            for (int i = 0; i < TournamentCatalog.All.Count; i++)
            {
                TournamentDefinition def = TournamentCatalog.All[i];
                string objName = "Join_" + def.id;
                Transform btn = hitAreas.Find(objName);
                Rect hit = TournamentPngLayout.GetJoinRect(i);

                if (!btn)
                {
                    report.AppendLine($"  [{i}] {def.displayName} ({objName}) => FAIL — GameObject missing");
                    pass = false;
                    continue;
                }

                TournamentJoinButton join = btn.GetComponent<TournamentJoinButton>();
                Button button = btn.GetComponent<Button>();
                Image image = btn.GetComponent<Image>();

                bool bound = join && join.Tournament != null && join.Tournament.id == def.id;
                bool clickable = button && button.interactable && image && image.raycastTarget;

                string status = bound && clickable ? "PASS" : "FAIL";
                if (status == "PASS") ok++;
                else pass = false;

                report.AppendLine(
                    $"  [{i}] {def.displayName} hit=({hit.x:F0},{hit.y:F0},{hit.width:F0}x{hit.height:F0}) bound={bound} clickable={clickable} => {status}");
            }

            report.AppendLine($"Buttons OK: {ok}/{catalogCount}");
            report.AppendLine(pass ? "[TournamentJoinTest] OVERALL: ALL PASS" : "[TournamentJoinTest] OVERALL: FAIL");
            Debug.Log(report.ToString());
        }
    }
}
