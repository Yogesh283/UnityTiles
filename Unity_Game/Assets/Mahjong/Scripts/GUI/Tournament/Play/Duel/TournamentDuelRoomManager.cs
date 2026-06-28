namespace Mkey.Tournament
{
    /// <summary>
    /// Legacy wrapper — use <see cref="TournamentMatchManager"/> instead.
    /// </summary>
    public static class TournamentDuelRoomManager
    {
        public const string DuelTournamentId = "duel_1v1";

        public static bool IsDuelMode => TournamentMatchManager.IsDuelMode;
        public static bool HasActiveRoom => TournamentMatchManager.HasActiveRoom;

        public static void CreateRoom(TournamentDefinition tournament) =>
            TournamentMatchManager.CreateRoom(tournament);

        public static void BeginSynchronizedMatch() =>
            TournamentMatchManager.BeginSynchronizedMatch();

        public static void OnLocalPlayerCompleted(int score, int moves, float elapsedSeconds) =>
            TournamentMatchManager.OnLocalPlayerCompleted(score, moves, elapsedSeconds);

        public static void DestroyRoom() =>
            TournamentMatchManager.DestroyRoom();
    }
}
