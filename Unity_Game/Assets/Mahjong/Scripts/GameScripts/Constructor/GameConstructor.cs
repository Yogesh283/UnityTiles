using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mkey
{
    public class GameConstructor : MonoBehaviour
    {
#if UNITY_EDITOR
        [SerializeField]
        private Text editModeText;
        [SerializeField]
        private Text infoText;

        [SerializeField]
        private RectTransform moveButtons;

        [SerializeField]
        private GridObject currentBrush;

        [SerializeField]
        private Transform layerButtonsParent;
        [SerializeField]
        private Button layerButtonPrefab;

        #region grid construct
        [Space(8, order = 0)]
        [Header("Grid", order = 1)]
        [SerializeField]
        private PanelContainerController GridPanelContainer;
        [SerializeField]
        private IncDecInputPanel IncDecGridPrefab;
        #endregion grid construct

        #region game construct
        [Space(8, order = 0)]
        [Header("Game construct", order = 0)]
        [SerializeField]
        private Button levelButtonPrefab;
        [SerializeField]
        private GameObject constructPanel;
        [SerializeField]
        private Button openConstructButton;
        [SerializeField]
        private RectTransform LevelButtonsParent;
        [SerializeField]
        private InputField LevelInputField;
        #endregion game construct

        #region temp vars
        private GameBoard MBoard { get { return GameBoard.Instance; } }
        private MatchGrid MGrid { get { return MBoard.MainGrid; } }
        private GameConstructSet GCSet { get { return GameConstructSet.Instance; } }
        private GameObjectsSet GOSet { get { return GCSet.GOSet; } }
        private LevelConstructSet LCSet { get { return GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel); } }
        #endregion temp vars

        #region properties
        public int CurrentLayer { get; private set; }
        #endregion properties

        #region default data
        private readonly string levelConstructSetSubFolder = "LevelConstructSets";  //resource folders
        private readonly string pathToSets = "Assets/Mahjong/Resources/";
        private readonly int minVertSize = 6;
        private readonly int maxVertSize = 20;
        private readonly int minHorSize = 6;
        private readonly int maxHorSize = 20;
        #endregion default data

        #region events
        public Action<int> ChangeCurrentLayerAction { get; set; }
        public Action<GridObject, int> AddTileAction { get; set; }
        public Action<int> RemoveTileAction { get; set; }
        public Action ReloadBoardAction { get; set; }

        private Action MoveGridAction { get; set; }
        private Action ChangeGridSizeAction { get; set; }
        private Action <int> ChangeLevelAction { get; set; }
        #endregion events

        public void InitStart()
        {
            if (GameBoard.GMode == GameMode.Edit)
            {
                if (!MBoard) return;
                if (!GCSet)
                {
                    Debug.Log("Game construct set not found!!!");
                    return;
                }
                if (!GOSet)
                {
                    Debug.Log("GameObjectSet not found!!! - ");
                    return;
                }

                currentBrush = GOSet.mahjongTilePrefab;
                if (GameLevelHolder.CurrentLevel > GCSet.levelSets.Count - 1) GameLevelHolder.CurrentLevel = GCSet.levelSets.Count - 1;

                if (editModeText) editModeText.text = "EDIT MODE" + '\n' + "Level " + (GameLevelHolder.CurrentLevel + 1) + '\n' + LCSet.name;

                Action changeGridObjectsAction = () =>
                {
                    LCSet.SaveObjects(MGrid.Cells);

                    // clamp current layer
                    int maxLayer = MGrid.GetMaxLayer() + 1;
                    if (maxLayer > GameConstructSet.MaxLayersCount - 1) maxLayer = GameConstructSet.MaxLayersCount - 1;
                    SetCurrentLayer(CurrentLayer > maxLayer ? maxLayer : CurrentLayer);
                    LayerButtonsRefresh();
                    RefreshLayersInfo();
                };

                #region add evenhandlers
                MoveGridAction += () =>
                {
                    RebuildGameBoard();
                };

                ChangeGridSizeAction += () => 
                {
                    RebuildGameBoard();
                };

                ChangeLevelAction += (level) => 
                {
                    CloseOpenedPanels();
                    RebuildGameBoard();
                    LevelButtonsRefresh();
                    UpdateLevelInputField();
                    SetCurrentLayer(MGrid.GetMaxLayer());
                    RefreshLayersInfo();
                    UpdateModeText();
                };

                ReloadBoardAction += changeGridObjectsAction;

                ChangeCurrentLayerAction += (layer) => 
                {
                    LayerButtonsRefresh();
                };

                RemoveTileAction += (layer) => {
                    changeGridObjectsAction?.Invoke(); 
                };

                AddTileAction += (gridObject, layer) => { 
                    changeGridObjectsAction?.Invoke(); 
                };
                #endregion add evenhandlers

                if (moveButtons) moveButtons.gameObject.SetActive(true);
                if (infoText) infoText.gameObject.SetActive(true);

                UpdateModeText();
                CreateLevelButtons();
                CreateLayerButtons();
                SetCurrentLayer(MGrid.GetMaxLayer());
                ShowConstructMenu(true);
                RefreshLayersInfo();
            }
        }

        #region show board
        private void RebuildGameBoard()
        {
            GCSet.Clean();
            LCSet.Clean();
            MBoard.CreateGameBoard();
            ReloadBoardAction?.Invoke();
        }

        private void UpdateModeText()
        {
            if (editModeText) editModeText.text = "EDIT MODE (" + (GameLevelHolder.CurrentLevel + 1) + "/" + GCSet.levelSets.Count + ")" + '\n' + "Level " + (GameLevelHolder.CurrentLevel + 1) + '\n' + LCSet.name;
        }
        #endregion show board

        #region layer
        private void CreateLayerButtons()
        {
            int layersCount = GameConstructSet.MaxLayersCount;

            Transform parent = layerButtonsParent;
            DestroyGOInChildrenWithComponent<Button>(parent);
            for (int i = layersCount - 1; i >= 0; i--)
            {
                int layer = i + 1;
                Button button = CreateButton(layerButtonPrefab, parent, "" + layer.ToString(), () =>
                {
                    SetCurrentLayer(layer - 1);
                    CloseOpenedPanels();

                });
                DataComponent description = button.GetOrAddComponent<DataComponent>();
                description.intValue = i;
            }
        }

        private void LayerButtonsRefresh()
        {
            Action<Button, bool> selectButton = (b, select) =>
            {
                b.GetComponent<Image>().color = (select) ? new Color(0.5f, 0.5f, 0.5f, 1) : new Color(1, 1, 1, 1);
            };

            Button[] levelButtons = layerButtonsParent.gameObject.GetComponentsInChildren<Button>();
            int maxLayer = MGrid.GetMaxLayer();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                DataComponent description = levelButtons[i].GetOrAddComponent<DataComponent>();
                selectButton(levelButtons[i], (description.intValue == currentBrush.Layer));
                levelButtons[i].interactable = (description.intValue <= maxLayer + 1);
            }
        }

        private void ShowLayersRange(int fromLayer, int toLayer, bool show)
        {
            for (int i = fromLayer; i <= toLayer; i++)
            {
                List<MahjongTile> mahjongTiles = MGrid.GetLayerObjects(i);
                foreach (var item in mahjongTiles)
                {
                    item.gameObject.SetActive(show);
                }
            }
        }

        private void SetCurrentLayer(int newLayer)
        {
            int prevLayer = currentBrush.Layer;
            currentBrush.SetLayer(newLayer);
            CurrentLayer = newLayer;

            if (newLayer > prevLayer)
            {
                ShowLayersRange(prevLayer + 1, newLayer, true);
            }
            else if (newLayer < prevLayer)
            {
                ShowLayersRange(newLayer + 1, GameConstructSet.MaxLayersCount - 1, false);
            }
            ChangeCurrentLayerAction?.Invoke(currentBrush.Layer);
        }

        private void RefreshLayersInfo()
        {
            string result = "";
            int sum = 0;
            for (int i = 1; i <= GameConstructSet.MaxLayersCount; i++)
            {
                List<MahjongTile> tiles = MGrid.GetLayerObjects(i - 1);
                result += (tiles.Count > 0) ? "Layer #" + i + ": " + tiles.Count + "; " : "";
                sum += tiles.Count;
            }
            result += '\n' + "; SUM:  " + sum + " tiles";
            if (infoText) infoText.text = result;
            infoText.color = (sum % 2 == 0) ? new Color(1, 1, 1) : new Color(1, 0, 0);
        }
        #endregion layer

        #region construct menus
        bool openedConstr = false;

        public void OpenConstructPanel()
        {
            SetConstructControlActivity(false);
            constructPanel.SetActive(true);

            RectTransform rt = constructPanel.GetComponent<RectTransform>();//Debug.Log(rt.offsetMin + " : " + rt.offsetMax);
            float startX = (!openedConstr) ? 0 : 1f;
            float endX = (!openedConstr) ? 1f : 0;

            SimpleTween.Value(constructPanel, startX, endX, 0.2f).SetEase(EaseAnim.EaseInCubic).
                               SetOnUpdate((float val) =>
                               {
                                   rt.transform.localScale = new Vector3(val, 1, 1);
                                   // rt.offsetMax = new Vector2(val, rt.offsetMax.y);
                               }).AddCompleteCallBack(() =>
                               {
                                   SetConstructControlActivity(true);
                                   openedConstr = !openedConstr;
                                   // LevelButtonsRefresh();
                               });


        }

        private void SetConstructControlActivity(bool activity)
        {
            Button[] buttons = constructPanel.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = activity;
            }
        }

        private void ShowConstructMenu(bool show)
        {
            constructPanel.SetActive(show);
            openConstructButton.gameObject.SetActive(show);
        }

        int scrollPosition = 0;
        public void CreateLevelButtons()
        {
            GCSet.Clean();

            int count = 5;
            int currLevel = GameLevelHolder.CurrentLevel;
            int minLevel = Mathf.Max(currLevel - count / 2, 0);
            RebuilLeveldButtonsPanel(minLevel, count);
            UpdateLevelInputField();
        }

        public void RemoveLevel()
        {
            Debug.Log("Click on Button <Remove level...> ");
            if (GCSet.LevelCount < 2)
            {
                Debug.Log("Can't remove the last level> ");
                return;
            }
            GCSet.RemoveLevel(GameLevelHolder.CurrentLevel);
            CreateLevelButtons();
            GameLevelHolder.CurrentLevel = (GameLevelHolder.CurrentLevel <= GCSet.LevelCount - 1) ? GameLevelHolder.CurrentLevel : GameLevelHolder.CurrentLevel - 1;
            ChangeLevelAction?.Invoke(GameLevelHolder.CurrentLevel);
        }

        public void InsertBefore()
        {
            Debug.Log("Click on Button <Insert level before...> ");
            LevelConstructSet lcs = ScriptableObjectUtility.CreateResourceAsset<LevelConstructSet>(pathToSets, levelConstructSetSubFolder, "", " " + 1.ToString());
            GCSet.InsertBeforeLevel(GameLevelHolder.CurrentLevel, lcs);
            CreateLevelButtons();
            ChangeLevelAction?.Invoke(GameLevelHolder.CurrentLevel);
        }

        public void InsertAfter()
        {
            Debug.Log("Click on Button <Insert level after...> ");
            LevelConstructSet lcs = ScriptableObjectUtility.CreateResourceAsset<LevelConstructSet>(pathToSets, levelConstructSetSubFolder, "", " " + 1.ToString());
            GCSet.InsertAfterLevel(GameLevelHolder.CurrentLevel, lcs);
            CreateLevelButtons();
            GameLevelHolder.CurrentLevel += 1;
            ChangeLevelAction?.Invoke(GameLevelHolder.CurrentLevel);
        }

        private void LevelButtonsRefresh()
        {
            Action<Button, bool> selectButton = (b, select) =>
            {
                b.GetComponent<Image>().color = (select) ? new Color(0.5f, 0.5f, 0.5f, 1) : new Color(1, 1, 1, 1);
            };

            Button[] levelButtons = LevelButtonsParent.gameObject.GetComponentsInChildren<Button>();
            for (int i = 0; i < levelButtons.Length; i++)
            {
                DataComponent description = levelButtons[i].GetOrAddComponent<DataComponent>();
                selectButton(levelButtons[i], (description.intValue == GameLevelHolder.CurrentLevel));
            }
        }

        public void ScrollLevelButtons(bool left)
        {
            int count = 5;
            if (!left)
            {
                if (scrollPosition + count < GCSet.levelSets.Count) scrollPosition++;
                else return;
            }
            else
            {
                if (scrollPosition > 0) scrollPosition--;
                else return;
            }

            int minLevel = Mathf.Max(scrollPosition, 0);
            RebuilLeveldButtonsPanel(minLevel, count);
        }

        /// <summary>
        /// parse input field
        /// </summary>
        /// <param name="val"></param>
        public void GotoLevel(string val)
        {
            int res;
            bool good = int.TryParse(val, out res);
            if (good)
            {
                res--;
                res = Mathf.Clamp(res, 0, GCSet.levelSets.Count - 1);
                if (res == GameLevelHolder.CurrentLevel) return;

                GameLevelHolder.CurrentLevel = Mathf.Clamp(res, 0, GCSet.levelSets.Count - 1);
                int count = 5;
                int currLevel = GameLevelHolder.CurrentLevel;
                int minLevel = Mathf.Max(currLevel - count / 2, 0);
                RebuilLeveldButtonsPanel(minLevel, count);
                ChangeLevelAction?.Invoke(GameLevelHolder.CurrentLevel);
            }
        }

        private void RebuilLeveldButtonsPanel(int minLevel, int count)
        {
            Transform parent = LevelButtonsParent.transform;
            DestroyGOInChildrenWithComponent<Button>(parent);
            int maxLevel = minLevel;

            for (int i = minLevel; i < minLevel + count; i++)
            {
                if (i < GCSet.levelSets.Count) maxLevel = i;
            }

            for (int i = maxLevel; i > maxLevel - count; i--)
            {
                if (i >= 0) minLevel = i;
            }
            scrollPosition = minLevel;

            for (int i = minLevel; i <= maxLevel; i++)
            {
                int level = i + 1;
                Button button = CreateButton(levelButtonPrefab, parent, "" + level.ToString(), () =>
                {
                    GameLevelHolder.CurrentLevel = level - 1;
                    ChangeLevelAction?.Invoke(GameLevelHolder.CurrentLevel);
                });
                DataComponent description = button.GetOrAddComponent<DataComponent>();
                description.intValue = i;
            }
            LevelButtonsRefresh();
        }

        private void UpdateLevelInputField()
        {
            if (LevelInputField) LevelInputField.text = (GameLevelHolder.CurrentLevel + 1).ToString();
        }
        #endregion construct menus

        #region grid settings
        public void OpenSettingsPanel_Click()
        {
            Debug.Log("open grid settings click");

            ScrollPanelController sRC = GridPanelContainer.ScrollPanel;
            if (sRC) // 
            {
                if (sRC) sRC.CloseScrollPanel(true, null);
            }
            else
            {
                CloseOpenedPanels();
                //instantiate ScrollRectController
                sRC = GridPanelContainer.InstantiateScrollPanel();
                sRC.textCaption.text = "Grid panel";

                //create  vert size block
                IncDecInputPanel.Create(sRC.scrollContent, IncDecGridPrefab, "VertSize", MGrid.LcSet.VertSize.ToString(),
                    () => { IncVertSize(); },
                    () => { DecVertSize(); },
                    (val) => { },
                    () => { return MGrid.LcSet.VertSize.ToString(); },
                    null);

                //create hor size block
                IncDecInputPanel.Create(sRC.scrollContent, IncDecGridPrefab, "HorSize", MGrid.LcSet.HorSize.ToString(),
                    () => { IncHorSize(); },
                    () => { DecHorSize(); },
                    (val) => { },
                    () => { return MGrid.LcSet.HorSize.ToString(); },
                    null);

                //create background block
                IncDecInputPanel.Create(sRC.scrollContent, IncDecGridPrefab, "BackGrounds", MGrid.LcSet.BackGround.ToString(),
                    () => { IncBackGround(); },
                    () => { DecBackGround(); },
                    (val) => { },
                    () => { return MGrid.LcSet.BackGround.ToString(); },
                    null);

                //create scale block
                IncDecInputPanel.Create(sRC.scrollContent, IncDecGridPrefab, "Scale", MGrid.LcSet.Scale.ToString(),
                    () => { IncScale(); },
                    () => { DecScale(); },
                    (val) => { },
                    () => { return MGrid.LcSet.Scale.ToString(); },
                    null);

                sRC.OpenScrollPanel(null);
            }
        }

        public void IncVertSize()
        {
            Debug.Log("Click on Button <VerticalSize...> ");
            int vertSize = MBoard.MainGrid.LcSet.VertSize;
            vertSize = (vertSize < maxVertSize) ? ++vertSize : maxVertSize;
            MBoard.MainGrid.LcSet.VertSize = vertSize;
            ChangeGridSizeAction?.Invoke();
        }

        public void DecVertSize()
        {
            Debug.Log("Click on Button <VerticalSize...> ");
            int vertSize = MGrid.LcSet.VertSize;
            vertSize = (vertSize > minVertSize) ? --vertSize : minVertSize;
            MGrid.LcSet.VertSize = vertSize;
            ChangeGridSizeAction?.Invoke();
        }

        public void IncHorSize()
        {
            Debug.Log("Click on Button <HorizontalSize...> ");
            int horSize = MGrid.LcSet.HorSize;
            horSize = (horSize < maxHorSize) ? ++horSize : maxHorSize;
            MGrid.LcSet.HorSize = horSize;
            ChangeGridSizeAction?.Invoke();
        }

        public void DecHorSize()
        {
            Debug.Log("Click on Button <HorizontalSize...> ");
            int horSize = MGrid.LcSet.HorSize;
            horSize = (horSize > minHorSize) ? --horSize : minHorSize;
            MGrid.LcSet.HorSize = horSize;
            ChangeGridSizeAction?.Invoke();
        }
