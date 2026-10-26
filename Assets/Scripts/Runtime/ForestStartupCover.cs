using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    // Same original emblem, type and palette as the native static launch card.
    // This layer also blocks input until save recovery or the first page is ready.
    public sealed class ForestStartupCover : MonoBehaviour
    {
        Text message, dots;
        public static ForestStartupCover Create(Font font)
        {
            var root = new GameObject("Preparing the forest shop", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ForestStartupCover));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0;
            var paper = new GameObject("Warm paper", typeof(RectTransform), typeof(Image)); paper.transform.SetParent(root.transform, false);
            var backdrop = paper.GetComponent<RectTransform>(); backdrop.anchorMin = Vector2.zero; backdrop.anchorMax = Vector2.one; backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
            paper.GetComponent<Image>().color = new Color32(255, 249, 228, 255);
            var emblem = new GameObject("Woodland shop emblem", typeof(RectTransform), typeof(ForestSymbolGraphic)); emblem.transform.SetParent(root.transform, false);
            Centre(emblem.GetComponent<RectTransform>(), 0, 155, 300, 300);
            emblem.GetComponent<ForestSymbolGraphic>().Symbol = ForestSymbol.Home;
            AddText(root.transform, font, "林间奇物铺", 64, new Color32(72,105,79,255), 0, -72, 940, 108);
            AddText(root.transform, font, "在树林间，慢慢经营一间小店", 29, new Color32(112,105,82,255), 0, -168, 940, 64);
            var cover = root.GetComponent<ForestStartupCover>();
            cover.message = AddText(root.transform, font, "正在准备林间小院", 29, new Color32(72,105,79,255), 0, -332, 940, 64);
            cover.dots = AddText(root.transform, font, "·", 36, new Color32(72,105,79,255), 0, -392, 360, 64);
            return cover;
        }
        static void Centre(RectTransform rect, float x, float y, float w, float h)
        { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f); rect.anchoredPosition = new Vector2(x,y); rect.sizeDelta = new Vector2(w,h); }
        static Text AddText(Transform parent, Font font, string value, int size, Color color, float x, float y, float w, float h)
        {
            var obj = new GameObject("Launch caption", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent,false);
            Centre(obj.GetComponent<RectTransform>(),x,y,w,h); var text = obj.GetComponent<Text>(); text.font=font; text.text=value; text.fontSize=size;
            text.color=color; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; return text;
        }
        public void Stage(string value) { message.text=value; }
        void Update() { dots.text = new string('·',1+(int)(Time.realtimeSinceStartup*2)%3); }
    }
}
