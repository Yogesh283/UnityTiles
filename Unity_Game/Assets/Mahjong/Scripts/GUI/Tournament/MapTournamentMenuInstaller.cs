using Mkey;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mkey.Tournament
{
    /// <summary>
    /// Adds a Tournament button on the map — identical style to ButtonNextLevel.
    /// </summary>
    public class MapTournamentMenuInstaller : MonoBehaviour
    {
        private const int MapSceneIndex = 1;
        private const float ButtonGap = 12f;

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
            if (FindFirstObjectByType<MapTournamentMenuInstaller>()) return;

            GameObject host = new GameObject(nameof(MapTournamentMenuInstaller));
            host.AddComponent<MapTournamentMenuInstaller>();
        }

        private void Start()
        {
            Transform bottomGui = transform.Find("BottomMapGui");
            if (!bottomGui)
                bottomGui = GameObject.Find("BottomMapGui")?.transform;
            if (!bottomGui) return;

            Transform levelButton = bottomGui.Find("ButtonNextLevel");
            if (!levelButton)
            {
                Debug.LogWarning("MapTournamentMenuInstaller: ButtonNextLevel not found.");
                return;
            }

            Transform existing = bottomGui.Find("ButtonTournaments");
            if (existing)
                Destroy(existing.gameObject);

            CreateTournamentButton(bottomGui, levelButton);
        }

        private static void CreateTournamentButton(Transform parent, Transform levelButton)
        {
            RectTransform levelRt = levelButton as RectTransform;
            Image levelImage = levelButton.GetComponent<Image>();
            Button levelBtn = levelButton.GetComponent<Button>();
            Text levelText = levelButton.GetComponentInChildren<Text>();

            GameObject buttonGo = new GameObject("ButtonTournaments", typeof(RectTransform));
            buttonGo.transform.SetParent(parent, false);

            RectTransform rt = buttonGo.GetComponent<RectTransform>();
            rt.anchorMin = levelRt.anchorMin;
            rt.anchorMax = levelRt.anchorMax;
            rt.pivot = levelRt.pivot;
            rt.sizeDelta = levelRt.sizeDelta;
            rt.anchoredPosition = new Vector2(
                levelRt.anchoredPosition.x,
                levelRt.anchoredPosition.y + levelRt.sizeDelta.y + ButtonGap);

            Image image = buttonGo.AddComponent<Image>();
            if (levelImage)
            {
                image.sprite = levelImage.sprite;
                image.color = levelImage.color;
                image.type = levelImage.type;
                image.preserveAspect = levelImage.preserveAspect;
                image.raycastTarget = true;
            }

            Button button = buttonGo.AddComponent<Button>();
            button.targetGraphic = image;
            if (levelBtn)
            {
                button.transition = levelBtn.transition;
                button.spriteState = levelBtn.spriteState;
                button.colors = levelBtn.colors;
            }

            TournamentMenuButton menuButton = buttonGo.AddComponent<TournamentMenuButton>();
            button.onClick.AddListener(menuButton.Click);

            if (levelButton.GetComponent<ButtonClickSound>())
                buttonGo.AddComponent<ButtonClickSound>();

            GameObject labelGo = new GameObject("TextTournament", typeof(RectTransform));
            labelGo.transform.SetParent(buttonGo.transform, false);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = new Vector2(0f, 7f);
            labelRt.sizeDelta = Vector2.zero;
            labelRt.offsetMin = new Vector2(44f, 18f);
            labelRt.offsetMax = new Vector2(-44f, -10f);

            Text label = labelGo.AddComponent<Text>();
            label.font = levelText ? levelText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = levelText ? levelText.fontStyle : FontStyle.Normal;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = levelText ? levelText.color : new Color(0.196f, 0.196f, 0.196f, 1f);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 34;
            label.resizeTextMaxSize = 56;
            label.fontSize = 56;
            label.text = "Tournament";
            label.raycastTarget = false;
        }
    }
}
