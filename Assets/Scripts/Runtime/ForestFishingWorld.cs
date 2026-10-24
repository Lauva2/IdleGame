using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        static readonly Vector3[] FishingOrigins = { V(-39.2f,0,46), V(-40.4f,0,50.8f), V(-53.4f,0,70) };
        readonly Transform[] fishingDocks=new Transform[3], fishingFloats=new Transform[3], fishingFloatBodies=new Transform[3];
        readonly Transform[] fishingRings=new Transform[9];
        readonly Transform[] fishingBaits=new Transform[9];
        readonly LineRenderer[] fishingLines=new LineRenderer[3];
        readonly Dictionary<int,Transform> fishReceipts=new Dictionary<int,Transform>();
        Transform fishReceipt; float fishReceiptAt;
        void BuildFishingWorld()
        {
            var pond=Group(forestScene,"Quiet pool beside reading pavilion",V(-50.8f,.1f,72.8f));
            Shape(pond,"Soft pond shore",Lathe(new[]{new Vector2(0,0),new Vector2(3.5f,0),new Vector2(3.6f,.04f),new Vector2(3.35f,.09f),new Vector2(0,.09f)},64),Vector3.zero,V(1,1,.80f),Hex(0xB6B797));
            Shape(pond,"Pond teal water",Lathe(new[]{new Vector2(0,0),new Vector2(3.23f,0),new Vector2(3.21f,.01f),new Vector2(0,.01f)},64),V(0,.105f,0),V(1,1,.8f),Hex(0x79AAA4));
            for(int i=0;i<13;i++)
            {
                float a=i*2.4f; var at=V(Mathf.Sin(a)*3.5f,.15f,Mathf.Cos(a)*2.85f);
                if(at.x<-.8f && at.z<.2f) continue;
                Ball(pond,"Rounded waterside stone",at,V(.55f,.27f,.42f),i%3==0?Hex(0xA3AC92):Hex(0x899B7F),V(0,i*47,0));
                for(int j=0;j<3;j++) Ball(pond,"Water grass leaf",at+V(.15f,.23f,0),V(.11f,.65f,.08f),Moss,V(0,j*50,20+j*8));
            }
            for(int i=0;i<5;i++)
            {
                var at=V(Mathf.Sin(i*2.4f)*2,.13f,Mathf.Cos(i*2.4f)*1.6f);
                Ball(pond,"Floating round pond leaf",at,V(.52f,.018f,.40f),Leaf,V(0,i*49,0));
                if(i%2==0) ForestModels.Flower(pond,at+V(0,.05f,0),.10f,Cream);
                Box(pond,"Quiet water reflection",at+V(.6f,.01f,.4f),V(.75f,.015f,.035f),Hex(0xC5DCCC),V(0,-10,0));
            }
            Combine(pond);
            for(int i=0;i<3;i++)
            {
                var dock=fishingDocks[i]=Group(forestScene,"Fishing spot "+FishingCatalog.Spots[i],FishingOrigins[i]); dock.localEulerAngles=V(0,i==1?180:i==2?30:0,0);
                var timber=Group(dock,"Small fishing landing");
                for(int plank=0;plank<7;plank++) Box(timber,"Honey landing plank",V(-.84f+plank*.28f,.34f,.1f),V(.26f,.12f,1.8f),plank%3==0?Honey:Wood);
                for(int side=-1;side<=1;side+=2)
                {
                    Tube(timber,"Landing waterside post",V(side*.78f,-.15f,.76f),V(side*.78f,.72f,.76f),.09f,DarkWood);
                    Ring(timber,"Coiled post rope",V(side*.78f,.58f,.76f),.11f,.023f,Cream);
                }
                Box(timber,"Fishing seat",V(.45f,.72f,-.45f),V(.70f,.11f,.53f),Honey);
                for(int side=-1;side<=1;side+=2) Box(timber,"Fishing seat foot",V(.45f+side*.23f,.53f,-.45f),V(.09f,.38f,.40f),Wood);
                var sign=Group(timber,"Fish carved sign",V(.94f,.1f,-.5f));
                Tube(sign,"Sign stake",Vector3.zero,V(0,1.15f,0),.07f,Wood); Box(sign,"Fishing sign board",V(0,1.02f,-.03f),V(.72f,.45f,.10f),Deep);
                Ball(sign,"Carved fish body",V(.03f,1.02f,-.10f),V(.35f,.18f,.035f),Cream);
                Shape(sign,"Carved fish tail",OakLeaf(),V(-.19f,1.02f,-.11f),V(.18f,.22f,.04f),Gold,V(90,0,90));
                Shape(timber,"Curved reed fishing rod",Sweep(new[]{V(-.66f,.4f,-.6f),V(-.63f,1.2f,-.25f),V(-.32f,2.1f,.09f),V(.1f,2.45f,.45f)},new[]{.048f,.035f,.022f,.012f}),Vector3.zero,Vector3.one,Honey);
                Tube(timber,"Rod cork grip",V(-.66f,.4f,-.6f),V(-.65f,.82f,-.45f),.067f,DarkWood);
                Ring(timber,"Small fishing reel",V(-.62f,.76f,-.49f),.11f,.027f,Gold,V(75,0,0));
                Combine(timber);
                for(int bait=0;bait<3;bait++) { var model=fishingBaits[i*3+bait]=ForestFishingModels.Bait(dock,bait); model.localPosition=V(.5f,.40f,.3f); model.localScale=Vector3.one*.8f; model.gameObject.SetActive(bait==0); }
                var bobber=fishingFloats[i]=Group(dock,"Float at water contact",V(.2f,.19f,2.4f));
                var body=fishingFloatBodies[i]=Group(bobber,"Cream and berry float");
                Ball(body,"Float round body",Vector3.zero,V(.16f,.22f,.16f),Cream);
                Ball(body,"Float red cap",V(0,.07f,0),V(.145f,.11f,.145f),Hex(0xBF7769));
                Tube(body,"Float stem",V(0,.04f,0),V(0,.28f,0),.017f,Honey); Ball(body,"Float golden tip",V(0,.29f,0),Vector3.one*.06f,Gold); Combine(body);
                for(int ring=0;ring<3;ring++) fishingRings[i*3+ring]=Ring(dock,"Water ripple "+ring,V(.2f,.235f,2.4f),.22f+ring*.16f,.014f,Hex(0xCAE1D0));
                var line=Group(dock,"Connected rod line").gameObject.AddComponent<LineRenderer>(); fishingLines[i]=line;
                line.useWorldSpace=false; line.positionCount=13; line.startWidth=line.endWidth=.022f; line.sharedMaterial=Mat(Cream,true); line.alignment=LineAlignment.View; line.generateLightingData=true; line.shadowCastingMode=ShadowCastingMode.Off;
                Hotspot(dock,"fishing",i,V(0,.8f,.2f),V(2.5f,2.4f,2.5f)); Hotspot(dock,"fishing",i,V(.2f,.3f,2.4f),V(1.0f,1.2f,1.0f));
            }
        }
        public void FocusFishing(int spot)
        {
            if(spot<0 || spot>2) return; ClearFishingReceipt();
            cameraFocus=fishingFloats[spot].position+V(0,.6f,-2.5f);
            worldCamera.orthographicSize=6.6f; worldCamera.transform.position=cameraFocus+V(.5f,18,-16); worldCamera.transform.LookAt(cameraFocus);
        }
        Vector2 FishingMarker(int spot)
        { var p=worldCamera.WorldToViewportPoint(fishingDocks[spot].TransformPoint(V(0,.15f,spot==2?-4:-.95f))); return new Vector2(p.x,p.y); }
        public void PresentFishCatch(FishCatch caught)
        {
            ClearFishingReceipt(); int key=caught.species*4+caught.quality;
            if(!fishReceipts.TryGetValue(key,out fishReceipt))
            { fishReceipt=ForestFishingModels.Fish(worldRoot,caught.species,caught.quality); fishReceipt.localScale=Vector3.one*.7f; fishReceipts[key]=fishReceipt; }
            fishReceiptAt=Time.unscaledTime; fishReceipt.position=fishingFloats[caught.spot].position+V(0,.55f,0); fishReceipt.rotation=Quaternion.Euler(0,worldCamera.transform.eulerAngles.y,12); fishReceipt.gameObject.SetActive(true);
        }
        void ClearFishingReceipt() { if(fishReceipt!=null) fishReceipt.gameObject.SetActive(false); }
        void UpdateFishingWorld()
        {
            if(fishingDocks[0]==null || state==null) return;
            var f=state.fishing;
            for(int i=0;i<3;i++)
            {
                bool active=f.running && f.spot==i && !state.report.pending;
                for(int bait=0;bait<3;bait++) fishingBaits[i*3+bait].gameObject.SetActive(bait==(f.spot==i?f.bait:0));
                float movement=active && !state.simpleAnimation ? Mathf.Sin(Time.unscaledTime*(f.phase==2?9:2))*(f.phase==2?.035f:.035f) : 0;
                fishingFloatBodies[i].localPosition=V(0,(active && f.phase==2?-.13f:0)+movement,0);
                var end=fishingFloats[i].localPosition+fishingFloatBodies[i].localPosition+V(0,.28f,0);
                for(int j=0;j<13;j++)
                {
                    float t=j/12f; var start=V(.1f,2.45f,.45f); var middle=(start+end)*.5f-V(0,.45f,0);
                    fishingLines[i].SetPosition(j,(1-t)*(1-t)*start+2*(1-t)*t*middle+t*t*end);
                }
                for(int j=0;j<3;j++)
                {
                    var ring=fishingRings[i*3+j]; ring.gameObject.SetActive(active && !state.simpleAnimation);
                    ring.localScale=Vector3.one*(1+(Mathf.Sin(Time.unscaledTime*(f.phase==2?3:1.2f)-j)+1)*.16f);
                }
            }
            if(fishReceipt!=null && fishReceipt.gameObject.activeSelf)
            {
                float t=(Time.unscaledTime-fishReceiptAt)/1.8f;
                if(t>=1 || !f.running || state.report.pending || !worldCamera.enabled) ClearFishingReceipt();
                else if(!state.simpleAnimation)
                { fishReceipt.position=fishingFloats[f.spot].position+V(0,.55f+Mathf.Sin(t*Mathf.PI)*.48f,0); fishReceipt.localEulerAngles=V(0,worldCamera.transform.eulerAngles.y,12+Mathf.Sin(t*Mathf.PI)*10); }
            }
        }
    }
}
