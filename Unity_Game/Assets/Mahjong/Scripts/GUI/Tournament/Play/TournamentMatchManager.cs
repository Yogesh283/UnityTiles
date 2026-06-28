using System;
using System.Collections;
using System.Collections.Generic;
using Mkey;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    /// <summary>
    /// Unified tournament match lifecycle for all room sizes (2–1000 players).
    /// </summary>
    [DefaultExecutionOrder(-88)]
    public class TournamentMatchManager : MonoBehaviour
    {
        private static TournamentMatchManager instance;
        private static TournamentRoom room;

        public static bool HasActiveRoom => room != null && !room.IsDestroyed;
        public static bool IsDuelMode => HasActiveRoom && room.IsDuel;
        public static bool IsMatchResolved => HasActiveRoom && room.isResolved;
        public static bool IsMatchLocked => HasActiveRoom && room.isLocked;
        public static string ActiveRoomId => HasActiveRoom ? room.roomId : null;
        public static int MatchLevelIndex => HasActiveRoom ? room.selectedLevelIndex : -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;
            GameObject host = new GameObject(nameof(TournamentMatchManager));
            instance = host.AddComponent<TournamentMatchManager>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static void CreateRoom(TournamentDefinition tournament) =>
            AttachRoom(TournamentRoomRegistry.JoinOrGetRoom(tournament));

        public static void AttachRoom(TournamentRoom existingRoom)
        {
            room = existingRoom;
        }

        public static bool PrepareMatchFromRoom()
        {
            if (!HasActiveRoom) return false;
            if (room.matchPrepared) return room.levelGenerated;

            if (!room.joinLocked)
                room.LockForMatch();

            if (!room.levelGenerated)
                room.GenerateSharedLevel();

            if (room.IsDuel)
                PrepareDuelOpponent();
            else
                GenerateSimulatedOpponents(room.maxPlayerCount - 1);

            room.matchPrepared = true;
            room.joinedPlayerCount = room.maxPlayerCount;

            Debug.Log(
                $"Tournament match prepared: {room.roomId} | {room.tournamentId} | " +
                $"players {room.maxPlayerCount} | level {room.selectedLevelIndex + 1}");

            return room.levelGenerated && room.selectedLevelIndex >= 0;
        }

        [Obsolete("Use AttachRoom + PrepareMatchFromRoom via TournamentRoomRegistry.")]
        public static bool CreateRoomForLobbyFull(TournamentDefinition tournament)
        {
            AttachRoom(TournamentRoomRegistry.JoinOrGetRoom(tournament));
            if (!HasActiveRoom) return false;
            room.ForceReadyForLaunch();
            return PrepareMatchFromRoom();
        }

        public static void BeginSynchronizedMatch()
        {
            if (!HasActiveRoom || room.isResolved) return;
            if (room.state == TournamentRoomState.Playing) return;
            if (!room.levelGenerated) return;

            TournamentServerClock.StartRoomClock();
            room.synchronizedStartServerMs = TournamentServerClock.NowMs;
            room.state = TournamentRoomState.Playing;
            room.MarkMatchPlaying();

            if (room.IsDuel)
            {
                room.duelOpponentFinishServerMs =
                    room.synchronizedStartServerMs + room.duelOpponentFinishOffsetMs;
            }
            else
            {
                ApplySimulatedOpponentTimestamps();
            }

            TournamentSession.StartGameplayTracking();
        }

        private void Update()
        {
            if (!HasActiveRoom || room.isResolved || !room.IsDuel) return;
            if (room.isLocked || room.state != TournamentRoomState.Playing) return;

            TournamentMatchParticipant opponent = GetDuelOpponent();
            if (opponent == null || opponent.hasCompleted || opponent.isEliminated) return;

            if (TournamentServerClock.NowMs >= room.duelOpponentFinishServerMs)
            {
                int simulatedScore = Mathf.Max(
                    500,
                    TournamentSession.FinalScore > 0
                        ? Mathf.RoundToInt(TournamentSession.FinalScore * UnityEngine.Random.Range(0.85f, 1.15f))
                        : UnityEngine.Random.Range(800, 2400));
                int simulatedMoves = Mathf.Max(1, TournamentSession.MoveCount + UnityEngine.Random.Range(2, 14));

                TryRegisterCompletion(
                    opponent.id,
                    room.duelOpponentFinishServerMs,
                    simulatedScore,
                    simulatedMoves,
                    (float)(room.duelOpponentFinishServerMs / 1000d));
            }
        }

        public static void OnLocalPlayerCompleted(int score, int moves, float elapsedSeconds)
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;

            if (room.IsDuel)
            {
                HandleDuelLocalComplete(score, moves, elapsedSeconds);
                return;
            }

            HandleMultiplayerLocalComplete(score, moves, elapsedSeconds);
        }

        public static void ForfeitAsLoss()
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;
            if (TournamentResultDialog.IsVisible) return;

            if (room.IsDuel)
            {
                TournamentMatchParticipant opponent = GetDuelOpponent();
                if (opponent == null) return;

                double nowMs = TournamentServerClock.IsRunning
                    ? TournamentServerClock.NowMs
                    : room.synchronizedStartServerMs;

                TryRegisterCompletion(
                    opponent.id,
                    nowMs,
                    Mathf.Max(500, ScoreHolder.Instance ? ScoreHolder.Count : 0),
                    Mathf.Max(1, TournamentSession.MoveCount + 1),
                    TournamentSession.GetLiveElapsedSeconds());
                return;
            }

            HandleMultiplayerLocalComplete(
                ScoreHolder.Instance ? ScoreHolder.Count : 0,
                TournamentSession.MoveCount,
                TournamentSession.GetLiveElapsedSeconds());
        }

        private static void HandleDuelLocalComplete(int score, int moves, float elapsedSeconds)
        {
            double nowMs = TournamentServerClock.NowMs;
            TournamentMatchParticipant opponent = GetDuelOpponent();

            if (opponent != null && !opponent.hasCompleted && nowMs >= room.duelOpponentFinishServerMs)
            {
                TryRegisterCompletion(
                    opponent.id,
                    room.duelOpponentFinishServerMs,
                    Mathf.Max(500, Mathf.RoundToInt(score * UnityEngine.Random.Range(0.85f, 1.15f))),
                    Mathf.Max(1, moves + UnityEngine.Random.Range(2, 14)),
                    (float)(room.duelOpponentFinishServerMs / 1000d));
                return;
            }

            TournamentSession.FinishGameplay(score);
            TournamentGameSessionController.StopTracking();

            TryRegisterCompletion(TournamentRoom.LocalPlayerId, nowMs, score, moves, elapsedSeconds);
        }

        private static void TryRegisterCompletion(
            string playerId,
            double serverCompletionMs,
            int score,
            int moves,
            float elapsedSeconds)
        {
            if (!HasActiveRoom || room.isResolved) return;

            TournamentMatchParticipant participant = room.GetParticipant(playerId);
            if (participant == null || participant.hasCompleted || participant.isEliminated) return;

            participant.hasCompleted = true;
            participant.completionServerMs = serverCompletionMs;
            participant.score = score;
            participant.moves = moves;
            participant.timeSeconds = elapsedSeconds;

            if (room.isLocked)
            {
                ResolveDuelIfBothFinished();
                return;
            }

            room.isLocked = true;
            room.winnerId = playerId;
            room.state = TournamentRoomState.Locked;
            TournamentSession.StopGameplay();

            if (playerId != TournamentRoom.LocalPlayerId)
            {
                FreezeLocalGameplay();
                TournamentGameSessionController.StopTracking();
            }

            bool localWon = playerId == TournamentRoom.LocalPlayerId;
            int prize = localWon ? TournamentPrizeTable.GetPrize(room.tournament.id, 1) : 0;
            FinalizeResult(rank: localWon ? 1 : 2, prize: prize, duelWin: localWon);
        }

        private static void ResolveDuelIfBothFinished()
        {
            if (!room.isLocked || room.isResolved) return;

            TournamentMatchParticipant local = room.localPlayer;
            TournamentMatchParticipant opponent = GetDuelOpponent();
            if (local == null || opponent == null || !local.hasCompleted || !opponent.hasCompleted)
                return;

            string winnerId = local.completionServerMs <= opponent.completionServerMs
                ? local.id
                : opponent.id;

            if (winnerId == room.winnerId) return;
            room.winnerId = winnerId;
        }

        private static void HandleMultiplayerLocalComplete(int score, int moves, float elapsedSeconds)
        {
            if (room.isResolved || room.isLocked) return;

            double nowMs = TournamentServerClock.NowMs;

            TournamentSession.FinishGameplay(score);
            TournamentGameSessionController.StopTracking();

            room.localPlayer.hasCompleted = true;
            room.localPlayer.score = score;
            room.localPlayer.moves = moves;
            room.localPlayer.timeSeconds = elapsedSeconds;
            room.localPlayer.completionServerMs = nowMs;

            foreach (TournamentMatchParticipant remote in room.remotePlayers)
            {
                if (remote.isEliminated) continue;
                remote.hasCompleted = true;
                if (remote.completionServerMs == double.MaxValue)
                    remote.completionServerMs = room.synchronizedStartServerMs + remote.timeSeconds * 1000d;
            }

            List<TournamentMatchParticipant> all = room.GetAllParticipants();
            TournamentMatchResult result = TournamentRankingService.BuildFinalResult(
                room.tournament,
                room.localPlayer,
                all,
                prizeWon: 0,
                room.selectedLevelIndex);

            int prize = TournamentPrizeTable.GetPrize(room.tournament.id, result.playerRank);
            result.prizeWon = prize;
            room.SetRankingData(result.leaderboard);
            FinalizeResult(result.playerRank, prize, duelWin: false, matchResult: result);
        }

        private static void FinalizeResult(
            int rank,
            int prize,
            bool duelWin,
            TournamentMatchResult matchResult = null)
        {
            if (!HasActiveRoom || room.isResolved) return;

            room.isResolved = true;
            room.state = TournamentRoomState.Resolved;
            TournamentSession.StopGameplay();
            FreezeLocalGameplay();

            if (prize > 0 && !room.rewardGranted)
            {
                room.rewardGranted = true;
                if (ApiConfig.Current.UseLocalSimulation)
                {
                    if (TournamentRewardGuard.TryClaimReward(room.roomId, prize) && CoinsHolder.Instance)
                    {
                        CoinsHolder.Add(prize);
                        LevelCoinRewardEffect.Play(prize);
                    }
                }
                else
                {
                    LevelCoinRewardEffect.Play(prize);
                }
            }

            if (matchResult == null)
            {
                matchResult = new TournamentMatchResult
                {
                    tournamentId = room.tournament.id,
                    tournamentName = room.tournament.displayName,
                    maxPlayers = room.tournament.maxPlayers,
                    levelIndex = room.selectedLevelIndex,
                    playerRank = rank,
                    playerScore = room.localPlayer.score,
                    playerTimeSeconds = room.localPlayer.timeSeconds,
                    playerMoves = room.localPlayer.moves,
                    prizeWon = prize,
                    entryFee = room.tournament.entryFee
                };
            }

            matchResult.prizeWon = prize;
            matchResult.playerRank = rank;
            matchResult.levelIndex = room.selectedLevelIndex;
            room.SetRankingData(matchResult.leaderboard);
            TournamentHistoryService.SaveResult(matchResult);
            SyncMatchToServer(matchResult);

            if (duelWin)
            {
                TournamentResultDialog.ShowDuelWin(prize, () => TournamentResultDialog.ReturnToTournamentPage());
                return;
            }

            if (prize > 0)
            {
                TournamentResultDialog.ShowRankWin(rank, prize, () => TournamentResultDialog.ReturnToTournamentPage());
                return;
            }

            if (!duelWin && room.IsDuel)
            {
                TournamentResultDialog.ShowDuelLoss(() => TournamentResultDialog.ReturnToTournamentPage());
                return;
            }

            TournamentResultDialog.ShowRankLoss(room.tournament.id, rank, () => TournamentResultDialog.ReturnToTournamentPage());
        }

        private static void FreezeLocalGameplay()
        {
            if (GameBoard.Instance)
                GameBoard.Instance.SetControlActivity(false, false);
        }

        private static TournamentMatchParticipant GetDuelOpponent() =>
            room.remotePlayers.Count > 0 ? room.remotePlayers[0] : null;

        private static void PrepareDuelOpponent()
        {
            room.remotePlayers.Clear();
            var rng = new System.Random(room.roomSeed);
            float opponentSeconds = 40f + (float)rng.NextDouble() * 80f;

            room.duelOpponentFinishOffsetMs = opponentSeconds * 1000d;
            room.remotePlayers.Add(new TournamentMatchParticipant
            {
                id = TournamentRoom.RemotePlayerId,
                displayName = "Opponent",
                isLocal = false
            });
        }

        private static void GenerateSimulatedOpponents(int count)
        {
            room.remotePlayers.Clear();
            if (count <= 0) return;

            var rng = new System.Random(room.roomSeed);

            for (int i = 0; i < count; i++)
            {
                float time = 45f + (float)rng.NextDouble() * 130f;
                room.remotePlayers.Add(new TournamentMatchParticipant
                {
                    id = "bot_" + i,
                    displayName = TournamentRankingService.GetBotName(i),
                    isLocal = false,
                    hasCompleted = false,
                    timeSeconds = time,
                    score = 900 + rng.Next(0, 3200),
                    moves = 12 + rng.Next(0, 55)
                });
            }
        }

        private static void ApplySimulatedOpponentTimestamps()
        {
            foreach (TournamentMatchParticipant remote in room.remotePlayers)
            {
                if (remote.isEliminated) continue;
                remote.completionServerMs = room.synchronizedStartServerMs + remote.timeSeconds * 1000d;
            }
        }

        public static void DestroyRoom()
        {
            if (room != null)
                room.Cleanup();

            room = null;
            TournamentServerClock.Reset();
            TournamentRoomRegistry.ReleaseLocalRoom();
        }

        private static void SyncMatchToServer(TournamentMatchResult matchResult)
        {
            if (!TournamentApiBridge.IsOnlineMode || !HasActiveRoom || matchResult == null)
                return;

            if (NetworkManager.HasInstance)
                NetworkManager.Instance.StartCoroutine(SubmitMatchRoutine(matchResult));
        }

        private static IEnumerator SubmitMatchRoutine(TournamentMatchResult matchResult)
        {
            var submitTask = TournamentService.SubmitScoreAsync(
                room.roomId,
                matchResult.playerScore,
                matchResult.playerMoves,
                Mathf.RoundToInt(matchResult.playerTimeSeconds));

            while (!submitTask.IsCompleted)
                yield return null;

            if (!submitTask.Result.Success)
            {
                Debug.LogWarning("[TournamentMatchManager] Submit score failed: " + submitTask.Result.ErrorMessage);
                yield break;
            }

            var walletTask = WalletService.SyncToCoinsHolderAsync();
            while (!walletTask.IsCompleted)
                yield return null;

            var historyTask = TournamentService.FetchHistoryAsync();
            while (!historyTask.IsCompleted)
                yield return null;

            if (historyTask.Result.Success)
                TournamentHistoryService.ApplyApiHistory(historyTask.Result.Data);
        }
    }
}
