using Mkey;

namespace Mkey.Tournament
{
    /// <summary>
    /// Picks one deterministic Mahjong level per room so every participant gets the same board.
    /// </summary>
    public static class TournamentLevelSelector
    {
        public static int GenerateRoomSeed(string tournamentId, string roomId)
        {
            unchecked
            {
                return tournamentId.GetHashCode() * 397 ^ roomId.GetHashCode();
            }
        }

        public static int PickLevelIndex(int roomSeed, TournamentDefinition tournament)
        {
            int levelCount = GameConstructSet.Instance ? GameConstructSet.Instance.LevelCount : 0;
            if (levelCount <= 0)
                levelCount = 100;

            var rng = new System.Random(roomSeed ^ (tournament != null ? tournament.id.GetHashCode() : 0));
            return rng.Next(0, levelCount);
        }
    }
}
