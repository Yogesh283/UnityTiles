using UnityEngine;
using Mkey.Shell;

namespace Mkey
{
    /// <summary>
    /// Authoritative Mahjong select/match logic.
    /// Shell delivery: BoardDirectTouchController → ApplyDirectTap / ApplyDirectRelease.
    /// Standalone (non-shell): TouchPad PointerDown / PointerUp (legacy).
    /// </summary>
    public class TileTouchBehavior : TouchPadMessageTarget
    {
        public MahjongTile mahjongTile;

        #region temp vars
        private TouchManager TouchM { get { return TouchManager.Instance; } }
        private Transform spriteTransform;
        private Vector3 spriteLocalPosition;
        private GameBoard MBoard { get { return GameBoard.Instance; } }
        private bool MayBeDeselect;
        private bool wasDragged;
        /// <summary>Last TouchPad press id that already ran selection on this tile (legacy path).</summary>
        private int lastHandledPressId = int.MinValue;
        #endregion temp vars

        private void Start()
        {
            PointerDownEvent = PointerDownEventHandler;
            PointerUpEvent = PointerUpEventHandler;
            DragEvent = DragEventHandler;
            mahjongTile = GetComponent<MahjongTile>();
            if (mahjongTile && mahjongTile.SRenderer)
            {
                spriteTransform = mahjongTile.SRenderer.transform;
                spriteLocalPosition = spriteTransform.localPosition;
            }
        }

        /// <summary>Shell direct-touch entry — one Began = one selection attempt.</summary>
        public void ApplyDirectTap()
        {
            RunSelectionAttempt(-1);
        }

        /// <summary>Shell direct-touch finger Ended — deselect-on-retap / drag release only.</summary>
        public void ApplyDirectRelease()
        {
            RunRelease();
        }

        #region touchbehavior
        private void PointerDownEventHandler(TouchPadEventArgs tpea)
        {
            // Shell: BoardDirectTouchController owns selection — ignore EventSystem path.
            if (BoardDirectTouchController.IsAuthoritative) return;

            if (MatchIQShellBridge.IsActive && GameBoard.GMode != GameMode.Play)
                GameBoard.GMode = GameMode.Play;

            if (GameBoard.GMode != GameMode.Play)
            {
#if UNITY_EDITOR
                GameConstructor gameConstructor = FindAnyObjectByType<GameConstructor>();
                Vector3 tPos = tpea.WorldPos;
                BoxCollider2D collider2D = mahjongTile.boxCollider;
                Bounds bounds = collider2D.bounds;
                int quadrant = GetQuadrant(bounds, tPos);
                if (quadrant == 1) gameConstructor.Cell_Click(mahjongTile.ParentCell);
                else if (quadrant == 2) gameConstructor.Cell_Click(mahjongTile.ParentCell.Neighbors.Main_2);
                else if (quadrant == 3) gameConstructor.Cell_Click(mahjongTile.ParentCell.Neighbors.Main_3);
                else if (quadrant == 4) gameConstructor.Cell_Click(mahjongTile.ParentCell.Neighbors.Main_4);
#endif
                return;
            }

            int pressId = TouchPad.CurrentPressId;
            if (lastHandledPressId == pressId)
            {
                TouchForensic.Fail(pressId, "PRESS_ALREADY_HANDLED",
                    $"tile={(mahjongTile ? mahjongTile.name : name)}");
                return;
            }
            lastHandledPressId = pressId;
            RunSelectionAttempt(pressId);
        }

        private void RunSelectionAttempt(int pressId)
        {
            if (MatchIQShellBridge.IsActive && GameBoard.GMode != GameMode.Play)
                GameBoard.GMode = GameMode.Play;

            if (GameBoard.GMode != GameMode.Play) return;

            MayBeDeselect = false;
            wasDragged = false;
            if (!TouchM) return;

            // Destroyed FirstObject must not block selection.
            TouchManager.PurgeDestroyedFirstObject();

            if (!mahjongTile) mahjongTile = GetComponent<MahjongTile>();
            if (!spriteTransform && mahjongTile && mahjongTile.SRenderer)
            {
                spriteTransform = mahjongTile.SRenderer.transform;
                spriteLocalPosition = spriteTransform.localPosition;
            }
            if (spriteTransform) spriteLocalPosition = spriteTransform.localPosition;

            string before = TouchM && TouchM.FirstObject ? TouchM.FirstObject.name : "null";
            if (pressId >= 0)
            {
                TouchForensic.Trace(pressId, "TILE_BEHAVIOR",
                    $"tile={(mahjongTile ? mahjongTile.name : "null")} beforeFirst={before}");
            }

            if (!mahjongTile || !mahjongTile.IsFreeToMatch())
            {
                TouchPipelineTrace.SelectionsRejected++;
                if (pressId >= 0)
                    TouchForensic.Fail(pressId, "NOT_FREE",
                        $"tile={(mahjongTile ? mahjongTile.name : "null")} before={before}");
                HighlightBothSelected(false);
                TouchM.SetFirstObject(null, null);
                TouchM.CanDrag = false;
                MBoard.FailedMatchEventRaise();
                return;
            }

            if (IsFirstObject())
            {
                MayBeDeselect = true;
                TouchM.CanDrag = !BoardDirectTouchController.IsAuthoritative;
                HighlightSelected(true);
                if (pressId >= 0)
                    TouchForensic.Trace(pressId, "SELECTION",
                        $"result=ALREADY_SELECTED tile={mahjongTile.name}");
                return;
            }

            if (TouchM.FirstObject && TouchM.FirstObject != spriteTransform)
            {
                if (CanMatchWith(TouchM.FirstObject))
                {
                    string otherName = TouchM.FirstObject.name;
                    HighlightSelected(true);
                    MatchWith(TouchM.FirstObject);
                    TouchM.CanDrag = false;
                    TouchPipelineTrace.SelectionsAccepted++;
                    TouchForensic.SelectionSuccess++;
                    if (pressId >= 0)
                        TouchForensic.Trace(pressId, "SELECTION",
                            $"result=MATCHED tile={mahjongTile.name} with={otherName}");
                    return;
                }

                TouchPipelineTrace.SelectionsRejected++;
                if (pressId >= 0)
                    TouchForensic.Fail(pressId, "SELECTION_STATE",
                        $"mismatch tile={mahjongTile.name} first={TouchM.FirstObject.name}");
                HighlightBothSelected(false);
                TouchM.SetFirstObject(null, null);
                TouchM.CanDrag = false;
                MBoard.FailedMatchEventRaise();
                return;
            }

            SetAsFirstObject();
            TouchPipelineTrace.SelectionsAccepted++;
            TouchForensic.SelectionSuccess++;
            if (pressId >= 0)
                TouchForensic.Trace(pressId, "SELECTION",
                    $"result=SUCCESS tile={mahjongTile.name} before={before} pressId={pressId}");
        }

