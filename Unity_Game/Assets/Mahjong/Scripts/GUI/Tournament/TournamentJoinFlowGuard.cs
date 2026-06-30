namespace Mkey.Tournament
{
    public static class TournamentJoinFlowGuard
    {
        public static bool IsJoining { get; private set; }

        public static bool CanStartJoin =>
            !IsJoining && !TournamentGlobalWaitingRoom.IsVisible;

        public static bool TryBegin()
        {
            if (!CanStartJoin)
                return false;
            IsJoining = true;
            return true;
        }

        public static void Reset() => IsJoining = false;
    }
}
