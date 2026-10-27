using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        // Quiet, non-interactive surroundings: no inventory, awards or fake tasks.
        void BuildSceneLife()
        {
            var stock = Group(shopArea, "Shop wrapping and restocking corner", V(5.2f, 4.64f, -1.65f));
            stock.localEulerAngles=V(0,90,0);
            LowTable(stock, "Wrapping table", V(0, 0, .15f), V(1.95f, .88f, .85f));
            Box(stock, "Folded wrapping linen", V(-.48f, .97f, .13f), V(.63f, .09f, .64f), Cream);
            Parcel(stock, V(.36f, .95f, .15f), .65f);
            Ring(stock, "Spool of string", V(-.3f, 1.05f, .26f), .11f, .045f, Honey);
            Crate(stock, V(.85f, 0, -.05f), .85f);
            Parcel(stock, V(.85f, .61f, -.05f), .54f);
            Box(stock, "Low woven stock mat", V(0, .01f, -.23f), V(2.5f, .025f, 2.1f), Hex(0xAE9470));
            Combine(stock);

            var cart = Group(worldRoot, "Shop front parcel cart", V(4.9f, .14f, -17.1f));
            cart.localEulerAngles = V(0, -17, 0);
            for(int i=0;i<5;i++) Box(cart,"Handcart bed board",V(-.66f+i*.33f,.63f,0),V(.30f,.11f,1.4f),Honey);
            for(int side=-1;side<=1;side+=2)
            {
                Tube(cart,"Handcart long handle",V(side*.7f,.55f,.55f),V(side*.7f,1.05f,-1.45f),.055f,Wood);
                Box(cart,"Low handcart side",V(side*.78f,.9f,0),V(.09f,.4f,1.5f),Wood);
                Ring(cart,"Wood cart wheel",V(side*.9f,.42f,.2f),.39f,.075f,DarkWood,V(0,0,90));
                for(int spoke=0;spoke<4;spoke++)
                {
                    float a=spoke*Mathf.PI/4;
                    Tube(cart,"Wheel spoke",V(side*.9f,.42f-Mathf.Sin(a)*.34f,.2f-Mathf.Cos(a)*.34f),V(side*.9f,.42f+Mathf.Sin(a)*.34f,.2f+Mathf.Cos(a)*.34f),.035f,Honey);
                }
                Ball(cart,"Wheel hub",V(side*.94f,.42f,.2f),Vector3.one*.15f,Copper);
            }
            Crate(cart,V(-.3f,.7f,.15f),.75f);Parcel(cart,V(.38f,.7f,-.1f),.72f);Parcel(cart,V(-.3f,1.25f,.15f),.45f);
            Combine(cart);
            GroundIsland(worldRoot,V(4.9f,.13f,-17),V(3.5f,.03f,2.5f),Hex(0xA5AC7F));

            var timber = Group(workshopArea,"Workshop timber and repair corner",V(-4.45f,1.19f,-1.55f));
            Box(timber,"Woodwork protective floor mat",V(0,.012f,0),V(2.2f,.025f,2.2f),Hex(0xB6A47E));
            for(int side=-1;side<=1;side+=2)Box(timber,"Low timber cradle",V(side*.61f,.15f,.2f),V(.18f,.28f,1.4f),DarkWood);
            for(int row=0;row<2;row++)for(int i=0;i<3-row;i++)
            {
                var at=V(-.5f+i*.48f+row*.24f,.35f+row*.36f,.14f);
                Tube(timber,"Seasoned timber log",at+V(0,0,-.67f),at+V(0,0,.67f),.22f,Wood);
                Tube(timber,"Pale sawn end",at+V(0,0,-.682f),at+V(0,0,-.71f),.185f,Honey);
                Ring(timber,"End grain ring",at+V(0,0,-.723f),.125f,.012f,DarkWood,V(90,0,0));
            }
            for(int i=0;i<3;i++)Box(timber,"Leaning repair board",V(.85f+i*.13f,.77f,.7f),V(.22f,1.5f,.1f),i%2==0?Honey:Wood,V(-13,0,-8-i*4));
            Tube(timber,"Repair mallet handle",V(-.58f,.14f,-.88f),V(.06f,.17f,-.88f),.04f,Wood);
            Box(timber,"Repair mallet head",V(.07f,.2f,-.88f),V(.18f,.24f,.38f),DarkWood);
            Combine(timber);
            var rail=Group(lowerRoom,"Workshop measuring and cutting rail",V(-4.8f,3.08f,2.83f));
            Box(rail,"Repair tool board",Vector3.zero,V(1.45f,.74f,.1f),Hex(0xB89059));
            Tube(rail,"Small saw spine",V(-.5f,.14f,-.12f),V(.23f,.14f,-.12f),.025f,DarkWood);
            Box(rail,"Saw blade",V(-.12f,.01f,-.12f),V(.72f,.22f,.035f),Hex(0xA7AAA0));
            Ring(rail,"Saw wooden handle",V(.36f,.08f,-.13f),.15f,.045f,Wood,V(90,0,0));
            Tube(rail,"Hanging measuring rule",V(-.48f,-.2f,-.13f),V(.46f,-.2f,-.13f),.03f,Cream);
            for(int i=0;i<7;i++)Box(rail,"Rule measure mark",V(-.4f+i*.13f,-.2f,-.166f),V(.012f,i%2==0?.045f:.025f,.01f),DarkWood);
            Combine(rail);

            var garden=Group(gardenArea,"Garden watering and seed corner",V(1.7f,.14f,6.05f));
            GroundIsland(garden,V(0,-.005f,0),V(4.4f,.02f,1.85f),Hex(0xADA97C));
            LowTable(garden,"Seed sorting bench",V(-.7f,0,.05f),V(1.65f,.83f,.65f));
            for(int i=0;i<3;i++)
            {
                Box(garden,"Folded seed packet",V(-1.16f+i*.43f,.92f,.04f),V(.31f,.055f,.4f),Cream,V(0,-12+i*12,0));
                Shape(garden,"Leaf on seed paper",OakLeaf(),V(-1.16f+i*.43f,.953f,.04f),V(.18f,.4f,.24f),i==1?Copper:Leaf);
            }
            Barrel(garden,V(1.05f,0,.1f),.85f);
            WateringCan(garden,V(.3f,.02f,-.25f),.9f);
            Tube(garden,"Garden rake handle",V(-1.8f,.07f,.3f),V(-1.56f,1.75f,.55f),.04f,Wood);
            Box(garden,"Garden rake head",V(-1.56f,1.73f,.55f),V(.64f,.08f,.11f),DarkWood);
            for(int i=0;i<5;i++)Tube(garden,"Rake curved tooth",V(-1.82f+i*.13f,1.73f,.55f),V(-1.82f+i*.13f,1.59f,.46f),.018f,DarkWood);
            ForestModels.Pot(garden,V(.05f,.03f,.56f),.55f,false);
            Combine(garden);

            var creek=Group(forestScene,"Creek walkers rest place",ForestCenters[1]+V(6.6f,.18f,1.4f));
            creek.localEulerAngles=V(0,-24,0);
            GroundIsland(creek,V(.2f,-.02f,0),V(4.4f,.02f,2.75f),Hex(0x9DA885));
            for(int side=-1;side<=1;side+=2)
            {
                Tube(creek,"Rest bench log leg",V(side*.82f,0,.18f),V(side*.82f,.55f,.18f),.18f,DarkWood);
                Box(creek,"Bench back support",V(side*.94f,.8f,.53f),V(.1f,1.1f,.1f),Wood,V(-9,0,0));
            }
            for(int i=0;i<3;i++)Box(creek,"Rest bench honey seat",V(0,.58f,-.1f+i*.23f),V(2.55f,.13f,.21f),i==1?Wood:Honey);
            Box(creek,"Rest bench back",V(0,1.0f,.58f),V(2.55f,.27f,.1f),Honey,V(-9,0,0));
            var bag=Group(creek,"Walkers linen satchel",V(-.68f,.68f,.1f));
            Ball(bag,"Soft satchel body",V(0,.19f,0),V(.51f,.42f,.35f),Cream);Ring(bag,"Satchel strap",V(0,.42f,0),.22f,.025f,DarkWood,V(90,0,0));
            Box(bag,"Satchel flap",V(0,.27f,-.18f),V(.43f,.17f,.03f),Honey);
            LowTable(creek,"Tea stump",V(1.78f,0,-.15f),V(.73f,.63f,.7f));
            var tea=ForestModels.Product(creek,6);tea.localPosition=V(1.78f,.72f,-.15f);tea.localScale=Vector3.one*.6f;
            for(int i=0;i<4;i++)
            {
                var at=V(-1.85f+i*.65f,.06f,.98f+Mathf.Sin(i)*.2f);
                Ball(creek,"Creek ground moss",at,V(.55f,.15f,.46f),Moss);
                if(i%2==0)ForestModels.Flower(creek,at+V(0,.28f,0),.11f,Cream);
            }
            Combine(creek);

            // Sparse clusters hug working corners; the public and private paths stay open.
            foreach(var at in new[]{V(-16,.14f,5),V(-13,.14f,2.2f),V(14,.14f,15),V(7,.14f,-16),V(-1,.14f,5.1f)})
            {
                GroundIsland(worldRoot,at,V(2.6f,.015f,1.75f),Hex(0x9BA877));
                for(int i=0;i<5;i++)
                {
                    var p=at+V(Mathf.Sin(i*2.4f)*(1+i%2*.4f),.04f,Mathf.Cos(i*2.4f)*.65f);
                    Shape(worldRoot,"Fallen warm leaf",OakLeaf(),p,V(.25f,.35f,.42f),i%2==0?Hex(0xAA9662):Hex(0x83975F),V(0,i*71,0));
                }
            }
        }
        static void GroundIsland(Transform p,Vector3 at,Vector3 scale,Color tint)
        {Shape(p,"Soft worn working ground",Lathe(new[]{new Vector2(0,0),new Vector2(.44f,0),new Vector2(.5f,.008f),new Vector2(.43f,.016f),new Vector2(0,.016f)},40),at,scale,tint);}
        static void LowTable(Transform p,string name,Vector3 at,Vector3 size)
        {
            var t=Group(p,name,at);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(t,"Tapered wooden leg",V(x*size.x*.39f,size.y*.45f,z*size.z*.35f),V(.10f,size.y*.9f,.10f),DarkWood);
            Box(t,"Rounded wooden tabletop",V(0,size.y,0),V(size.x,.12f,size.z),Honey);
            Box(t,"Under table stretcher",V(0,size.y*.28f,0),V(size.x*.8f,.09f,size.z*.66f),Wood);
        }
        static void Parcel(Transform p,Vector3 at,float size)
        {
            var t=Group(p,"Linen wrapped parcel",at);t.localScale=Vector3.one*size;
            Box(t,"Cream parcel",V(0,.29f,0),V(.94f,.58f,.8f),Cream);
            Box(t,"Parcel twine cross",V(0,.588f,0),V(.035f,.025f,.82f),DarkWood);
            Box(t,"Parcel twine length",V(0,.589f,0),V(.96f,.026f,.035f),DarkWood);
            Ring(t,"Twine bow left",V(-.09f,.63f,0),.09f,.017f,Wood);Ring(t,"Twine bow right",V(.09f,.63f,0),.09f,.017f,Wood);
            Shape(t,"Parcel leaf tag",OakLeaf(),V(.23f,.606f,-.18f),V(.25f,.5f,.33f),Deep,V(0,20,0));
        }
        static void Crate(Transform p,Vector3 at,float size)
        {
            var t=Group(p,"Slatted wooden supply crate",at);t.localScale=Vector3.one*size;
            Box(t,"Crate bottom",V(0,.06f,0),V(1,.1f,.85f),DarkWood);
            for(int side=-1;side<=1;side+=2)for(int i=0;i<3;i++)
            {
                Box(t,"Crate side slat",V(side*.48f,.17f+i*.2f,0),V(.08f,.15f,.85f),Honey);
                Box(t,"Crate end slat",V(0,.17f+i*.2f,side*.39f),V(.93f,.15f,.08f),Wood);
            }
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Box(t,"Crate corner upright",V(x*.44f,.35f,z*.35f),V(.1f,.7f,.1f),DarkWood);
        }
        static void Barrel(Transform p,Vector3 at,float size)
        {
            var b=Group(p,"Rainwater collecting barrel",at);b.localScale=Vector3.one*size;
            Shape(b,"Wood barrel staves",Lathe(new[]{new Vector2(0,0),new Vector2(.42f,0),new Vector2(.51f,.55f),new Vector2(.46f,1.1f),new Vector2(.40f,1.1f),new Vector2(.4f,.9f),new Vector2(0,.9f)},24),Vector3.zero,Vector3.one,Wood);
            foreach(float y in new[]{.17f,.87f})Ring(b,"Barrel copper hoop",V(0,y,0),y<.5f?.46f:.49f,.036f,Copper);
            Shape(b,"Collected blue rainwater",Lathe(new[]{new Vector2(0,0),new Vector2(.4f,0),new Vector2(0,.01f)},24),V(0,.96f,0),Vector3.one,Blue);
            Tube(b,"Barrel small tap",V(.43f,.28f,0),V(.66f,.28f,0),.045f,Copper);
        }
        static void WateringCan(Transform p,Vector3 at,float size)
        {
            var c=Group(p,"Garden blue watering can",at);c.localScale=Vector3.one*size;
            Shape(c,"Rounded watering can body",Lathe(new[]{new Vector2(0,0),new Vector2(.3f,0),new Vector2(.36f,.12f),new Vector2(.33f,.58f),new Vector2(.27f,.63f),new Vector2(0,.63f)},24),Vector3.zero,Vector3.one,Blue);
            Ring(c,"Can carrying handle",V(0,.72f,0),.28f,.035f,DarkWood,V(90,0,0));
            Tube(c,"Raised watering spout",V(-.2f,.18f,0),V(-.8f,.6f,0),.075f,Blue,.65f);
            Ball(c,"Brass watering rose",V(-.83f,.64f,0),V(.15f,.13f,.28f),Copper);
            for(int i=0;i<3;i++)Ball(c,"Watering rose hole",V(-.9f,.67f,-.08f+i*.08f),Vector3.one*.027f,DarkWood);
        }
    }
}
