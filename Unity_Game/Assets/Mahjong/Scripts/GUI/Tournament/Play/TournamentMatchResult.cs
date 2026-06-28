using System;
using System.Collections.Generic;

namespace Mkey.Tournament
{
    [Serializable]
    public class TournamentParticipantResult
    {
        public string name;
        public int score;
        public float timeSeconds;
        public int moves;
        public int rank;
        public bool isPlayer;
        public double completionServerMs;
    }

    [Serializable]
    public class TournamentMatchResult
    {
        public string tournamentId;
        public string tournamentName;
        public int maxPlayers;
        public int levelIndex;
        public int playerRank;
        public int playerScore;
        public float playerTimeSeconds;
        public int playerMoves;
        public int prizeWon;
        public int entryFee;
        public List<TournamentParticipantResult> leaderboard = new List<TournamentParticipantResult>();
    }
}
