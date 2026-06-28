namespace Mkey.Tournament
{
    /// <summary>
    /// Backward-compatible wrapper around <see cref="TournamentPrizeTable"/>.
    /// </summary>
    public static class TournamentPrizeService
    {
        public static int GetPaidRankCount(TournamentDefinition tournament)
        {
            if (tournament == null) return 0;
            return TournamentPrizeTable.GetPaidRankCount(tournament.id);
        }

        public static int CalculatePrize(TournamentDefinition tournament, int rank)
        {
            if (tournament == null) return 0;
            return TournamentPrizeTable.GetPrize(tournament.id, rank);
        }
    }
}
