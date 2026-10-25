using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        static readonly Vector3[] FishingOrigins = { V(-39.2f,0,46), V(-48.8f,0,50.8f), V(-53.4f,0,70) };
        readonly Transform[] fishingDocks=new Transform[3], fishingFloats=new Transform[3], fishingFloatBodies=new Transform[3];
        readonly Transform[] fishingRings=new Transform[9];
        readonly Transform[] fishingBaits=new Transform[9];
        readonly LineRenderer[] fishingLines=new LineRenderer[3];
        readonly Transform[] fishingRods=new Transform[3], fishingRodTips=new Transform[3], fishingReelHandles=new Transform[3], fishingBaskets=new Transform[3];
        readonly Transform[] fishingDrops=new Transform[18];
        readonly Dictionary<int,Transform> fishReceipts=new Dictionary<int,Transform>();
        Transform fishReceipt; FishCatch presentedCatch; float receiptScale,receiptYaw;
        Vector3 receiptRestShift;
        public bool FishingFocused { get; private set; }
        int focusedFishingSpot;
        float fishingFramingY=.5f;
        Vector3 fishingCameraAnchor;
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
                Box(timber,"Fishing seat",V(.62f,.72f,.10f),V(.70f,.11f,.53f),Honey);
                for(int side=-1;side<=1;side+=2) Box(timber,"Fishing seat foot",V(.62f+side*.23f,.53f,.10f),V(.09f,.38f,.40f),Wood);
                var sign=Group(timber,"Fish carved sign",V(.94f,.1f,-.5f));
                Tube(sign,"Sign stake",Vector3.zero,V(0,1.15f,0),.07f,Wood); Box(sign,"Fishing sign board",V(0,1.02f,-.03f),V(.72f,.45f,.10f),Deep);
                Ball(sign,"Carved fish body",V(.03f,1.02f,-.10f),V(.35f,.18f,.035f),Cream);
                Shape(sign,"Carved fish tail",OakLeaf(),V(-.19f,1.02f,-.11f),V(.18f,.22f,.04f),Gold,V(90,0,90));
                Combine(timber);
                BuildFishingShore(dock,i);
                var rod=fishingRods[i]=Group(dock,"Reeling reed rod",V(-.66f,.4f,-.6f));
                Shape(rod,"Curved flexible reed",Sweep(new[]{Vector3.zero,V(.03f,.8f,.35f),V(.34f,1.7f,.69f),V(.76f,2.05f,1.05f)},new[]{.048f,.035f,.022f,.012f}),Vector3.zero,Vector3.one,Honey);
                Tube(rod,"Rod cork grip",Vector3.zero,V(.01f,.42f,.15f),.067f,DarkWood);
                Ring(rod,"Small fishing reel",V(.04f,.36f,.11f),.11f,.027f,Gold,V(75,0,0));Combine(rod);
                fishingRodTips[i]=Group(rod,"Actual line guide",V(.76f,2.05f,1.05f));
                var handle=fishingReelHandles[i]=Group(rod,"Turning reel handle",V(.04f,.36f,.11f));
                Tube(handle,"Reel crank",Vector3.zero,V(.13f,0,0),.018f,Cream);Ball(handle,"Reel handle grip",V(.13f,0,0),V(.07f,.10f,.07f),DarkWood);Combine(handle);
                fishingBaskets[i]=BuildCatchBasket(dock);
                for(int bait=0;bait<3;bait++) { var model=fishingBaits[i*3+bait]=ForestFishingModels.Bait(dock,bait); model.localPosition=V(.55f,.40f,.65f); model.localScale=Vector3.one*.8f; model.gameObject.SetActive(bait==0); }
                var bobber=fishingFloats[i]=Group(dock,"Float at water contact",V(.2f,.19f,2.4f));
                var body=fishingFloatBodies[i]=Group(bobber,"Cream and berry float");
                Ball(body,"Float round body",Vector3.zero,V(.16f,.22f,.16f),Cream);
                Ball(body,"Float red cap",V(0,.07f,0),V(.145f,.11f,.145f),Hex(0xBF7769));
                Tube(body,"Float stem",V(0,.04f,0),V(0,.28f,0),.017f,Honey); Ball(body,"Float golden tip",V(0,.29f,0),Vector3.one*.06f,Gold); Combine(body);
                for(int ring=0;ring<3;ring++) fishingRings[i*3+ring]=Ring(dock,"Water ripple "+ring,V(.2f,.235f,2.4f),.22f+ring*.16f,.014f,Hex(0xCAE1D0));
                for(int drop=0;drop<6;drop++){var d=fishingDrops[i*6+drop]=Ball(dock,"Short landing splash",Vector3.zero,V(.035f,.08f,.035f),Hex(0xB6D8CD));d.gameObject.SetActive(false);}
                var line=Group(dock,"Connected rod line").gameObject.AddComponent<LineRenderer>(); fishingLines[i]=line;
                line.useWorldSpace=false; line.positionCount=13; line.startWidth=line.endWidth=.022f; line.sharedMaterial=Mat(Cream,true); line.alignment=LineAlignment.View; line.generateLightingData=true; line.shadowCastingMode=ShadowCastingMode.Off;
                Hotspot(dock,"fishing",i,V(0,.8f,.2f),V(2.5f,2.4f,2.5f)); Hotspot(dock,"fishing",i,V(.2f,.3f,2.4f),V(1.0f,1.2f,1.0f));
            }
        }
        public void FocusFishing(int spot,float viewportY=.5f)
        {
            if(spot<0 || spot>2) return;
            if(!FishingFocused || focusedFishingSpot!=spot)ClearFishingReceipt();
            FishingFocused=true;focusedFishingSpot=spot;
            cameraFocus=fishingDocks[spot].TransformPoint(V(0,.6f,1.4f));
            worldCamera.orthographicSize=5.2f; worldCamera.transform.position=cameraFocus+V(.5f,18,-16); worldCamera.transform.LookAt(cameraFocus);
            fishingFramingY=Mathf.Clamp(viewportY,.5f,.82f);
            var framing=-worldCamera.transform.up*((fishingFramingY-.5f)*worldCamera.orthographicSize*2);
            cameraFocus+=framing;worldCamera.transform.position+=framing;fishingCameraAnchor=cameraFocus-transform.position;
        }
        public void LeaveFishingFocus(){if(!FishingFocused)return;FishingFocused=false;if(Region>=4 && Region<=6)SetRegion(Region);}
        Vector2 FishingMarker(int spot)
        { var p=worldCamera.WorldToViewportPoint(fishingDocks[spot].TransformPoint(V(0,.15f,spot==2?-4:spot==1?2:-.95f))); return new Vector2(p.x,p.y); }
        public void PresentFishCatch(FishCatch caught)
        {
            ClearFishingReceipt(); int key=caught.species*4+caught.quality;
            if(!fishReceipts.TryGetValue(key,out fishReceipt))
            { fishReceipt=ForestFishingModels.Fish(worldRoot,caught.species,caught.quality); fishReceipts[key]=fishReceipt; }
            presentedCatch=caught;
            var vertices=new List<Vector3>();Bounds shapeBounds=new Bounds();bool first=true;
            foreach(var r in fishReceipt.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!r.enabled)continue;var filter=r.GetComponent<MeshFilter>();if(filter==null || filter.sharedMesh==null)continue;
                var relative=fishReceipt.worldToLocalMatrix*r.localToWorldMatrix;
                foreach(var vertex in filter.sharedMesh.vertices){var p=relative.MultiplyPoint3x4(vertex);vertices.Add(p);if(first){shapeBounds=new Bounds(p,Vector3.zero);first=false;}else shapeBounds.Encapsulate(p);}
            }
            receiptScale=1.22f/Mathf.Max(.1f,shapeBounds.size.x);fishReceipt.localScale=Vector3.one*receiptScale;
            var resting=Quaternion.Euler(70,0,0);Bounds restBounds=new Bounds();first=true;
            foreach(var vertex in vertices){var p=(resting*vertex)*receiptScale;if(first){restBounds=new Bounds(p,Vector3.zero);first=false;}else restBounds.Encapsulate(p);}
            receiptRestShift=V(-restBounds.center.x,.105f-restBounds.min.y,-restBounds.center.z);
            float yawDelta=Mathf.DeltaAngle(worldCamera.transform.eulerAngles.y,fishingDocks[caught.spot].eulerAngles.y);if(yawDelta>90)yawDelta-=180;if(yawDelta< -90)yawDelta+=180;
            receiptYaw=worldCamera.transform.eulerAngles.y+yawDelta;fishReceipt.gameObject.SetActive(true);
        }
        void ClearFishingReceipt() { if(fishReceipt!=null) fishReceipt.gameObject.SetActive(false); presentedCatch=null; }
        void UpdateFishingWorld()
        {
            if(fishingDocks[0]==null || state==null) return;
            var f=state.fishing;
            bool showing=fishReceipt!=null && fishReceipt.gameObject.activeSelf && presentedCatch!=null && f.running && f.phase==3 && !state.report.pending && worldCamera.enabled;
            if(fishReceipt!=null && fishReceipt.gameObject.activeSelf && !showing)ClearFishingReceipt();
            float caughtProgress=f.phase==3?Mathf.Clamp01(1-(float)(f.remaining/f.duration)):0;
            if(showing)
            {
                int spot=presentedCatch.spot;var dock=fishingDocks[spot];var basket=fishingBaskets[spot];
                float travel=Mathf.SmoothStep(0,1,Mathf.Clamp01(caughtProgress/.66f));
                var from=dock.TransformPoint(V(.2f,.18f,1.88f));var into=basket.position+Quaternion.Euler(0,receiptYaw,0)*receiptRestShift;
                fishReceipt.position=state.simpleAnimation?into:Vector3.Lerp(from,into,travel)+Vector3.up*(Mathf.Sin(travel*Mathf.PI)*1.28f);
                float tilt=state.simpleAnimation?0:Mathf.Lerp(66,0,Mathf.SmoothStep(0,1,Mathf.Clamp01((caughtProgress-.27f)/.33f)));
                float roll=state.simpleAnimation?70:Mathf.Lerp(0,70,Mathf.SmoothStep(0,1,Mathf.Clamp01((caughtProgress-.35f)/.25f)));
                float settle=state.simpleAnimation?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((caughtProgress-.35f)/.25f));
                fishReceipt.rotation=Quaternion.Euler(roll,Mathf.LerpAngle(worldCamera.transform.eulerAngles.y,receiptYaw,settle),tilt);
                fishReceipt.localScale=Vector3.one*receiptScale;
            }
            for(int i=0;i<3;i++)
            {
                bool active=f.running && f.spot==i && !state.report.pending;
                bool motion=active && !state.simpleAnimation && (f.phase!=3 || showing);
                for(int bait=0;bait<3;bait++) fishingBaits[i*3+bait].gameObject.SetActive(bait==(f.spot==i?f.bait:0));
                float progress=active && f.duration>0?Mathf.Clamp01(1-(float)(f.remaining/f.duration)):0;
                float reel=motion && f.phase==2?Mathf.SmoothStep(0,1,Mathf.Clamp01((progress-.20f)/.80f)):0;
                float cast=motion && f.phase==1?Mathf.Clamp01((float)(f.duration-f.remaining)/.42f):1;
                float angle=motion?(f.phase==2?Mathf.Lerp(7,-30,reel):f.phase==3?Mathf.Lerp(-30,0,Mathf.SmoothStep(0,1,caughtProgress)):Mathf.Lerp(-19,0,cast)):0;
                fishingRods[i].localRotation=Quaternion.Euler(angle,0,0);
                fishingReelHandles[i].localEulerAngles=V(0,0,motion && f.phase==2?progress*1080:0);
                Vector3 floatAt=V(.2f,.19f,2.4f);
                if(motion && f.phase==2)floatAt=Vector3.Lerp(floatAt,V(.2f,.20f,1.88f),reel);
                if(motion && f.phase==1 && cast<1)floatAt=Vector3.Lerp(V(.2f,1.1f,.7f),floatAt,cast)+Vector3.up*(Mathf.Sin(cast*Mathf.PI)*.55f);
                fishingFloats[i].localPosition=floatAt;
                float movement=motion && f.phase==1 && cast>=1?Mathf.Sin(Time.unscaledTime*2)*.025f:0;
                fishingFloatBodies[i].localPosition=V(0,(active && f.phase==2?-.13f:0)+movement,0);
                var end=floatAt+fishingFloatBodies[i].localPosition+V(0,.28f,0);
                bool attached=showing && presentedCatch.spot==i && caughtProgress<.67f && !state.simpleAnimation;
                if(attached)end=fishingDocks[i].InverseTransformPoint(fishReceipt.TransformPoint(ForestFishingModels.MouthPosition(presentedCatch.species)));
                fishingLines[i].enabled=!(motion && f.phase==3 && caughtProgress>=.67f);
                var start=fishingDocks[i].InverseTransformPoint(fishingRodTips[i].position);
                fishingFloats[i].gameObject.SetActive(!(motion && f.phase==3 && caughtProgress>=.67f));
                if(attached){float t=.72f;var middle=(start+end)*.5f-V(0,.035f,0);fishingFloats[i].localPosition=(1-t)*(1-t)*start+2*(1-t)*t*middle+t*t*end-V(0,.28f,0);fishingFloatBodies[i].localPosition=Vector3.zero;}
                for(int j=0;j<13;j++)
                {
                    float t=j/12f;float slack=active && (f.phase==2||attached)?.035f:.40f;var middle=(start+end)*.5f-V(0,slack,0);
                    fishingLines[i].SetPosition(j,(1-t)*(1-t)*start+2*(1-t)*t*middle+t*t*end);
                }
                for(int j=0;j<3;j++)
                {
                    var ring=fishingRings[i*3+j];ring.gameObject.SetActive(motion && (f.phase==1 && cast>=1||f.phase==2||f.phase==3 && caughtProgress<.24f));
                    ring.localPosition=V(floatAt.x,.235f,f.phase==3?1.88f:floatAt.z);ring.localScale=Vector3.one*(1+(Mathf.Sin(Time.unscaledTime*(f.phase==2?3:1.2f)-j)+1)*.16f);
                }
                for(int j=0;j<6;j++)
                {
                    var drop=fishingDrops[i*6+j];bool splash=motion && f.phase==3 && showing && caughtProgress<.22f;drop.gameObject.SetActive(splash);
                    if(splash){float t=caughtProgress/.22f,a=j*Mathf.PI/3;drop.localPosition=V(.2f+Mathf.Cos(a)*(.08f+t*.40f),.23f+Mathf.Sin(t*Mathf.PI)*(.25f+j%2*.12f),1.88f+Mathf.Sin(a)*(.08f+t*.32f));drop.localScale=V(.035f,.08f*(1-t*.65f),.035f);}
                }
            }
        }
    }
}
