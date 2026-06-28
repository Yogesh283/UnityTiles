using System;
using System.Collections.Generic;

namespace Mkey.Tournament
{
    [Serializable]
    public class TournamentHistoryEntry
    {
        public string tournamentId;
        public string tournamentName;
        public int rank;
        public int maxPlayers;
        public int score;
        public float timeSeconds;
        public int moves;
        public int prizeWon;
        public int entryFee;
        public long completedUtcTicks;
        public int levelIndex;
    }

    [Serializable]
    internal class TournamentHistorySaveData
    {
        public List<TournamentHistoryEntry> entries = new List<TournamentHistoryEntry>();
    }
}
