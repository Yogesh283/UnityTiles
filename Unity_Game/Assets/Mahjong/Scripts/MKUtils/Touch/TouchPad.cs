//#define useinterface

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;
using System.Linq;
using Mkey.Shell;

/*
    TouchPad — EventSystem → OverlapPoint
    + forensic TOUCH_TRACE (no selection behavior change)
 */

namespace Mkey
{
    public class TouchPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler, IDropHandler, IPointerExitHandler
    {
        #region events
        public Action<TouchPadEventArgs> ScreenDragEvent;
        public Action<TouchPadEventArgs> ScreenPointerDownEvent;
        public Action<TouchPadEventArgs> ScreenPointerUpEvent;
        #endregion events

        #region properties
        public Vector2 ScreenDragDirection { get { return ScreenTouchPos - oldPosition; } }
        public Vector3 WorldTouchPos
        {
            get { return CameraMain ? CameraMain.ScreenToWorldPoint(ScreenTouchPos) : Vector3.zero; }
        }
        public Vector2 ScreenTouchPos { get; private set; }
        public bool IsTouched { get; private set; }
        public bool IsActive { get; private set; }
        /// <summary>Current physical press id (synced with TouchForensic).</summary>
        public static int CurrentPressId { get; private set; }
        private Camera CameraMain { get { return Camera.main; } }
        #endregion properties

        [SerializeField] private bool dlog = false;
        [SerializeField] private bool onlyTopCollider = true;

        #region temp vars
        private List<Collider2D> hitList;
        private List<Collider2D> newHitList;
        private List<Collider2D> pointerDownHitList;
        private TouchPadEventArgs tpea;
        private int pointerID;
        private Vector2 oldPosition;
        private float inactiveSinceRealtime = -1f;
        private int activePressId;
        #endregion temp vars

        public static TouchPad Instance;

        // Raw↔Pointer correlation (set by MatchIQShellBridge probe)
        public static int PendingRawPressId;
        public static Vector2 PendingRawScreen;
        public static float PendingRawTime = -999f;
        public static int PendingRawFingerId = -1;

        private static TouchPadMessageTarget ResolveTouchTarget(Collider2D col)
        {
            if (!col) return null;
            TileTouchBehavior tileTouch = col.GetComponent<TileTouchBehavior>()
                ?? col.GetComponentInParent<TileTouchBehavior>();
            if (tileTouch) return tileTouch;
            return col.GetComponent<TouchPadMessageTarget>()
                ?? col.GetComponentInParent<TouchPadMessageTarget>();
        }

        private void DispatchPointerDown(Collider2D col, TouchPadEventArgs args)
        {
            TouchPadMessageTarget target = ResolveTouchTarget(col);
            if (target == null)
            {
                TouchForensic.Fail(activePressId, "WRONG_TILE", $"collider={col.name} no TouchPadMessageTarget");
                return;
            }
            // Shell: BoardDirectTouchController is the only Mahjong tile-selection path.
            if (BoardDirectTouchController.IsAuthoritative && target is TileTouchBehavior)
                return;
            target.PointerDown(args);
            if (args.firstSelected == null) args.firstSelected = target;
        }

        private static void Dispatch(Collider2D col, Action<TouchPadMessageTarget> call)
        {
            TouchPadMessageTarget target = ResolveTouchTarget(col);
            if (target == null) return;
            if (BoardDirectTouchController.IsAuthoritative && target is TileTouchBehavior)
                return;
            call(target);
        }

        void Awake()
        {
            IsActive = true;
            hitList = new List<Collider2D>();
            newHitList = new List<Collider2D>();
            pointerDownHitList = new List<Collider2D>();
            tpea = new TouchPadEventArgs();
            if (Instance) Destroy(gameObject);
            else Instance = this;
        }

