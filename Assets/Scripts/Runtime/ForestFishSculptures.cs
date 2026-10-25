using System;
using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public static partial class ForestFishingModels
    {
        static readonly Dictionary<string, Mesh> sculptures = new Dictionary<string, Mesh>();
        static Vector4 S(float x,float top,float belly,float width) => new Vector4(x,top,belly,width);
        // x, upper silhouette, lower silhouette, half-depth. Every species has
        // an authored head/back/belly profile rather than a scaled ellipsoid.
        static readonly Vector4[][] Profiles = {
            new[]{S(-.65f,.08f,.01f,.035f),S(-.42f,.16f,-.04f,.08f),S(-.08f,.25f,-.12f,.145f),S(.29f,.26f,-.08f,.14f),S(.51f,.23f,.02f,.075f),S(.64f,.19f,.13f,.025f)},
            new[]{S(-.94f,.09f,.02f,.03f),S(-.66f,.19f,-.05f,.085f),S(-.23f,.25f,-.11f,.13f),S(.24f,.29f,-.11f,.15f),S(.62f,.25f,-.06f,.11f),S(.88f,.15f,.08f,.025f)},
            new[]{S(-1.10f,.06f,.01f,.025f),S(-.85f,.15f,-.04f,.07f),S(-.40f,.22f,-.07f,.115f),S(.14f,.25f,-.08f,.13f),S(.67f,.24f,-.06f,.12f),S(.96f,.19f,-.02f,.09f),S(1.06f,.12f,.04f,.035f)},
            new[]{S(-.72f,.10f,.01f,.035f),S(-.46f,.36f,-.13f,.14f),S(-.10f,.53f,-.26f,.22f),S(.26f,.49f,-.23f,.23f),S(.59f,.31f,-.10f,.135f),S(.73f,.19f,.10f,.035f)},
            new[]{S(-.55f,.11f,.02f,.025f),S(-.35f,.36f,-.15f,.09f),S(-.06f,.59f,-.30f,.17f),S(.22f,.43f,-.23f,.17f),S(.43f,.25f,-.05f,.10f),S(.53f,.17f,.10f,.025f)},
            new[]{S(-.84f,.12f,.02f,.035f),S(-.57f,.29f,-.09f,.12f),S(-.17f,.46f,-.18f,.20f),S(.29f,.53f,-.18f,.25f),S(.64f,.44f,-.17f,.20f),S(.88f,.25f,-.06f,.09f)},
            new[]{S(-.91f,.11f,.01f,.035f),S(-.65f,.34f,-.17f,.14f),S(-.20f,.58f,-.28f,.23f),S(.29f,.63f,-.28f,.27f),S(.66f,.42f,-.13f,.19f),S(.84f,.23f,.07f,.055f)},
            new[]{S(-.96f,.10f,.01f,.035f),S(-.70f,.24f,-.08f,.10f),S(-.27f,.34f,-.16f,.17f),S(.19f,.39f,-.16f,.20f),S(.57f,.33f,-.12f,.16f),S(.86f,.23f,-.01f,.09f),S(.96f,.15f,.08f,.03f)},
            new[]{S(-.63f,.11f,.02f,.03f),S(-.42f,.43f,-.25f,.12f),S(-.10f,.75f,-.40f,.21f),S(.21f,.68f,-.32f,.22f),S(.49f,.40f,-.10f,.135f),S(.62f,.20f,.10f,.035f)},
            new[]{S(-.94f,.10f,.02f,.03f),S(-.68f,.28f,-.12f,.11f),S(-.24f,.43f,-.22f,.19f),S(.27f,.47f,-.20f,.23f),S(.67f,.34f,-.09f,.16f),S(.88f,.19f,.06f,.05f)},
            new[]{S(-1.02f,.10f,.02f,.035f),S(-.77f,.20f,-.08f,.09f),S(-.36f,.29f,-.15f,.16f),S(.15f,.34f,-.17f,.24f),S(.64f,.31f,-.16f,.31f),S(.96f,.23f,-.10f,.22f),S(1.06f,.16f,.02f,.10f)},
            new[]{S(-.58f,.12f,.03f,.035f),S(-.36f,.39f,-.15f,.14f),S(-.06f,.56f,-.31f,.23f),S(.24f,.49f,-.28f,.23f),S(.48f,.31f,-.10f,.14f),S(.60f,.20f,.09f,.04f)}
        };
        static readonly Vector2[][] Tails = {
            O(.04f,.04f,-.18f,.16f,-.43f,.22f,-.50f,.07f,-.48f,-.17f,-.23f,-.20f,.04f,-.04f),
            O(.04f,.04f,-.27f,.20f,-.59f,.43f,-.63f,.35f,-.33f,0,-.63f,-.35f,-.59f,-.43f,-.27f,-.20f,.04f,-.04f),
            O(.04f,.03f,-.13f,.11f,-.32f,.14f,-.39f,.05f,-.37f,-.10f,-.16f,-.13f,.04f,-.03f),
            O(.04f,.05f,-.20f,.26f,-.48f,.32f,-.54f,.18f,-.36f,0,-.53f,-.23f,-.43f,-.32f,-.17f,-.20f,.04f,-.05f),
            O(.04f,.04f,-.17f,.16f,-.36f,.20f,-.46f,.07f,-.43f,-.14f,-.23f,-.21f,.04f,-.04f),
            O(.04f,.05f,-.22f,.27f,-.52f,.31f,-.55f,.16f,-.48f,0,-.56f,-.22f,-.44f,-.31f,-.18f,-.22f,.04f,-.05f),
            O(.04f,.05f,-.19f,.24f,-.54f,.42f,-.64f,.32f,-.39f,.01f,-.63f,-.37f,-.49f,-.43f,-.19f,-.24f,.04f,-.05f),
            O(.04f,.04f,-.22f,.21f,-.52f,.28f,-.50f,.07f,-.46f,0,-.51f,-.26f,-.25f,-.21f,.04f,-.04f),
            O(.04f,.05f,-.20f,.25f,-.49f,.36f,-.59f,.24f,-.38f,0,-.56f,-.27f,-.45f,-.35f,-.18f,-.20f,.04f,-.05f),
            O(.04f,.04f,-.23f,.25f,-.59f,.51f,-.83f,.46f,-.65f,.20f,-.35f,0,-.70f,-.22f,-.81f,-.49f,-.56f,-.47f,-.21f,-.20f,.04f,-.04f),
            O(.04f,.04f,-.22f,.19f,-.43f,.23f,-.53f,.12f,-.54f,-.10f,-.43f,-.23f,-.19f,-.20f,.04f,-.04f),
            O(.04f,.06f,-.20f,.29f,-.55f,.62f,-.91f,.53f,-.73f,.29f,-.55f,.09f,-.94f,-.10f,-.77f,-.24f,-.86f,-.54f,-.53f,-.57f,-.22f,-.25f,.04f,-.06f)
        };
        static readonly float[] EyeX = { .48f,.68f,.91f,.52f,.39f,.67f,.64f,.74f,.47f,.69f,.86f,.46f };
        static readonly float[] EyeY = { .17f,.17f,.13f,.22f,.20f,.27f,.29f,.22f,.28f,.23f,.18f,.24f };
        static Vector2[] O(params float[] xy)
        {var r=new Vector2[xy.Length/2];for(int i=0;i<r.Length;i++)r[i]=new Vector2(xy[i*2],xy[i*2+1]);return r;}
        static Vector4 Section(int species,float t)
        {
            var p=Profiles[species];float f=Mathf.Clamp01(t)*(p.Length-1);int k=Mathf.Min((int)f,p.Length-2);float u=f-k;
            Vector4 a=p[Mathf.Max(0,k-1)],b=p[k],c=p[k+1],d=p[Mathf.Min(p.Length-1,k+2)];
            var v=.5f*((2*b)+(-a+c)*u+(2*a-5*b+4*c-d)*u*u+(-a+3*b-3*c+d)*u*u*u);
            v.w=Mathf.Max(.01f,v.w);v.y=Mathf.Max(v.z+.01f,v.y);return v;
        }
        static Vector3 Skin(int species,float t,float angle,float offset=0)
        {var p=Section(species,t);float ry=(p.y-p.z)*.5f;return V(p.x,(p.y+p.z)*.5f+(ry+offset)*Mathf.Sin(angle),(p.w+offset)*Mathf.Cos(angle));}
        static Vector3 Flank(int species,float x,float y,int side,float offset=.003f)
        {
            float low=0,high=1;for(int j=0;j<16;j++){float mid=(low+high)*.5f;if(Section(species,mid).x<x)low=mid;else high=mid;}
            var p=Section(species,(low+high)*.5f);float middle=(p.y+p.z)*.5f,ry=(p.y-p.z)*.5f;
            y=Mathf.Clamp(y,p.z+.005f,p.y-.005f);float q=(y-middle)/ry;
            return V(x,y,side*(p.w*Mathf.Sqrt(Mathf.Max(.005f,1-q*q))+offset));
        }
        static Mesh Cached(string key,Func<Mesh> make)
        {if(!sculptures.TryGetValue(key,out var m)){m=make();m.name="Authored fish "+key;sculptures[key]=m;}return m;}
        static Mesh MeshOf(List<Vector3> v,List<int> t)
        {var m=new Mesh();m.SetVertices(v);m.SetTriangles(t,0);m.RecalculateNormals();m.RecalculateBounds();return m;}
        static void FishQuad(List<int> t,int a,int b,int c,int d){t.AddRange(new[]{a,b,c,a,c,d});}
        static Mesh Body(int species,bool belly)
        {
            var v=new List<Vector3>();var tri=new List<int>();const int rings=32,sides=24;
            for(int k=0;k<=rings;k++)for(int j=0;j<=sides;j++)
            {
                float t=k/(float)rings,angle=j*Mathf.PI*2/sides;
                if(belly){float half=1.12f*Mathf.Sin(t*Mathf.PI);angle=Mathf.PI*1.5f+(j/(float)sides*2-1)*half;t=.10f+.80f*t;}
                v.Add(Skin(species,t,angle,belly?.003f:0));
            }
            for(int k=0;k<rings;k++)for(int j=0;j<sides;j++){int n=k*(sides+1)+j;FishQuad(tri,n,n+sides+1,n+sides+2,n+1);}
            if(!belly)
            {
                int a=v.Count;var first=Profiles[species][0];var last=Profiles[species][Profiles[species].Length-1];
                v.Add(V(first.x,(first.y+first.z)*.5f,0));v.Add(V(last.x,(last.y+last.z)*.5f,0));
                for(int j=0;j<sides;j++)tri.AddRange(new[]{a,j,j+1,a+1,rings*(sides+1)+j+1,rings*(sides+1)+j});
            }
            return MeshOf(v,tri);
        }
        static Mesh FinMesh(Vector2[] contour,float curve)
        {
            // Rounded contours, bowed surfaces and thin rims avoid rigid slabs.
            float area=0;for(int i=0;i<contour.Length;i++){var a=contour[i];var b=contour[(i+1)%contour.Length];area+=a.x*b.y-a.y*b.x;}if(area<0)Array.Reverse(contour);
            for(int pass=0;pass<2;pass++){var smooth=new Vector2[contour.Length*2];for(int i=0;i<contour.Length;i++){var a=contour[i];var b=contour[(i+1)%contour.Length];smooth[i*2]=a*.75f+b*.25f;smooth[i*2+1]=a*.25f+b*.75f;}contour=smooth;}
            int count=contour.Length;var v=new List<Vector3>();var tri=new List<int>();
            for(int side=0;side<2;side++)for(int r=0;r<3;r++)for(int j=0;j<count;j++)
            {float t=r*.5f;var at=contour[j]*t;float z=curve*Mathf.Sin(t*Mathf.PI)+(side==0?-1:1)*(.004f+.009f*Mathf.Sin(t*Mathf.PI));v.Add(V(at.x,at.y,z));}
            for(int side=0;side<2;side++)for(int r=0;r<2;r++)for(int j=0;j<count;j++)
            {int a=side*3*count+r*count+j,b=side*3*count+(r+1)*count+j,c=side*3*count+(r+1)*count+(j+1)%count,d=side*3*count+r*count+(j+1)%count;if(side==0)FishQuad(tri,a,d,c,b);else FishQuad(tri,a,b,c,d);}
            for(int j=0;j<count;j++)FishQuad(tri,2*count+j,2*count+(j+1)%count,5*count+(j+1)%count,5*count+j);
            return MeshOf(v,tri);
        }
        static void Fin(Transform p,int species,string name,Vector2[] outline,Vector3 at,Color color,Vector3 rotation=default(Vector3),float curve=.035f)
        {Shape(p,name,Cached(species+name,()=>FinMesh(outline,curve)),at,Vector3.one,color,rotation);}
        static void Mark(Transform p,int species,int index,int side,float x,float y,float width,float height,Color color)
        {
            Shape(p,"Painted flush flank mark",Cached(species+"paint"+index+"/"+side,()=>
            {var v=new List<Vector3>{Flank(species,x,y,side)};var tri=new List<int>();for(int j=0;j<=20;j++){float a=j*Mathf.PI*2/20;v.Add(Flank(species,x+Mathf.Cos(a)*width,y+Mathf.Sin(a)*height,side));}for(int j=1;j<=20;j++)if(side<0)tri.AddRange(new[]{0,j+1,j});else tri.AddRange(new[]{0,j,j+1});return MeshOf(v,tri);}),Vector3.zero,Vector3.one,color);
        }
        public static Vector3 MouthPosition(int species)
        {var end=Profiles[species][Profiles[species].Length-1];return V(end.x-.01f,(end.y+end.z)*.5f,0);}
        public static Transform Fish(Transform parent,int species,int quality=0)
        {
            var p=Group(parent,FishingCatalog.Species[species].Name+" · "+FishingCatalog.Qualities[Mathf.Clamp(quality,0,3)]);
            Color body=Color.Lerp(Hex(BodyColors[species]),Cream,quality*.025f),fin=Hex(FinColors[species]);var tail=Profiles[species][0];
            Shape(p,"Individual sculpted head back belly",Cached(species+"body",()=>Body(species,false)),Vector3.zero,Vector3.one,body);
            Shape(p,"Painted curved cream underside",Cached(species+"belly",()=>Body(species,true)),Vector3.zero,Vector3.one,Color.Lerp(body,Cream,.43f));
            Fin(p,species,"Distinct rounded tail",Tails[species],V(tail.x,(tail.y+tail.z)*.5f,0),fin,curve:species==9||species==11?.075f:.035f);
            switch(species)
            {
                case 0:
                    Fin(p,species,"Rear medaka dorsal",O(-.22f,-.02f,-.27f,.14f,-.08f,.20f,.12f,0),V(-.29f,.20f,0),fin);
                    Fin(p,species,"Long medaka anal",O(-.36f,.02f,-.32f,-.16f,.05f,-.16f,.21f,0),V(-.10f,-.07f,0),fin);break;
                case 1: Fin(p,species,"Narrow swept minnow dorsal",O(-.25f,-.02f,-.25f,.26f,-.12f,.23f,.25f,0),V(-.18f,.26f,0),fin);break;
                case 2: Fin(p,species,"Low ribbon loach dorsal",O(-.44f,-.02f,-.33f,.10f,.17f,.11f,.43f,0),V(-.27f,.21f,0),fin);break;
                case 3: Fin(p,species,"Arched crucian dorsal",O(-.49f,-.04f,-.30f,.20f,.08f,.25f,.38f,.08f,.42f,-.03f),V(-.06f,.48f,0),fin);break;
                case 4:
                    Fin(p,species,"High bitterling dorsal",O(-.25f,-.02f,-.23f,.26f,.03f,.30f,.27f,-.04f),V(-.09f,.46f,0),fin);
                    Fin(p,species,"Diamond lower bitterling fin",O(-.26f,.03f,-.27f,-.26f,.07f,-.25f,.28f,0),V(-.06f,-.23f,0),fin);break;
                case 5:
                    Fin(p,species,"First rounded perch crest",O(-.32f,-.05f,-.29f,.19f,-.22f,.27f,-.13f,.19f,-.04f,.30f,.06f,.21f,.17f,.27f,.25f,-.02f),V(.13f,.47f,0),fin);
                    Fin(p,species,"Second perch dorsal",O(-.24f,-.03f,-.27f,.18f,-.14f,.27f,.05f,.25f,.22f,-.02f),V(-.42f,.33f,0),fin);break;
                case 6: Fin(p,species,"Red carp broad sail",O(-.58f,-.08f,-.39f,.24f,-.07f,.33f,.26f,.15f,.40f,-.08f),V(-.02f,.55f,0),fin);break;
                case 7:
                    Fin(p,species,"Trout swept dorsal",O(-.28f,-.02f,-.30f,.24f,-.12f,.31f,.22f,0),V(-.05f,.35f,0),fin);
                    Fin(p,species,"Small trout adipose",O(-.12f,-.01f,-.12f,.11f,.03f,.14f,.12f,0),V(-.62f,.26f,0),fin);break;
                case 8:
                    Fin(p,species,"Moon bream high dorsal",O(-.22f,-.03f,-.24f,.36f,-.08f,.41f,.18f,.05f,.22f,-.05f),V(-.05f,.67f,0),fin);
                    Fin(p,species,"Moon bream lower fringe",O(-.43f,.04f,-.39f,-.21f,.01f,-.25f,.28f,0),V(-.05f,-.30f,0),fin);break;
                case 9: Fin(p,species,"Golden carp flowing dorsal",O(-.55f,-.04f,-.42f,.19f,-.05f,.30f,.28f,.17f,.43f,-.04f),V(-.11f,.40f,0),fin,curve:.07f);break;
                case 10: Fin(p,species,"Catfish rounded long dorsal",O(-.49f,-.03f,-.42f,.12f,.06f,.18f,.38f,0),V(-.35f,.25f,0),fin);break;
                case 11: Fin(p,species,"Star fish billowing sail",O(-.32f,-.05f,-.31f,.26f,-.12f,.46f,.11f,.43f,.19f,.20f,.32f,-.03f),V(-.07f,.48f,0),fin,curve:.07f);break;
            }
            for(int side=-1;side<=1;side+=2)
            {
                var point=Flank(species,EyeX[species]-.22f,.04f,side,-.015f);
                Fin(p,species,"Curved swimming fin "+side,O(.04f,.02f,-.13f,-.09f,-.29f,-.18f,-.35f,-.11f,-.24f,.01f),point,fin,V(side*38,side*-23,0));
                float eyeSize=species==0||species==4?.072f:species==10?.065f:.083f;
                var eye=Flank(species,EyeX[species],EyeY[species],side,.013f);
                Ball(p,"Set-in dark eye",eye,V(eyeSize,eyeSize,.040f),Ink);
                Ball(p,"Soft eye reflection",eye+V(.009f,.015f,side*.020f),V(.023f,.023f,.009f),Cream);
                var gill=Flank(species,EyeX[species]-.18f,EyeY[species]-.03f,side,.004f);
                Shape(p,"Curved gill crease",Sweep(new[]{gill+V(.025f,.055f,0),gill+V(-.020f,0,.004f*side),gill+V(.015f,-.070f,0)},new[]{.005f,.008f,.004f},1),Vector3.zero,Vector3.one,Color.Lerp(body,Deep,.30f));
                if(species==7)for(int k=0;k<13;k++)Mark(p,species,k,side,-.62f+k%5*.25f,.13f+(k/5-1)*.105f,.022f,.022f,Hex(0x8C7460));
                else if(species==5)for(int k=0;k<5;k++)Mark(p,species,k,side,-.52f+k*.21f,.16f,.025f,.13f,Color.Lerp(body,Deep,.34f));
                else if(species==4)Mark(p,species,0,side,-.09f,.17f,.12f,.18f,Color.Lerp(body,Hex(0x996D93),.42f));
                else if(species==9)for(int k=0;k<10;k++)Mark(p,species,k,side,-.62f+k*.13f,.13f,.077f,.018f,Gold);
                else if(species==1)for(int k=0;k<10;k++)Mark(p,species,k,side,-.65f+k*.14f,.12f,.078f,.014f,Color.Lerp(body,Cream,.65f));
                else if(species==2)for(int k=0;k<7;k++)Mark(p,species,k,side,-.81f+k*.26f,.10f,.065f,.042f,Color.Lerp(body,Deep,.31f));
                else if(species==11)for(int k=0;k<5;k++)Mark(p,species,k,side,-.30f+k*.14f,.19f+Mathf.Sin(k*1.8f)*.12f,.022f,.035f,Cream);
                else for(int k=0;k<5;k++)Mark(p,species,k,side,-.35f+k*.15f,.06f,.025f,.012f,Color.Lerp(body,Cream,.30f));
            }
            var mouth=MouthPosition(species);
            Ball(p,"Rounded joined upper lip",mouth,V(.045f,.038f,species==10?.16f:.075f),Color.Lerp(body,fin,.35f));
            if(species==5)Ball(p,"Broad perch lower jaw",mouth+V(-.055f,-.085f,0),V(.20f,.065f,.17f),Color.Lerp(body,Cream,.25f));
            if(species==2||species==6||species==9||species==10)
            {
                int pairs=species==2?3:species==10?2:1;float reach=species==10?.43f:species==2?.20f:.14f;
                for(int side=-1;side<=1;side+=2)for(int k=0;k<pairs;k++)
                {
                    var start=mouth+V(-.035f-k*.055f,-.02f,side*(species==10?.11f:.04f));
                    Shape(p,"Joined curved barbel",Sweep(new[]{start,start+V(reach*.5f,-.035f,side*reach*.40f),start+V(reach*.70f,-.13f-k*.025f,side*reach*.75f)},new[]{.012f,.010f,.003f},1),Vector3.zero,Vector3.one,Color.Lerp(fin,Cream,.60f));
                }
            }
            Combine(p);return p;
        }
        static void ReleaseSculptures(){foreach(var mesh in sculptures.Values)if(mesh!=null)UnityEngine.Object.Destroy(mesh);sculptures.Clear();}
    }
}
