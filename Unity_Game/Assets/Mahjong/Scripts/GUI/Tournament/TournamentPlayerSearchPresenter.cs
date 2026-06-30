namespace Mkey.Tournament
{
    /// <summary>
    /// Text-only search phase labels for the existing waiting room (no layout changes).
    /// </summary>
    public static class TournamentPlayerSearchPresenter
    {
        public static string PlayersFoundLine(int current, int max) =>
            $"Players Found\n{current} / {max}";

        public static string PlayersConnectedLine(int current, int max) =>
            $"{current} / {max} Players Connected";

        public static string StatusForPhase(string searchPhase, int dots)
        {
            switch (searchPhase)
            {
                case "searching":
                    return "🔍 Searching for Players..." + new string('.', dots);
                case "player_joined":
                    return "Player Joined";
                case "players_connected":
                    return "Player Found!";
                case "match_found":
                    return "Match Found!\nStarting in...";
                case "starting":
                    return "Match Found!";
                default:
                    return "🔍 Searching for Players..." + new string('.', dots);
            }
        }

        public static string FormatPlayerBlock(
            int slot,
            string displayName,
            string rankLine,
            string userUuid,
            string avatarUrl)
        {
            string avatarLine = string.IsNullOrEmpty(avatarUrl) ? "Avatar: —" : "Avatar: ✓";
            string name = string.IsNullOrEmpty(displayName) ? "—" : displayName;
            string id = string.IsNullOrEmpty(userUuid) ? "—" : TournamentRoom.FormatShortId(userUuid);
            string rank = string.IsNullOrEmpty(rankLine) ? "Rank —" : rankLine;
            return $"Player {slot}\n{avatarLine}\n{name}\n{rank}\nID: {id}";
        }
    }
}
