using System.Threading.Tasks;
using Mkey.Tournament;
using UnityEngine;

namespace Mkey.Network
{
    /// <summary>
    /// Bridges API room data into existing tournament room registry without changing UI.
    /// </summary>
    public static class TournamentApiBridge
    {
        public static bool IsOnlineMode => !ApiConfig.Current.UseLocalSimulation;

        public static RoomResponseDto CurrentRoom { get; private set; }

        public static bool HasActiveApiRoom =>
            IsOnlineMode && CurrentRoom != null && !string.IsNullOrEmpty(CurrentRoom.roomId);

        public static void ApplyJoinResponse(TournamentDefinition tournament, RoomResponseDto room)
        {
            if (tournament == null || room == null) return;

            CurrentRoom = room;
            TournamentRoom registryRoom = TournamentRoomRegistry.JoinOrGetRoom(tournament);
            registryRoom?.ApplyApiRoomData(
                room.roomId,
                room.levelIndex,
                room.levelSeed,
                room.playerCount,
                room.status,
                room.waitingSeconds);
            TournamentSession.BindRoom(room.roomId, room.levelIndex);
        }

        public static async Task<bool> RefreshActiveRoomAsync()
        {
            if (!HasActiveApiRoom) return false;

            var result = await TournamentService.FetchRoomSnapshotAsync(CurrentRoom.roomId);
            if (!result.Success || result.Data == null) return false;

            CurrentRoom.playerCount = result.Data.players != null ? result.Data.players.Count : CurrentRoom.playerCount;
            CurrentRoom.status = result.Data.status;

            TournamentRoom local = TournamentRoomRegistry.LocalRoom;
            local?.ApplyApiRoomData(
                result.Data.roomId,
                result.Data.levelIndex,
                CurrentRoom.levelSeed,
                CurrentRoom.playerCount,
                result.Data.status,
                CurrentRoom.waitingSeconds);

            return true;
        }

        public static TournamentRoomSnapshot GetApiSnapshot(TournamentDefinition tournament)
        {
            if (!HasActiveApiRoom || tournament == null)
                return default;

            bool shouldLaunch = CurrentRoom.status == "starting" ||
                                CurrentRoom.status == "active" ||
                                CurrentRoom.playerCount >= CurrentRoom.maxPlayers;

            return new TournamentRoomSnapshot
            {
                hasRoom = true,
                currentPlayers = Mathf.Max(1, CurrentRoom.playerCount),
                maxPlayers = tournament.maxPlayers,
                countdownSeconds = CurrentRoom.waitingSeconds,
                statusMessage = GetStatusMessage(CurrentRoom.status, CurrentRoom.playerCount, tournament.maxPlayers),
                shouldLaunch = shouldLaunch
            };
        }

        public static void Clear()
        {
            CurrentRoom = null;
        }

        private static string GetStatusMessage(string status, int players, int maxPlayers)
        {
            if (status == "starting" || status == "active") return "Game Starting!";
            if (players >= maxPlayers) return "Lobby full — starting soon";
            if (players <= 1) return "Finding players...";
            return "Players joining...";
        }
    }
}