        private int AllocatePressIdFromPointer(Vector2 pointerPos)
        {
            // Correlate with raw touch if it arrived within 250ms.
            if (PendingRawPressId > 0 && Time.unscaledTime - PendingRawTime < 0.25f)
            {
                int id = PendingRawPressId;
                float dx = pointerPos.x - PendingRawScreen.x;
                float dy = pointerPos.y - PendingRawScreen.y;
                TouchForensic.PointerDownReceived++;
                TouchForensic.Trace(id, "POINTER_DOWN",
                    $"RAW_RECEIVED=YES POINTER_DOWN_RECEIVED=YES " +
                    $"RAW_TOUCH_POSITION=({PendingRawScreen.x:F1},{PendingRawScreen.y:F1}) " +
                    $"POINTER_EVENT_POSITION=({pointerPos.x:F1},{pointerPos.y:F1}) " +
                    $"delta=({dx:F1},{dy:F1}) UNITY_SCREEN_SIZE=({Screen.width}x{Screen.height}) " +
                    $"safeArea={Screen.safeArea}");
                PendingRawPressId = 0;
                return id;
            }

            int fresh = TouchForensic.BeginPress();
            TouchForensic.PointerDownReceived++;
            TouchForensic.Trace(fresh, "POINTER_DOWN",
                $"RAW_RECEIVED=NO_OR_LATE POINTER_DOWN_RECEIVED=YES " +
                $"POINTER_EVENT_POSITION=({pointerPos.x:F1},{pointerPos.y:F1}) " +
                $"UNITY_SCREEN_SIZE=({Screen.width}x{Screen.height}) safeArea={Screen.safeArea}");
            return fresh;
        }

        public void OnPointerDown(PointerEventData data)
        {
            TouchPipelineTrace.PhysicalTaps++;

            // One primary finger for Mahjong selection — ignore secondary pointers.
            if (IsTouched && data.pointerId != pointerID)
            {
                TouchForensic.Trace(activePressId, "SECONDARY_POINTER_IGNORED",
                    $"got={data.pointerId} primary={pointerID}");
                return;
            }

            if (!IsActive)
            {
                int id = AllocatePressIdFromPointer(data.position);
                activePressId = id;
                CurrentPressId = id;
                TouchForensic.Fail(id, "TOUCHPAD_INACTIVE",
                    $"screen=({data.position.x:F0},{data.position.y:F0}) pointerId={data.pointerId}");
                return;
            }

            if (IsTouched)
            {
                TouchForensic.Trace(activePressId, "STALE_ISTOUCHED_RECOVERED",
                    $"prevPointerId={pointerID}");
                ClearPressLists();
                IsTouched = false;
            }

            IsTouched = true;
            int pressId = AllocatePressIdFromPointer(data.position);
            activePressId = pressId;
            CurrentPressId = pressId;
            TouchForensic.TouchPadReceived++;
            TouchForensic.Trace(pressId, "TOUCHPAD_RECEIVED",
                $"IsActive=true pointerId={data.pointerId} touchCount={Input.touchCount}");

            TouchPipelineTrace.PointerDowns++;
            tpea = new TouchPadEventArgs();
            ScreenTouchPos = data.position;
            oldPosition = ScreenTouchPos;
            pointerID = data.pointerId;

            Camera cam = Camera.main;
            if (!cam)
            {
                TouchForensic.Fail(pressId, "CAMERA_MISS", "Camera.main null");
                ScreenPointerDownEvent?.Invoke(tpea);
                return;
            }

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(ScreenTouchPos.x, ScreenTouchPos.y, Mathf.Abs(cam.transform.position.z)));
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            TouchForensic.Trace(pressId, "WORLD",
                $"world=({world.x:F3},{world.y:F3},{world.z:F3}) cam={cam.name} ortho={cam.orthographicSize}");
            TouchForensic.Trace(pressId, "RAYCAST_ALL", TouchForensic.DumpColliders(new Vector2(world.x, world.y)));
#endif

            tpea.SetTouch(ScreenTouchPos, Vector2.zero, TouchPhase.Began, onlyTopCollider);
            hitList = new List<Collider2D>();
            hitList.AddRange(tpea.hits);
            pointerDownHitList = new List<Collider2D>(hitList);

            if (hitList.Count == 0)
            {
                TouchForensic.Fail(pressId, "RAYCAST_MISS",
                    $"screen=({ScreenTouchPos.x:F0},{ScreenTouchPos.y:F0}) world=({world.x:F2},{world.y:F2})");
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                TouchForensicMarkers.Show(ScreenTouchPos, world, world);
#endif
            }
            else
            {
                TouchForensic.RaycastHit++;
                MahjongTile tile = hitList[0] ? hitList[0].GetComponentInParent<MahjongTile>() : null;
                if (tile)
                {
                    TouchForensic.ValidTile++;
                    bool free = false;
                    try { free = tile.IsFreeToMatch(); } catch { /* ignore */ }
                    TouchForensic.Trace(pressId, "RAYCAST",
                        $"hits={hitList.Count} tile={tile.name} layer={tile.Layer} free={free}");
                    if (free) TouchForensic.FreeTile++;
                    else TouchForensic.Fail(pressId, "NOT_FREE", $"tile={tile.name}");

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    Vector3 cpos = tile.boxCollider ? tile.boxCollider.bounds.center : tile.transform.position;
                    TouchForensicMarkers.Show(ScreenTouchPos, world, cpos);
#endif
                }
                else
                {
                    TouchForensic.Fail(pressId, "WRONG_TILE", $"hit={hitList[0].name}");
                }
            }

