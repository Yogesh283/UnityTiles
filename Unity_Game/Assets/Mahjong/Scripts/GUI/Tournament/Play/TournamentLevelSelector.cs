using Mkey;

namespace Mkey.Tournament
{
    /// <summary>
    /// Picks one deterministic Mahjong level per room so every participant gets the same board.
    /// </summary>
    public static class TournamentLevelSelector
    {
        /// <summary>Must match Backend pick_level_index default level_count.</summary>
        public const int DefaultLevelCount = 100;

        public static int GenerateRoomSeed(string tournamentId, string roomId)
        {
            int a = TournamentStringHash.Compute(tournamentId);
            int b = TournamentStringHash.Compute(roomId);
            return TournamentStringHash.ToInt32(TournamentStringHash.ToInt32((long)a * 397) ^ b);
        }

        public static int PickLevelIndex(int roomSeed, TournamentDefinition tournament)
        {
            int levelCount = GameConstructSet.Instance ? GameConstructSet.Instance.LevelCount : 0;
            if (levelCount <= 0)
                levelCount = DefaultLevelCount;

            int tournamentHash = tournament != null ? TournamentStringHash.Compute(tournament.id) : 0;
            var rng = new System.Random(roomSeed ^ tournamentHash);
            return rng.Next(0, levelCount);
        }
    }
}
