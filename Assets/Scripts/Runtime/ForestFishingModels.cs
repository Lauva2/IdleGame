using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // Original fish sculptures; shape, fins, whiskers and painted markings are
    // authored here and share the woodland's matte material and portrait light.
    public static class ForestFishingModels
    {
        static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        static readonly Dictionary<int,Mesh> fins = new Dictionary<int,Mesh>();
        static readonly uint[] BodyColors = {0x8DB8A1,0xBCC9B9,0x8F9663,0xB7A57C,0xCDA6A0,0x6D997D,0xCB9A66,0x92B3A4,0xA7BBC5,0xD0B572,0x708F99,0xB0C8C9};
        static readonly uint[] FinColors = {0xD4C88F,0x88ABA7,0xC5B67A,0x947852,0xBB7D79,0xA8B48B,0xBA7565,0xC79E7F,0xD4CFAC,0xB98952,0x597E8A,0xDCC58D};
        static Mesh Fin(int kind)
        {
            if(fins.TryGetValue(kind,out var result)) return result;
            Vector2[] outline = kind==0 ? new[]{new Vector2(0,0),new Vector2(-.48f,.35f),new Vector2(-.34f,0),new Vector2(-.48f,-.35f)}
                :kind==1 ? new[]{new Vector2(-.4f,0),new Vector2(-.03f,.38f),new Vector2(.36f,0)}
                :kind==2 ? new[]{new Vector2(0,0),new Vector2(-.24f,.30f),new Vector2(-.5f,.35f),new Vector2(-.58f,0),new Vector2(-.5f,-.35f),new Vector2(-.24f,-.30f)}
                :new[]{new Vector2(-.18f,0),new Vector2(0,-.30f),new Vector2(.28f,0)};
            float area=0; for(int i=0;i<outline.Length;i++) { var a=outline[i]; var b=outline[(i+1)%outline.Length]; area+=a.x*b.y-a.y*b.x; } if(area<0) System.Array.Reverse(outline);
            int count=outline.Length; var vertices=new Vector3[count*2]; var colors=new Color[count*2]; var triangles=new List<int>();
            for(int side=0;side<2;side++) for(int i=0;i<count;i++) { vertices[side*count+i]=V(outline[i].x,outline[i].y,side==0?-.025f:.025f); colors[side*count+i]=Color.white; }
            for(int i=1;i<count-1;i++) { triangles.Add(0);triangles.Add(i+1);triangles.Add(i);triangles.Add(count);triangles.Add(count+i);triangles.Add(count+i+1); }
            for(int i=0;i<count;i++) { int next=(i+1)%count; triangles.Add(i);triangles.Add(next);triangles.Add(count+next);triangles.Add(i);triangles.Add(count+next);triangles.Add(count+i); }
            result=new Mesh{name="Original fish fin "+kind}; result.vertices=vertices; result.triangles=triangles.ToArray(); result.colors=colors; result.RecalculateNormals();result.RecalculateBounds();fins[kind]=result;return result;
        }
        public static Transform Fish(Transform parent,int species,int quality=0)
        {
            var kind=FishingCatalog.Species[species]; int form=kind.Form;
            var p=Group(parent,kind.Name+" · "+FishingCatalog.Qualities[Mathf.Clamp(quality,0,3)]);
            Color body=Color.Lerp(Hex(BodyColors[species]),Cream,quality*.025f), fin=Hex(FinColors[species]);
            float length=form==2?1.7f:form==3?1.6f:form==4?1.1f:1.35f;
            float height=form==1?.74f:form==2?.25f:form==4?.64f:.46f, depth=form==2?.27f:form==1?.42f:.36f;
            if(species==4) { length=1.13f;height=.86f;depth=.32f; }
            if(species==6) { length=1.65f;height=.62f;depth=.39f; }
            if(species==8) { length=1.20f;height=.98f;depth=.34f; }
            if(species==9) { length=1.70f;height=.56f;depth=.37f; }
            if(form==2)
                Shape(p,"Gently curved loach body",Sweep(new[]{V(-.78f,.02f,0),V(-.30f,.10f,.02f),V(.25f,.08f,0),V(.73f,.1f,0)},new[]{.055f,.13f,.14f,.08f},1),Vector3.zero,Vector3.one,body);
            else Ball(p,"Rounded fish body",V(0,.10f,0),V(length,height,depth),body);
            Ball(p,"Soft cream belly",V(.02f,.10f-height*.18f,0),V(length*.77f,height*.60f,depth*.80f),Color.Lerp(body,Cream,.48f));
            float tailWidth=species==4?.63f:species==8?1.13f:form==4?1.28f:.9f,tailHeight=species==4?.67f:species==8?.86f:form==2?.6f:form==4?1.22f:1;
            var tail=Shape(p,"Shaped tail fin",Fin(form==1||form==4?2:0),V(-length*.42f,.1f,0),V(tailWidth,tailHeight,1),fin);
            tail.localEulerAngles=V(0,0,form==4?-8:0);
            float dorsalWidth=species==6?1.35f:species==4?.50f:form==2?.7f:1, dorsalHeight=species==8?1.1f:species==4?.48f:form==2?.38f:form==4?1.12f:.70f;
            Shape(p,"Dorsal sail",Fin(1),V(-.08f,.1f+height*.40f,0),V(dorsalWidth,dorsalHeight,.85f),fin);
            for(int ray=0;ray<4;ray++)
            {
                float y=(ray-1.5f)*.14f*tailHeight;
                Tube(p,"Fine tail fin ray",V(-length*.42f-.08f,.1f, -.03f),V(-length*.42f-.39f*tailWidth,.1f+y,-.03f),.006f,Color.Lerp(fin,Cream,.32f));
            }
            for(int side=-1;side<=1;side+=2)
            {
                Shape(p,"Side swimming fin",Fin(3),V(.04f,.06f,side*depth*.44f),V(.70f,.72f,1),fin,V(side*32,side*-12,0));
                Ball(p,"Gentle dark eye",V(length*.32f,.1f+height*.18f,side*depth*.38f),V(.095f,.095f,.055f),Ink);
                Ball(p,"Eye catch light",V(length*.32f+.014f,.1f+height*.18f+.022f,side*(depth*.38f+.025f)),V(.028f,.028f,.012f),Cream);
                Tube(p,"Curved gill mark",V(length*.22f,.1f+height*.21f,side*depth*.47f),V(length*.19f,.1f-height*.19f,side*depth*.46f),.012f,Color.Lerp(body,Deep,.38f));
            }
            float snout=form==2?.73f:length*.49f;
            Ring(p,"Small rounded mouth",V(snout,.09f,0),form==3?.075f:.045f,.013f,Color.Lerp(fin,Cream,.35f),V(0,0,90));
            if(form==2 || form==3 || species==6 || species==9) for(int side=-1;side<=1;side+=2)
            {
                float reach=form==3?.28f:form==2?.18f:.11f;
                Tube(p,"Soft whisker",V(snout-.04f,.025f,side*.08f),V(snout+reach,-.06f,side*.22f),.012f,Cream);
                Tube(p,"Whisker tip",V(snout+reach,-.06f,side*.22f),V(snout+reach+.05f,-.14f,side*.25f),.009f,Cream,.55f);
            }
            for(int side=-1;side<=1;side+=2) for(int i=0;i<(species==7?11:6);i++)
            {
                float x=-.35f+i%4*.17f, y=.1f+(i/4-.4f)*height*.34f;
                if(species==4) {x=-.10f+(i%2)*.08f;y=.10f+(i/2-1)*.10f;}
                if(species==8) {x=-.30f+i*.07f;y=.22f-Mathf.Sin(i*.5f)*.23f;}
                Color mark=species==7?Hex(0x987B63):species==4?Color.Lerp(body,Deep,.38f):species==11?Cream:Color.Lerp(body,Cream,.32f);
                Ball(p,"Painted flank scale",V(x,y,side*depth*.48f),V(species==7?.05f:.075f,species==7?.045f:.035f,.017f),mark,V(0,0,-20));
            }
            if(species==1 || species==5 || species==9)
                for(int side=-1;side<=1;side+=2) Tube(p,"Long painted side stripe",V(-.40f,.15f,side*depth*.46f),V(.27f,.15f,side*depth*.46f),.016f,species==9?Gold:Color.Lerp(body,Deep,.3f));
            if(quality==3) for(int side=-1;side<=1;side+=2) Ball(p,"Pearl quality glint",V(-.13f,.23f,side*depth*.48f),V(.06f,.06f,.02f),Gold);
            Combine(p);return p;
        }
        public static Transform Bait(Transform parent,int bait)
        {
            var p=Group(parent,FishingCatalog.Baits[bait]);
            Shape(p,"Small bait bowl",Lathe(new[]{new Vector2(0,0),new Vector2(.23f,0),new Vector2(.33f,.18f),new Vector2(.29f,.21f),new Vector2(0,.14f)}),Vector3.zero,Vector3.one,Wood);
            if(bait==0) for(int i=0;i<9;i++) Ball(p,"Warm grain",V(Mathf.Sin(i*2.4f)*.17f,.21f+i%3*.025f,Mathf.Cos(i*2.4f)*.16f),V(.065f,.07f,.11f),i%2==0?Cream:Gold,V(0,i*37,0));
            else if(bait==1)
            {
                for(int i=0;i<7;i++) Ball(p,"Curled soft worm",V(Mathf.Sin(i*.65f)*.17f,.22f,Mathf.Cos(i*.65f)*.15f),V(.08f,.08f,.08f),Honey);
                Ball(p,"Worm head",V(.01f,.23f,.16f),V(.10f,.10f,.10f),Copper);
            }
            else
            {
                for(int i=0;i<4;i++) Ball(p,"Berry bait",V((i%2-.5f)*.15f,.24f+i/2*.04f,(i/2-.5f)*.14f),Vector3.one*.16f,Hex(0xB46A68));
                Ball(p,"Berry bait leaf",V(.06f,.32f,.01f),V(.22f,.025f,.10f),Leaf,V(0,30,0));
            }
            Combine(p);return p;
        }
        public static void Release() { foreach(var mesh in fins.Values) if(mesh!=null) Object.Destroy(mesh); fins.Clear(); }
    }
}
