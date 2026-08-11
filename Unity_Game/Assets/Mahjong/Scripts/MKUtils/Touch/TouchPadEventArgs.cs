using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Mkey.Shell;

namespace Mkey
{
    [Serializable]
    public class TouchPadEventArgs
    {
        public TouchPadMessageTarget firstSelected;
        public Collider2D[] hits;

        public Vector2 PriorAxe { get { return priorityAxe; } }
        public Vector2 DragDirection { get { return touchDeltaPosRaw; } }
        public Vector2 LastDragDirection { get { return lastDragDir; } }
        public Vector3 WorldPos { get { return wPos; } }

        private Vector2 touchDeltaPosRaw;
        private Vector2 priorityAxe;
        private Vector2 lastDragDir;
        private Vector3 wPos;
        private Vector2 touchPos;

        // ~56px fingertip assist — scales with ortho so off-center taps still hit the intended tile.
        private const float FingerAssistScreenPx = 56f;
        private const float FingerAssistWorldMin = 0.25f;
        private const float FingerAssistWorldMax = 0.60f;

        private static readonly Collider2D[] s_overlapBuf = new Collider2D[24];
        private static readonly List<Collider2D> s_hitScratch = new List<Collider2D>(24);

        private static Camera ResolveBoardCamera()
        {
            Camera cam = Camera.main;
            if (cam && cam.isActiveAndEnabled) return cam;
            foreach (Camera c in Camera.allCameras)
            {
                if (c && c.orthographic && c.isActiveAndEnabled) return c;
            }
            return cam;
        }

        private static float AssistRadiusWorld(Camera cam)
        {
            if (!cam || !cam.orthographic) return 0.32f;
            float pxToWorld = (cam.orthographicSize * 2f) / Mathf.Max(1, Screen.height);
            float r = FingerAssistScreenPx * pxToWorld;
            return Mathf.Clamp(r, FingerAssistWorldMin, FingerAssistWorldMax);
        }

        private static void IgnoreShellUiBlocks(List<RaycastResult> results)
        {
            if (results == null || results.Count == 0) return;
            results.RemoveAll(r =>
            {
                if (!r.gameObject) return true;
                if (r.gameObject.GetComponent<TouchPad>()) return true;
                Graphic g = r.gameObject.GetComponent<Graphic>();
                if (g != null && g.color.a <= 0.02f) return true;
                if (g != null && !g.enabled) return true;
                CanvasGroup cg = r.gameObject.GetComponentInParent<CanvasGroup>();
                if (cg && (cg.alpha < 0.05f || !cg.blocksRaycasts || !cg.interactable)) return true;
                Selectable sel = r.gameObject.GetComponentInParent<Selectable>();
                if (sel && !sel.IsInteractable()) return true;
                return false;
            });
            if (MatchIQShellBridge.IsActive) results.Clear();
        }

        /// <summary>
        /// Exact OverlapPoint first; if miss, screen-scaled assist circle.
        /// Does not change Mahjong free/blocked rules — only which collider is probed.
        /// </summary>
        private static List<Collider2D> OverlapBoard(Vector2 worldXY, Camera cam)
        {
            s_hitScratch.Clear();
            int n = Physics2D.OverlapPointNonAlloc(worldXY, s_overlapBuf);
            if (n > 0)
            {
                for (int i = 0; i < n; i++)
                    if (s_overlapBuf[i]) s_hitScratch.Add(s_overlapBuf[i]);
                return s_hitScratch;
            }

            float r = AssistRadiusWorld(cam);
            n = Physics2D.OverlapCircleNonAlloc(worldXY, r, s_overlapBuf);
            for (int i = 0; i < n; i++)
                if (s_overlapBuf[i]) s_hitScratch.Add(s_overlapBuf[i]);
            return s_hitScratch;
        }

