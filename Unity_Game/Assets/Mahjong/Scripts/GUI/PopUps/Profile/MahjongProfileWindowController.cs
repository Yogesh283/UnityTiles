using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
    public class MahjongProfileWindowController : PopUpsController
    {
        [SerializeField]
        private Text playerName;

        [SerializeField]
        private InputField inputField;

        [SerializeField]
        private Image avatarImage;

        [SerializeField]
        private Button changeButton;

        [SerializeField]
        private ProfileAvatarButton  avatarButtonPrefab;

        [SerializeField]
        private RectTransform buttonsContainer;

        #region temp vars


        private GameBoard MBoard => GameBoard.Instance;
        private GuiController MGui => GuiController.Instance;
        private GameConstructSet GCSet => GameConstructSet.Instance;
        private GameObjectsSet GOSet => (GCSet) ? GCSet.GOSet : null;
        private GameLevelHolder MGLevel => GameLevelHolder.Instance;
        private CoinsHolder MCoins => CoinsHolder.Instance;
        #endregion temp vars

        #region regular
        private void Start()
        {
            if (inputField)
            {
                inputField.gameObject.SetActive(false);
                inputField.onEndEdit.AddListener((name) =>
                {
                    PlayerDataHolder.Instance.SetFullName(name);
                    inputField.gameObject.SetActive(false);
                    if (changeButton) changeButton.gameObject.SetActive(true);
                    if (playerName) playerName.enabled = true;
                    SetControlActivity(true);
                });
                inputField.enabled = false;
            }

            PlayerDataHolder.Instance.ChangeEvent += RefreshFullName;
            AvatarsHolder.Instance.ChangeEvent += RefreshPlayerAvatar;

            RefreshPlayerAvatar(AvatarsHolder.AvatarIndex);
            RefreshFullName(PlayerDataHolder.FullName);
            CreateButtons();
        }

        private void OnDestroy()
        {
            PlayerDataHolder.Instance.ChangeEvent -= RefreshFullName;
            AvatarsHolder.Instance.ChangeEvent -= RefreshPlayerAvatar;
        }
        #endregion regular

        public void Change_Click()
        {
            if (!inputField) return;
            if (changeButton) changeButton.gameObject.SetActive(false);
            inputField.gameObject.SetActive(true);
            if (playerName) playerName.enabled = false;
            SetControlActivity(false);

            inputField.enabled = true;
            inputField.interactable = true;
            inputField.ActivateInputField();
            inputField.Select();
            Debug.Log("change : " + inputField);
        }

        private void CreateButtons()
        {
            ProfileAvatarButton[] ts = buttonsContainer.GetComponentsInChildren<ProfileAvatarButton>();
            foreach (var item in ts)
            {
                DestroyImmediate(item.gameObject);
            }
            if (AvatarsHolder.Instance.avatars.Length == 0) return;

            for (int i = 0; i < AvatarsHolder.Instance.avatars.Length; i++)
            {
                ProfileAvatarButton t = Instantiate(avatarButtonPrefab, buttonsContainer);
                t.avatarImage.sprite = AvatarsHolder.Instance.avatars[i];
                int index = i;
                t.index = index;
                t.button.onClick.AddListener(()=> { 
                    AvatarsHolder.Instance.SetIndex(index); 
                });
            }
            RefresButtons();
        }

        #region event handlers

        private void SetNewAvatar(Sprite sprite)
        {
          // if (sprite && avatarImage) avatarImage.sprite = sprite;
        }

        private void RemoveAvatar()
        {
            //if (avatarImage) avatarImage.sprite = defaultAvatar;
        }

        private void RefreshFullName(string pName)
        {
            if (playerName) playerName.text = pName;
            if (inputField) inputField.text = PlayerDataHolder.FullName;
        }

        private void RefreshPlayerAvatar(int index)
        {
            if (avatarImage) avatarImage.sprite = AvatarsHolder.Instance.GetAvatarSprite();
            RefresButtons();
        }

        private void RefresButtons()
        {
            ProfileAvatarButton[] pButtons = buttonsContainer.GetComponentsInChildren<ProfileAvatarButton>();
            int index = AvatarsHolder.AvatarIndex;
            foreach (var pB in pButtons)
            {
                pB.CheckButton(pB.index == index);
            }
        }
        #endregion event handlers 
    }
}