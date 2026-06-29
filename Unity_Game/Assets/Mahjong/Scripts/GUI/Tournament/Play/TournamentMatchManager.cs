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

        private static int pendingRank;
        private static int pendingPrize;
        private static bool pendingDuelWin;
        private static bool pendingResultCached;

        private float onlineRoomPollTimer;
        private bool onlineRoomPollInFlight;

        private const float OnlineRoomPollIntervalSeconds = 2.5f;

        private static bool duelServerScoreSubmitted;

        private static bool UseSimulatedDuelOpponent => ApiConfig.Current.UseLocalSimulation;

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
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;
            if (room.state != TournamentRoomState.Playing) return;

            if (UseSimulatedDuelOpponent)
            {
                if (room.IsDuel)
                    TickSimulatedDuelOpponent();
                else
                    TickSimulatedRaceFinishers();
                return;
            }

            if (!TournamentApiBridge.IsOnlineMode || string.IsNullOrEmpty(room.roomId)) return;

            onlineRoomPollTimer += Time.unscaledDeltaTime;
            if (onlineRoomPollTimer < OnlineRoomPollIntervalSeconds || onlineRoomPollInFlight)
                return;

            onlineRoomPollTimer = 0f;
            onlineRoomPollInFlight = true;
            instance.StartCoroutine(PollOnlineRoomRoutine());
        }

        private static void TickSimulatedDuelOpponent()
        {
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

                RegisterDuelParticipant(
                    opponent.id,
                    room.duelOpponentFinishServerMs,
                    simulatedScore,
                    simulatedMoves,
                    (float)(room.duelOpponentFinishServerMs / 1000d));
                TryResolveDuelInstant();
            }
        }

        private static void TickSimulatedRaceFinishers()
        {
            if (room.isResolved || room.isLocked || room.localPlayer.hasCompleted) return;

            double nowMs = TournamentServerClock.NowMs;
            foreach (TournamentMatchParticipant remote in room.remotePlayers)
            {
                if (remote.isEliminated || remote.hasCompleted) continue;
                if (nowMs >= remote.completionServerMs)
                    remote.hasCompleted = true;
            }

            int paidCount = TournamentPrizeTable.GetPaidRankCount(room.tournament.id);
            if (paidCount <= 0) return;

            int finishedCount = CountFinishedParticipants();
            if (finishedCount >= paidCount)
                FinalizeInstantRaceLoss(CountFinishedParticipantsExcludingLocal() + 1);
        }

        private static IEnumerator PollOnlineRoomRoutine()
        {
            try
            {
                if (!HasActiveRoom || room.isResolved || room.isLocked)
                    yield break;

                var fetchTask = TournamentService.FetchRoomSnapshotAsync(room.roomId);
                while (!fetchTask.IsCompleted)
                    yield return null;

                if (!fetchTask.Result.Success || fetchTask.Result.Data == null)
                    yield break;

                ApplyOnlineRoomSnapshot(fetchTask.Result.Data);
            }
            finally
            {
                if (instance)
                    instance.onlineRoomPollInFlight = false;
            }
        }

        private static void ApplyOnlineRoomSnapshot(RoomSnapshotDto snapshot)
        {
            if (IsRoomFinished(snapshot.status))
            {
                ApplyServerRankFromSnapshot(snapshot);
                return;
            }

            if (room.IsDuel)
            {
                ApplyDuelOpponentSubmission(snapshot);
                return;
            }

            if (!room.localPlayer.hasCompleted && HasPaidWinnerSlotsFilled(snapshot))
                ApplyInstantRaceLossFromSnapshot(snapshot);
        }

        private static bool IsRoomFinished(string status) =>
            status == "finished" || status == "locked";

        private static bool HasPaidWinnerSlotsFilled(RoomSnapshotDto snapshot)
        {
            int paid = snapshot.paidWinnerSlots > 0
                ? snapshot.paidWinnerSlots
                : TournamentPrizeTable.GetPaidRankCount(room.tournament.id);
            int submitted = snapshot.submittedCount > 0
                ? snapshot.submittedCount
                : CountSubmittedPlayers(snapshot);
            return paid > 0 && submitted >= paid;
        }

        private static int CountSubmittedPlayers(RoomSnapshotDto snapshot)
        {
            if (snapshot.players == null) return 0;
            int count = 0;
            foreach (RoomPlayerDto player in snapshot.players)
            {
                if (player != null && player.hasSubmitted)
                    count++;
            }
            return count;
        }

        private static void ApplyDuelOpponentSubmission(RoomSnapshotDto snapshot)
        {
            if (room.localPlayer.hasCompleted) return;

            RoomPlayerDto opponentDto = FindOpponentPlayer(snapshot);
            if (opponentDto == null || !opponentDto.hasSubmitted) return;

            TournamentMatchParticipant opponent = GetDuelOpponent();
            if (opponent == null || opponent.hasCompleted) return;

            double finishMs = room.synchronizedStartServerMs +
                              Mathf.Max(1, opponentDto.elapsedSeconds) * 1000d;

            RegisterDuelParticipant(
                opponent.id,
                finishMs,
                Mathf.Max(0, opponentDto.score),
                Mathf.Max(1, opponentDto.moves),
                Mathf.Max(1f, opponentDto.elapsedSeconds));

            ResolveDuel(localWon: false);
        }

        private static void ApplyInstantRaceLossFromSnapshot(RoomSnapshotDto snapshot)
        {
            int rank = CountSubmittedPlayers(snapshot) + 1;
            FinalizeInstantRaceLoss(rank);
        }

        private static void ApplyServerRankFromSnapshot(RoomSnapshotDto snapshot)
        {
            if (room.isResolved) return;

            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;
            RoomPlayerDto localDto = null;
            if (snapshot.players != null)
            {
                foreach (RoomPlayerDto player in snapshot.players)
                {
                    if (player != null && player.userId == localUserId)
                    {
                        localDto = player;
                        break;
                    }
                }
            }

            if (localDto == null || localDto.rank <= 0)
                return;

            int prize = TournamentPrizeTable.GetPrize(room.tournament.id, localDto.rank);
            if (localDto.rank > 0 && prize == 0 && localDto.score > 0)
                prize = 0;

            if (!room.localPlayer.hasCompleted)
            {
                TournamentSession.FinishGameplay(ScoreHolder.Instance ? ScoreHolder.Count : 0);
                TournamentGameSessionController.StopTracking();
            }

            ApplyServerFinish(localDto.rank, prize, room.IsDuel && localDto.rank == 1);
        }

        private static RoomPlayerDto FindOpponentPlayer(RoomSnapshotDto snapshot)
        {
            if (snapshot.players == null) return null;
            int localUserId = NetworkManager.HasInstance ? NetworkManager.Instance.UserId : 0;

            foreach (RoomPlayerDto player in snapshot.players)
            {
                if (player == null || player.userId == localUserId)
                    continue;
                return player;
            }

            return null;
        }

        public static void OnLocalPlayerCompleted(int score, int moves, float elapsedSeconds)
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;
            if (room.localPlayer != null && room.localPlayer.hasCompleted) return;

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
                if (UseSimulatedDuelOpponent)
                {
                    TournamentMatchParticipant duelOpponent = GetDuelOpponent();
                    if (duelOpponent == null) return;

                    double nowMs = TournamentServerClock.IsRunning
                        ? TournamentServerClock.NowMs
                        : room.synchronizedStartServerMs;

                    float elapsed = TournamentSession.GetLiveElapsedSeconds();
                    int score = ScoreHolder.Instance ? ScoreHolder.Count : 0;

                    RegisterDuelParticipant(
                        TournamentRoom.LocalPlayerId,
                        nowMs,
                        score,
                        TournamentSession.MoveCount,
                        elapsed);

                    RegisterDuelParticipant(
                        duelOpponent.id,
                        nowMs,
                        Mathf.Max(score + 100, 500),
                        Mathf.Max(1, TournamentSession.MoveCount + 1),
                        Mathf.Max(1f, elapsed * 0.85f));

                    TryResolveDuelInstant();
                    return;
                }

                TournamentSession.FinishGameplay(ScoreHolder.Instance ? ScoreHolder.Count : 0);
                TournamentGameSessionController.StopTracking();
                RegisterDuelParticipant(
                    TournamentRoom.LocalPlayerId,
                    TournamentServerClock.NowMs,
                    ScoreHolder.Instance ? ScoreHolder.Count : 0,
                    TournamentSession.MoveCount,
                    TournamentSession.GetLiveElapsedSeconds());

                if (instance != null)
                    instance.StartCoroutine(OnlineSubmitAndResolveRoutine(
                        ScoreHolder.Instance ? ScoreHolder.Count : 0,
                        TournamentSession.MoveCount,
                        Mathf.RoundToInt(TournamentSession.GetLiveElapsedSeconds()),
                        duelMode: true));
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

            if (UseSimulatedDuelOpponent &&
                opponent != null && !opponent.hasCompleted && nowMs >= room.duelOpponentFinishServerMs)
            {
                RegisterDuelParticipant(
                    opponent.id,
                    room.duelOpponentFinishServerMs,
                    Mathf.Max(500, Mathf.RoundToInt(score * UnityEngine.Random.Range(0.85f, 1.15f))),
                    Mathf.Max(1, moves + UnityEngine.Random.Range(2, 14)),
                    (float)(room.duelOpponentFinishServerMs / 1000d));
            }

            TournamentSession.FinishGameplay(score);
            TournamentGameSessionController.StopTracking();
            FreezeLocalGameplay();

            RegisterDuelParticipant(TournamentRoom.LocalPlayerId, nowMs, score, moves, elapsedSeconds);

            if (TournamentApiBridge.IsOnlineMode && !UseSimulatedDuelOpponent && instance != null)
            {
                instance.StartCoroutine(OnlineSubmitAndResolveRoutine(score, moves, elapsedSeconds, duelMode: true));
                return;
            }

            TryResolveDuelInstant();
        }

        private static IEnumerator OnlineSubmitAndResolveRoutine(int score, int moves, float elapsedSeconds, bool duelMode)
        {
            FreezeLocalGameplay();
            TournamentSession.FinishGameplay(score);
            TournamentGameSessionController.StopTracking();

            var submitTask = TournamentService.SubmitScoreAsync(
                room.roomId,
                score,
                moves,
                Mathf.RoundToInt(elapsedSeconds));

            while (!submitTask.IsCompleted)
                yield return null;

            if (submitTask.Result.Success && submitTask.Result.Data != null)
            {
                duelServerScoreSubmitted = true;

                if (submitTask.Result.Data.finalized)
                {
                    ApplyServerFinish(
                        submitTask.Result.Data.rank,
                        submitTask.Result.Data.prize,
                        duelMode && submitTask.Result.Data.rank == 1);
                }
                else if (!duelMode)
                {
                    room.localPlayer.hasCompleted = true;
                    room.localPlayer.score = score;
                    room.localPlayer.moves = moves;
                    room.localPlayer.timeSeconds = elapsedSeconds;
                    room.localPlayer.completionServerMs = TournamentServerClock.NowMs;
                }
                else
                {
                    TryResolveDuelInstant();
                }

                yield break;
            }

            if (duelMode)
                TryResolveDuelInstant();
            else
                FinalizeSimulatedMultiResult(score, moves, elapsedSeconds);
        }

        private static void ApplyServerFinish(int rank, int prize, bool duelWin)
        {
            if (!HasActiveRoom || room.isResolved) return;

            rank = Mathf.Max(1, rank);
            prize = Mathf.Max(0, prize);
            room.isLocked = true;
            room.state = TournamentRoomState.Locked;
            FinalizeResult(rank, prize, duelWin: duelWin);
        }

        private static void RegisterDuelParticipant(
            string playerId,
            double serverCompletionMs,
            int score,
            int moves,
            float elapsedSeconds)
        {
            if (!HasActiveRoom) return;

            TournamentMatchParticipant participant = room.GetParticipant(playerId);
            if (participant == null || participant.isEliminated) return;
            if (participant.hasCompleted) return;

            participant.hasCompleted = true;
            participant.completionServerMs = serverCompletionMs;
            participant.score = score;
            participant.moves = moves;
            participant.timeSeconds = elapsedSeconds;
        }

        private static void TryResolveDuelInstant()
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;

            TournamentMatchParticipant local = room.localPlayer;
            TournamentMatchParticipant opponent = GetDuelOpponent();
            if (local == null || opponent == null) return;

            bool localDone = local.hasCompleted;
            bool oppDone = opponent.hasCompleted;
            if (!localDone && !oppDone) return;

            if (localDone && !oppDone)
            {
                ResolveDuel(localWon: true);
                return;
            }

            if (oppDone && !localDone)
            {
                ResolveDuel(localWon: false);
                return;
            }

            ResolveDuel(DidLocalWinDuel(local, opponent));
        }

        private static void ResolveDuel(bool localWon)
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;

            room.duelWaitingForOpponent = false;
            room.isLocked = true;
            room.winnerId = localWon ? room.localPlayer.id : GetDuelOpponent()?.id;
            room.state = TournamentRoomState.Locked;
            TournamentSession.StopGameplay();
            FreezeLocalGameplay();
            TournamentGameSessionController.StopTracking();

            int prize = localWon ? TournamentPrizeTable.GetPrize(room.tournament.id, 1) : 0;
            FinalizeResult(rank: localWon ? 1 : 2, prize: prize, duelWin: localWon);
        }

        private static bool DidLocalWinDuel(
            TournamentMatchParticipant local,
            TournamentMatchParticipant opponent)
        {
            if (local.timeSeconds < opponent.timeSeconds) return true;
            if (local.timeSeconds > opponent.timeSeconds) return false;
            if (local.score > opponent.score) return true;
            if (local.score < opponent.score) return false;
            return local.moves <= opponent.moves;
        }

        private static void TryRegisterCompletion(
            string playerId,
            double serverCompletionMs,
            int score,
            int moves,
            float elapsedSeconds)
        {
            RegisterDuelParticipant(playerId, serverCompletionMs, score, moves, elapsedSeconds);
            TryResolveDuelInstant();
        }

        private static void ResolveDuelIfBothFinished()
        {
            TryResolveDuelInstant();
        }

        private static void HandleMultiplayerLocalComplete(int score, int moves, float elapsedSeconds)
        {
            if (room.isResolved || room.isLocked) return;

            if (TournamentApiBridge.IsOnlineMode && !UseSimulatedDuelOpponent && instance != null)
            {
                instance.StartCoroutine(OnlineSubmitAndResolveRoutine(score, moves, elapsedSeconds, duelMode: false));
                return;
            }

            FinalizeSimulatedMultiResult(score, moves, elapsedSeconds);
        }

        private static void FinalizeSimulatedMultiResult(int score, int moves, float elapsedSeconds)
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
                if (!remote.hasCompleted)
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

        private static void FinalizeInstantRaceLoss(int rank)
        {
            if (!HasActiveRoom || room.isResolved || room.isLocked) return;

            rank = Mathf.Max(1, rank);
            TournamentSession.StopGameplay();
            FreezeLocalGameplay();
            TournamentGameSessionController.StopTracking();

            int prize = TournamentPrizeTable.GetPrize(room.tournament.id, rank);
            FinalizeResult(rank, prize, duelWin: false);
        }

        private static int CountFinishedParticipants()
        {
            int count = 0;
            if (room.localPlayer != null && room.localPlayer.hasCompleted)
                count++;
            foreach (TournamentMatchParticipant remote in room.remotePlayers)
            {
                if (!remote.isEliminated && remote.hasCompleted)
                    count++;
            }
            return count;
        }

        private static int CountFinishedParticipantsExcludingLocal()
        {
            int count = 0;
            foreach (TournamentMatchParticipant remote in room.remotePlayers)
            {
                if (!remote.isEliminated && remote.hasCompleted)
                    count++;
            }
            return count;
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

            pendingRank = rank;
            pendingPrize = prize;
            pendingDuelWin = duelWin;
            pendingResultCached = true;

            ShowPendingResultDialog();
        }

        public static void ShowPendingResultDialog()
        {
            if (!pendingResultCached || !HasActiveRoom) return;

            int rank = pendingRank;
            int prize = pendingPrize;
            bool duelWin = pendingDuelWin;

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

        private static void ClearPendingResult()
        {
            pendingResultCached = false;
            pendingRank = 0;
            pendingPrize = 0;
            pendingDuelWin = false;
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
            ClearPendingResult();
            duelServerScoreSubmitted = false;
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
            if (!duelServerScoreSubmitted)
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

                duelServerScoreSubmitted = true;
            }

            duelServerScoreSubmitted = false;

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
