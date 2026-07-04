using UnityEngine;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Live stats over turnamant1.png dash placeholders (Players, Entry, Prize Pool, Top Win).
    /// </summary>
    public static class TournamentCardOverlays
    {
        private static readonly Color StatTextColor = Color.black;

        public static void Build(Transform statsLayer)
        {
            // Stat values are now baked into turnamant1.png, so the code no longer draws its own
            // Players/Entry/Prize/Top-Win text (that overlapped the baked numbers). Only the JOIN
            // button state is still driven from code (see RefreshAll -> RefreshJoinButtons).
        }

        public static void Rebuild(Transform statsLayer)
        {
            if (!statsLayer)
                return;

            ClearChildren(statsLayer);
            Build(statsLayer);
        }

        public static void EnsureBuilt(Transform statsLayer)
        {
            if (!statsLayer)
                return;

            if (statsLayer.childCount == 0)
                Build(statsLayer);
            else
                RefreshAll(statsLayer);
        }

        public static void RefreshAll(Transform statsLayer, Transform hitOverlay = null)
        {
            // Stat text is baked into the image now — only refresh the JOIN/FULL button state.
            if (hitOverlay)
                RefreshJoinButtons(hitOverlay);
        }

        public static void RefreshJoinButtons(Transform overlay)
        {
            if (!overlay)
                return;

            Transform hitAreas = overlay.Find("HitAreas");
            if (!hitAreas)
                return;

            foreach (Transform child in hitAreas)
            {
                if (!child.name.StartsWith("Join_"))
                    continue;

                string tournamentId = child.name.Substring("Join_".Length);
                TournamentDefinition tournament = FindCatalogTournament(tournamentId);
                if (tournament == null)
                    continue;

                TournamentJoinButton join = child.GetComponent<TournamentJoinButton>();
                if (join)
                    join.RefreshTournament(tournament);

                bool isFull = IsFull(tournament);
                Text label = child.Find("Label")?.GetComponent<Text>();
                if (label)
                    label.text = isFull ? "FULL" : "JOIN";

                Button button = child.GetComponent<Button>();
                if (button)
                    button.interactable = !isFull;
            }
        }

        private static void CreateStatCell(Transform statsLayer, string column, string name, Rect rect, string value)
        {
            RectTransform rt = TournamentUIFactory.CreateRect(statsLayer, name);
            TournamentPngLayout.PlaceFromTopLeftAnchored(rt, rect);

            CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            Text label = rt.gameObject.AddComponent<Text>();
            label.font = TournamentUITheme.Font;
            label.text = value;
            label.fontSize = TournamentPngLayout.OverlayFont(18f);
            label.fontStyle = FontStyle.Bold;
            label.color = StatTextColor;
            label.alignment = TextAnchor.MiddleCenter;
            label.supportRichText = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
        }

        private static void SetStatText(Transform statsLayer, string name, string value)
        {
            Transform cell = statsLayer.Find(name);
            if (!cell)
                return;

            Text label = cell.GetComponent<Text>();
            if (label)
            {
                label.text = value;
                label.color = StatTextColor;
            }
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child)
                    Object.Destroy(child.gameObject);
            }
        }

        private static TournamentDefinition FindCatalogTournament(string tournamentId)
        {
            foreach (TournamentDefinition tournament in TournamentCatalog.All)
            {
                if (tournament != null && tournament.id == tournamentId)
                    return tournament;
            }

            return null;
        }

        private static string StatName(string column, string tournamentId) =>
            $"Stat_{column}_{tournamentId}";

        private static string FormatPlayers(TournamentDefinition tournament)
        {
            if (tournament == null)
                return "-";

            int current = GetPlayerCount(tournament);
            return $"{current:N0}/{tournament.maxPlayers:N0}";
        }

        private static string FormatEntryFee(TournamentDefinition tournament) =>
            tournament == null ? "-" : tournament.entryFee.ToString("N0");

        private static string FormatPrizePool(TournamentDefinition tournament) =>
            tournament == null ? "-" : tournament.prizePool.ToString("N0");

        private static string FormatFourthColumn(TournamentDefinition tournament)
        {
            if (tournament == null)
                return "-";

            if (tournament.HasPlatformFee)
                return tournament.platformFee.ToString("N0");

            return TournamentPrizeTable.GetPrize(tournament.id, 1).ToString("N0");
        }

        private static int GetPlayerCount(TournamentDefinition tournament)
        {
            TournamentRoomSnapshot snap = TournamentRoomRegistry.GetSnapshot(tournament.id);
            if (snap.hasRoom && snap.currentPlayers > 0)
                return snap.currentPlayers;

            return SimulateLobbySize(tournament);
        }

        private static int SimulateLobbySize(TournamentDefinition tournament)
        {
            string status = tournament.statusLabel ?? string.Empty;
            if (status.Equals("FULL", System.StringComparison.OrdinalIgnoreCase))
                return tournament.maxPlayers;

            if (status.Equals("FILLING", System.StringComparison.OrdinalIgnoreCase))
                return Mathf.Clamp(Mathf.RoundToInt(tournament.maxPlayers * 0.62f), 1, tournament.maxPlayers);

            if (status.Contains("STARTING"))
                return Mathf.Clamp(Mathf.RoundToInt(tournament.maxPlayers * 0.88f), 1, tournament.maxPlayers);

            return Mathf.Clamp(Mathf.RoundToInt(tournament.maxPlayers * 0.12f), 1, tournament.maxPlayers);
        }

        private static bool IsFull(TournamentDefinition tournament) =>
            tournament != null &&
            string.Equals(tournament.statusLabel, "FULL", System.StringComparison.OrdinalIgnoreCase);
    }
}