            if (hitList.Count > 0)
            {
                for (int i = 0; i < hitList.Count; i++)
                    DispatchPointerDown(hitList[i], tpea);
            }

            ScreenPointerDownEvent?.Invoke(tpea);

            if (TouchForensic.PointerDownReceived > 0 && TouchForensic.PointerDownReceived % 10 == 0)
                TouchForensic.LogSummary();
        }

        private void ClearPressLists()
        {
            hitList = new List<Collider2D>();
            newHitList = new List<Collider2D>();
            pointerDownHitList = new List<Collider2D>();
        }

        /// <summary>Cancel/lost-focus: drop press without waiting for PointerUp.</summary>
        public void ForceClearPress(string reason)
        {
            IsTouched = false;
            ClearPressLists();
            CurrentPressId = TouchForensic.BeginPress();
            TouchForensic.Trace(activePressId, "PRESS_FORCE_CLEAR", reason);
        }

        public void OnBeginDrag(PointerEventData data)
        {
            if (!IsActive)
            {
                TouchForensic.Fail(activePressId, "TOUCHPAD_INACTIVE", "OnBeginDrag");
                return;
            }
            if (data.pointerId != pointerID)
            {
                TouchForensic.Fail(activePressId, "INVALID_POINTER",
                    $"OnBeginDrag got={data.pointerId} expect={pointerID}");
                return;
            }
            ScreenTouchPos = data.position;
            tpea.SetTouch(ScreenTouchPos, ScreenTouchPos - oldPosition, TouchPhase.Moved, onlyTopCollider);
            oldPosition = ScreenTouchPos;
            newHitList = new List<Collider2D>(tpea.hits);
            for (int i = 0; i < hitList.Count; i++)
            {
                if (hitList[i]) Dispatch(hitList[i], x => x.DragBegin(tpea));
            }
            ScreenDragEvent?.Invoke(tpea);
            hitList = newHitList;
        }

        public void OnDrag(PointerEventData data)
        {
            if (!IsActive || data.pointerId != pointerID) return;
            ScreenTouchPos = data.position;
            tpea.SetTouch(ScreenTouchPos, ScreenTouchPos - oldPosition, TouchPhase.Moved, onlyTopCollider);
            oldPosition = ScreenTouchPos;
            newHitList = new List<Collider2D>(tpea.hits);

            foreach (Collider2D cHit in hitList)
            {
                if (newHitList.IndexOf(cHit) == -1)
                {
                    if (cHit) Dispatch(cHit, x => x.DragExit(tpea));
                }
                else
                {
                    if (cHit) Dispatch(cHit, x => x.Drag(tpea));
                }
            }
            for (int i = 0; i < newHitList.Count; i++)
            {
                if (hitList.IndexOf(newHitList[i]) == -1)
                {
                    if (newHitList[i]) Dispatch(newHitList[i], x => x.DragEnter(tpea));
                }
            }
            hitList = newHitList;
            ScreenDragEvent?.Invoke(tpea);
        }

        public void OnPointerUp(PointerEventData data)
        {
            // Always end the press lifecycle — a missed path must not leave IsTouched stuck.
            if (!IsActive || data.pointerId != pointerID)
            {
                IsTouched = false;
                ClearPressLists();
                TouchForensic.Trace(activePressId, "POINTER_UP_SKIP",
                    $"IsActive={IsActive} pointerId={data.pointerId} expect={pointerID}");
                return;
            }
            IsTouched = false;

            TouchPipelineTrace.PointerUps++;
            TouchForensic.Trace(activePressId, "POINTER_UP",
                $"pointerId={data.pointerId} screen=({data.position.x:F0},{data.position.y:F0})");
            ScreenTouchPos = data.position;
            tpea.SetTouch(ScreenTouchPos, ScreenTouchPos - oldPosition, TouchPhase.Ended, onlyTopCollider);
            oldPosition = ScreenTouchPos;

            var upTargets = new List<Collider2D>();
            if (pointerDownHitList != null) upTargets.AddRange(pointerDownHitList);
            foreach (Collider2D cHit in hitList)
            {
                if (cHit && upTargets.IndexOf(cHit) == -1) upTargets.Add(cHit);
            }

            foreach (Collider2D cHit in upTargets)
            {
                if (cHit) Dispatch(cHit, x => x.PointerUp(tpea));
            }

            newHitList = new List<Collider2D>(tpea.hits);
            foreach (Collider2D cHit in newHitList)
            {
                if (cHit && upTargets.IndexOf(cHit) == -1) Dispatch(cHit, x => x.PointerUp(tpea));
                if (cHit) Dispatch(cHit, x => x.DragDrop(tpea));
            }

            hitList = new List<Collider2D>();
            newHitList = new List<Collider2D>();
            pointerDownHitList = new List<Collider2D>();
            ScreenPointerUpEvent?.Invoke(tpea);
        }

