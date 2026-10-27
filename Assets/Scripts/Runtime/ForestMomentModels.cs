using UnityEngine;
using UnityEngine.Rendering;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // Original meshes, using the existing woodland palette and rounded edges.
    public static class ForestMomentModels
    {
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);
        static Material glass;
        static Mesh star;
        static readonly Color Linen = Hex(0xC6B68C), Coral = Hex(0xB56B62);
        public static Transform Clue(Transform parent, int id)
        {
            var p = Group(parent, "Moment first clue " + id);
            if (id == 0)
            {
                Shape(p, "Rounded weathered tree stump", Lathe(new[] { new Vector2(0,0), new Vector2(.95f,0), new Vector2(.84f,.28f), new Vector2(.72f,1.6f), new Vector2(.64f,1.72f), new Vector2(0,1.72f) }, 24), Vector3.zero, V(1,1,.85f), Wood);
                Ball(p, "Stump top rings", V(0,1.68f,0), V(1.34f,.13f,1.1f), Honey);
                Ring(p, "Tree top age ring", V(0,1.76f,0), .4f,.027f,DarkWood);
                Ball(p, "Deep warm tree hollow", V(0,.88f,-.61f), V(1.1f,.98f,.18f), Hex(0x473D2B));
                Ring(p, "Rounded hollow lip", V(0,.88f,-.65f), .51f,.065f,Honey,V(90,0,0));
                for (int i = 0; i < 5; i++) Tube(p,"Soft bark groove",V(Mathf.Sin(i*1.25f)*.78f,.15f,Mathf.Cos(i*1.25f)*.64f),V(Mathf.Sin(i*1.25f)*.64f,1.4f,Mathf.Cos(i*1.25f)*.53f),.018f,DarkWood);
                Acorn(p,V(-.6f,.16f,-.95f),1.4f); Acorn(p,V(.45f,.13f,-1.03f),1);
                Ball(p,"Fern beside hollow",V(.9f,.37f,.1f),V(.32f,.85f,.16f),Leaf,V(0,20,-30));
            }
            else if (id == 1)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = (i%2==0?-.25f:.3f), z = -.8f+i*.47f;
                    Ball(p,"Soft footprint heel",V(x,.045f,z),V(.27f,.035f,.34f),DarkWood,V(0,i%2==0?-15:15,0));
                    for(int toe=0;toe<3;toe++) Ball(p,"Small toe print",V(x+(toe-1)*.095f,.047f,z+.21f),V(.07f,.03f,.09f),DarkWood);
                }
                Ball(p,"Pale bank pebble",V(-.65f,.14f,.3f),V(.75f,.25f,.55f),Hex(0xB5B999));
                LeafNote(p,V(-.6f,.31f,.3f),.55f,Blue);
            }
            else
            {
                Ball(p,"Mossy note stone",V(0,.18f,0),V(1.8f,.35f,1.25f),Hex(0xA9AF92));
                LeafNote(p,V(0,.39f,-.1f),1.4f,Hex(0xFFF2CC));
                for(int line=0;line<3;line++) Tube(p,"Handwritten note strokes",V(-.28f+line*.05f,.43f,-.34f+line*.14f),V(.29f,.43f,-.34f+line*.14f),.018f,Deep);
                Acorn(p,V(.7f,.2f,-.6f),.7f);
            }
            Combine(p); return p;
        }
        public static Transform Object(Transform parent, int id)
        {
            var p = Group(parent,"Moment found object " + id);
            if(id==0)
            {
                for(int i=0;i<7;i++)LeafNote(p,V(Mathf.Sin(i*2.4f)*.65f,.08f+i%2*.02f,Mathf.Cos(i*2.4f)*.5f),.7f,i%2==0?Honey:Moss);
                Button(p,V(0,.24f,-.12f),1.25f); Acorn(p,V(.6f,.17f,.3f),.65f); Combine(p);
            }
            else if(id==1)
            {
                Ball(p,"Smooth bottle shore",V(0,.07f,0),V(1.6f,.14f,1.1f),Linen);
                var bottle=Bottle(p,V(0,.36f,0));bottle.localEulerAngles=V(-65,0,18);bottle.localScale=Vector3.one*1.3f;
                LeafNote(p,V(-.7f,.14f,-.25f),.55f,Leaf);
            }
            else Book(p,false,0);
            return p;
        }
        public static Transform Keepsake(Transform parent,int id,int choice)
        {
            var p=Group(parent,"Moment keepsake " + id + " choice " + choice);
            if(id==0)
            {
                if(choice==0)
                {
                    Box(p,"Oak keepsake box",V(0,.3f,0),V(1.4f,.6f,1.05f),Honey);
                    Box(p,"Open green-lined lid",V(0,.86f,.45f),V(1.5f,.8f,.14f),Deep,V(-12,0,0));
                    Box(p,"Soft linen in box",V(0,.63f,-.05f),V(1.15f,.1f,.8f),Linen);
                    Button(p,V(0,.72f,-.1f),1.2f);Acorn(p,V(.42f,.76f,.1f),.55f);
                }
                else
                {
                    Tube(p,"Branch bag hook",V(-.65f,0,.28f),V(-.65f,1.7f,.28f),.09f,Wood);
                    Tube(p,"Bag hanging branch",V(-.65f,1.65f,.28f),V(.2f,1.65f,.28f),.07f,Wood);
                    Ball(p,"Rounded linen pouch",V(0,.69f,0),V(1.18f,1.15f,.85f),Linen);
                    Tube(p,"Pouch tie left",V(-.2f,1.18f,0),V(-.04f,1.65f,.28f),.025f,DarkWood);
                    Tube(p,"Pouch tie right",V(.2f,1.18f,0),V(.15f,1.65f,.28f),.025f,DarkWood);
                    var button=Group(p,"Button displayed on pouch",V(0,.73f,-.42f));button.localEulerAngles=V(-78,0,0);Button(button,Vector3.zero,1.05f);
                    LeafNote(p,V(-.75f,.18f,-.35f),.65f,Leaf);
                }
            }
            else if(id==1)
            {
                Tube(p,"Keepsake upright",V(-.75f,0,0),V(-.75f,2.2f,0),.08f,choice==0?Wood:Linen);
                Tube(p,"Keepsake crossbar",V(-.85f,2.1f,0),V(.8f,2.1f,0),.065f,choice==0?Wood:Linen);
                if(choice==0)
                {
                    Tube(p,"Bottle hanging cord",V(0,2.1f,0),V(0,1.6f,0),.023f,DarkWood);
                    Bottle(p,V(0,.63f,0));
                    Tube(p,"Bell string",V(.48f,2.1f,0),V(.48f,.65f,0),.02f,DarkWood);
                    LeafNote(p,V(.48f,.57f,0),.7f,Coral);
                }
                else
                {
                    Box(p,"Reed shelf",V(0,.24f,0),V(1.55f,.13f,.8f),Honey);
                    Bottle(p,V(.2f,.31f,0));
                    for(int i=0;i<3;i++){Tube(p,"Reed stalk",V(-.6f+i*.16f,0,.28f),V(-.6f+i*.16f,1.6f+i*.16f,.28f),.025f,Leaf);Ball(p,"Reed soft seedhead",V(-.6f+i*.16f,1.7f+i*.16f,.28f),V(.11f,.5f,.11f),Linen);}
                    Tube(p,"Message cord",V(.55f,2.1f,0),V(.55f,1.35f,0),.022f,DarkWood);LeafNote(p,V(.55f,1.18f,0),.65f,Cream);
                }
            }
            else Book(p,true,choice);
            // Glass stays separate for correct transparent sorting.
            return p;
        }
        static void Acorn(Transform p,Vector3 at,float size)
        { var a=Group(p,"Little remembered acorn",at);a.localScale=Vector3.one*size;Ball(a,"Acorn body",V(0,.13f,0),V(.3f,.36f,.3f),Honey);Ball(a,"Acorn cap",V(0,.29f,0),V(.35f,.13f,.35f),DarkWood);Tube(a,"Acorn stem",V(0,.32f,0),V(.02f,.43f,0),.025f,Wood); }
        static void Button(Transform p,Vector3 at,float size)
        {
            var b=Group(p,"Copper four-hole button",at);b.localScale=Vector3.one*size;
            Shape(b,"Round copper button",Lathe(new[]{new Vector2(0,0),new Vector2(.32f,0),new Vector2(.34f,.055f),new Vector2(.3f,.105f),new Vector2(0,.105f)},28),Vector3.zero,Vector3.one,Copper);
            Ring(b,"Button raised edge",V(0,.105f,0),.28f,.021f,Gold);
            for(int i=0;i<4;i++)Ball(b,"Button sewing hole",V((i%2==0?-.08f:.08f),.111f,i<2?-.08f:.08f),V(.065f,.018f,.065f),DarkWood);
        }
        static Transform Bottle(Transform p,Vector3 at)
        {
            var b=Group(p,"Rounded sea-glass letter bottle",at);
            var body=Shape(b,"Soft green translucent glass",Lathe(new[]{new Vector2(0,0),new Vector2(.3f,0),new Vector2(.4f,.1f),new Vector2(.4f,.65f),new Vector2(.18f,.84f),new Vector2(.16f,1.05f),new Vector2(0,1.05f)},24),Vector3.zero,Vector3.one,Blue);
            if(glass==null)glass=new Material(Resources.Load<Shader>("ForestGlass")){color=new Color(.57f,.82f,.77f,.35f),name="Keepsake sea glass"};
            var render=body.GetComponent<MeshRenderer>();render.sharedMaterial=glass;render.shadowCastingMode=ShadowCastingMode.Off;
            Box(b,"Rolled pale letter",V(0,.43f,0),V(.34f,.48f,.14f),Cream,V(0,0,-12));
            Tube(b,"Letter red thread",V(-.16f,.43f,-.09f),V(.16f,.43f,-.09f),.025f,Coral);
            Shape(b,"Cork stopper",Lathe(new[]{new Vector2(0,0),new Vector2(.18f,0),new Vector2(.19f,.18f),new Vector2(0,.18f)},18),V(0,1.01f,0),Vector3.one,Honey);
            Ring(b,"Bottle neck cord",V(0,.96f,0),.18f,.025f,DarkWood);return b;
        }
        static void LeafNote(Transform p,Vector3 at,float size,Color color)
        { var n=Shape(p,"Leaf-shaped paper",OakLeaf(),at,Vector3.one*size,color,V(-78,0,15));Tube(n,"Paper leaf midrib",V(0,0,0),V(0,.75f,0),.012f,Gold); }
        static void Book(Transform p,bool finished,int choice)
        {
            Box(p,"Short reading table",V(0,.58f,0),V(1.85f,.16f,1.3f),Wood);
            for(int side=-1;side<=1;side+=2)for(int end=-1;end<=1;end+=2)Box(p,"Reading table leg",V(side*.72f,.27f,end*.43f),V(.13f,.55f,.13f),DarkWood);
            for(int side=-1;side<=1;side+=2)
            {
                Box(p,"Open moss-green book cover",V(side*.4f,.7f,0),V(.82f,.075f,.98f),Deep,V(0,0,side*6));
                Box(p,"Warm open book pages",V(side*.38f,.77f,-.015f),V(.72f,.09f,.87f),Cream,V(0,0,side*6));
                for(int line=0;line<(finished?4:2);line++)Tube(p,"Short written book lines",V(side*.38f-.21f,.86f,-.27f+line*.13f),V(side*.38f+.21f,.86f,-.27f+line*.13f),.013f,Wood);
            }
            Tube(p,"Book bound spine",V(0,.72f,-.5f),V(0,.72f,.5f),.055f,Honey);
            if(finished)
            {
                Tube(p,"Ribbon from page",V(.3f,.87f,-.1f),V(.48f,.88f,-.88f),.026f,Coral);
                if(choice==0)
                {
                    var leaf=Group(p,"Leaf bookmark with pale stitched edge",V(.46f,.56f,-.95f));leaf.localEulerAngles=V(-38,0,-12);
                    Shape(leaf,"Pale leaf bookmark binding",OakLeaf(),Vector3.zero,V(1.13f,1,1.15f),Cream);
                    Shape(leaf,"Deep green leaf bookmark",OakLeaf(),V(0,.034f,0),V(.88f,1,.94f),Deep);
                    Tube(leaf,"Leaf bookmark golden midrib",V(0,.065f,-.38f),V(0,.065f,.39f),.018f,Gold);
                }
                else Shape(p,"Five-point bookmark facing the path",Star(),V(.49f,.77f,-.95f),Vector3.one*1.7f,Gold,V(-58,0,0));
            }
            Acorn(p,V(-.82f,.8f,.37f),.55f);Combine(p);
        }
        static Mesh Star()
        {
            if(star!=null)return star;
            var vertices=new Vector3[22];var triangles=new System.Collections.Generic.List<int>();vertices[0]=V(0,0,0);vertices[11]=V(0,.045f,0);
            for(int i=0;i<10;i++){float a=i*Mathf.PI/5,r=i%2==0?.27f:.12f;vertices[i+1]=V(Mathf.Sin(a)*r,0,Mathf.Cos(a)*r);vertices[i+12]=vertices[i+1]+V(0,.045f,0);}
            for(int i=0;i<10;i++){int a=i+1,b=(i+1)%10+1;triangles.AddRange(new[]{0,b,a,11,a+11,b+11,a,b+11,a+11,a,b,b+11});}
            star=new Mesh{name="Original five-point keepsake bookmark"};star.vertices=vertices;star.triangles=triangles.ToArray();star.RecalculateNormals();star.RecalculateBounds();return star;
        }
        public static void Release(){if(glass!=null)UnityEngine.Object.Destroy(glass);if(star!=null)UnityEngine.Object.Destroy(star);glass=null;star=null;}
    }
}
