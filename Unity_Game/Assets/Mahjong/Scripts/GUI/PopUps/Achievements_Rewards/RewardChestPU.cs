using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mkey
{
	public class RewardChestPU : PopUpsController
	{
        [SerializeField]
        private RectTransform chest;
        [SerializeField]
        private Sprite openedChest;
        [SerializeField]
        private Sprite closedChest;
        [SerializeField]
        private RewardChestLine hintLine;
        [SerializeField]
        private RewardChestLine shuffleLine;
        [SerializeField]
        private Image chestLight;
        [SerializeField]
        private GameObject buttonCollect;
        [SerializeField]
        private GameObject buttonCollect2;
        [SerializeField]
        private bool autoOpen;

        #region temp vars
        private bool move = false;
        private int colorTweenID;
        #endregion temp vars

        public Action OpenChestEvent;

        #region regular
        void Start()
        {
            if(autoOpen) Open_Click();
        }

        private void OnDestroy()
        {
            SimpleTween.Cancel(colorTweenID, false);
        }
#endregion regular

        public void Open_Click()
        {
            Open(()=>
            {
                if (buttonCollect2) buttonCollect2.gameObject.SetActive(true);
                if (buttonCollect) buttonCollect.gameObject.SetActive(true);
                SetControlActivity(true); 
            });

            SetControlActivity(false);
            if (buttonCollect) buttonCollect.SetActive(false);
            if (buttonCollect2) buttonCollect2.gameObject.SetActive(false);
            OpenChestEvent?.Invoke();
        }

        public void Claim_Click()
        {
           // Open(() => { });     // Open(CloseWindow);
            //foreach (var item in chestLines)
            //{
            //    if (item && item.IsActive) item.ApplyReward();
            //}

            //SetControlActivity(false);
            //if (buttonOpen) buttonOpen.SetActive(false);
            //if (buttonClaim) buttonClaim.SetActive(false);
            //TweenExt.DelayAction(gameObject, 0.5f, CloseWindow);
        }

        public void ScaleOut(Action completeCallBack, float delay)
        {
            if (chest) chest.localScale = Vector3.zero;
            SimpleTween.Value(gameObject, Vector3.zero, Vector3.one, 0.5f).SetOnUpdate((Vector3 val) =>
            {
                if (chest) chest.localScale = val;
            })
            .SetDelay(delay)
            .SetEase(EaseAnim.EaseOutBounce)
            .AddCompleteCallBack(completeCallBack);
        }

        public void Open(Action completeCallBack)
        {
            if(!chest)
            {
                completeCallBack?.Invoke();
                return;
            }

            TweenSeq ts = new TweenSeq();

            Image im = chest.GetComponent<Image>();

            chest.localScale = Vector3.zero;

            ts.Add((callBack) =>
            {
                SimpleTween.Value(gameObject, Vector3.one, new Vector3(1.5f, 0.5f, 1f), 0.1f).SetOnUpdate((Vector3 val) =>
                {
                    if (chest) chest.localScale = val;
                })
          .AddCompleteCallBack(callBack);
            });

            ts.Add((callBack) =>
            {
                if (move)
                    SimpleTween.Value(gameObject, 0, 5, 0.15f).SetOnUpdate((float val) =>
                    {
                        if (chest) { chest.anchoredPosition -= new Vector2(0, val); }
                    });
                SimpleTween.Value(gameObject, new Vector3(1.55f, 0.5f, 1f), new Vector3(1.00f, 1.00f, 1.00f), 0.25f).SetOnUpdate((Vector3 val) =>
                {
                    if (chest) chest.localScale = val;
                })
          .SetEase(EaseAnim.EaseOutBounce)
          .AddCompleteCallBack(callBack);
            });

            ts.Add((callBack) =>
            {
                if (openedChest && im)
                {
                    im.sprite = openedChest;
                }
                if (chestLight)
                {
                    chestLight.gameObject.SetActive(true);
                    colorTweenID = SimpleTween.Value(gameObject, -Mathf.PI / 4f, Mathf.PI / 4f, 1f).SetOnUpdate((float val) =>
                    {
                        if (chestLight) chestLight.color = new Color(1, 1, 1, Mathf.Cos(val));
                    }).SetCycled().ID;
                }
                callBack?.Invoke();
            });

            ts.Add((callBack) =>
            {
                shuffleLine.Show(null);
                hintLine.Show(callBack);
            });

            ts.Add((callBack) =>
            {
                SimpleTween.Value(gameObject, 0, 1, 1.5f).AddCompleteCallBack(callBack);
            });

            ts.Add((callBack) =>
            {
                completeCallBack?.Invoke();
            });
            ts.Start();
        }
    }
}
