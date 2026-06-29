using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey
{
    /// <summary>
    /// Map home screen: removes generated CTA buttons and centers the legacy play buttons.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class HomePageController : MonoBehaviour
    {
        private const int MapSceneIndex = 1;
        private const float ButtonSpacing = 12f;
        private const float MinPlayButtonWidth = 580f;
        private const float TextHorizontalPadding = 44f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            TryInstall(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall(scene);
        }

        private static void TryInstall(Scene scene)
        {
            if (scene.buildIndex != MapSceneIndex) return;
            if (FindFirstObjectByType<HomePageController>()) return;

            GameObject host = new GameObject(nameof(HomePageController));
            host.AddComponent<HomePageController>();
        }

        private void Start()
        {
            StartCoroutine(SetupHomeUi());
        }

        private IEnumerator SetupHomeUi()
        {
            yield return null;

            RemoveGeneratedButtons();

            Transform tournamentButton = null;
            for (int i = 0; i < 10 && !tournamentButton; i++)
            {
                tournamentButton = GameObject.Find("BottomMapGui")?.transform.Find("ButtonTournaments");
                if (!tournamentButton) yield return null;
            }

            yield return WaitForLandingIntro();
            HideLogo();
            CenterLegacyButtons();
        }

        private static void HideLogo()
        {
            GameObject logo = GameObject.Find("Logo");
            if (logo) logo.SetActive(false);
        }

        private static void RemoveGeneratedButtons()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (!canvas) return;

            Transform generated = canvas.transform.Find("HomePageButtons");
            if (generated) Destroy(generated.gameObject);
        }

        private static IEnumerator WaitForLandingIntro()
        {
            while (GameObject.Find("LandingDarkOverlay"))
                yield return null;
        }

        private static void CenterLegacyButtons()
        {
            Transform bottomGui = GameObject.Find("BottomMapGui")?.transform;
            if (!bottomGui) return;

            Transform levelButton = bottomGui.Find("ButtonNextLevel");
            if (!levelButton) return;

            Transform tournamentButton = bottomGui.Find("ButtonTournaments");
            levelButton.gameObject.SetActive(true);
            if (tournamentButton) tournamentButton.gameObject.SetActive(true);

            Canvas canvas = bottomGui.GetComponentInParent<Canvas>();
            if (!canvas) return;

            RectTransform groupRt = GetOrCreateCenterGroup(canvas.transform);

            if (tournamentButton)
            {
                tournamentButton.SetParent(groupRt, false);
                PrepareForLayout(tournamentButton as RectTransform);
                tournamentButton.SetAsFirstSibling();
            }

            levelButton.SetParent(groupRt, false);
            PrepareForLayout(levelButton as RectTransform);
            levelButton.SetAsLastSibling();

            LayoutRebuilder.ForceRebuildLayoutImmediate(groupRt);
            ApplyPlayPrefixLabels(levelButton, tournamentButton);
            EnsurePlayButtonLayout(levelButton);
            EnsurePlayButtonLayout(tournamentButton);
            NormalizePlayButtonWidths(levelButton, tournamentButton);
            HighlightButton(levelButton);
            HighlightButton(tournamentButton);
        }

        private static void ApplyPlayPrefixLabels(Transform levelButton, Transform tournamentButton)
        {
            SetPlayPrefix(levelButton);
            SetPlayPrefix(tournamentButton);
        }

        private static void SetPlayPrefix(Transform button)
        {
            if (!button) return;

            Text label = button.GetComponentInChildren<Text>();
            if (!label || string.IsNullOrEmpty(label.text)) return;
            if (label.text.StartsWith("Play ")) return;

            label.text = "Play " + label.text;
        }

        private static void EnsurePlayButtonLayout(Transform button)
        {
            if (!button) return;

            RectTransform rt = button as RectTransform;
            if (rt && rt.sizeDelta.x < MinPlayButtonWidth)
                rt.sizeDelta = new Vector2(MinPlayButtonWidth, rt.sizeDelta.y);

            Text label = button.GetComponentInChildren<Text>();
            if (!label) return;

            RectTransform labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = Vector2.zero;
            labelRt.offsetMin = new Vector2(TextHorizontalPadding, 14f);
            labelRt.offsetMax = new Vector2(-TextHorizontalPadding, -10f);

            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 32;
            if (label.resizeTextMaxSize < 56)
                label.resizeTextMaxSize = 56;
        }

        private static void NormalizePlayButtonWidths(Transform levelButton, Transform tournamentButton)
        {
            float width = MinPlayButtonWidth;
            if (levelButton is RectTransform levelRt)
                width = Mathf.Max(width, levelRt.sizeDelta.x);
            if (tournamentButton is RectTransform tourRt)
                width = Mathf.Max(width, tourRt.sizeDelta.x);

            ApplyButtonWidth(levelButton, width);
            ApplyButtonWidth(tournamentButton, width);
        }

        private static void ApplyButtonWidth(Transform button, float width)
        {
            if (button is RectTransform rt)
                rt.sizeDelta = new Vector2(width, rt.sizeDelta.y);
        }

        private static void HighlightButton(Transform button)
        {
            if (!button) return;

            Image image = button.GetComponent<Image>();
            if (image)
            {
                image.color = new Color(1f, 0.98f, 0.9f, 1f);
                AddOutlineIfMissing(button.gameObject, new Color(1f, 0.82f, 0.35f, 0.9f), new Vector2(3f, -3f));
                EnsureGlowLayer(button, image);
            }

            Text label = button.GetComponentInChildren<Text>();
            if (label)
            {
                label.fontStyle = FontStyle.Bold;
                label.color = new Color(0.32f, 0.14f, 0.04f, 1f);
                AddOutlineIfMissing(label.gameObject, new Color(1f, 0.9f, 0.55f, 0.75f), new Vector2(1.2f, -1.2f));

                if (!label.GetComponent<Shadow>())
                {
                    Shadow shadow = label.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
                    shadow.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }

            Button uiButton = button.GetComponent<Button>();
            if (uiButton)
            {
                ColorBlock colors = uiButton.colors;
                colors.normalColor = new Color(1f, 1f, 0.96f, 1f);
                colors.highlightedColor = new Color(1f, 0.95f, 0.82f, 1f);
                colors.pressedColor = new Color(0.92f, 0.88f, 0.75f, 1f);
                uiButton.colors = colors;
            }

            RectTransform rt = button as RectTransform;
            if (rt) rt.localScale = Vector3.one * 1.04f;
        }

        private static void EnsureGlowLayer(Transform button, Image sourceImage)
        {
            if (button.Find("HighlightGlow")) return;

            GameObject glowGo = new GameObject("HighlightGlow", typeof(RectTransform));
            glowGo.transform.SetParent(button, false);
            glowGo.transform.SetAsFirstSibling();

            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero;
            glowRt.anchorMax = Vector2.one;
            glowRt.offsetMin = new Vector2(-10f, -10f);
            glowRt.offsetMax = new Vector2(10f, 10f);

            Image glowImage = glowGo.AddComponent<Image>();
            if (sourceImage && sourceImage.sprite) glowImage.sprite = sourceImage.sprite;
            glowImage.color = new Color(1f, 0.78f, 0.22f, 0.38f);
            glowImage.raycastTarget = false;
        }

        private static void AddOutlineIfMissing(GameObject target, Color color, Vector2 distance)
        {
            if (target.GetComponent<Outline>()) return;

            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = true;
        }

        private static RectTransform GetOrCreateCenterGroup(Transform canvasRoot)
        {
            Transform existing = canvasRoot.Find("CenterPlayButtons");
            if (existing) return existing as RectTransform;

            GameObject groupGo = new GameObject("CenterPlayButtons", typeof(RectTransform));
            groupGo.transform.SetParent(canvasRoot, false);

            RectTransform rt = groupGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            VerticalLayoutGroup layout = groupGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = ButtonSpacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = groupGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        private static void PrepareForLayout(RectTransform rt)
        {
            if (!rt) return;

            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }
}