        private void DragEventHandler(TouchPadEventArgs tpea)
        {
            if (BoardDirectTouchController.IsAuthoritative) return;
            wasDragged = TouchM.MinDragReached;
        }

        private void PointerUpEventHandler(TouchPadEventArgs tpea)
        {
            if (BoardDirectTouchController.IsAuthoritative) return;
            RunRelease();
        }

        private void RunRelease()
        {
            if (!TouchM || !TouchM.FirstObject) return;
            TouchM.PointerUpObject = null;

            if (IsFirstObject() && !wasDragged)
            {
                if (MayBeDeselect)
                {
                    TouchM.SetFirstObject(null, null);
                    HighlightSelected(false);
                }
                return;
            }

            if (IsFirstObject() && wasDragged)
            {
                TouchM.ResetDragEventRaise(null);
                return;
            }

            if (!wasDragged) return;

            if (CanMatchWith(TouchM.FirstObject))
            {
                TouchM.PointerUpObject = spriteTransform;
                TouchM.CanDrag = false;
                HighlightSelected(true);
                MatchWith(TouchM.FirstObject);
            }
            else
            {
                TouchM.CanDrag = false;
                TouchM.ResetDragEventRaise(null);
                if (!IsFirstObject())
                    MBoard.FailedMatchEventRaise();
            }
        }
        #endregion touchbehavior

        private bool CanMatchWith(Transform other)
        {
            if (other == spriteTransform || other == null) return false;
            if (!mahjongTile.IsFreeToMatch()) return false;
            MahjongTile otherTile = other.GetComponentInParent<MahjongTile>();
            if (!otherTile) return false;
            return mahjongTile.SpriteCanMatchhWith(otherTile.MSprite);
        }

        private void MatchWith(Transform draggable)
        {
            MahjongTile other = draggable.GetComponentInParent<MahjongTile>();
            TouchM.SetFirstObject(null, null);
            TouchM.CanDrag = false;
            MBoard.CollectMatch(GetComponent<MahjongTile>(), other);
        }

        private void HighlightSelected(bool highlight)
        {
            if (mahjongTile)
            {
                mahjongTile.SetToFront(highlight);
                mahjongTile.HighlightSelected(highlight);
            }
        }

        private void HighlightBothSelected(bool highlight)
        {
            HighlightSelected(highlight);
            if (TouchM.FirstObject)
            {
                MahjongTile mTile = TouchM.FirstObject.GetComponentInParent<MahjongTile>();
                if (mTile)
                {
                    mTile.HighlightSelected(highlight);
                    mTile.SetToFront(highlight);
                }
            }
        }

        private void SetInitialposition()
        {
            if (spriteTransform) spriteTransform.localPosition = spriteLocalPosition;
        }

        public bool IsFirstObject()
        {
            return TouchM.FirstObject && TouchM.FirstObject == spriteTransform;
        }

        public void SetAsFirstObject()
        {
            TouchM.SetFirstObject(spriteTransform, (cBack) =>
            {
                SetInitialposition();
                TouchM.CanDrag = false;
            });
            HighlightSelected(true);
            // Shell direct-touch is tap-only; disable TouchPad-driven drag follow.
            TouchM.CanDrag = !BoardDirectTouchController.IsAuthoritative;
        }

        #region constructor
        private int GetQuadrant(Bounds bounds, Vector3 touchPos)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector2 size = max - min;
            Vector2 sizeH = size * 0.5f;
            if (touchPos.x < min.x + sizeH.x && touchPos.y < min.y + sizeH.y) return 1;
            else if (touchPos.x < min.x + sizeH.x) return 2;
            else if (touchPos.y < min.y + sizeH.y) return 4;
            else return 3;
        }
        #endregion constructor
    }
}
