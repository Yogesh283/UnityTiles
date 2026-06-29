using System.Collections.Generic;
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
            registryRoom?.ApplyOnlinePlayers(room.players);
            TournamentSession.BindRoom(room.roomId, room.levelIndex);
        }

        public static async Task<bool> RefreshActiveRoomAsync()
        {
            if (!HasActiveApiRoom) return false;

            var result = await TournamentService.FetchRoomSnapshotAsync(CurrentRoom.roomId);
            if (!result.Success || result.Data == null) return false;

            if (result.Data.players != null)
            {
                CurrentRoom.players = result.Data.players;
                CurrentRoom.playerCount = result.Data.players.Count;
            }

            CurrentRoom.status = result.Data.status;

            TournamentRoom local = TournamentRoomRegistry.LocalRoom;
            local?.ApplyApiRoomData(
                result.Data.roomId,
                result.Data.levelIndex,
                CurrentRoom.levelSeed,
                CurrentRoom.playerCount,
                result.Data.status,
                CurrentRoom.waitingSeconds);
            local?.ApplyOnlinePlayers(result.Data.players);

            return true;
        }

        public static TournamentRoomSnapshot GetApiSnapshot(TournamentDefinition tournament)
        {
            if (!HasActiveApiRoom || tournament == null)
                return default;

            bool shouldLaunch = CurrentRoom.status == "starting" ||
                                CurrentRoom.status == "active" ||
                                CurrentRoom.playerCount >= CurrentRoom.maxPlayers;

            BuildPlayerLabels(CurrentRoom.players, out string localUuid, out string opponentUuid, out string opponentName);

            return new TournamentRoomSnapshot
            {
                hasRoom = true,
                currentPlayers = Mathf.Max(1, CurrentRoom.playerCount),
                maxPlayers = tournament.maxPlayers,
                countdownSeconds = CurrentRoom.waitingSeconds,
                statusMessage = GetStatusMessage(CurrentRoom.status, CurrentRoom.playerCount, tournament.maxPlayers),
                shouldLaunch = shouldLaunch,
                localPlayerUuid = localUuid,
                opponentUuid = opponentUuid,
                opponentName = opponentName
            };
        }

        public static void Clear()
        {
            CurrentRoom = null;
        }

        private static void BuildPlayerLabels(
            List<RoomPlayerDto> players,
            out string localUuid,
            out string opponentUuid,
            out string opponentName)
        {
            localUuid = NetworkManager.HasInstance ? NetworkManager.Instance.UserUuid : string.Empty;
            opponentUuid = string.Empty;
            opponentName = string.Empty;

            if (players == null || players.Count == 0) return;

            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            foreach (RoomPlayerDto player in players)
            {
                if (player == null) continue;

                if (player.userId == localUserId)
                {
                    if (!string.IsNullOrEmpty(player.userUuid))
                        localUuid = player.userUuid;
                    continue;
                }

                opponentUuid = !string.IsNullOrEmpty(player.userUuid)
                    ? player.userUuid
                    : "player_" + player.userId;
                opponentName = string.IsNullOrEmpty(player.displayName)
                    ? TournamentRoom.FormatShortId(opponentUuid)
                    : player.displayName;
                return;
            }
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
