using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed class ForestButtonVisual : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text caption;
        public Color normalBackground, normalText;
        void LateUpdate()
        {
            background.color = button.interactable ? normalBackground : new Color32(207, 207, 187, 255);
            caption.color = button.interactable ? normalText : new Color32(102, 106, 95, 255);
        }
    }
}
