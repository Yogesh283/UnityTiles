using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Live status + fee labels aligned to turnamant1.png card rows (no layout redesign).
    /// </summary>
    public static class TournamentCardOverlays
    {
        public static void Build(Transform overlay, int balance)
        {
            int index = 0;
            foreach (TournamentDefinition tournament in TournamentCatalog.All)
            {
                float top = TournamentPngLayout.GetCardTop(index);
                CreateStatusBadge(overlay, new Rect(24f, top + 6f, 128f, 30f), tournament.statusLabel);
                CreateFeeLine(overlay, new Rect(24f, top + 38f, 280f, 26f), tournament, balance);
                index++;
            }
        }

        private static void CreateStatusBadge(Transform parent, Rect rect, string status)
        {
            RectTransform rt = TournamentUIFactory.CreateRect(parent, "Status_" + status);
            TournamentPngLayout.PlaceFromTopLeft(rt, rect);

            Color bg = TournamentPremiumUI.GetStatusColor(status);
            Image panel = TournamentUIFactory.CreateSlicedImage(rt, "Bg", bg, TournamentSpriteFactory.Badge, false);
            TournamentUIFactory.StretchRect(panel.rectTransform);

            Text label = TournamentUIFactory.CreateText(
                rt, "Label", string.IsNullOrEmpty(status) ? "OPEN" : status,
                TournamentPngLayout.OverlayFont(14f), FontStyle.Bold,
                TournamentPremiumTheme.TextWhite, TextAnchor.MiddleCenter);
            TournamentUIFactory.StretchRect(label.rectTransform);
        }

        private static void CreateFeeLine(
            Transform parent,
            Rect rect,
            TournamentDefinition tournament,
            int balance)
        {
            RectTransform rt = TournamentUIFactory.CreateRect(parent, "Fee_" + tournament.id);
            TournamentPngLayout.PlaceFromTopLeft(rt, rect);

            bool canAfford = balance >= tournament.entryFee;
            Color color = canAfford
                ? new Color(0.75f, 0.95f, 0.78f, 0.95f)
                : new Color(1f, 0.55f, 0.45f, 0.95f);

            string text = $"Entry {tournament.entryFee:N0}  •  Win up to {GetTopPrize(tournament):N0}";
            Text label = TournamentUIFactory.CreateText(
                rt, "Label", text, TournamentPngLayout.OverlayFont(13f), FontStyle.Bold,
                color, TextAnchor.MiddleLeft);
            TournamentUIFactory.StretchRect(label.rectTransform);
        }

        private static int GetTopPrize(TournamentDefinition tournament)
        {
            if (tournament == null) return 0;
            return TournamentPrizeTable.GetPrize(tournament.id, 1);
        }

        public static void RefreshAffordability(Transform overlay, int balance)
        {
            if (!overlay) return;
            foreach (Transform child in overlay)
            {
                if (!child.name.StartsWith("Fee_")) continue;
                Text label = child.GetComponentInChildren<Text>();
                if (!label) continue;

                string id = child.name.Substring("Fee_".Length);
                TournamentDefinition tournament = FindTournament(id);
                if (tournament == null) continue;

                bool canAfford = balance >= tournament.entryFee;
                label.color = canAfford
                    ? new Color(0.75f, 0.95f, 0.78f, 0.95f)
                    : new Color(1f, 0.55f, 0.45f, 0.95f);
            }
        }

        private static TournamentDefinition FindTournament(string id)
        {
            foreach (TournamentDefinition t in TournamentCatalog.All)
            {
                if (t.id == id) return t;
            }
            return null;
        }
    }
}
