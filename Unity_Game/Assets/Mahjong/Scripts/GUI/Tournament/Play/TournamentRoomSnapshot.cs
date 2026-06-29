namespace Mkey.Tournament
{
    /// <summary>Read-only waiting-room state for UI polling (no UI changes required).</summary>
    public struct TournamentRoomSnapshot
    {
        public bool hasRoom;
        public int currentPlayers;
        public int maxPlayers;
        public float countdownSeconds;
        public string statusMessage;
        public bool shouldLaunch;
        public string localPlayerUuid;
        public string opponentUuid;
        public string opponentName;
    }
}
