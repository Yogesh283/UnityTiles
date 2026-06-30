namespace Mkey.Tournament
{
    public static class TournamentRankTier
    {
        public static string FormatRankLine(int bestRank, string rankTier)
        {
            if (!string.IsNullOrEmpty(rankTier))
                return $"Rank {rankTier}";

            if (bestRank > 0 && bestRank < 9999)
                return $"Rank #{bestRank}";

            return "Rank —";
        }

        public static string FormatLevelLine(int gameLevel)
        {
            return gameLevel > 0 ? $"Level {gameLevel}" : "Level —";
        }

        public static string FormatUuidLine(string userUuid)
        {
            if (string.IsNullOrEmpty(userUuid))
                return "UUID: —";
            return $"UUID:\n{TournamentRoom.FormatShortId(userUuid)}";
        }
    }
}
