using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mkey.Tournament
{
    public enum TournamentRoomState
    {
        WaitingForPlayers,
        Ready,
        Playing,
        Locked,
        Resolved,
        Destroyed
    }

    [Serializable]
    public class TournamentMatchParticipant
    {
        public string id;
        public string displayName;
        public bool isLocal;
        public bool hasCompleted;
        public bool isDisconnected;
        public bool isEliminated;
        public double completionServerMs = double.MaxValue;
        public float disconnectedAtRealtime;
        public int score;
        public int moves;
        public float timeSeconds;
    }

    /// <summary>
    /// Tournament room for all sizes (2, 10, 50, 100, 500, 1000).
    /// </summary>
    public class TournamentRoom
    {
        public const string LocalPlayerId = "local_player";
        public const string RemotePlayerId = "remote_player";

        public string roomId;
        public string tournamentId;
        public int roomSeed;
        public int maxPlayerCount;
        public int joinedPlayerCount;
        public int simulatedRemoteCount;
        public bool localPlayerJoined;

        public TournamentDefinition tournament;
        public TournamentRoomState state = TournamentRoomState.WaitingForPlayers;
        public bool joinLocked;
        public bool launchReady;
        public bool matchPrepared;
        public bool isLocked;
        public bool isResolved;
        public bool rewardGranted;
        public bool levelGenerated;
        public string winnerId;
        public bool duelWaitingForOpponent;

        public float countdownDuration;
        public float countdownRemaining;

        public int selectedLevelIndex = -1;
        public double synchronizedStartServerMs;
        public double duelOpponentFinishServerMs;
        public double duelOpponentFinishOffsetMs;

        public TournamentMatchParticipant localPlayer = new TournamentMatchParticipant();
        public List<TournamentMatchParticipant> remotePlayers = new List<TournamentMatchParticipant>();
        public List<TournamentParticipantResult> rankingData = new List<TournamentParticipantResult>();

        private float[] simulatedJoinTimes;

        public bool IsDestroyed => state == TournamentRoomState.Destroyed;
        public bool IsDuel => tournament != null && tournament.maxPlayers <= 2;
        public bool IsLobbyFull => CurrentPlayerCount >= maxPlayerCount;
        public int CurrentPlayerCount => (localPlayerJoined ? 1 : 0) + simulatedRemoteCount;

        public void Initialize(TournamentDefinition definition)
        {
            tournament = definition;
            tournamentId = definition.id;
            maxPlayerCount = definition.maxPlayers;
            roomId = "room_" + Guid.NewGuid().ToString("N").Substring(0, 12);
            roomSeed = TournamentLevelSelector.GenerateRoomSeed(definition.id, roomId);

            state = TournamentRoomState.WaitingForPlayers;
            joinLocked = false;
            launchReady = false;
            matchPrepared = false;
            isLocked = false;
            isResolved = false;
            rewardGranted = false;
            levelGenerated = false;
            localPlayerJoined = false;
            winnerId = null;
            duelWaitingForOpponent = false;
            joinedPlayerCount = 0;
            simulatedRemoteCount = 0;
            selectedLevelIndex = -1;
            remotePlayers.Clear();
            rankingData.Clear();

            countdownDuration = Mathf.Max(5f, definition.waitingSeconds);
            countdownRemaining = countdownDuration;
            PrecomputeJoinSchedule();

            localPlayer = new TournamentMatchParticipant
            {
                id = LocalPlayerId,
                displayName = "You",
                isLocal = true
            };
        }

        public void EnsureLocalPlayerJoined()
        {
            if (localPlayerJoined || joinLocked) return;
            localPlayerJoined = true;
            joinedPlayerCount = 1;
        }

        public void PrecomputeJoinSchedule()
        {
            int remoteSlots = Mathf.Max(0, maxPlayerCount - 1);
            simulatedJoinTimes = new float[remoteSlots];
            var rng = new System.Random(roomSeed);

            for (int i = 0; i < remoteSlots; i++)
                simulatedJoinTimes[i] = (float)rng.NextDouble() * countdownDuration * 0.92f;

            Array.Sort(simulatedJoinTimes);
        }

        public void TickWaiting(float deltaTime)
        {
            if (launchReady || state == TournamentRoomState.Destroyed) return;
            if (state != TournamentRoomState.WaitingForPlayers && state != TournamentRoomState.Ready)
                return;

            countdownRemaining -= deltaTime;
            UpdateSimulatedJoins();

            if (IsLobbyFull && !joinLocked)
                LockForMatch();

            if (countdownRemaining <= 0f)
            {
                if (!joinLocked)
                    LockForMatch();
                launchReady = true;
            }

            // Anti-freeze: never wait more than countdown + grace
            if (countdownRemaining <= -3f)
            {
                if (!joinLocked)
                    LockForMatch();
                launchReady = true;
            }
        }

        private void UpdateSimulatedJoins()
        {
            if (joinLocked || simulatedJoinTimes == null) return;

            float elapsed = countdownDuration - countdownRemaining;
            int filled = 0;
            for (int i = 0; i < simulatedJoinTimes.Length; i++)
            {
                if (elapsed >= simulatedJoinTimes[i])
                    filled++;
            }

            simulatedRemoteCount = filled;
        }

        public void LockForMatch()
        {
            if (joinLocked) return;

            joinLocked = true;
            simulatedRemoteCount = maxPlayerCount - (localPlayerJoined ? 1 : 0);
            joinedPlayerCount = maxPlayerCount;
            GenerateSharedLevel();
            state = TournamentRoomState.Ready;
        }

        public void ForceReadyForLaunch()
        {
            if (!joinLocked)
                LockForMatch();

            if (!levelGenerated)
                GenerateSharedLevel();

            launchReady = true;
            state = TournamentRoomState.Ready;
        }

        public void GenerateSharedLevel()
        {
            if (levelGenerated || tournament == null) return;
            selectedLevelIndex = TournamentLevelSelector.PickLevelIndex(roomSeed, tournament);
            levelGenerated = true;
        }

        /// <summary>
        /// Applies server-authoritative room data from FastAPI without changing UI flow.
        /// </summary>
        public void ApplyApiRoomData(
            string apiRoomId,
            int levelIndex,
            int levelSeed,
            int playerCount,
            string status,
            int waitingSeconds)
        {
            if (!string.IsNullOrEmpty(apiRoomId))
                roomId = apiRoomId;

            roomSeed = levelSeed;
            if (levelIndex >= 0)
            {
                selectedLevelIndex = levelIndex;
                levelGenerated = true;
            }

            simulatedRemoteCount = Mathf.Max(0, playerCount - (localPlayerJoined ? 1 : 0));
            joinedPlayerCount = Mathf.Max(joinedPlayerCount, playerCount);

            if (waitingSeconds > 0)
            {
                countdownDuration = waitingSeconds;
                countdownRemaining = waitingSeconds;
            }

            if (status == "starting" || status == "active")
            {
                joinLocked = true;
                launchReady = true;
                state = TournamentRoomState.Ready;
            }
            else if (playerCount >= maxPlayerCount)
            {
                joinLocked = true;
                state = TournamentRoomState.Ready;
            }
        }

        public void MarkMatchPlaying()
        {
            state = TournamentRoomState.Playing;
        }

        public void TickMatch(float deltaTime)
        {
            if (state != TournamentRoomState.Playing || isResolved || isLocked) return;

            for (int i = 0; i < remotePlayers.Count; i++)
            {
                TournamentMatchParticipant p = remotePlayers[i];
                if (p.hasCompleted || p.isEliminated) continue;

                if (p.isDisconnected &&
                    p.disconnectedAtRealtime > 0f &&
                    Time.realtimeSinceStartup - p.disconnectedAtRealtime > TournamentRoomRegistry.ReconnectTimeoutSeconds)
                {
                    p.isEliminated = true;
                }
            }
        }

        public void MarkLocalEliminated()
        {
            if (localPlayer == null) return;
            localPlayer.isEliminated = true;
            localPlayer.isDisconnected = true;
        }

        public string GetStatusMessage()
        {
            if (launchReady) return "Game Starting!";
            if (joinLocked || IsLobbyFull) return "Lobby full — starting soon";
            if (CurrentPlayerCount <= 1) return "Finding players...";
            return "Players joining...";
        }

        public void SetRankingData(List<TournamentParticipantResult> rankings)
        {
            rankingData.Clear();
            if (rankings == null) return;
            rankingData.AddRange(rankings);
        }

        public List<TournamentMatchParticipant> GetAllParticipants()
        {
            var list = new List<TournamentMatchParticipant>();
            if (localPlayerJoined && localPlayer != null)
                list.Add(localPlayer);
            list.AddRange(remotePlayers);
            return list;
        }

        public TournamentMatchParticipant GetParticipant(string playerId)
        {
            if (localPlayer != null && localPlayer.id == playerId) return localPlayer;
            for (int i = 0; i < remotePlayers.Count; i++)
            {
                if (remotePlayers[i].id == playerId) return remotePlayers[i];
            }
            return null;
        }

        public void Cleanup()
        {
            remotePlayers.Clear();
            rankingData.Clear();
            simulatedJoinTimes = null;
            localPlayer = null;
            winnerId = null;
            tournament = null;
            localPlayerJoined = false;
            state = TournamentRoomState.Destroyed;
        }
    }
}
