using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        bool fishingImmersed;
        void FishingChrome(bool immersed)
        {
            fishingImmersed=immersed;
            foreach(string name in new[]{"Current goal","Production updates","Navigation"}) safe.Find(name).gameObject.SetActive(!immersed);
            page.offsetMin=new Vector2(0,immersed?0:124);page.offsetMax=new Vector2(0,immersed?-100:-244);
        }
        void BeginFishingViewport(ForestWorld3D world)
        {
            FishingChrome(true);bindings.Clear();animationBindings.Clear();
            if(content!=null){content.parent.gameObject.SetActive(false);Destroy(content.parent.gameObject);}
            Canvas.ForceUpdateCanvases();
            var view=Rect(page,"Uninterrupted fishing water",0,0,1080,page.rect.height);Fill(view);
            view.gameObject.AddComponent<RawImage>().texture=world.View;view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<ForestWorldPointer>().world=world;
            world.SetViewportAspect(1080/Mathf.Max(1,page.rect.height));
            var title=sheetPanel.GetComponentInChildren<Text>();title.fontSize=36;title.rectTransform.anchoredPosition=new Vector2(35,-2);title.rectTransform.sizeDelta=new Vector2(800,78);
            var close=sheetPanel.Find("关闭");close.GetComponent<RectTransform>().anchoredPosition=new Vector2(886,-10);close.GetComponentInChildren<Text>().text="离开";
            var scroll=(RectTransform)sheetContent.parent;scroll.offsetMin=new Vector2(0,16);scroll.offsetMax=new Vector2(0,-88);
        }
        void FinishFishingSheet()
        {
            float height=Mathf.Max(100,sheetY+20);sheetContent.sizeDelta=new Vector2(0,height);
            AnchorBar(sheetPanel,false,Mathf.Min(sheetHeightLimit,height+104));
            Canvas.ForceUpdateCanvases();sheetContent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;
        }
    }
}
