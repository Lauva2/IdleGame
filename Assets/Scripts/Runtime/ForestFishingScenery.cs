using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        Material quietWaterMaterial;
        Mesh ShapedPool(float radius,float height)
        {
            var mesh=Lathe(new[]{new Vector2(0,0),new Vector2(radius,0),new Vector2(radius,height),new Vector2(0,height)},64);
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++){var p=vertices[i];float a=Mathf.Atan2(p.z,p.x),s=1+.075f*Mathf.Sin(a*3+.6f)+.035f*Mathf.Cos(a*5);vertices[i]=V(p.x*s,p.y,p.z*s);}
            mesh.vertices=vertices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        void DeepWater(Transform parent)
        {
            if(quietWaterMaterial==null)quietWaterMaterial=new Material(Resources.Load<Shader>("ForestWater")){name="Painted quiet creek depth",color=Color.white};
            // Use the shared geometry owner, then paint a continuous radial depth
            // gradient; the creek edge has no separate hard dark centre disc.
            var profile=new Vector2[7];for(int i=0;i<7;i++)profile[i]=new Vector2((6-i)*1.98f/6,0);
            var mesh=Lathe(profile,64);var vertices=mesh.vertices;var colors=new Color[vertices.Length];
            for(int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];float t=Mathf.Clamp01(new Vector2(p.x,p.z).magnitude/1.98f),a=Mathf.Atan2(p.z,p.x),s=1+.075f*Mathf.Sin(a*3+.6f)+.035f*Mathf.Cos(a*5);
                vertices[i]=V(p.x*s,p.y,p.z*s);colors[i]=Color.Lerp(Hex(0x477571),Hex(0x73AAA7),Mathf.SmoothStep(0,1,t));
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateNormals();mesh.RecalculateBounds();
            var water=Shape(parent,"Soft painted deep-water hollow",mesh,V(.15f,.192f,2.28f),V(1.02f,1,.72f),Color.white);
            water.GetComponent<MeshRenderer>().sharedMaterial=quietWaterMaterial;
        }
        Transform BuildCatchBasket(Transform dock)
        {
            var p=Group(dock,"Actual catch basket",V(-.24f,.40f,-.50f));
            Shape(p,"Open woven catch basket",Lathe(new[]{new Vector2(0,0),new Vector2(.43f,0),new Vector2(.57f,.30f),new Vector2(.58f,.34f),new Vector2(.51f,.34f),new Vector2(.38f,.07f),new Vector2(0,.07f)},32),Vector3.zero,V(1.2f,1,.8f),Honey);
            Ring(p,"Rounded basket lip",V(0,.34f,0),.55f,.025f,Cream).localScale=V(1.2f,1,.8f);
            for(int i=0;i<14;i++)
            {float a=i*Mathf.PI*2/14;Tube(p,"Woven basket upright",V(Mathf.Cos(a)*.44f*1.2f,.02f,Mathf.Sin(a)*.44f*.8f),V(Mathf.Cos(a)*.57f*1.2f,.30f,Mathf.Sin(a)*.57f*.8f),.012f,Wood);}
            Ball(p,"Soft linen inside basket",V(0,.08f,0),V(1.03f,.026f,.66f),Cream);Combine(p);return p;
        }
        void BuildFishingShore(Transform dock,int spot)
        {
            var p=Group(dock,"Distinct habitat landmark");
            if(spot==0)
            {
                Shape(p,"Shallow clear sandy water",Lathe(new[]{new Vector2(0,0),new Vector2(1.68f,0),new Vector2(1.68f,.008f),new Vector2(0,.008f)},56),V(.4f,.188f,2.35f),V(1.35f,1,.65f),Hex(0x91BDB0));
                for(int i=0;i<11;i++)
                {
                    float a=i*2.4f;var at=V(.4f+Mathf.Cos(a)*(1+i%3*.2f),.208f,2.3f+Mathf.Sin(a)*.75f);
                    Ball(p,"Flat pale stream pebble",at,V(.24f+i%2*.13f,.035f,.17f),i%3==0?Hex(0xC7C8A0):Hex(0xACC2AA),V(0,i*47,0));
                }
                for(int side=-1;side<=1;side+=2)
                {
                    var at=V(side*1.72f,.20f,2.45f);Reeds(p,at,false);
                    ForestModels.Flower(p,at+V(.18f,.05f,-.3f),.17f,Hex(0xEEE0A9));
                }
                Box(p,"Worn pale stepping stone",V(-1.0f,.19f,-.95f),V(.60f,.15f,.46f),Hex(0xC4C4A0),V(0,28,0));
            }
            else if(spot==1)
            {
                Shape(p,"Irregular moss rim of deep hollow",ShapedPool(2.15f,.10f),V(.15f,.06f,2.30f),V(1.04f,1,.78f),Hex(0x8C9C83));
                DeepWater(p);
                for(int i=0;i<5;i++)
                {
                    var at=V(-1.85f+i*.72f,.20f,3.25f+Mathf.Sin(i*1.8f)*.20f);
                    Ball(p,"Dark moss bank rock",at,V(.65f,.38f,.48f),Hex(0x778D75),V(0,i*39,0));
                    Ball(p,"Soft moss on bank rock",at+V(0,.17f,0),V(.47f,.06f,.31f),Moss);
                }
                Reeds(p,V(-1.60f,.2f,1.5f),true);Reeds(p,V(1.65f,.2f,2.1f),true);
                Box(p,"Bridge pool quiet reflection",V(-.8f,.215f,2.5f),V(.95f,.015f,.035f),Hex(0x8CB2A6),V(0,-12,0));
                Tube(p,"Old waterside mooring stump",V(1.25f,.1f,-.7f),V(1.25f,.63f,-.7f),.17f,DarkWood);
            }
            else
            {
                var table=Group(p,"Reading pool small side stand",V(1.25f,.15f,-.62f));
                Box(table,"Weathered reading shelf",V(0,.64f,0),V(.95f,.10f,.63f),Wood);
                for(int side=-1;side<=1;side+=2)Box(table,"Reading shelf foot",V(side*.31f,.32f,0),V(.10f,.64f,.4f),DarkWood);
                Box(table,"Closed forest field notebook",V(-.15f,.73f,0),V(.48f,.07f,.37f),Deep,V(0,-13,0));
                Box(table,"Cream notebook page edge",V(-.15f,.715f,-.02f),V(.43f,.045f,.34f),Cream,V(0,-13,0));
                Shape(table,"Small waterside teacup",Lathe(new[]{new Vector2(0,0),new Vector2(.11f,0),new Vector2(.14f,.17f),new Vector2(.12f,.18f),new Vector2(.08f,.07f),new Vector2(0,.07f)}),V(.23f,.69f,.03f),Vector3.one,Cream);
                Ring(table,"Teacup handle",V(.37f,.78f,.03f),.065f,.017f,Honey,V(90,0,0));
                Reeds(p,V(-1.65f,.2f,2.1f),false);
                for(int i=0;i<3;i++){var at=V(1.5f+i*.35f,.2f,2.5f+i*.30f);Ball(p,"Pavilion pool lily leaf",at,V(.58f,.02f,.43f),Leaf,V(0,i*49,0));ForestModels.Flower(p,at+V(0,.07f,0),.13f,i==1?Hex(0xDCA9A0):Cream);}
            }
            Combine(p);
        }
        static void Reeds(Transform parent,Vector3 at,bool tall)
        {
            for(int i=0;i<5;i++)
            {
                float height=(tall?.75f:.48f)+(i%3)*.12f;var root=at+V((i%3-1)*.13f,0,(i/3-.5f)*.14f);
                Shape(parent,"Curved bank reed",Sweep(new[]{root,root+V(.02f,height*.65f,.02f),root+V((i-2)*.045f,height,.11f)},new[]{.025f,.024f,.006f},.7f),Vector3.zero,Vector3.one,i%2==0?Leaf:Moss);
                if(tall && i%2==0)Ball(parent,"Soft reed seed head",root+V((i-2)*.045f,height,.11f),V(.075f,.23f,.075f),Honey);
            }
        }
    }
}
