using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Mkey
{
    public class RewardChestLine : MonoBehaviour
    {
        [SerializeField]
        private GameObject rewardGroup;
        [SerializeField]
        private AudioClip clip;
        [SerializeField]
        private UnityEvent ApplyRewardEvent;

        public bool IsActive { get; private set; }

        #region temp vars
        private SoundMaster MSound => SoundMaster.Instance;
        #endregion temp vars

        #region regular
        private void Start()
        {
            if (rewardGroup) { rewardGroup.SetActive(false); }
        }

        internal void Show(Action completeCallBack)
        {
            if (!rewardGroup)
            {
                completeCallBack?.Invoke();
                return;
            }

            if (rewardGroup)
            {
                if (clip) MSound.PlayClip(0, clip);
                rewardGroup.SetActive(true);
                IsActive = true;
                RectTransform rT = rewardGroup.GetComponent<RectTransform>();
                SimpleTween.Value(gameObject, Vector3.zero, Vector3.one * 1.1f, 0.5f).SetOnUpdate((Vector3 t) => { if (rT) rT.localScale = t; }).AddCompleteCallBack(completeCallBack).SetEase(EaseAnim.EaseOutBounce);
            }
        }

        internal void ApplyReward()
        {
            ApplyRewardEvent?.Invoke();
        }
        #endregion regular
    }
}