        /// <summary>
        /// Topmost visible tile under the finger. Never prefer a lower layer under a higher one
        /// when both contain the point (no click-through). On assist-only misses, prefer nearest
        /// fingertip tile so slightly off-center taps stay human-friendly.
        /// </summary>
        private static void PreferTopMahjongCollider(List<Collider2D> hl, Vector2 touchWorld)
        {
            if (hl == null || hl.Count <= 1) return;
            for (int i = hl.Count - 1; i >= 0; i--)
                if (!hl[i]) hl.RemoveAt(i);
            if (hl.Count <= 1) return;

            bool anyContains = false;
            for (int i = 0; i < hl.Count; i++)
            {
                if (hl[i].OverlapPoint(touchWorld)) { anyContains = true; break; }
            }

            hl.Sort((a, b) =>
            {
                MahjongTile ta = a.GetComponentInParent<MahjongTile>();
                MahjongTile tb = b.GetComponentInParent<MahjongTile>();
                int ha = ta ? 1 : 0;
                int hb = tb ? 1 : 0;
                if (ha != hb) return hb.CompareTo(ha);

                bool ca = a.OverlapPoint(touchWorld);
                bool cb = b.OverlapPoint(touchWorld);
                if (ca != cb) return cb.CompareTo(ca);

                int la = ta ? ta.Layer : -1;
                int lb = tb ? tb.Layer : -1;

                // Exact / containing hits: higher layer always wins (no click-through).
                if (anyContains && la != lb) return lb.CompareTo(la);

                Vector2 ca2 = a.bounds.center;
                Vector2 cb2 = b.bounds.center;
                float da = (ca2 - touchWorld).sqrMagnitude;
                float db = (cb2 - touchWorld).sqrMagnitude;
                int dCmp = da.CompareTo(db);
                if (dCmp != 0) return dCmp;

                // Assist-only equidistant: higher layer still preferred.
                if (la != lb) return lb.CompareTo(la);
                return b.transform.position.z.CompareTo(a.transform.position.z);
            });
        }

        private void FillHits(Camera cam, bool onlyTopCollider, float distRCZ, float camPosZ, bool filterByUiZ)
        {
            Vector2 touchXY = new Vector2(wPos.x, wPos.y);
            List<Collider2D> hl = OverlapBoard(touchXY, cam);
            if (filterByUiZ && hl.Count > 0)
                hl.RemoveAll(coll => coll && (coll.transform.position.z - camPosZ > distRCZ));
            PreferTopMahjongCollider(hl, touchXY);

            if (onlyTopCollider)
                hits = hl.Count > 0 ? new Collider2D[] { hl[0] } : Array.Empty<Collider2D>();
            else
                hits = hl.ToArray();
        }

        public void SetTouch(Touch touch, bool onlyTopCollider)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = touch.position;
            List<RaycastResult> results = new List<RaycastResult>();
            if (EventSystem.current) EventSystem.current.RaycastAll(pointerData, results);
            IgnoreShellUiBlocks(results);
            if (results.Count > 0) { hits = Array.Empty<Collider2D>(); return; }

            touchPos = touch.position;
            Camera cam = ResolveBoardCamera();
            if (!cam) { hits = Array.Empty<Collider2D>(); return; }
            wPos = cam.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, Mathf.Abs(cam.transform.position.z)));
            FillHits(cam, onlyTopCollider, float.MaxValue, cam.transform.position.z, false);

            touchDeltaPosRaw = touch.deltaPosition;
            if (touch.phase == TouchPhase.Moved)
            {
                lastDragDir = touchDeltaPosRaw;
                priorityAxe = GetPriorityOneDirAbs(touchDeltaPosRaw);
            }
        }

        public void SetTouch(Vector2 position, Vector2 deltaPosition, TouchPhase touchPhase, bool onlyTopCollider)
        {
            float distRCZ = float.MaxValue;
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = position;
            List<RaycastResult> results = new List<RaycastResult>();
            if (EventSystem.current) EventSystem.current.RaycastAll(pointerData, results);
            IgnoreShellUiBlocks(results);

            Camera cam = ResolveBoardCamera();
            if (!cam) { hits = Array.Empty<Collider2D>(); return; }

            float camPosZ = cam.transform.position.z;
            bool filterZ = !MatchIQShellBridge.IsActive && results.Count > 0;
            if (filterZ)
                distRCZ = results[0].worldPosition.z - camPosZ;

            touchPos = position;
            wPos = cam.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, Mathf.Abs(cam.transform.position.z)));
            FillHits(cam, onlyTopCollider, distRCZ, camPosZ, filterZ);

            touchDeltaPosRaw = deltaPosition;
            if (touchPhase == TouchPhase.Moved)
            {
                lastDragDir = touchDeltaPosRaw;
                priorityAxe = GetPriorityOneDirAbs(touchDeltaPosRaw);
            }
        }

        public GameObject GetIconDrag()
        {
            return firstSelected != null ? firstSelected.GetDataIcon() : null;
        }

        private Vector2 GetPriorityOneDirAbs(Vector2 sourceDir)
        {
            if (Mathf.Abs(sourceDir.x) > Mathf.Abs(sourceDir.y))
                return new Vector2(1f, 0f);
            return new Vector2(0f, 1f);
        }
    }
}
