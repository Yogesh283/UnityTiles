using Mkey;
using Mkey.Network;
using UnityEngine;

namespace Mkey.Tournament
{
    public static class TournamentGameBridge
    {
        /// <summary>
        /// Waiting room finished — attach registry room, prepare match, load game scene.
        /// </summary>
        public static void LaunchGameFromWaitingRoom()
        {
            TournamentTransitionProbe.LogLaunchGameFromWaitingRoom();
            if (!TournamentSession.IsActive || TournamentSession.Tournament == null)
                return;

            try
            {
                TournamentRoom registryRoom = TournamentRoomRegistry.LocalRoom;
                if (registryRoom == null)
                    registryRoom = TournamentRoomRegistry.JoinOrGetRoom(TournamentSession.Tournament);

                if (registryRoom == null)
                {
                    Debug.LogError("TournamentGameBridge: no room available for launch.");
                    SafeReturnToTournamentPage();
                    return;
                }

                TournamentMatchManager.AttachRoom(registryRoom);

                // Pull the server-authoritative level/seed while the live room is still available.
                // The registry room retains this data even if the API session briefly drops,
                // so BOTH duel clients can still enter the board.
                if (TournamentApiBridge.IsOnlineMode && TournamentApiBridge.HasMatchedRoom)
                    TournamentMatchManager.SyncLevelFromServerAuthority();

                if (!TournamentMatchManager.PrepareMatchFromRoom())
                    TournamentRoomRegistry.ForcePrepareForLaunch();

                TournamentMatchManager.SyncLevelFromServerAuthority();

                // Last-resort: force a shared level so the second player never gets stuck
                // on the waiting room when the API room is momentarily unavailable at launch.
                if (!TournamentMatchManager.HasActiveRoom ||
                    TournamentMatchManager.MatchLevelIndex < 0)
                {
                    TournamentFlowLog.LevelLoaded("forcing shared level — API room unavailable at launch");
                    TournamentRoomRegistry.ForcePrepareForLaunch();
                }

                if (!TournamentMatchManager.HasActiveRoom ||
                    TournamentMatchManager.MatchLevelIndex < 0)
                {
                    Debug.LogError("TournamentGameBridge: match preparation failed.");
                    SafeReturnToTournamentPage();
                    return;
                }

                TournamentSession.BindRoom(
                    TournamentMatchManager.ActiveRoomId,
                    TournamentMatchManager.MatchLevelIndex,
                    TournamentMatchManager.ActiveRoomSeed);
                TournamentSession.PrepareGameLevel();

                // Log the exact shared sync data both clients must agree on (server authoritative).
                RoomResponseDto apiRoom = TournamentApiBridge.CurrentRoom;
                Debug.Log(
                    "[TournamentSync] Launch handoff — shared state: " +
                    $"room={TournamentMatchManager.ActiveRoomId} " +
                    $"level={TournamentMatchManager.MatchLevelIndex} " +
                    $"seed={TournamentMatchManager.ActiveRoomSeed} " +
                    $"match_start_at_ms={apiRoom?.matchStartAtMs?.ToString() ?? "null"} " +
                    $"server_now_ms={apiRoom?.serverNowMs?.ToString() ?? "null"} " +
                    $"players={apiRoom?.playerCount}");

                Debug.Log(
                    $"[Tournament] Launch level {TournamentSession.MatchLevelIndex + 1} " +
                    $"seed {TournamentSession.RoomSeed} room {TournamentSession.ActiveRoomId}");
                TournamentFlowLog.LevelLoaded(
                    $"level={TournamentSession.MatchLevelIndex} seed={TournamentSession.RoomSeed} room={TournamentSession.ActiveRoomId}");

                TournamentSession.MarkLobbyLaunchReady();
                TournamentGlobalWaitingRoom.Hide();

                if (SceneLoader.Instance)
                    SceneLoader.Instance.LoadScene(TournamentSession.GameSceneIndex);
                else
                    UnityEngine.SceneManagement.SceneManager.LoadScene(TournamentSession.GameSceneIndex);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                SafeReturnToTournamentPage();
            }
        }

        public static void HandleLevelComplete()
        {
            if (!TournamentSession.IsActive || TournamentSession.Tournament == null)
                return;

            if (TournamentMatchManager.IsMatchResolved || TournamentMatchManager.IsMatchLocked)
                return;

            TournamentLevelRewardService.GrantOnLevelComplete();

            int score = ScoreHolder.Instance ? ScoreHolder.Count : 0;
            TournamentMatchManager.OnLocalPlayerCompleted(
                score,
                TournamentSession.MoveCount,
                TournamentSession.GetLiveElapsedSeconds());
        }

        private static void SafeReturnToTournamentPage()
        {
            TournamentResultDialog.ReturnToTournamentPage();
        }
    }
}
