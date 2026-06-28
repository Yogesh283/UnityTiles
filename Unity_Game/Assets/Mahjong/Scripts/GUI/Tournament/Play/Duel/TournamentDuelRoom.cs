using System;

namespace Mkey.Tournament
{
    public enum TournamentDuelRoomState
    {
        WaitingForPlayers,
        Ready,
        Playing,
        Locked,
        Destroyed
    }

    [Serializable]
    public class TournamentDuelParticipant
    {
        public string id;
        public string displayName;
        public bool isLocal;
        public bool hasCompleted;
        public double completionServerMs = double.MaxValue;
        public int score;
        public int moves;
        public float elapsedSeconds;
    }

    public class TournamentDuelRoom
    {
        public const string LocalPlayerId = "local_player";
        public const string OpponentPlayerId = "opponent_player";

        public string roomId;
        public TournamentDefinition tournament;
        public TournamentDuelRoomState state = TournamentDuelRoomState.WaitingForPlayers;
        public bool isLocked;
        public bool rewardGranted;
        public string winnerId;
        public double synchronizedStartServerMs;
        public double opponentFinishServerMs;
        public TournamentDuelParticipant localPlayer = new TournamentDuelParticipant();
        public TournamentDuelParticipant opponent = new TournamentDuelParticipant();

        public bool IsDestroyed => state == TournamentDuelRoomState.Destroyed;

        public void Initialize(TournamentDefinition definition)
        {
            tournament = definition;
            roomId = "duel_" + Guid.NewGuid().ToString("N").Substring(0, 10);
            state = TournamentDuelRoomState.WaitingForPlayers;
            isLocked = false;
            rewardGranted = false;
            winnerId = null;

            localPlayer.id = LocalPlayerId;
            localPlayer.displayName = "You";
            localPlayer.isLocal = true;

            opponent.id = OpponentPlayerId;
            opponent.displayName = "Opponent";
            opponent.isLocal = false;
        }

        public TournamentDuelParticipant GetParticipant(string playerId)
        {
            if (playerId == LocalPlayerId) return localPlayer;
            if (playerId == OpponentPlayerId) return opponent;
            return null;
        }

        public bool LocalWon => winnerId == LocalPlayerId;
    }
}
