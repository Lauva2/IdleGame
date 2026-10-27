using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        sealed class CameraGrove
        {
            public MeshRenderer[] renderers;
            public Bounds bounds;
            public bool hidden;
        }
        readonly List<CameraGrove> cameraGroves=new List<CameraGrove>();
        Vector3 regionCameraHome;
        float regionCameraSize=22.5f;
        static readonly Vector2[] ClearingCameraHull={new Vector2(-11,10),new Vector2(-5,-12),new Vector2(6,-12),new Vector2(12,8),new Vector2(11,24),new Vector2(6,24)};
        bool shopCutaway,workshopCutaway;
        bool compositionDirty=true;
        void RegisterCameraGrove(Transform grove)
        {
            var renderers=grove.GetComponentsInChildren<MeshRenderer>();
            if(renderers.Length==0)return;
            var b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
            cameraGroves.Add(new CameraGrove{renderers=renderers,bounds=b});
        }
        void RememberRegionCamera()
        {
            regionCameraHome=CameraFocus;regionCameraSize=CameraSize;
            shopCutaway=Region==3;workshopCutaway=Region==2;
            compositionDirty=true;RefreshCameraComposition();
        }
        float MinimumCameraSize=>FishingFocused?3.8f:Region==7?4.5f:5;
        float MaximumCameraSize=>FishingFocused?10:Region==0?27:Region>=4 && Region<=6?18:Mathf.Max(11,regionCameraSize*1.55f);
        void ClampCompositionCamera()
        {
            var local=CameraFocus;var anchor=FishingFocused?fishingCameraAnchor:regionCameraHome;
            float rx,rz;
            if(FishingFocused){rx=rz=Mathf.Max(1.3f,6.5f-CameraSize*.45f);}
            else if(Region==0){rx=Mathf.Max(1.5f,28-CameraSize*.85f);rz=Mathf.Max(1.8f,30-CameraSize*.88f);}
            else if(Region>=4 && Region<=6){rx=rz=Mathf.Clamp(10-CameraSize*.6f,1.5f,4.8f);}
            else if(Region==7){rx=Mathf.Max(1,7-CameraSize*.5f);rz=Mathf.Max(1,7-CameraSize*.5f);}
            else{rx=Mathf.Max(1,10-CameraSize*.65f);rz=Mathf.Max(1,6.5f-CameraSize*.48f);}
            var bounded=new Vector3(Mathf.Clamp(local.x,anchor.x-rx,anchor.x+rx),local.y,Mathf.Clamp(local.z,anchor.z-rz,anchor.z+rz));
            if(!FishingFocused && Region==0)bounded=ClampClearingFocus(bounded);
            var shift=bounded-local;cameraFocus+=shift;worldCamera.transform.position+=shift;
            compositionDirty=true;RefreshCameraComposition();
        }
        void RefreshCameraComposition()
        {
            if(worldCamera==null || !compositionDirty)return;compositionDirty=false;
            var at=CameraFocus;
            // Hysteresis keeps roof switches stable while dragging near a room boundary.
            shopCutaway=Region==3 || Region==0 && RoomUnderCamera(shopArea,at,shopCutaway);
            workshopCutaway=Region==2 || Region==0 && RoomUnderCamera(workshopArea,at,workshopCutaway);
            if(roof!=null)roof.gameObject.SetActive(!shopCutaway);
            if(workshopRoof!=null)workshopRoof.gameObject.SetActive(!workshopCutaway);
            if(shopRoofCap!=null)shopRoofCap.gameObject.SetActive(shopCutaway);
            if(workshopRoofCap!=null)workshopRoofCap.gameObject.SetActive(workshopCutaway);
            if(workshopFrontBeam!=null)workshopFrontBeam.gameObject.SetActive(!workshopCutaway && Region==0);
            var point=worldCamera.WorldToViewportPoint(cameraFocus);
            foreach(var grove in cameraGroves)
            {
                bool hide=false;
                if(CameraSize<(grove.hidden?17:15) && grove.bounds.center.y>cameraFocus.y+.5f)
                {
                    Vector3 bmin=new Vector3(float.PositiveInfinity,float.PositiveInfinity,float.PositiveInfinity),bmax=-Vector3.one*float.PositiveInfinity;
                    var b=grove.bounds;
                    for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    {
                        var p=worldCamera.WorldToViewportPoint(b.center+Vector3.Scale(b.extents,new Vector3(x,y,z)));
                        bmin=Vector3.Min(bmin,p);bmax=Vector3.Max(bmax,p);
                    }
                    float pad=grove.hidden?.075f:.025f;
                    hide=bmin.z<point.z-.2f && bmin.x-pad<point.x && bmax.x+pad>point.x && bmin.y-pad<point.y && bmax.y+pad>point.y;
                }
                if(hide==grove.hidden)continue;
                grove.hidden=hide;
                foreach(var r in grove.renderers)if(r!=null)r.shadowCastingMode=hide?ShadowCastingMode.ShadowsOnly:ShadowCastingMode.On;
            }
        }
        bool RoomUnderCamera(Transform area,Vector3 at,bool wasOpen)
        {
            var d=at-(area.position-transform.position);
            float margin=wasOpen?1.5f:0;
            return CameraSize<(wasOpen?17:15) && Mathf.Abs(d.x)<8.5f+margin && d.z>-7-margin && d.z<7+margin;
        }
        Vector3 ClampClearingFocus(Vector3 at)
        {
            // A convex route envelope links the shop, workshop, beds and greenhouse.
            // Its corners sit on working areas, rather than the outer tree ring.
            float scale=Mathf.Clamp((27-CameraSize)/22,.08f,1);
            var p=new Vector2(at.x-regionCameraHome.x,at.z-regionCameraHome.z);
            bool inside=true;float best=float.PositiveInfinity;Vector2 nearest=p;
            for(int i=0;i<ClearingCameraHull.Length;i++)
            {
                var a=ClearingCameraHull[i]*scale;var b=ClearingCameraHull[(i+1)%ClearingCameraHull.Length]*scale;var edge=b-a;
                if(edge.x*(p.y-a.y)-edge.y*(p.x-a.x)<0)inside=false;
                var point=a+edge*Mathf.Clamp01(Vector2.Dot(p-a,edge)/edge.sqrMagnitude);float distance=(point-p).sqrMagnitude;
                if(distance<best){best=distance;nearest=point;}
            }
            if(!inside){at.x=regionCameraHome.x+nearest.x;at.z=regionCameraHome.z+nearest.y;}
            // Keep an actual working landmark comfortably inside a close frame.
            // A polygon alone still permits an empty wedge north of the workshop.
            var right=worldCamera.transform.right;var up=worldCamera.transform.up;
            float halfX=CameraSize*worldCamera.aspect*.6f,halfY=CameraSize*.65f;
            Vector3 bestShift=Vector3.zero;float distanceBest=float.PositiveInfinity;
            foreach(var landmark in new[]{ShopOrigin+new Vector3(.5f,5.4f,0),WorkshopOrigin+new Vector3(0,2.5f,0),GardenOrigin+new Vector3(0,.7f,0),GreenhouseOrigin+new Vector3(0,1.4f,0),new Vector3(-1,.6f,3.1f)})
            {
                var relative=landmark-at;float x=Vector3.Dot(relative,right),y=Vector3.Dot(relative,up);
                float sx=x-Mathf.Clamp(x,-halfX,halfX),sy=y-Mathf.Clamp(y,-halfY,halfY);
                float det=right.x*up.z-right.z*up.x;
                var shift=new Vector3((sx*up.z-right.z*sy)/det,0,(right.x*sy-sx*up.x)/det);
                if(shift.sqrMagnitude<distanceBest){distanceBest=shift.sqrMagnitude;bestShift=shift;}
            }
            return at+bestShift;
        }
    }
}
