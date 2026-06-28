using UnityEngine;
using UnityEngine.UI;

namespace Mkey {
    public class SettingsWindowController : PopUpsController
    {
        [SerializeField]
        private Text playerName;
        [SerializeField]
        private Image avatarImage;
        [SerializeField]
        private ToggleButton highlightModeButton;
        [SerializeField]
        private Button restartButton;

        #region regular
        private void Start()
        {
            RefreshPlayerName(PlayerDataHolder.FullName);
            RefreshPlayerAvatar(AvatarsHolder.AvatarIndex);
            PlayerDataHolder.Instance.ChangeEvent += RefreshPlayerName;
            AvatarsHolder.Instance.ChangeEvent += RefreshPlayerAvatar;
            if (GameBoard.Instance)
            {
                RefreshHiglightModeButton(GameBoard.Instance.IsHihglightFreeMode);
            }
            else
            {
                RefreshHiglightModeButton(PlayerPrefsExtension.GetBool("free_highlight", false));
            }
            if (GameBoard.Instance)
            {
                GameBoard.Instance.ChangeFreeHiglightModeAction += RefreshHiglightModeButton;
            }
            if (restartButton) restartButton.gameObject.SetActive(GameBoard.Instance); // hide restart on map scene
        }

        private void OnDestroy()
        {
            PlayerDataHolder.Instance.ChangeEvent -= RefreshPlayerName;
            AvatarsHolder.Instance.ChangeEvent -= RefreshPlayerAvatar;
            if (GameBoard.Instance)
            {
                GameBoard.Instance.ChangeFreeHiglightModeAction -= RefreshHiglightModeButton;
            }
        }
        #endregion regular

        private void RefreshPlayerName(string plName)
        {
            if (playerName) playerName.text = plName;
        }

        private void RefreshPlayerAvatar(int index)
        {
            if (avatarImage) avatarImage.sprite = AvatarsHolder.Instance.GetAvatarSprite();
        }

        public void ToggleHiglihtMode()
        {
            if (GameBoard.Instance)
            {
                GameBoard.Instance.SetHiglightFreeMode(!GameBoard.Instance.IsHihglightFreeMode);
            }
            else
            {
               bool highlight =  PlayerPrefsExtension.GetBool("free_highlight", false);
               PlayerPrefsExtension.SetBool("free_highlight", !highlight);
                RefreshHiglightModeButton(!highlight);
            }
        }

        private void RefreshHiglightModeButton(bool highlight)
        {
            if (highlightModeButton)
            {
                highlightModeButton.SetOnWithoutNotify(highlight);
            }
        }
    }
}
