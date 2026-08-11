using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;

namespace Mkey
{
    /// <summary>Restored from E-drive TilesClash; drag threshold raised so taps never become drags.</summary>
    public class TouchManager : TouchPadMessageTarget, IPointerExitHandler
    {
        public bool dlog = false;
        public static TouchManager Instance;
        public Transform PointerUpObject;
        public Transform FirstObject;

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

        // E-drive used 0.1f which turned every finger jitter into a drag.
        private const float MinDragWorld = 0.45f;

        #region regular
        private IEnumerator Start()
        {
            if (Instance != null) Destroy(gameObject);
            else Instance = this;

            while (!TouchPad.Instance) yield return new WaitForEndOfFrame();
            // Shell can enter Play after this Start — always hook the pad.
            TouchPad.Instance.ScreenDragEvent += LastScreenDragHandler;
            TouchPad.Instance.ScreenPointerDownEvent += LastScreenPointerDownEventHandler;
            TouchPad.Instance.ScreenPointerUpEvent += LastScreePointerUpEventHandler;
            dragPathLength = 0;
        }
        #endregion regular

        public static bool IsMobileDevice()
        {
            if (SystemInfo.deviceType == DeviceType.Desktop) return false;
            if (SystemInfo.deviceType == DeviceType.Handheld) return true;
            return false;
        }

        internal static void SetTouchActivity(bool activity)
        {
            if (TouchPad.Instance) TouchPad.Instance.SetTouchActivity(activity);
            if (!activity && Instance != null)
            {
                // Shuffle / collect disable: drop drag + selection so nothing stays sticky.
                Instance.CanDrag = false;
                Instance.MinDragReached = false;
                Instance.SetFirstObject(null, null);
            }
        }

        /// <summary>Clear FirstObject if the transform was destroyed (match/collect).</summary>
        public static void PurgeDestroyedFirstObject()
        {
            if (Instance == null) return;
            // Live Unity object: overloaded != null is true.
            if (Instance.FirstObject != null) return;
            // True C# null (no selection) — nothing to purge.
            if (ReferenceEquals(Instance.FirstObject, null)) return;
            // Destroyed Unity object (fake-null) — wipe sticky selection.
            Instance.FirstObject = null;
            Instance.PointerUpObject = null;
            Instance.CanDrag = false;
            Instance.MinDragReached = false;
        }

        public static void ClearSelectionState()
        {
            if (Instance == null) return;
            Instance.CanDrag = false;
            Instance.MinDragReached = false;
            Instance.SetFirstObject(null, null);
        }

        #region touchpad handlers
        private void LastScreenDragHandler(TouchPadEventArgs tpea)
        {
            if (!CanDrag) return;
            tPEA = tpea;
            Vector3 newPos = tpea.WorldPos;
            dragPathLength += (newPos - dragPos).magnitude;
            dragPos = newPos;
            dragDirection = dragPos - pointerDownPos;
            dragMagnitude = dragDirection.magnitude;
            MinDragReached = dragMagnitude >= MinDragWorld || dragPathLength >= MinDragWorld;
#if UNITY_EDITOR
            if (dlog) Debug.Log("drag: " + gameObject.name + " ; Draggable: " + FirstObject + " ; distance:" + dragMagnitude);
#endif
            if (FirstObject && MinDragReached)
            {
                if (!followStarted) StartCoroutine(SlowFollowC());
            }
        }

        private IEnumerator SlowFollowC()
        {
            followStarted = true;
            if (FirstObject && CanDrag) FirstObject.position = draggableStartPos + dragDirection;
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
            // Shell direct-touch owns selection — TouchPad Up must not clear FirstObject.
            if (BoardDirectTouchController.IsAuthoritative) return;

            CanDrag = false;
            if (FirstObject && PointerUpObject) return;
            if (FirstObject) ResetDragEventRaise(null);
        }
        #endregion touchpad handlers

        #region interface implement
        public void OnPointerExit(PointerEventData eventData)
        {
            if (BoardDirectTouchController.IsAuthoritative) return;
            if (GameBoard.GMode == GameMode.Play)
            {
                CanDrag = false;
                if (FirstObject) ResetDragEventRaise(null);
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
