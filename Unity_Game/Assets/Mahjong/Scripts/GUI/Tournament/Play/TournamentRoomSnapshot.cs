namespace Mkey.Tournament
{
    /// <summary>Read-only waiting-room state for UI polling (no UI changes required).</summary>
    public struct TournamentRoomSnapshot
    {
        public bool hasRoom;
        public string roomId;
        public int currentPlayers;
        public int maxPlayers;
        public float countdownSeconds;
        public int startCountdownSeconds;
        public long matchStartAtMs;
        public string status;
        public string searchStatus;
        public string statusMessage;
        public bool shouldLaunch;
        public string localPlayerUuid;
        public string localPlayerName;
        public string localPlayerRankLine;
        public string localPlayerAvatarUrl;
        public string opponentUuid;
        public string opponentName;
        public string opponentRankLine;
        public string opponentAvatarUrl;
        public System.Collections.Generic.List<Mkey.Network.RoomPlayerDto> players;
    }
}
