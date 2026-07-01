using System.Runtime.CompilerServices;
using UnityEngine;

namespace Mkey.Tournament
{
    public static class TournamentJoinFlowGuard
    {
        private const string Tag = "[TournamentJoinFlowGuard]";

        public static bool IsJoining { get; private set; }

        public static bool IsRoomEstablished { get; private set; }

        public static bool CanStartJoin =>
            !IsJoining && !IsRoomEstablished && !TournamentGlobalWaitingRoom.IsVisible;

        /// <summary>True while a join/matchmaking flow is legitimately in progress (overlay may stay up).</summary>
        public static bool IsActiveMatchmaking =>
            IsJoining ||
            (IsRoomEstablished &&
             TournamentSession.IsActive &&
             TournamentMatchManager.HasActiveRoom);

        public static void LogState(string context)
        {
            bool waitingVisible = TournamentGlobalWaitingRoom.IsVisible;
            Debug.Log(
                $"{Tag} {context}\n" +
                $"  CanStartJoin = {CanStartJoin}\n" +
                $"  IsJoining = {IsJoining}\n" +
                $"  IsRoomEstablished = {IsRoomEstablished}\n" +
                $"  TournamentGlobalWaitingRoom.IsVisible = {waitingVisible}");

            if (!CanStartJoin)
                Debug.LogWarning($"{Tag} {context} — {GetBlockReason()}");
        }

        public static string GetBlockReason()
        {
            if (IsJoining)
                return "CanStartJoin = FALSE — Reason = IsJoining == true";

            if (IsRoomEstablished)
                return "CanStartJoin = FALSE — Reason = IsRoomEstablished == true";

            if (TournamentGlobalWaitingRoom.IsVisible)
                return "CanStartJoin = FALSE — Reason = WaitingRoomVisible == true";

            return "CanStartJoin = TRUE";
        }

        public static bool CheckCanStartJoin(string context)
        {
            LogState(context);
            return CanStartJoin;
        }

        public static bool TryBegin([CallerMemberName] string caller = null)
        {
            LogState($"TryBegin from {caller}");

            if (!CanStartJoin)
            {
                Debug.LogWarning($"{Tag} TryBegin BLOCKED from {caller} — {GetBlockReason()}");
                return false;
            }

            IsJoining = true;
            Debug.Log($"{Tag} TryBegin OK from {caller} — IsJoining set to true");
            return true;
        }

        public static void MarkRoomEstablished([CallerMemberName] string caller = null)
        {
            Debug.Log(
                $"{Tag} MarkRoomEstablished from {caller} — " +
                $"IsRoomEstablished=true, IsJoining=false (was IsJoining={IsJoining})");
            IsRoomEstablished = true;
            IsJoining = false;
        }

        public static void Reset()
        {
            ApplyReset("Reset");
        }

        public static void ResetFrom([CallerMemberName] string caller = null)
        {
            ApplyReset(caller);
        }

        private static void ApplyReset(string caller)
        {
            Debug.Log(
                $"{Tag} Reset from {caller} — " +
                $"cleared IsJoining (was {IsJoining}), IsRoomEstablished (was {IsRoomEstablished})");
            IsJoining = false;
            IsRoomEstablished = false;
            LogState($"after Reset from {caller}");
        }
    }
}
