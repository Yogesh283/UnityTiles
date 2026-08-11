using UnityEngine;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Authoritative Mahjong tile selection for the RN+Unity shell.
    /// Touch target = FULL visible tile surface (SpriteRenderer), not the Physics2D collider.
    /// </summary>
    public class BoardDirectTouchController : MonoBehaviour
    {
        public static BoardDirectTouchController Instance { get; private set; }

        public static bool IsAuthoritative
        {
            get
            {
                return Instance != null
                    && Instance.isActiveAndEnabled
                    && MatchIQShellBridge.IsActive;
            }
        }

        [Tooltip("Extra screen pixels outside the visible edge (finger imprecision).")]
        [SerializeField] private float edgeToleranceScreenPx = 8f;
        [SerializeField] private float fallbackTopHudFraction = 0.10f;
        [SerializeField] private float fallbackBottomHudFraction = 0.12f;

        private Camera cachedCam;
        private float nextCamRefreshAt = -1f;
        private MahjongTile[] cachedTiles;
        private float nextTileCacheAt = -1f;
        private int primaryFingerId = -1;
        private TileTouchBehavior pressTarget;
        private int nextTouchSeq;

        private readonly Candidate[] candidates = new Candidate[48];

        private struct Candidate
        {
            public MahjongTile tile;
            public bool insideVisual;
            public int layer;
            public int sortingOrder;
            public float centerDistSqr;
        }

        public static BoardDirectTouchController EnsureExists()
        {
            if (Instance) return Instance;
            GameObject go = new GameObject("BoardDirectTouchController");
            DontDestroyOnLoad(go);
            return go.AddComponent<BoardDirectTouchController>();
        }

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) ClearFingerState();
        }

        private void OnApplicationPause(bool pause)
        {
            if (pause) ClearFingerState();
        }

        private void Update()
        {
            if (!MatchIQShellBridge.IsActive) return;
            if (MatchIQShellBridge.IsPaused) return;

            TouchManager.PurgeDestroyedFirstObject();

            if (Input.touchCount > 0)
            {
                ProcessTouches();
                return;
            }

            if (primaryFingerId >= 0) ClearFingerState();

            if (Application.isEditor || Application.platform == RuntimePlatform.WindowsPlayer)
                ProcessMouse();
        }

        private void ProcessTouches()
        {
            if (primaryFingerId >= 0 && !FingerStillActive(primaryFingerId))
                ClearFingerState();

            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (primaryFingerId >= 0 && t.fingerId != primaryFingerId)
                        {
                            LogFail(nextTouchSeq, "DUPLICATE_TOUCH", t.fingerId, t.position, null,
                                $"primaryStill={primaryFingerId}");
                            continue;
                        }
                        HandleBegan(t.fingerId, t.position);
                        break;

                    case TouchPhase.Ended:
                    case TouchPhase.Canceled:
                        if (t.fingerId == primaryFingerId)
                            HandleEnded();
                        break;
                }
            }
        }

        private bool FingerStillActive(int fingerId)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == fingerId) return true;
            }
            return false;
        }

        private void ProcessMouse()
        {
            if (Input.GetMouseButtonDown(0))
                HandleBegan(0, Input.mousePosition);
            else if (primaryFingerId == 0 && Input.GetMouseButtonUp(0))
                HandleEnded();
        }

        private void HandleBegan(int fingerId, Vector2 screenPos)
        {
            int id = ++nextTouchSeq;
            primaryFingerId = fingerId;
            pressTarget = null;

            if (TouchPad.Instance && !TouchPad.Instance.IsActive)
            {
                LogFail(id, "INPUT_GATED", fingerId, screenPos, null, "TouchPad.IsActive=false");
                return;
            }

            Camera cam = ResolveCamera(true);
            if (!cam)
            {
                LogFail(id, "NO_CAMERA", fingerId, screenPos, null, null);
                return;
            }

            float planeDist = Mathf.Abs(cam.transform.position.z);
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, planeDist));
            Vector2 worldXY = new Vector2(world.x, world.y);
            float edgeTolWorld = EdgeToleranceWorld(cam);

            LogTouchHeader(id, fingerId, screenPos, world, cam, edgeTolWorld);

            int candidateCount;
            bool insideVisual;
            bool topmost;
            MahjongTile tile = ResolveTileByVisualSurface(worldXY, edgeTolWorld, out candidateCount, out insideVisual, out topmost);
            if (!tile)
            {
                if (!IsInsideFallbackBoardBand(screenPos))
                    LogFail(id, "OUTSIDE_BOARD", fingerId, screenPos, world, null);
                else
                    LogFail(id, "NO_VISUAL_HIT", fingerId, screenPos, world,
                        $"candidates={candidateCount} edgeTolWorld={edgeTolWorld:F3}");
                return;
            }

            TileTouchBehavior behavior = tile.GetComponent<TileTouchBehavior>()
                ?? tile.GetComponentInParent<TileTouchBehavior>();
            if (!behavior)
            {
                LogFail(id, "WRONG_TILE", fingerId, screenPos, world, tile.name);
                return;
            }

            bool free = false;
            try { free = tile.IsFreeToMatch(); } catch { /* ignore */ }

            // Intended visual tile only — never hop to another tile if blocked.
            behavior.ApplyDirectTap();
            pressTarget = behavior;

            string result = free ? "SELECTED" : "NOT_FREE";
            LogTouchResult(id, fingerId, screenPos, world, tile, insideVisual, topmost, free, result, candidateCount);
            if (!free)
                LogFail(id, "NOT_FREE", fingerId, screenPos, world, tile.name);
        }

        private void HandleEnded()
        {
            TileTouchBehavior target = pressTarget;
            ClearFingerState();
            if (target) target.ApplyDirectRelease();
        }

        private void ClearFingerState()
        {
            primaryFingerId = -1;
            pressTarget = null;
        }

        private bool IsInsideFallbackBoardBand(Vector2 screenPos)
        {
            float yMin = Screen.height * Mathf.Clamp01(fallbackBottomHudFraction);
            float yMax = Screen.height * (1f - Mathf.Clamp01(fallbackTopHudFraction));
            return screenPos.y >= yMin && screenPos.y <= yMax
                && screenPos.x >= 0f && screenPos.x <= Screen.width;
        }

        private Camera ResolveCamera(bool forceRefresh)
        {
            if (!forceRefresh && cachedCam && cachedCam.isActiveAndEnabled && Time.unscaledTime < nextCamRefreshAt)
                return cachedCam;

            cachedCam = null;
            Camera main = Camera.main;
            if (main && main.orthographic && main.isActiveAndEnabled)
                cachedCam = main;

            if (!cachedCam)
            {
                Camera[] cams = Camera.allCameras;
                int bestDepth = int.MinValue;
                for (int i = 0; i < cams.Length; i++)
                {
                    Camera c = cams[i];
                    if (!c || !c.orthographic || !c.isActiveAndEnabled) continue;
                    if (c.depth >= bestDepth)
                    {
                        bestDepth = (int)c.depth;
                        cachedCam = c;
                    }
                }
            }

            nextCamRefreshAt = Time.unscaledTime + 0.5f;
            return cachedCam;
        }

        private float EdgeToleranceWorld(Camera cam)
        {
            if (!cam || !cam.orthographic) return 0.08f;
            float pixelH = Mathf.Max(1f, cam.pixelRect.height);
            float pxToWorld = (cam.orthographicSize * 2f) / pixelH;
            return Mathf.Clamp(edgeToleranceScreenPx, 5f, 10f) * pxToWorld;
        }

        private MahjongTile[] GetActiveTiles()
        {
            if (cachedTiles != null && Time.unscaledTime < nextTileCacheAt)
                return cachedTiles;

            cachedTiles = null;
            GameBoard board = GameBoard.Instance;
            if (board && board.MainGrid != null)
                cachedTiles = board.MainGrid.GetTiles();
            nextTileCacheAt = Time.unscaledTime + 0.35f;
            return cachedTiles;
        }

        /// <summary>
        /// Select the visually topmost tile whose FULL visible surface contains the touch
        /// (plus a small edge tolerance). Does not use collider size as the touch target.
        /// </summary>
        private MahjongTile ResolveTileByVisualSurface(
            Vector2 worldXY, float edgeTolWorld,
            out int candidateCount, out bool insideVisual, out bool topmost)
        {
            candidateCount = 0;
            insideVisual = false;
            topmost = false;

            MahjongTile[] tiles = GetActiveTiles();
            if (tiles == null || tiles.Length == 0) return null;

            int count = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                MahjongTile tile = tiles[i];
                if (!tile || !tile.isActiveAndEnabled) continue;
                if (!tile.gameObject.activeInHierarchy) continue;

                if (!IsTouchInsideVisibleSurface(tile, worldXY, edgeTolWorld, out float centerDistSqr, out int sorting))
                    continue;

                candidates[count] = new Candidate
                {
                    tile = tile,
                    insideVisual = true,
                    layer = tile.Layer,
                    sortingOrder = sorting,
                    centerDistSqr = centerDistSqr
                };
                count++;
                if (count >= candidates.Length) break;
            }

            candidateCount = count;
            if (count == 0) return null;

            int best = 0;
            for (int i = 1; i < count; i++)
            {
                Candidate a = candidates[i];
                Candidate b = candidates[best];
                // Visually on top: higher Mahjong layer, then sprite sorting order.
                if (a.layer != b.layer)
                {
                    if (a.layer > b.layer) best = i;
                    continue;
                }
                if (a.sortingOrder != b.sortingOrder)
                {
                    if (a.sortingOrder > b.sortingOrder) best = i;
                    continue;
                }
                if (a.centerDistSqr < b.centerDistSqr) best = i;
            }

            insideVisual = true;
            topmost = true;
            return candidates[best].tile;
        }

        /// <summary>
        /// Full-surface test in the sprite's local space so left/right/top/bottom/corners
        /// match the visible tile even after BoardScreenFitter scale/move.
        /// </summary>
        private static bool IsTouchInsideVisibleSurface(
            MahjongTile tile, Vector2 worldXY, float edgeTolWorld,
            out float centerDistSqr, out int sortingOrder)
        {
            centerDistSqr = float.MaxValue;
            sortingOrder = 0;

            SpriteRenderer sr = tile.SRenderer;
            Transform visual = sr ? sr.transform : tile.transform;

            Vector3 world3 = new Vector3(worldXY.x, worldXY.y, visual.position.z);
            Vector3 local = visual.InverseTransformPoint(world3);

            float minX, maxX, minY, maxY;
            Vector2 worldCenter;

            if (sr && sr.sprite)
            {
                // sprite.bounds is in the SpriteRenderer's local space.
                Bounds lb = sr.sprite.bounds;
                minX = lb.min.x;
                maxX = lb.max.x;
                minY = lb.min.y;
                maxY = lb.max.y;
                sortingOrder = sr.sortingOrder;
                worldCenter = sr.bounds.center;
            }
            else if (sr)
            {
                // No sprite asset — use runtime world bounds mapped to local.
                Bounds wb = sr.bounds;
                Vector3 locMin = visual.InverseTransformPoint(wb.min);
                Vector3 locMax = visual.InverseTransformPoint(wb.max);
                minX = Mathf.Min(locMin.x, locMax.x);
                maxX = Mathf.Max(locMin.x, locMax.x);
                minY = Mathf.Min(locMin.y, locMax.y);
                maxY = Mathf.Max(locMin.y, locMax.y);
                sortingOrder = sr.sortingOrder;
                worldCenter = wb.center;
            }
            else if (tile.boxCollider)
            {
                // Last resort: collider local rect (still not permanently enlarged).
                BoxCollider2D box = tile.boxCollider;
                Vector2 c = box.offset;
                Vector2 s = box.size * 0.5f;
                minX = c.x - s.x;
                maxX = c.x + s.x;
                minY = c.y - s.y;
                maxY = c.y + s.y;
                worldCenter = box.bounds.center;
            }
            else
            {
                return false;
            }

            // Convert world-space edge tolerance into local units using lossy scale.
            Vector3 lossy = visual.lossyScale;
            float tolX = edgeTolWorld / Mathf.Max(0.0001f, Mathf.Abs(lossy.x));
            float tolY = edgeTolWorld / Mathf.Max(0.0001f, Mathf.Abs(lossy.y));

            bool inside =
                local.x >= (minX - tolX) && local.x <= (maxX + tolX) &&
                local.y >= (minY - tolY) && local.y <= (maxY + tolY);

            centerDistSqr = ((Vector2)worldCenter - worldXY).sqrMagnitude;
            return inside;
        }

        private void LogTouchHeader(int id, int fingerId, Vector2 screen, Vector3 world, Camera cam, float edgeTolWorld)
        {
            if (!MatchIQShellBridge.IsActive) return;
            Rect pr = cam.pixelRect;
            Debug.Log(
                $"[DIRECT_TOUCH] id={id} fingerId={fingerId} " +
                $"screen=({screen.x:F1},{screen.y:F1}) " +
                $"Screen={Screen.width}x{Screen.height} " +
                $"cam={cam.name} pixelRect=({pr.x:F0},{pr.y:F0},{pr.width:F0}x{pr.height:F0}) " +
                $"camPos={cam.transform.position} ortho={cam.orthographicSize:F3} " +
                $"world=({world.x:F3},{world.y:F3}) edgeTolWorld={edgeTolWorld:F3}");
        }

        private void LogTouchResult(
            int id, int fingerId, Vector2 screen, Vector3 world,
            MahjongTile tile, bool insideVisual, bool topmost, bool free, string result, int candidateCount)
        {
            if (!MatchIQShellBridge.IsActive) return;
            string bounds = "?";
            if (tile && tile.SRenderer)
            {
                Bounds b = tile.SRenderer.bounds;
                bounds = $"(({b.min.x:F2},{b.min.y:F2})-({b.max.x:F2},{b.max.y:F2}))";
            }
            Debug.Log(
                $"[DIRECT_TOUCH] id={id} fingerId={fingerId} " +
                $"screen=({screen.x:F0},{screen.y:F0}) world=({world.x:F2},{world.y:F2}) " +
                $"tile={tile.name} visualBounds={bounds} " +
                $"insideVisualBounds={(insideVisual ? "true" : "false")} " +
                $"topmost={(topmost ? "true" : "false")} free={(free ? "true" : "false")} " +
                $"result={result} candidateCount={candidateCount}");
        }

        private void LogFail(int id, string reason, int fingerId, Vector2 screen, Vector3? world, string extra)
        {
            if (!MatchIQShellBridge.IsActive) return;
            string w = world.HasValue ? $" world=({world.Value.x:F2},{world.Value.y:F2})" : "";
            string e = string.IsNullOrEmpty(extra) ? "" : " " + extra;
            Debug.Log(
                $"[DIRECT_TOUCH_FAIL] id={id} reason={reason} fingerId={fingerId} " +
                $"screen=({screen.x:F0},{screen.y:F0}){w}{e}");
        }
    }
}
