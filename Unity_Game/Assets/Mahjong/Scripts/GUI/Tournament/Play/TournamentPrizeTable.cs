using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Configured coin rewards per tournament type and rank.
    /// </summary>
    public static class TournamentPrizeTable
    {
        public static int GetPrize(string tournamentId, int rank)
        {
            if (string.IsNullOrEmpty(tournamentId) || rank < 1) return 0;

            switch (tournamentId)
            {
                case "duel_1v1":
                    return rank == 1 ? 160 : 0;

                case "quick_cup":
                    if (rank == 1) return 500;
                    if (rank == 2) return 200;
                    if (rank == 3) return 100;
                    return 0;

                case "mega_clash":
                    if (rank == 1) return 2500;
                    if (rank == 2) return 1500;
                    if (rank == 3) return 1000;
                    if (rank >= 4 && rank <= 10) return ShareEqual(3000, 4, 10, rank);
                    return 0;

                case "grand_clash":
                    if (rank == 1) return 10000;
                    if (rank == 2) return 6000;
                    if (rank == 3) return 4000;
                    if (rank >= 4 && rank <= 20) return ShareEqual(20000, 4, 20, rank);
                    return 0;

                case "championship":
                    return GetChampionshipPrize(rank);

                case "world_cup":
                    return GetWorldCupPrize(rank);

                default:
                    return 0;
            }
        }

        public static int GetPaidRankCount(string tournamentId)
        {
            switch (tournamentId)
            {
                case "duel_1v1": return 1;
                case "quick_cup": return 3;
                case "mega_clash": return 10;
                case "grand_clash": return 20;
                case "championship": return 100;
                case "world_cup": return 200;
                default: return 0;
            }
        }

        public static bool IsWinningRank(string tournamentId, int rank) =>
            GetPrize(tournamentId, rank) > 0;

        private static int GetChampionshipPrize(int rank)
        {
            if (rank == 1) return 80000;
            if (rank == 2) return 50000;
            if (rank == 3) return 30000;
            if (rank >= 4 && rank <= 100) return ShareEqual(240000, 4, 100, rank);
            return 0;
        }

        private static int GetWorldCupPrize(int rank)
        {
            if (rank == 1) return 200000;
            if (rank == 2) return 120000;
            if (rank == 3) return 80000;
            if (rank >= 4 && rank <= 200) return ShareEqual(1200000, 4, 200, rank);
            return 0;
        }

        private static int ShareEqual(int pool, int rankFrom, int rankTo, int rank)
        {
            if (rank < rankFrom || rank > rankTo) return 0;
            int slots = rankTo - rankFrom + 1;
            int baseShare = pool / slots;
            int remainder = pool - baseShare * slots;
            return rank == rankFrom ? baseShare + remainder : baseShare;
        }
    }
}
