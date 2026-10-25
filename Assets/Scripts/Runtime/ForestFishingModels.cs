using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // Original fish sculptures; shape, fins, whiskers and painted markings are
    // authored here and share the woodland's matte material and portrait light.
    public static partial class ForestFishingModels
    {
        static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
        static readonly uint[] BodyColors = {0x8DB8A1,0xBCC9B9,0x8F9663,0xB7A57C,0xCDA6A0,0x6D997D,0xCB9A66,0x92B3A4,0xA7BBC5,0xD0B572,0x708F99,0xB0C8C9};
        static readonly uint[] FinColors = {0xD4C88F,0x88ABA7,0xC5B67A,0x947852,0xBB7D79,0xA8B48B,0xBA7565,0xC79E7F,0xD4CFAC,0xB98952,0x597E8A,0xDCC58D};
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
        public static void Release() { ReleaseSculptures(); }
    }
}
