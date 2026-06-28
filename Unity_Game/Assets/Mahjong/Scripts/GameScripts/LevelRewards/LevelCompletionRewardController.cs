using UnityEngine;

namespace Mkey
{
    /// <summary>
    /// Listens for level wins and grants one-time coin rewards (levels 1–300).
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class LevelCompletionRewardController : MonoBehaviour
    {
        private static LevelCompletionRewardController instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance) return;

            GameObject host = new GameObject(nameof(LevelCompletionRewardController));
            instance = host.AddComponent<LevelCompletionRewardController>();
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

        private void OnEnable()
        {
            GameEvents.WinLevelAction += OnWinLevel;
        }

        private void OnDisable()
        {
            GameEvents.WinLevelAction -= OnWinLevel;
        }

        private void OnWinLevel()
        {
            if (Tournament.TournamentSession.IsActive) return;

            int levelIndex = GameLevelHolder.CurrentLevel;
            if (!LevelCompletionRewardService.TryGrantReward(levelIndex, out int levelNumber, out int balance))
                return;

            LevelCoinRewardEffect.Play(LevelCompletionRewardService.CoinsPerLevel);
            ShowRewardPopup(balance);
        }

        private static void ShowRewardPopup(int balance)
        {
            GuiController gui = GuiController.Instance;
            if (!gui)
                gui = Object.FindFirstObjectByType<GuiController>();

            WarningMessController messagePrefab = Resources.Load<WarningMessController>("PopUps/Message");
            if (!gui || !messagePrefab)
            {
                Debug.LogWarning("LevelCompletionReward: popup unavailable — coins were still added.");
                return;
            }

            string body =
                $"+{LevelCompletionRewardService.CoinsPerLevel} Coins Added\n" +
                $"Current Balance: {balance:N0} Coins";

            gui.ShowMessageWithYesNoCloseButton(
                messagePrefab,
                "🎉 Level Complete!",
                body,
                () => { },
                null,
                null);
        }
    }
}
