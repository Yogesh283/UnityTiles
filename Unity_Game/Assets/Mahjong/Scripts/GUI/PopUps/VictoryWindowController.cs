using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Mkey
{
    /// <summary>
    /// Image-based victory popup (art has Back + Continue baked in).
    /// Code only overlays live Time / Score / Accuracy / Beat % and invisible button hits.
    /// </summary>
    public class VictoryWindowController : PopUpsController
    {
        [SerializeField] private Text NextLevelNumber;
        [SerializeField] private Text ScoreCount;
        [SerializeField] private Text greetingText;

        private const int UiVersion = 15;
        private static int appliedUiVersion;

        private bool built;
        private RectTransform contentRoot;
        private RectTransform overlayRoot;
        private Image artImage;
        private Text timeValueText;
        private Text scoreValueText;
        private Text accuracyValueText;
        private Text beatText;
        private Button backButton;
        private Button nextButton;
        private Font uiFont;

        // Match empty dark wells in art — no laggy grey boxes
        private static readonly Color ValueCream = new Color(0.98f, 0.95f, 0.88f, 1f);
        private static readonly Color SlotCover = new Color(0.04f, 0.035f, 0.03f, 1f);
        private static readonly Color PanelCover = new Color(0.05f, 0.045f, 0.04f, 1f);

        private GameConstructSet GCSet => GameConstructSet.Instance;
        private LevelConstructSet LCSet => GCSet ? GCSet.GetLevelConstructSet(GameLevelHolder.CurrentLevel) : null;
        private GuiController MGui => GuiController.Instance;

        private void OnDestroy() => SimpleTween.Cancel(gameObject, false);

        public override void RefreshWindow()
        {
            EnsureUi();
            Populate();
            StartCoroutine(KeepLegacyHidden());
            StartCoroutine(FitOverlayToArt());
            base.RefreshWindow();
        }

        public void Map_Click() => GoNextLevel();

        public void Next_Click() => GoNextLevel();

        private void GoNextLevel()
        {
            if (Shell.MatchIQShellBridge.IsActive)
            {
                CloseWindow();
                Shell.MatchIQShellBridge.ReturnMatchResult(true);
                return;
            }

            CloseWindow();
            ShowStory(() =>
            {
                GameLevelHolder.CurrentLevel++;
                SceneLoader.Instance.ReLoadCurrentScene(true);
            });
        }

        private IEnumerator KeepLegacyHidden()
        {
            for (int i = 0; i < 10; i++)
            {
                StripLegacyVisuals();
                yield return null;
            }
        }

        private IEnumerator FitOverlayToArt()
        {
            yield return null;
            yield return null;
            ApplyArtFit();
            yield return new WaitForSecondsRealtime(0.15f);
            ApplyArtFit();
        }

        private void ApplyArtFit()
        {
            if (!artImage || !overlayRoot || !artImage.sprite) return;

            RectTransform parent = artImage.rectTransform;
            float pW = parent.rect.width;
            float pH = parent.rect.height;
            if (pW < 2f || pH < 2f) return;

            Sprite sp = artImage.sprite;
            float sAspect = sp.rect.width / Mathf.Max(1f, sp.rect.height);
            float pAspect = pW / pH;

            float w;
            float h;
            if (sAspect > pAspect)
            {
                w = pW;
                h = w / sAspect;
            }
            else
            {
                h = pH;
                w = h * sAspect;
            }

            overlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRoot.pivot = new Vector2(0.5f, 0.5f);
            overlayRoot.sizeDelta = new Vector2(w, h);
            overlayRoot.anchoredPosition = Vector2.zero;
            overlayRoot.localScale = Vector3.one;
        }

        private void EnsureUi()
        {
            if (built && appliedUiVersion == UiVersion && contentRoot)
                return;

            built = false;
            appliedUiVersion = UiVersion;

            StripLegacyVisuals();
            DestroyOldRoots();

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                     ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform host = ResolveHost();
            ExpandFull(host);
            GuiFader_v2 fader = GetComponent<GuiFader_v2>();
            if (fader && fader.guiMask) ExpandFull(fader.guiMask);

            contentRoot = CreateRect(host, "ImageVictoryPopup");
            Stretch(contentRoot, 0f, 0f, 1f, 1f);
            contentRoot.SetAsLastSibling();

            BuildImagePopup(contentRoot);

            if (NextLevelNumber) NextLevelNumber.gameObject.SetActive(false);
            if (ScoreCount) ScoreCount.gameObject.SetActive(false);
            if (greetingText) greetingText.gameObject.SetActive(false);

            built = true;
        }

        private void BuildImagePopup(RectTransform root)
        {
            Image dim = CreateImage(root, "Dim", new Color(0.04f, 0.05f, 0.08f, 0.8f), null, true);
            Stretch(dim.rectTransform, 0f, 0f, 1f, 1f);

            Sprite art = Resources.Load<Sprite>("Victory/VictoryPopupArt");
            if (!art)
            {
                Debug.LogError("[Victory] Missing Resources/Victory/VictoryPopupArt sprite.");
                return;
            }

            RectTransform artFrame = CreateRect(root, "ArtFrame");
            Stretch(artFrame, 0f, 0f, 1f, 1f);

            artImage = CreateImage(artFrame, "Art", Color.white, art);
            artImage.preserveAspect = true;
            Stretch(artImage.rectTransform, 0f, 0f, 1f, 1f);

            overlayRoot = CreateRect(artFrame, "OverlayRoot");
            overlayRoot.anchorMin = new Vector2(0.5f, 0.5f);
            overlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
            overlayRoot.pivot = new Vector2(0.5f, 0.5f);
            overlayRoot.sizeDelta = new Vector2(800f, 1400f);

            BuildOverlays(overlayRoot);
        }

        private void BuildOverlays(RectTransform o)
        {
            // Live values — full column wells, text dead-center in each card
            timeValueText = CreateStatValue(o, "TimeVal", 0.155f, 0.505f, 0.360f, 0.575f);
            scoreValueText = CreateStatValue(o, "ScoreVal", 0.398f, 0.505f, 0.602f, 0.575f);
            accuracyValueText = CreateStatValue(o, "AccVal", 0.640f, 0.505f, 0.845f, 0.575f);

            // Cover baked Beat 00.00% + live %
            Image beatCover = CreateImage(o, "BeatCover", PanelCover, null);
            Stretch(beatCover.rectTransform, 0.18f, 0.418f, 0.82f, 0.458f);
            beatText = CreateText(o, "Beat", "Beat 0% of players!", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(beatText.rectTransform, 0.18f, 0.418f, 0.82f, 0.458f);

            // Invisible hits — Back / Continue are painted in the art
            backButton = CreateInvisibleButton(o, "BackHit", 0.16f, 0.175f, 0.84f, 0.255f);
            backButton.onClick.AddListener(Map_Click);

            nextButton = CreateInvisibleButton(o, "NextHit", 0.16f, 0.085f, 0.84f, 0.165f);
            nextButton.onClick.AddListener(Next_Click);
        }

        private Text CreateStatValue(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            // Cover fills the card value zone; text is geometrically centered inside
            Image cover = CreateImage(parent, name + "Cover", SlotCover, null);
            Stretch(cover.rectTransform, x0, y0, x1, y1);

            Text text = CreateText(parent, name, "00:00", 34, FontStyle.Bold, ValueCream, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, x0, y0, x1, y1);
            text.alignByGeometry = true;
            text.resizeTextForBestFit = false;
            text.lineSpacing = 1f;
            return text;
        }

        private Button CreateInvisibleButton(RectTransform parent, string name, float x0, float y0, float x1, float y1)
        {
            Image hit = CreateImage(parent, name, new Color(1f, 1f, 1f, 0.01f), null, true);
            Stretch(hit.rectTransform, x0, y0, x1, y1);
            Button btn = hit.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            ColorBlock cb = btn.colors;
            cb.normalColor = new Color(1f, 1f, 1f, 0.01f);
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.06f);
            cb.pressedColor = new Color(1f, 1f, 1f, 0.12f);
            btn.colors = cb;
            return btn;
        }

        private void Populate()
        {
            int score = ScoreHolder.Count;
            int maxScore = Mathf.Max(1, ScoreHolder.AverageScore);
            if (score > maxScore) score = maxScore;
            float accuracy = score / (float)maxScore * 100f;
            float beatPlayers = Mathf.Clamp(accuracy * 0.9f, 12f, 99.5f);

            if (timeValueText) timeValueText.text = CampaignLevelTimer.FormatElapsed();
            if (scoreValueText) scoreValueText.text = score.ToString("N0");
            if (accuracyValueText) accuracyValueText.text = accuracy.ToString("0.0") + "%";
            if (beatText) beatText.text = $"Beat <color=#5CFF6A>{beatPlayers:0.00}%</color> of players!";
        }

        #region legacy / host
        private void StripLegacyVisuals()
        {
            GuiFader_v2 fader = GetComponent<GuiFader_v2>();
            if (fader && fader.backGround)
            {
                Image bg = fader.backGround.GetComponent<Image>();
                if (bg)
                {
                    bg.sprite = null;
                    bg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
                }
            }

            if (fader && fader.guiPanel)
            {
                Image panelImg = fader.guiPanel.GetComponent<Image>();
                if (panelImg)
                {
                    panelImg.sprite = null;
                    panelImg.color = Color.clear;
                    panelImg.enabled = false;
                }

                foreach (Transform child in fader.guiPanel)
                {
                    if (child.name == "ImageVictoryPopup") continue;
                    child.gameObject.SetActive(false);
                }
            }

            var kill = new List<GameObject>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!t || t == transform) continue;
                string n = t.name;
                if (n == "ButtonNextLevel" || n == "Greeting" || n == "TextScore" ||
                    n == "TextScoreCount" || n == "TextNextLevel")
                    kill.Add(t.gameObject);
            }
            for (int i = 0; i < kill.Count; i++) Destroy(kill[i]);
        }

        private void DestroyOldRoots()
        {
            string[] names =
            {
                "ImageVictoryPopup", "PremiumResultPopup", "IntelligentVictory",
                "IntelligentVictory_OLD", "PremiumVictoryRoot"
            };
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (!t || t == transform) continue;
                for (int i = 0; i < names.Length; i++)
                {
                    if (t.name == names[i])
                    {
                        Destroy(t.gameObject);
                        break;
                    }
                }
            }
        }

        private RectTransform ResolveHost()
        {
            GuiFader_v2 fader = GetComponent<GuiFader_v2>();
            if (fader && fader.guiPanel) return fader.guiPanel;
            Transform panel = transform.Find("GuiMask/Panel") ?? transform.Find("Panel");
            if (panel) return panel as RectTransform;
            return transform as RectTransform;
        }

        private static void ExpandFull(RectTransform rt)
        {
            if (!rt) return;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }
        #endregion

        #region helpers
        private RectTransform CreateRect(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Image CreateImage(Transform parent, string name, Color color, Sprite sprite, bool raycast = false)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = raycast;
            return img;
        }

        private Text CreateText(Transform parent, string name, string content, int size, FontStyle style, Color color, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            Text text = go.GetComponent<Text>();
            text.font = uiFont;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = (uiFont != null && uiFont.dynamic) ? style : FontStyle.Normal;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static void Stretch(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private void ShowStory(Action completeCallBack)
        {
            if (LCSet && LCSet.LevelWinStoryPage && MGui)
                MGui.ShowPopUp(LCSet.LevelWinStoryPage, completeCallBack);
            else
                completeCallBack?.Invoke();
        }
        #endregion
    }
}
