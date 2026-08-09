using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
    23.08.2020 - first
    23.10.2020 - check overlay raycaster
    14.12.2020 - check EvenSystemObject existing
 */
namespace Mkey
{
    [Serializable]
    public class TouchPadEventArgs
    {
        /// <summary>
        /// First selected object.
        /// </summary>
        public TouchPadMessageTarget firstSelected;
        /// <summary>
        /// The cast results.
        /// </summary>
        public Collider2D[] hits;
        /// <summary>
        /// Priority dragging direction.  (0,1) or (1,0)
        /// </summary>
        public Vector2 PriorAxe
        {
            get { return priorityAxe; }
        }
        /// <summary>
        /// Touch delta position in screen coordinats;
        /// </summary>
        public Vector2 DragDirection
        {
            get { return touchDeltaPosRaw; }
        }
        /// <summary>
        /// Last drag direction.
        /// </summary>
        public Vector2 LastDragDirection
        {
            get { return lastDragDir; }
        }
        /// <summary>
        /// Return touch world position.
        /// </summary>
        public Vector3 WorldPos
        {
            get { return wPos; }
        }

        private Vector2 touchDeltaPosRaw;
        private Vector2 priorityAxe;
        private Vector2 lastDragDir;
        private Vector3 wPos;
        private Vector2 touchPos;

        /// <summary>
        /// Fill touch arguments from touch object;
        /// </summary>
        public void SetTouch(Touch touch, bool onlyTopCollider)
        {
            // check overlay
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = touch.position;
            List<RaycastResult> results = new List<RaycastResult>();
            if(EventSystem.current) EventSystem.current.RaycastAll(pointerData, results);
            IgnoreCatcherHits(results);
            //  Debug.Log("results.Count: " + results.Count);
            if (results.Count > 0) { hits = new Collider2D[0]; return; }

            touchPos = touch.position;
            if ((Camera.main)) wPos = Camera.main.ScreenToWorldPoint(touchPos);

            if (onlyTopCollider)
            {
                List<Collider2D> hl = new List<Collider2D>(Physics2D.OverlapPointAll(new Vector2(wPos.x, wPos.y)));
                if (hl.Count > 0)
                {
                    hits = new Collider2D[] { hl[0] };
                }
                else
                {
                    hits = new Collider2D[0];
                }
            }
            else
            {
                hits = Physics2D.OverlapPointAll(new Vector2(wPos.x, wPos.y));
            }

            touchDeltaPosRaw = touch.deltaPosition;

            if (touch.phase == TouchPhase.Moved)
            {
                lastDragDir = touchDeltaPosRaw;
                priorityAxe = GetPriorityOneDirAbs(touchDeltaPosRaw);
            }
        }

        /// <summary>
        /// Fill touch arguments.
        /// </summary>
        public void SetTouch(Vector2 position, Vector2 deltaPosition, TouchPhase touchPhase, bool onlyTopCollider)
        {
            float distRCZ = float.MaxValue;
            // check overlay
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = position;
            List<RaycastResult> results = new List<RaycastResult>();
            if(EventSystem.current)   EventSystem.current.RaycastAll(pointerData, results);
            IgnoreCatcherHits(results);
            //  Debug.Log("results.Count: " + results.Count);
            float camPosZ = (Camera.main) ? Camera.main.transform.position.z : -float.MaxValue;

            if (results.Count > 0)
            {
                distRCZ = (Camera.main) ? results[0].worldPosition.z - camPosZ : float.MaxValue;
            }

            touchPos = position;
            if (Camera.main) wPos = Camera.main.ScreenToWorldPoint(touchPos);

            List<Collider2D> hl = new List<Collider2D>(Physics2D.OverlapPointAll(new Vector2(wPos.x, wPos.y)));
            if (hl.Count > 0)
            {
                hl.RemoveAll((coll)=> { return (coll.transform.position.z - camPosZ > distRCZ); });
            }

            if (onlyTopCollider)
            {
                if (hl.Count > 0)
                {
                    hits = new Collider2D[] { hl[0] };
                }
                else
                {
                    hits = new Collider2D[0];
                }
            }
            else
            {
                hits = Physics2D.OverlapPointAll(new Vector2(wPos.x, wPos.y));
            }

            touchDeltaPosRaw = deltaPosition;

            if (touchPhase == TouchPhase.Moved)
            {
                lastDragDir = touchDeltaPosRaw;
                priorityAxe = GetPriorityOneDirAbs(touchDeltaPosRaw);
            }
        }


        /// <summary>
        /// Return drag icon for firs touched elment or null.
        /// </summary>
        public GameObject GetIconDrag()
        {
            if (firstSelected != null)
            {
                GameObject icon = firstSelected.GetDataIcon();
                return icon;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Drops UI raycast hits that must not shadow the tile board: the full-screen
        /// <see cref="TouchPad"/> catcher itself and any fully-transparent overlay. These are
        /// always hit on a board tap, and using them to compute the depth cutoff (their
        /// worldPosition.z vs the camera z is unreliable on device) wrongly filtered out every
        /// tile — so a tap registered but no tile was ever selected. Only a real, visible UI
        /// panel should block the tiles beneath it.
        /// </summary>
        private static void IgnoreCatcherHits(List<RaycastResult> results)
        {
            if (results == null || results.Count == 0) return;
            results.RemoveAll(r =>
            {
                if (!r.gameObject) return true;
                if (r.gameObject.GetComponent<TouchPad>()) return true;
                Graphic g = r.gameObject.GetComponent<Graphic>();
                return g != null && g.color.a <= 0.02f;
            });
        }

        private Vector2 GetPriorityOneDirAbs(Vector2 sourceDir)
        {

            if (Mathf.Abs(sourceDir.x) > Mathf.Abs(sourceDir.y))
            {
                float x = (sourceDir.x > 0) ? 1 : 1;
                return new Vector2(x, 0f);
            }
            else
            {
                float y = (sourceDir.y > 0) ? 1 : 1;
                return new Vector2(0f, y);
            }
        }
    }
}