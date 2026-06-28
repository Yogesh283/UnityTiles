using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace Mkey
{
    public class TouchManager : TouchPadMessageTarget, IPointerExitHandler
    {
        public bool dlog = false;
        public static TouchManager Instance;
        public Transform PointerUpObject;
        public Transform FirstObject; // can be drag
        //{
        //    get; private set;
        //}

        public bool CanDrag = false;
        public bool MinDragReached = false;

        #region temp vars
        private Vector3 dragPos;
        private Vector3 pointerDownPos;
        private Vector3 draggableStartPos;
        private TouchPadEventArgs tPEA;
        private bool followStarted = false;
        private Vector3 dragDirection;
        private float dragMagnitude;
        private float dragPathLength;
        private Action<Action> ResetDragEvent;
        #endregion temp vars

        #region regular
        private IEnumerator Start()
        {
            if (Instance != null) Destroy(gameObject);
            else
            {
                Instance = this;
            }

            while (!TouchPad.Instance) yield return new WaitForEndOfFrame();
            if (GameBoard.GMode == GameMode.Play)
            {
                TouchPad.Instance.ScreenDragEvent += LastScreenDragHandler;
                TouchPad.Instance.ScreenPointerDownEvent += LastScreenPointerDownEventHandler;
                TouchPad.Instance.ScreenPointerUpEvent += LastScreePointerUpEventHandler;
            }
            dragPathLength = 0;
        }
        #endregion regular

        /// <summary>
        /// Return true touch pad run on mobile device
        /// </summary>
        public static bool IsMobileDevice()
        {
            //check if our current system info equals a desktop
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                //we are on a desktop device, so don't use touch
                return false;
            }
            //if it isn't a desktop, lets see if our device is a handheld device aka a mobile device
            else if (SystemInfo.deviceType == DeviceType.Handheld)
            {
                //we are on a mobile device, so lets use touch input
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enable or disable touch pad callbacks handling.
        /// </summary>
        internal static void SetTouchActivity(bool activity)
        {
            TouchPad.Instance.SetTouchActivity(activity);
        }

        #region touchpad handlers
        private void LastScreenDragHandler(TouchPadEventArgs tpea)
        {
            if (!CanDrag) return;
            tPEA = tpea;
            dragPathLength += (dragPos - pointerDownPos).magnitude;
            dragPos = tpea.WorldPos;
            dragDirection = dragPos - pointerDownPos;
            dragMagnitude = dragDirection.magnitude;
            MinDragReached = (dragPathLength > 0.1f);
#if UNITY_EDITOR
            if (dlog) Debug.Log("drag: " + gameObject.name + " ; Draggable: " + FirstObject + " ; distance:" + dragMagnitude);
#endif
            if (FirstObject )
            {
                // Debug.Log("follow _ 0");
                if (!followStarted) StartCoroutine(SlowFollowC()); // &&  !criticalDrag
            }
        }
        private IEnumerator SlowFollowC() // slow motion
        {
           // Debug.Log("follow_1");
            followStarted = true;
            if(FirstObject && CanDrag) FirstObject.position = draggableStartPos + dragDirection;  // show drag
            yield return new WaitForEndOfFrame();
            followStarted = false;
            if (dlog) Debug.Log("end follow cor");
        }

        private void LastScreenPointerDownEventHandler(TouchPadEventArgs tpea)
        {
            pointerDownPos = tpea.WorldPos;
            dragPos = pointerDownPos;
            dragMagnitude = 0;
            dragPathLength = 0;
            MinDragReached = false;
        }

        private void LastScreePointerUpEventHandler(TouchPadEventArgs tpea)
        {
            // Debug.Log("LastScreenPointerUpEventHandler");
            CanDrag = false;
            if (FirstObject && PointerUpObject) return;
            if (FirstObject)
            {
                ResetDragEventRaise(null);
            }
        }
        #endregion touchpad handlers

        #region interface implement
        public void OnPointerExit(PointerEventData eventData)
        {
            if (GameBoard.GMode == GameMode.Play)
            {
                CanDrag = false;
                if (FirstObject)
                {
                    // FirstObject.GetComponentInParent<TileTouchBehavior>().SetInitialposition();
                    ResetDragEventRaise(null);
                }
            }
        }
        #endregion interface implement

        public void SetFirstObject(Transform firstObject, Action<Action> resetDrag)
        {
            if (firstObject)
            {
                FirstObject = firstObject;
                PointerUpObject = null;
                draggableStartPos = firstObject.transform.position;
            }
            else
            {
                FirstObject = null;
                PointerUpObject = null;
            }
            ResetDragEvent = resetDrag;
        }

        public void ResetDragEventRaise(Action completeCallBack)
        {
            if (dlog) Debug.Log("Reset drag");
            ResetDragEvent?.Invoke(completeCallBack);
        }
    }
}