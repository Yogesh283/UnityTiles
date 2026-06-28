using UnityEngine;

namespace Mkey
{
    [CreateAssetMenu(fileName = "PopupButtonThemeData", menuName = "Mkey/Popup Button Theme Data")]
    public class PopupButtonThemeData : ScriptableObject
    {
        public Sprite greenNormal;
        public Sprite greenPressed;
        public Sprite orangeNormal;
        public Sprite orangePressed;
        public Sprite greenLongNormal;
        public Sprite greenLongPressed;
        public Sprite orangeLongNormal;
        public Sprite orangeLongPressed;
        public Sprite greenSmallNormal;
        public Sprite greenSmallPressed;
    }
}
