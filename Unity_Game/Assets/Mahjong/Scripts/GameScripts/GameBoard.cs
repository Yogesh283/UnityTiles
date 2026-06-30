using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mkey
{
    public class GameBoard : MonoBehaviour
    {
        #region settings 
        [Space(8)]
        [Header("Game settings")]
        public bool showScore;
        #endregion settings

        [Header("Collect properties")]
        public float speed = 8;
        public EaseAnim ease_0;     // outsine
        public EaseAnim ease_1;      // outbounce
        public AudioClip collectSound;

        #region references
        [Header("Main references")]
        [Space(8)]
        public Transform GridContainer;
        public SpriteRenderer backGround;
        public GameConstructor gConstructor;
        [SerializeField]
        private ScoreController scoreController;
        [SerializeField]
        private PopUpsController winPrefab;
        [SerializeField]
        private PopUpsController noMatchesPrefab;
        [SerializeField]
        private GameObject collectPrefab;
        [SerializeField]
        private GUIFlyer scoreFlyerPrefab;
        #endregion references

        #region grid
        public MatchGrid MainGrid { get; private set; }
        #endregion grid

        #region states
        public static GameMode GMode = GameMode.Play; // Play or Edit
        #endregion states

        #region properties
        public Sprite BackGround
        {
            get { return backGround.sprite; }
            set { if (backGround) backGround.sprite = value; }
        }

        private SoundMaster MSound { get { return SoundMaster.Instance; } }

        public GuiController MGui => GuiController.Instance;
        #endregion properties

        #region sets
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        private GameLevelHolder MGLevel => GameLevelHolder.Instance;
        #endregion sets

        #region events
        public Action WinAction;
        public Action NoMatchesAction;
        public Action<int> ChangePossibleMatchesAction;
        public Action<GridCell, GridCell, MahjongTile, MahjongTile> BeforeCollectAction;
        public Action<Sprite, Sprite> CollectAction;
        public Action EndCollectAnimatioAction;
        public Action FailedMatchAction;
        public Action ShuffleGridEndAction;
        public Action ShuffleGridBeginAction;
        public Action UndoEndAction;
        public Action <bool> ChangeFreeHiglightModeAction;
        #endregion events

        public static GameBoard Instance { get; private set; }

        #region regular
        private void Awake()
        {
            if (Instance) Destroy(gameObject);
            else
            {
                Instance = this;
            }
#if UNITY_EDITOR
            if (GCSet && GCSet.testMode) GameLevelHolder.CurrentLevel = Mathf.Abs(GCSet.testLevel);
#endif
            if (Tournament.TournamentSession.IsActive && Tournament.TournamentSession.MatchLevelIndex >= 0)
                GameLevelHolder.CurrentLevel = Tournament.TournamentSession.MatchLevelIndex;
            ScoreHolder.Instance.SetCount(0);
        }

        private void Start()
        {
            #region game sets 
            if (!GCSet)
            {
                Debug.Log("Game construct set not found!!!");
                return;
            }

            if (!LCSet)
            {
                Debug.Log("Level construct set not found!!! - " + GameLevelHolder.CurrentLevel);
                return;
            }

            if (!GOSet)
            {
                Debug.Log("MatcSet not found!!! - " + GameLevelHolder.CurrentLevel);
                return;
            }
            #endregion game sets 

            DestroyGrid();
            CreateGameBoard();
            GameLevelHolder.StartLevel();

            if (GMode == GameMode.Edit)
            {
#if UNITY_EDITOR
                Debug.Log("start edit mode");
                if (gConstructor)
                {
                    gConstructor.gameObject.SetActive(true);
                    gConstructor.InitStart();
                }
#endif
            }

            else if (GMode == GameMode.Play)
            {
                Debug.Log("start play mode");

                if (gConstructor) DestroyImmediate(gConstructor.gameObject);

                ScoreHolder.Instance.SetAverageScore(scoreController.GetMaxLevelScore(MainGrid.GetTiles().Length / 2));
                Debug.Log("max level score: " + ScoreHolder.AverageScore);

                #region set board eventhandlers
                UndoEndAction += () =>
                {
                    MainGrid.CacheBlockers();
                    UpdatePossibleMatches();
                    if (!CheckExistingHint()) RemoveHint();
                    if (IsHihglightFreeMode)
                    {
                        HighlihtFree(true);
                    }
                };

                ShuffleGridEndAction += () =>
                {
                    UpdatePossibleMatches();
                };

                BeforeCollectAction += (c1, c2, m1, m2) =>
                {
                    if (hintPair != null && hintPair.ContaiAny(m1, m2)) RemoveHint(); // remove hint
                };

                CollectAction += (s1, s2) =>
                {
                    ScoreHolder.Add(scoreController.GetMatchScore());
                    if (MainGrid.GetTiles().Length == 0) { WinAction?.Invoke(); return; }   // check win 
                    UpdatePossibleMatches();
                    if (possibleMatches.Count == 0) NoMatchesAction?.Invoke();
                    GameEvents.MatchSpritesEvent?.Invoke(s1, s2);
                    if (IsHihglightFreeMode)
                    {
                        HighlihtFree(true);
                    }
                };

                NoMatchesAction += () =>
                {
                    if (Tournament.TournamentSession.IsActive)
                    {
                        // Tournament scene may not have GuiController — auto-shuffle instead of blocking popup.
                        ShuffleGrid(() => { });
                        return;
                    }

                    if (MGui)
                        MGui.ShowPopUp(noMatchesPrefab);
                };

                WinAction += () =>
                {
                    if (Tournament.TournamentSession.IsActive)
                    {
                        if (Tournament.TournamentMatchManager.IsMatchResolved ||
                            Tournament.TournamentMatchManager.IsMatchLocked)
                            return;

                        Tournament.TournamentGameBridge.HandleLevelComplete();
                        return;
                    }

                    MGui.ShowPopUp(winPrefab);  // show win message
                    MGLevel.PassLevel();        // pass level
                    GameEvents.WinLevelAction?.Invoke();
                };

                ChangeFreeHiglightModeAction += (highlight) => 
                {
                    HighlihtFree(highlight);
                };
                #endregion set board eventhandlers
                MainGrid.CalcObjects();

                UpdatePossibleMatches();

                LoadHighlightMode();
            }
        }
        #endregion regular

        #region grid construct restart
        public void CreateGameBoard()
        {
            Debug.Log("Create gameboard ");
            Debug.Log("level set: " + LCSet.name);
            Debug.Log("current level: " + GameLevelHolder.CurrentLevel);

            BackGround = GOSet.GetBackGround(LCSet.BackGround);

            if (GMode == GameMode.Play)
            {
                Func<LevelConstructSet, Transform, MatchGrid> create = (lC, container) =>
                {
                    MatchGrid g = new MatchGrid(lC, GOSet, container, GMode);
                    g.Cells.ForEach((c) =>
                    {
#if UNITY_EDITOR
                        c.name = c.ToString();  
#endif
                    });
                    g.SetMahjongSprites();
                    return g;
                };

                MainGrid = create(LCSet, GridContainer);
            }
            else // edit mode
            {
#if UNITY_EDITOR

                if (MainGrid != null && MainGrid.LcSet == LCSet)
                {
                    MainGrid.Rebuild(GOSet, GMode);
                }
                else
                {
                    DestroyGrid();
                    MainGrid = new MatchGrid(LCSet, GOSet, GridContainer, GMode);
                }

                // set cells delegates for constructor
                for (int i = 0; i < MainGrid.Cells.Count; i++)
                {
                    MainGrid.Cells[i].GetComponent<Collider2D>().enabled = true;
                    MainGrid.Cells[i].GCPointerDownEvent = (c) =>
                     {
                         gConstructor.GetComponent<GameConstructor>().Cell_Click(c);
                     };
                }
#endif
            }

            MainGrid.SetTofrontAll(false);
        }

        public void RestartLevel()
        {
            MainGrid.Rebuild(GOSet, GMode);
            MainGrid.SetMahjongSprites();
            MainGrid.SetTofrontAll(false);
            hintPair = null;
            ScoreHolder.Instance.SetCount(0);
            UpdatePossibleMatches();
            if (IsHihglightFreeMode)
            {
                HighlihtFree(true);
            }
            GameEvents.RestartAction?.Invoke();
        }

        /// <summary>
        /// destroy default main grid cells
        /// </summary>
        private void DestroyGrid()
        {
            GridCell[] gcs = gameObject.GetComponentsInChildren<GridCell>();
            for (int i = 0; i < gcs.Length; i++)
            {
                DestroyImmediate(gcs[i].gameObject);
            }
        }
        #endregion grid construct restart

        #region states
        public void ShuffleGrid(Action completeCallBack)
        {
            if (!MainGrid.CanShuffle())
            {
                ShuffleGridBeginAction?.Invoke();
                MainGrid.HardShuffle();
                MainGrid.SetTofrontAll(false);
                hintPair = null;
                possibleMatches = null;
                if (IsHihglightFreeMode)
                {
                    HighlihtFree(true);
                }
                ShuffleGridEndAction?.Invoke();
                completeCallBack?.Invoke();
                return;
            }
            // standart shuffle action
            SetControlActivity(false, false);
            ShuffleGridBeginAction?.Invoke();
            ParallelTween pT0 = new ();
            ParallelTween pT1 = new ();
            hintPair = null;
            possibleMatches = null;

            TweenSeq tweenSeq = new();
            List<MahjongTile> mahjongTiles = GetComponentsInChildren<MahjongTile>(true).ToList();

            mahjongTiles.ForEach((mT) => { pT0.Add((callBack) => { mT.MixJump(transform.position, callBack); }); });

            mahjongTiles.ForEach((mT) => { pT1.Add((callBack) => { mT.ReversMixJump(callBack); }); });

            tweenSeq.Add((callBack) =>
            {
                pT0.Start(callBack);
            });

            tweenSeq.Add((callBack) =>
            {
                MainGrid.ShuffleGridSprites();
                pT1.Start(() =>
                {
                    SetControlActivity(true, true);
                    ShuffleGridEndAction?.Invoke();
                    completeCallBack?.Invoke();
                    callBack();
                });
            });

            tweenSeq.Start();
        }

        internal void SetControlActivity(bool activityGrid, bool activityMenu)
        {
            TouchManager.SetTouchActivity(activityGrid);
            HeaderGUIController.Instance.SetControlActivity(activityMenu);
            FooterGUIController.Instance.SetControlActivity(activityMenu);
        }
        #endregion states

        #region collect match
        PossibleMatches possibleMatches;
        private int pairNumber = 0;
        private Canvas parentCanvas;
        public int GetPossibleMatchesCount()
        {
            return (possibleMatches != null) ? possibleMatches.Count : 0;
        }

        public void CollectMatch(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            StartCoroutine(CollectMatchC(mahjongTile_1, mahjongTile_2));
        }

        private IEnumerator CollectMatchC(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            SetControlActivity(false, false);
            try
            {
                GridCell gridCell_1 = mahjongTile_1.GetComponentInParent<GridCell>();
                GridCell gridCell_2 = mahjongTile_2.GetComponentInParent<GridCell>();

                BeforeCollectAction?.Invoke(gridCell_1, gridCell_2, mahjongTile_1, mahjongTile_2);
                Sprite sprite_1 = mahjongTile_1.MSprite;
                Sprite sprite_2 = mahjongTile_2.MSprite;

                gridCell_1.UnLinkObject(mahjongTile_1.Layer);
                gridCell_2.UnLinkObject(mahjongTile_2.Layer);

                yield return CollectAnimationC(mahjongTile_1, mahjongTile_2);
                EndCollectAnimatioAction?.Invoke();
                Destroy(mahjongTile_1.gameObject);
                Destroy(mahjongTile_2.gameObject);
                yield return new WaitForEndOfFrame();
                CollectAction?.Invoke(sprite_1, sprite_2);
            }
            finally
            {
                if (!this || !isActiveAndEnabled)
                {
                    // Coroutine ended — leave controls as-is.
                }
                else if (Tournament.TournamentSession.IsActive &&
                         (Tournament.TournamentMatchManager.IsMatchResolved ||
                          Tournament.TournamentMatchManager.IsMatchLocked))
                {
                    // Match ended — keep board locked until result dialog.
                }
                else
                {
                    SetControlActivity(true, true);
                }
            }
        }

        private IEnumerator CollectAnimationC(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            MahjongTile leftTile = (mahjongTile_1.spriteTransform.position.x < mahjongTile_2.spriteTransform.position.x) ? mahjongTile_1 : mahjongTile_2;
            MahjongTile rightTile = (leftTile == mahjongTile_1) ? mahjongTile_2 : mahjongTile_1;

            // get box collider bounds
            Bounds bounds_1 = leftTile.boxCollider.bounds;
            Vector3 min = bounds_1.min;
            Vector3 max = bounds_1.max;
            Vector2 size = max - min;
            Vector2 size05 = size * 0.5f;
            Vector2 size15 = size * 1.5f;
            Vector2 size01 = size * 0.1f;

            // calc anim points
            Vector3 wPos_10 = leftTile.spriteTransform.position;
            Vector3 wPos_11 = rightTile.spriteTransform.position;
            Vector3 wPos_center = (wPos_10 + wPos_11) * 0.5f;

            if(Mathf.Abs(wPos_center.x) > 1.5f * size.x)
            {
                wPos_center = new Vector3(wPos_center.x > 0 ? 1.5f : -1.5f, wPos_center.y, wPos_center.z); // offset to center
            }

            Vector3 wPos_20 = wPos_center - new Vector3(size.x, 0, 0);
            Vector3 wPos_21 = wPos_center + new Vector3(size.x, 0, 0);

            Vector3 wPos_30 = wPos_20 - new Vector3(size15.x, 0, 0);
            Vector3 wPos_31 = wPos_21 + new Vector3(size15.x, 0, 0);

            Vector3 wPos_40 = wPos_center - new Vector3(size05.x + size01.x, 0, 0);
            Vector3 wPos_41 = wPos_center + new Vector3(size05.x + size01.x, 0, 0);
            
            // play animtion movements

            // 1) move to points 20, 21 
            bool moveComplete = false;
            //SimpleTween.Move(leftTile.spriteTransform.gameObject, wPos_10, wPos_20, 0.25f);
            //SimpleTween.Move(rightTile.spriteTransform.gameObject, wPos_11, wPos_21, 0.25f).AddCompleteCallBack(()=> { moveComplete = true; });
            //yield return new WaitWhile(() => !moveComplete);

            // 2) move to points 30, 31 
            float time = (wPos_30 - wPos_10).magnitude / speed;
            if (time < 0.2f) time = 0.2f;
            if (time > 0.4f) time = 0.4f;
            moveComplete = false;
            SimpleTween.Move(leftTile.spriteTransform.gameObject, wPos_10, wPos_30, time).SetEase(ease_0);
            SimpleTween.Move(rightTile.spriteTransform.gameObject, wPos_11, wPos_31, time).SetEase(ease_0).AddCompleteCallBack(() => { moveComplete = true; });
            yield return new WaitWhile(() => !moveComplete);

            // 3) move to points 40, 41 
            time = (wPos_40 - wPos_30).magnitude / speed;
            moveComplete = false;
            SimpleTween.Move(leftTile.spriteTransform.gameObject, wPos_30, wPos_40, time).SetEase(ease_1);
            SimpleTween.Move(rightTile.spriteTransform.gameObject, wPos_31, wPos_41, time).SetEase(ease_1).AddCompleteCallBack(() => { moveComplete = true; });
            TweenExt.DelayAction(rightTile.spriteTransform.gameObject, time * 0.9f, ()=>
            {
                if (collectPrefab) Instantiate(collectPrefab, wPos_center, Quaternion.identity, transform);
                if (showScore) InstantiateScoreFlyer(wPos_center + new Vector3(0, size.y, 0), "+" + scoreController.GetMatchScore().ToString());
            });
            if (collectSound) MSound.PlayClip(time * 0.6f, collectSound);
            yield return new WaitWhile(() => !moveComplete);

            yield return new WaitForSeconds(0.1f);
        }

        private void UpdatePossibleMatches()
        {
            possibleMatches = new PossibleMatches(MainGrid.GetFreeToMatchTiles());
            ChangePossibleMatchesAction?.Invoke(possibleMatches.Count);
            pairNumber = 0;
        }

        private void InstantiateScoreFlyer(Vector3 wPosition, string score)
        {
            if (!scoreFlyerPrefab) return;
            if (!parentCanvas)
            {
                GameObject gC = GameObject.Find("CanvasMain");
                if (gC) parentCanvas = gC.GetComponent<Canvas>();
                if (!parentCanvas) parentCanvas = FindFirstObjectByType<Canvas>();
            }

            GUIFlyer flyer = scoreFlyerPrefab.CreateFlyer(parentCanvas, score);
            if (flyer)
            {
                flyer.transform.localScale = transform.lossyScale;
                flyer.transform.position = wPosition; //  transform.position;
            }
        }

        public void FastCollect(MahjongTile mahjongTile_1, MahjongTile mahjongTile_2)
        {
            SetControlActivity(false, false);
            GridCell gridCell_1 = mahjongTile_1.GetComponentInParent<GridCell>();
            GridCell gridCell_2 = mahjongTile_2.GetComponentInParent<GridCell>();

            BeforeCollectAction?.Invoke(gridCell_1, gridCell_2, mahjongTile_1, mahjongTile_2);
            Sprite sprite_1 = mahjongTile_1.MSprite;
            Sprite sprite_2 = mahjongTile_2.MSprite;

            mahjongTile_1.transform.parent = null;
            mahjongTile_2.transform.parent = null;

            Destroy(mahjongTile_1.gameObject);
            Destroy(mahjongTile_2.gameObject);
            CollectAction?.Invoke(sprite_1, sprite_2);
            SetControlActivity(true, true);
        }
        #endregion collect match

        #region hint
        MatchPair hintPair;
        public void TrySelectHintMatch(Action<bool> selectCallBack)
        {
            if (possibleMatches == null) 
            {
                UpdatePossibleMatches();
            }
            RemoveHint();

            if (possibleMatches.Count > pairNumber)
            {
                hintPair = possibleMatches.GetMatchPair(pairNumber);
                hintPair.mahjongTile_1.HighlightHint(true);
                hintPair.mahjongTile_2.HighlightHint(true);
                // paarNumber++;
                selectCallBack?.Invoke(true);
            }
            else
            {
                pairNumber = 0;
                selectCallBack?.Invoke(false);
            }
        }

        public bool IsAlreadyHint()
        {
            return hintPair !=null && hintPair.mahjongTile_1 && hintPair.mahjongTile_2;
        }

        public void RemoveHint()
        {
            if (hintPair != null)
            {
                if (hintPair.mahjongTile_1) hintPair.mahjongTile_1.HighlightHint(false);
                if (hintPair.mahjongTile_2) hintPair.mahjongTile_2.HighlightHint(false);
            }
            hintPair = null;
        }

        public bool CheckExistingHint()
        {
            if (hintPair == null || !hintPair.mahjongTile_1 || !hintPair.mahjongTile_2) return false;
            if (possibleMatches.ContainMatchPair(hintPair)) return true;
            return false;
        }
        #endregion hint

        #region undo
        public void RaiseUndoEvents()
        {
            UndoEndAction?.Invoke();
        }
        #endregion undo

        #region free highlight
        public bool IsHihglightFreeMode { get; set; }

        public void SetHiglightFreeMode(bool highlight)
        {
            if (IsHihglightFreeMode == highlight) return;
            PlayerPrefsExtension.SetBool("free_highlight", highlight);
            IsHihglightFreeMode = highlight;
            ChangeFreeHiglightModeAction?.Invoke(highlight);
        }

        private void LoadHighlightMode()
        {
            bool isFreeHihglighted = PlayerPrefsExtension.GetBool("free_highlight", false);
            SetHiglightFreeMode(isFreeHihglighted);
        }

        private void HighlihtFree(bool highlight)
        {
            List<MahjongTile> freeTiles = MainGrid.GetFreeToMatchTiles();
            List<MahjongTile> allTiles = MainGrid.GetTiles().ToList();

            if (highlight)
            {
                foreach (var item in allTiles)
                {
                   item.SetFreeHiglightColor(freeTiles.Contains(item) ? true : false);
                }
            }
            else
            {
                foreach (var item in allTiles)
                {
                    item.SetFreeHiglightColor(true);
                }
            }
        }
        #endregion free highlight

        /// <summary>
        /// starts when the game is interrupted
        /// </summary>
        public void BreakLevelEventRaise()
        {
            GameEvents.BreakLevelAction?.Invoke();
        }

        public void FailedMatchEventRaise() 
        {
            FailedMatchAction?.Invoke();
        }
    }
}
