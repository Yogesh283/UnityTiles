using UnityEngine;



namespace Mkey

{

	public class TileTouchBehavior : TouchPadMessageTarget

	{

        public MahjongTile mahjongTile;



        #region temp vars

        private TouchManager TouchM { get { return TouchManager.Instance; } }

        private Transform spriteTransform;

        private Vector2 spritetransformPosition;

        private GameBoard MBoard { get { return GameBoard.Instance; } }

        private bool MayBeDeselect;

        #endregion temp vars



        private void Start()

        {

            if (GameBoard.GMode == GameMode.Play)

            {

                PointerDownEvent = PointerDownEventHandler;

                PointerUpEvent = PointerUpEventHandler;

                DragEvent = DragEventHandler;

                mahjongTile = GetComponent<MahjongTile>();

                spriteTransform = mahjongTile.SRenderer.transform;

                spritetransformPosition = spriteTransform.position;

            }

            else

            {

                PointerDownEvent = PointerDownEventHandler;

            }

        }



        #region touchbehavior

        private void PointerDownEventHandler(TouchPadEventArgs tpea)

        {

            if (GameBoard.GMode == GameMode.Play)

            {

                Debug.Log("pointer down: " + this);

                MayBeDeselect = false;

                wasDragged = false;



                if (mahjongTile.IsFreeToMatch())

                {

                    if (IsFirstObject())

                    {

                        MayBeDeselect = true;

                        TouchM.CanDrag = true;

                        return;

                    }

                    else if (TouchM.FirstObject && TouchM.FirstObject != spriteTransform)

                    {

                        if (CanMatchWith(TouchM.FirstObject))

                        {

                            HighlightSelected(true);

                            MatchWith(TouchM.FirstObject);

                            TouchM.CanDrag = false;

                            return;

                        }

                        else

                        {

                            HighlightBothSelected(false);

                            TouchM.SetFirstObject(null, null);

                            TouchM.CanDrag = false;

                            MBoard.FailedMatchEventRaise();

                            return;

                        }

                    }



                    SetAsFirstObject();

                }

                else

                {

                    HighlightBothSelected(false);

                    TouchM.SetFirstObject(null, null);

                    TouchM.CanDrag = false;

                    MBoard.FailedMatchEventRaise();

                }

            }

            else

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

            }

        }



        bool wasDragged = false;

        private void DragEventHandler(TouchPadEventArgs tpea)

        {

            wasDragged = TouchM.MinDragReached;

        }



        private void PointerUpEventHandler(TouchPadEventArgs tpea)

        {

            if (!TouchM.FirstObject) return;

            TouchM.PointerUpObject = null;

            if (IsFirstObject() && !wasDragged)

            {

                if (MayBeDeselect)

                {

                    TouchM.SetFirstObject(null, null);

                    HighlightSelected(false);

                    return;

                }

            }

            else if (IsFirstObject() && wasDragged)

            {

                TouchM.ResetDragEventRaise(null);

                return;

            }



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

                {

                    MBoard.FailedMatchEventRaise();

                }

            }

        }

        #endregion touchbehavior



        private bool CanMatchWith(Transform other)

        {

            if (other == spriteTransform || other == null) return false;

            if (!mahjongTile.IsFreeToMatch()) return false;

            MahjongTile otherTile = other.GetComponentInParent<MahjongTile>();

            if (!otherTile) return false;

            return (mahjongTile.SpriteCanMatchhWith(otherTile.MSprite));

        }



        private void MatchWith(Transform draggable)

        {

            MahjongTile other = draggable.GetComponentInParent<MahjongTile>();

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

            if (spriteTransform) spriteTransform.position = spritetransformPosition;

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

            TouchM.CanDrag = true;

        }



        #region constructor

        // 2 3

        // 1 4

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


