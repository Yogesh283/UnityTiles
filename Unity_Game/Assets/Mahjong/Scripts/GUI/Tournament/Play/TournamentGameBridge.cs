using Mkey;
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

                if (!TournamentMatchManager.PrepareMatchFromRoom())
                    TournamentRoomRegistry.ForcePrepareForLaunch();

                if (!TournamentMatchManager.HasActiveRoom ||
                    TournamentMatchManager.MatchLevelIndex < 0)
                {
                    Debug.LogError("TournamentGameBridge: match preparation failed.");
                    SafeReturnToTournamentPage();
                    return;
                }

                TournamentSession.BindRoom(
                    TournamentMatchManager.ActiveRoomId,
                    TournamentMatchManager.MatchLevelIndex);

                TournamentSession.PrepareGameLevel();

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
