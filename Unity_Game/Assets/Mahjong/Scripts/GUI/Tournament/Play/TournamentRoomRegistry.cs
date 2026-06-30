using System;
using System.Collections.Generic;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Central registry: one active room per tournament type, join tracking, no duplicates.
    /// </summary>
    public static class TournamentRoomRegistry
    {
        private static readonly Dictionary<string, TournamentRoom> RoomsByTournamentId =
            new Dictionary<string, TournamentRoom>();

        private static TournamentRoom localRoom;
        private static float localDisconnectStartedAt = -1f;

        public const float ReconnectTimeoutSeconds = 120f;

        public static TournamentRoom LocalRoom => localRoom;

        public static bool HasLocalRoom =>
            localRoom != null && !localRoom.IsDestroyed;

        /// <summary>
        /// Called after entry fee is paid — creates or reuses the room and adds the local player once.
        /// </summary>
        public static TournamentRoom JoinOrGetRoom(TournamentDefinition tournament)
        {
            if (tournament == null) return null;

            if (localRoom != null &&
                !localRoom.IsDestroyed &&
                localRoom.tournamentId == tournament.id &&
                localRoom.localPlayerJoined)
            {
                return localRoom;
            }

            if (!RoomsByTournamentId.TryGetValue(tournament.id, out TournamentRoom room) ||
                room == null || room.IsDestroyed)
            {
                room = new TournamentRoom();
                room.Initialize(tournament);
                RoomsByTournamentId[tournament.id] = room;
            }

            room.EnsureLocalPlayerJoined();
            localRoom = room;
            localDisconnectStartedAt = -1f;
            return room;
        }

        public static TournamentRoom GetRoom(string tournamentId)
        {
            if (string.IsNullOrEmpty(tournamentId)) return null;
            RoomsByTournamentId.TryGetValue(tournamentId, out TournamentRoom room);
            return room != null && !room.IsDestroyed ? room : null;
        }

        public static TournamentRoomSnapshot GetSnapshot(string tournamentId)
        {
            if (TournamentApiBridge.HasActiveApiSession &&
                TournamentSession.Tournament != null &&
                TournamentSession.Tournament.id == tournamentId)
            {
                return TournamentApiBridge.GetApiSnapshot(TournamentSession.Tournament);
            }

            TournamentRoom room = GetRoom(tournamentId);
            if (room == null)
            {
                return new TournamentRoomSnapshot
                {
                    hasRoom = false,
                    currentPlayers = 1,
                    maxPlayers = 2,
                    countdownSeconds = 0f,
                    statusMessage = "Finding players...",
                    shouldLaunch = false
                };
            }

            return new TournamentRoomSnapshot
            {
                hasRoom = true,
                currentPlayers = room.CurrentPlayerCount,
                maxPlayers = room.maxPlayerCount,
                countdownSeconds = Mathf.Max(0f, room.countdownRemaining),
                statusMessage = room.GetStatusMessage(),
                shouldLaunch = room.launchReady
            };
        }

        public static void TickWaitingRoom(float deltaTime)
        {
            if (localRoom == null || localRoom.IsDestroyed) return;
            if (localRoom.state != TournamentRoomState.WaitingForPlayers &&
                localRoom.state != TournamentRoomState.Ready)
                return;

            localRoom.TickWaiting(deltaTime);
        }

        public static void TickActiveMatch(float deltaTime)
        {
            if (localRoom == null || localRoom.IsDestroyed) return;
            if (localRoom.state != TournamentRoomState.Playing) return;

            localRoom.TickMatch(deltaTime);

            if (localDisconnectStartedAt >= 0f &&
                Time.realtimeSinceStartup - localDisconnectStartedAt >= ReconnectTimeoutSeconds)
            {
                localRoom.MarkLocalEliminated();
                localDisconnectStartedAt = -1f;
                TournamentMatchManager.ForfeitAsLoss();
            }
        }

        public static void NotifyLocalDisconnect()
        {
            if (!HasLocalRoom || !TournamentSession.IsActive) return;
            if (localRoom.state != TournamentRoomState.Playing) return;
            if (localRoom.localPlayer == null) return;

            localRoom.localPlayer.isDisconnected = true;
            localRoom.localPlayer.disconnectedAtRealtime = Time.realtimeSinceStartup;
            localDisconnectStartedAt = Time.realtimeSinceStartup;
        }

        public static void TryReconnectLocal()
        {
            if (!HasLocalRoom || localDisconnectStartedAt < 0f) return;

            if (Time.realtimeSinceStartup - localDisconnectStartedAt <= ReconnectTimeoutSeconds)
            {
                if (localRoom.localPlayer != null)
                    localRoom.localPlayer.isDisconnected = false;
                localDisconnectStartedAt = -1f;
            }
        }

        public static bool ForcePrepareForLaunch()
        {
            if (!HasLocalRoom) return false;
            localRoom.ForceReadyForLaunch();
            return localRoom.levelGenerated;
        }

        public static void ReleaseLocalRoom()
        {
            if (localRoom != null)
            {
                string tid = localRoom.tournamentId;
                localRoom.Cleanup();
                if (!string.IsNullOrEmpty(tid))
                    RoomsByTournamentId.Remove(tid);
            }

            localRoom = null;
            localDisconnectStartedAt = -1f;
            TournamentApiBridge.Clear();
        }

        public static void ClearAll()
        {
            foreach (KeyValuePair<string, TournamentRoom> pair in RoomsByTournamentId)
                pair.Value?.Cleanup();

            RoomsByTournamentId.Clear();
            localRoom = null;
            localDisconnectStartedAt = -1f;
            TournamentApiBridge.Clear();
        }
    }
}
