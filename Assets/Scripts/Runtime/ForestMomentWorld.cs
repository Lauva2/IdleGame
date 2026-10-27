using ForestShop.Core;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        readonly Transform[] momentClues = new Transform[3], momentObjects = new Transform[3], momentKeepsakes = new Transform[6], momentHalos = new Transform[3];
        readonly Collider[] momentFirstTouches = new Collider[3], momentObjectTouches = new Collider[3];
        readonly float[] momentRevealAt = { -10, -10, -10 };
        static readonly Vector3[] MomentFirstOffsets={ V(2.3f,.17f,-2.5f), V(3.8f,.17f,-3.2f), V(-.8f,.17f,-3.8f) };
        static readonly Vector3[] MomentObjectOffsets={ V(4.2f,.17f,-.6f), V(4.6f,.17f,-.7f), V(1.7f,.17f,-2.7f) };
        static bool TreeCoversMoment(Vector3 tree)
        {
            for(int id=0;id<3;id++)foreach(var offset in new[]{MomentFirstOffsets[id],MomentObjectOffsets[id]})
            {
                var at=ForestCenters[id]+offset;
                if(Mathf.Abs(tree.x-at.x)<3 && tree.z>at.z-6 && tree.z<at.z+3)return true;
            }
            return false;
        }
        void BuildMoments()
        {
            for (int id = 0; id < 3; id++)
            {
                var clue = momentClues[id] = ForestMomentModels.Clue(forestScene, id); clue.localPosition = ForestCenters[id] + MomentFirstOffsets[id];
                Hotspot(clue,"forest-moment",id*2,V(0,.7f,0),V(2,1.8f,2));momentFirstTouches[id]=clue.GetComponentInChildren<Collider>();
                var found = momentObjects[id] = ForestMomentModels.Object(forestScene,id);found.localPosition=ForestCenters[id]+MomentObjectOffsets[id];
                Hotspot(found,"forest-moment",id*2+1,V(0,.55f,0),V(2.1f,1.6f,1.9f));momentObjectTouches[id]=found.GetComponentInChildren<Collider>();
                for(int choice=0;choice<2;choice++)
                {
                    var m=momentKeepsakes[id*2+choice]=ForestMomentModels.Keepsake(forestScene,id,choice);
                    m.localPosition=id==2?found.localPosition:choice==0?clue.localPosition+V(1.4f,0,-.3f):found.localPosition;
                    Hotspot(m,"forest-moment",id*2+1,V(0,1,0),V(2.1f,2.5f,1.9f));m.gameObject.SetActive(false);
                }
                momentHalos[id]=Ring(forestScene,"Quiet keepsake cue " + id,Vector3.zero,.82f,.025f,Gold);
                clue.gameObject.SetActive(false);found.gameObject.SetActive(false);momentHalos[id].gameObject.SetActive(false);
            }
        }
        Transform MomentTarget(int id)
        {
            int stage=state.moments.stages[id];
            return stage==0?momentClues[id]:stage<3?momentObjects[id]:momentKeepsakes[id*2+state.moments.choices[id]];
        }
        public Vector2 MomentMarker(int id)
        {
            if(state==null || !ForestMomentRules.Unlocked(state,id))return new Vector2(-10,-10);
            // The shared viewport layout places the badge beside this real source.
            var at=worldCamera.WorldToViewportPoint(MomentTarget(id).position+V(0,.6f,0));
            return at.z<=0?new Vector2(-10,-10):new Vector2(at.x,at.y);
        }
        public Rect MomentMarkerFootprint(int id)
        {
            var min=Vector2.one*float.PositiveInfinity;var max=-Vector2.one*float.PositiveInfinity;
            foreach(var renderer in MomentTarget(id).GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.enabled)continue;var b=renderer.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    var p=worldCamera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,V(x,y,z)));
                    min=Vector2.Min(min,new Vector2(p.x,p.y));max=Vector2.Max(max,new Vector2(p.x,p.y));
                }
            }
            return float.IsInfinity(min.x)?new Rect(-10,-10,0,0):Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        public void FocusMoment(int id)
        {
            if(state==null || !ForestMomentRules.Unlocked(state,id))return;
            var focus=MomentTarget(id).position+V(0,.75f,0);var move=focus-cameraFocus;
            cameraFocus=focus;worldCamera.transform.position+=move;worldCamera.orthographicSize=6.6f;
            compositionDirty=true;RefreshCameraComposition();
        }
        public void PresentMoment(int id)
        { if(state!=null && id>=0 && id<3 && state.moments.stages[id]==3)momentRevealAt[id]=Time.unscaledTime; }
        void BindMoments()
        {
            if(state==null || momentClues[0]==null)return;
            for(int id=0;id<3;id++)
            {
                bool available=ForestMomentRules.Unlocked(state,id),here=available && Region==id+4 && state.forest.location==id;
                int stage=state.moments.stages[id],choice=state.moments.choices[id];
                momentClues[id].gameObject.SetActive(available);momentFirstTouches[id].enabled=here && stage==0;
                momentObjects[id].gameObject.SetActive(available && stage>0 && stage<3);momentObjectTouches[id].enabled=here;
                for(int c=0;c<2;c++)
                {
                    var keepsake=momentKeepsakes[id*2+c];keepsake.gameObject.SetActive(available && stage==3 && choice==c);
                    keepsake.localScale=Vector3.one*(state.simpleAnimation?1:Mathf.Lerp(.84f,1,Mathf.SmoothStep(0,1,(Time.unscaledTime-momentRevealAt[id])/.45f)));
                }
                var halo=momentHalos[id];halo.gameObject.SetActive(here && !FishingFocused && stage<3);
                if(available){halo.position=MomentTarget(id).position+V(0,.035f,0);halo.localScale=Vector3.one*(state.simpleAnimation?1:1+Mathf.Sin(Time.unscaledTime*2)*.035f);}
            }
        }
    }
}