/*
        public void IncDistX()
        {
            Debug.Log("Click on Button <DistanceX...> ");
            int dist = Mathf.RoundToInt(MGrid.LcSet.DistX * 100f);
            dist += 5;
            MGrid.LcSet.DistX = (dist > 100) ? 1f : dist / 100f;
            RebuildGameBoard();
        }

        public void DecDistX()
        {
            Debug.Log("Click on Button <DistanceX...> ");
            int dist = Mathf.RoundToInt(MGrid.LcSet.DistX * 100f);
            dist -= 5;
            MGrid.LcSet.DistX = (dist > 0f) ? dist / 100f : 0f;
            RebuildGameBoard();
        }

        public void IncDistY()
        {
            Debug.Log("Click on Button <DistanceY...> ");
            int dist = Mathf.RoundToInt(MGrid.LcSet.DistY * 100f);
            dist += 5;
            MGrid.LcSet.DistY = (dist > 100) ? 1f : dist / 100f;
            RebuildGameBoard();
        }

        public void DecDistY()
        {
            Debug.Log("Click on Button <DistanceY...> ");
            int dist = Mathf.RoundToInt(MGrid.LcSet.DistY * 100f);
            dist -= 5;
            MGrid.LcSet.DistY = (dist > 0f) ? dist / 100f : 0f;
            RebuildGameBoard();
        }
*/
        public void IncScale()
        {
            Debug.Log("Click on Button <Scale...> ");
            int scale = Mathf.RoundToInt(MGrid.LcSet.Scale * 100f);
            scale += 5;
            MGrid.LcSet.Scale = scale / 100f;
            MGrid.SetScale(MGrid.LcSet.Scale);
        }

        public void DecScale()
        {
            Debug.Log("Click on Button <Scale...> ");
            int scale = Mathf.RoundToInt(MGrid.LcSet.Scale * 100f);
            scale -= 5;
            MGrid.LcSet.Scale = (scale > 0f) ? scale / 100f : 0f;
            MGrid.SetScale(MGrid.LcSet.Scale);
        }

        public void IncBackGround()
        {
            Debug.Log("Click on Button <BackGround...> ");
            MGrid.LcSet.IncBackGround(GOSet.BackGroundsCount);
            MBoard.BackGround = GOSet.GetBackGround(LCSet.BackGround);
        }

        public void DecBackGround()
        {
            Debug.Log("Click on Button <BackGround...> ");
            MGrid.LcSet.DecBackGround(GOSet.BackGroundsCount);
            MBoard.BackGround = GOSet.GetBackGround(LCSet.BackGround);
        }
        #endregion grid settings

        #region grid brushes
        public void Cell_Click(GridCell cell)
        {
            Debug.Log("Click on cell <" + cell.ToString() + "...> ");
            LevelConstructSet lCSet = MGrid.LcSet;
            if (currentBrush)
            {
                if (cell.HaveLayerObject(currentBrush.Layer))
                {
                    currentBrush.DestroyHierCompetitor(cell, false, true);
                    RemoveTileAction?.Invoke(currentBrush.Layer);
                }
                else
                {
                    if (currentBrush.CanSetBySize(cell) && currentBrush.CanSetByLayer(cell, currentBrush.Layer)) // additional check
                    {
                        currentBrush.DestroyHierCompetitor(cell, true, true);
                        GridObject gridObject = cell.SetObject(currentBrush, currentBrush.Layer);
                        if (gridObject) AddTileAction?.Invoke(gridObject, currentBrush.Layer);
                    }
                }
            }
        }

        private void CloseOpenedPanels()
        {
            ScrollPanelController[] sRCs = GetComponentsInChildren<ScrollPanelController>();
            foreach (var item in sRCs)
            {
                item.CloseScrollPanel(true, null);
            }
        }
        #endregion grid brushes

        #region move tiles
        public void MoveTilesUp()
        {
            LCSet.SetOffset(new Vector2Int(-1, 0), MGrid);
            MoveGridAction?.Invoke();
        }

        public void MoveTilesDown()
        {
            LCSet.SetOffset(new Vector2Int(1, 0), MGrid);
            MoveGridAction?.Invoke();
        }

        public void MoveTilesLeft()
        {
            LCSet.SetOffset(new Vector2Int(0, -1), MGrid);
            MoveGridAction?.Invoke();
        }

        public void MoveTilesRight()
        {
            LCSet.SetOffset(new Vector2Int(0, 1), MGrid);
            MoveGridAction?.Invoke();
        }
        #endregion move tiles

        #region load assets
        T[] LoadResourceAssets<T>(string subFolder) where T : BaseScriptable
        {
            T[] t = Resources.LoadAll<T>(subFolder);
            if (t != null && t.Length > 0)
            {
                string s = "";
                foreach (var m in t)
                {
                    s += m.ToString() + "; ";
                }
                Debug.Log("Scriptable assets loaded," + typeof(T).ToString() + ", count: " + t.Length + "; sets : " + s);
            }
            else
            {
                Debug.Log("Scriptable assets " + typeof(T).ToString() + " not found!!!");
            }
            return t;
        }
        #endregion load assets

        #region utils
        private void DestroyGOInChildrenWithComponent<T>(Transform parent) where T : Component
        {
            if (!parent) return;
            T[] existComp = parent.GetComponentsInChildren<T>();
            for (int i = 0; i < existComp.Length; i++)
            {
                if (parent.gameObject != existComp[i].gameObject) DestroyImmediate(existComp[i].gameObject);
            }
        }

        private Button CreateButton(Button prefab, Transform parent, Sprite sprite, System.Action listener)
        {
            Button button = CreateButton(prefab, parent, listener);
            button.GetComponent<Image>().sprite = sprite;
            return button;
        }

        private Button CreateButton(Button prefab, Transform parent, System.Action listener)
        {
            Button button = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            button.transform.SetParent(parent);
            button.transform.localScale = new Vector3(1, 1, 1);
            button.onClick.RemoveAllListeners();

            if (listener != null) button.onClick.AddListener(() =>
            {
                listener();
            });

            return button;
        }

        private Button CreateButton(Button prefab, Transform parent, Sprite sprite, string text, System.Action listener)
        {
            Button button = CreateButton(prefab, parent, sprite, listener);
            Text t = button.GetComponentInChildren<Text>();
            if (t && text != null) t.text = text;
            return button;
        }

        private Button CreateButton(Button prefab, Transform parent, string text, System.Action listener)
        {
            Button button = CreateButton(prefab, parent, listener);
            Text t = button.GetComponentInChildren<Text>();
            if (t && text != null) t.text = text;
            return button;
        }

        private void SelectButton(Button b)
        {
            Text t = b.GetComponentInChildren<Text>();
            if (!t) return;
            t.enabled = true;
            t.gameObject.SetActive(true);
            t.text = "selected";
            t.color = Color.black;
        }

        private void DeselectButton(Button b)
        {
            Text t = b.GetComponentInChildren<Text>();
            if (!t) return;
            t.enabled = true;
            t.gameObject.SetActive(true);
            t.text = "";
        }
        #endregion utils
#endif
    }
}
