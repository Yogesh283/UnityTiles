using UnityEngine;

namespace Mkey.Tournament
{
    [CreateAssetMenu(fileName = "TournamentVisualsData", menuName = "Mkey/Tournament Visuals Data")]
    public class TournamentVisualsData : ScriptableObject
    {
        [Header("Map green theme")]
        public Sprite background;
        public Sprite logo;

        [Header("Panels")]
        public Sprite panelSprite;
        public Sprite popupPanel;

        [Header("Orange buttons (Pink Button sprites)")]
        public Sprite buttonNormal;
        public Sprite buttonHover;
        public Sprite buttonPressed;
        public Sprite buttonDisabled;
        public Sprite longButtonNormal;
        public Sprite longButtonHover;

        [Header("Premium JOIN buttons")]
        public Sprite joinButtonNormal;
        public Sprite joinButtonHover;

        [Header("Map menu button")]
        public Sprite mapPlayButtonNormal;
        public Sprite mapPlayButtonPressed;
    }
}