        public void OnPointerExit(PointerEventData data)
        {
            IsTouched = false;
            TouchForensic.Trace(activePressId, "POINTER_EXIT",
                $"pointerId={data.pointerId} — clearing press like cancel");
            if (!IsActive || data.pointerId != pointerID) return;

            ScreenTouchPos = data.position;
            tpea.SetTouch(ScreenTouchPos, ScreenTouchPos - oldPosition, TouchPhase.Ended, onlyTopCollider);
            oldPosition = ScreenTouchPos;

            var exitTargets = new List<Collider2D>();
            if (pointerDownHitList != null) exitTargets.AddRange(pointerDownHitList);
            foreach (Collider2D cHit in hitList)
            {
                if (cHit && exitTargets.IndexOf(cHit) == -1) exitTargets.Add(cHit);
            }
            foreach (Collider2D cHit in exitTargets)
            {
                if (cHit) Dispatch(cHit, x => x.PointerUp(tpea));
            }
            newHitList = new List<Collider2D>(tpea.hits);
            foreach (Collider2D cHit in newHitList)
            {
                if (cHit && exitTargets.IndexOf(cHit) == -1) Dispatch(cHit, x => x.PointerUp(tpea));
                if (cHit) Dispatch(cHit, x => x.DragDrop(tpea));
            }
            hitList = new List<Collider2D>();
            newHitList = new List<Collider2D>();
            pointerDownHitList = new List<Collider2D>();
        }

        public void OnDrop(PointerEventData data) { }

        public Vector3 GetWorldTouchPos()
        {
            return CameraMain ? CameraMain.ScreenToWorldPoint(ScreenTouchPos) : Vector3.zero;
        }

        public void SetTouchActivity(bool activity)
        {
            if (IsActive == activity)
            {
                if (activity) inactiveSinceRealtime = -1f;
                return;
            }

            IsActive = activity;
            TouchForensic.Trace(CurrentPressId, "TOUCHPAD_ACTIVITY", $"IsActive={activity}");
            if (!activity)
            {
                inactiveSinceRealtime = Time.realtimeSinceStartup;
                IsTouched = false;
                hitList = new List<Collider2D>();
                newHitList = new List<Collider2D>();
                pointerDownHitList = new List<Collider2D>();
            }
            else
            {
                inactiveSinceRealtime = -1f;
                IsTouched = false;
                hitList = new List<Collider2D>();
                newHitList = new List<Collider2D>();
                pointerDownHitList = new List<Collider2D>();
                CurrentPressId = TouchForensic.BeginPress(); // invalidate prior press handles
            }
#if UNITY_EDITOR
            if (dlog) Debug.Log("touch activity: " + activity);
#endif
        }

        public bool TryRecoverStuckInactive(float maxSeconds)
        {
            if (IsActive) return false;
            if (inactiveSinceRealtime < 0f) return false;
            if (Time.realtimeSinceStartup - inactiveSinceRealtime < maxSeconds) return false;
            SetTouchActivity(true);
            TouchForensic.Fail(CurrentPressId, "TOUCHPAD_INACTIVE",
                $"recovered_stuck_inactive after={maxSeconds:0.0}s");
            return true;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ForceClearPress("APP_FOCUS_LOST");
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) ForceClearPress("APP_PAUSE");
        }

        private void OnDisable()
        {
            ForceClearPress("ON_DISABLE");
        }

#if useinterface
        private T[] GetInterfaces<T>(GameObject gObj)
        {
            if (!typeof(T).IsInterface) throw new SystemException("Specified type is not an interface!");
            var mObjs = MonoBehaviour.FindObjectsOfType<MonoBehaviour>();
            return (from a in mObjs where a.GetType().GetInterfaces().Any(k => k == typeof(T)) select (T)(object)a).ToArray();
        }
        private T GetInterface<T>(GameObject gObj)
        {
            if (!typeof(T).IsInterface) throw new SystemException("Specified type is not an interface!");
            return GetInterfaces<T>(gObj).FirstOrDefault();
        }
#endif
    }
}
