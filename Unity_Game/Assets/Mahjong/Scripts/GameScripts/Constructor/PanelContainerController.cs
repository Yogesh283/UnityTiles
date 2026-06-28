using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Mkey
{
    public class PanelContainerController : MonoBehaviour
    {
        public Button OpenCloseButton;
        public Button BrushSelectButton;
        public Image selector;
        public Image brushImage;
        public Text BrushName;
        public string capital;
        public List<GridObject> gridObjects;

        [SerializeField]
        private ScrollPanelController ScrollPanelPrefab;

        [SerializeField]
        private ScrollPanelController ScrollPanelPrefabSmall;

        internal ScrollPanelController ScrollPanel;

        public ScrollPanelController InstantiateScrollPanel()
        {
            return InstantiateScrollPanel(ScrollPanelPrefab);
        }

        public ScrollPanelController InstantiateScrollPanelSmall()
        {
            return InstantiateScrollPanel(ScrollPanelPrefabSmall);
        }

        private ScrollPanelController InstantiateScrollPanel(ScrollPanelController scrollPanelPrefab)
        {
            if (!scrollPanelPrefab) return null;

            if (ScrollPanel) DestroyImmediate(ScrollPanel.gameObject);

            RectTransform panel = Instantiate(scrollPanelPrefab).GetComponent<RectTransform>();
            panel.SetParent(GetComponent<RectTransform>());
            panel.anchoredPosition = new Vector2(0, 0);
            ScrollPanel = panel.GetComponent<ScrollPanelController>();
            return ScrollPanel;
        }
    }
}