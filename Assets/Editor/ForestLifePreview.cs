using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ForestShop.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop.Editor
{
    // Prepared native Editor preview. Never opens the ordinary SaveStore.
    [InitializeOnLoad]
    public static class ForestLifePreview
    {
        const string Pending="ForestLifePreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestLifePreview.Output","");
        static ForestApp app;static readonly StringBuilder log=new StringBuilder();
        static ForestLifePreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output=null;
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            var absolute=Path.GetFullPath(output??throw new ArgumentException("Explicit preview output required"));
            if(!absolute.StartsWith(Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(absolute))throw new IOException("Use a new local preview directory");
            Directory.CreateDirectory(absolute);SessionState.SetString("ForestLifePreview.Output",absolute);
            BuildTools.Prepare();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static object Call(string name,params object[] args)=>typeof(ForestApp).GetMethod(name,Private).Invoke(app,args);
        static T Field<T>(string name)=>(T)typeof(ForestApp).GetField(name,Private).GetValue(app);
        static void Field(string name,object value)=>typeof(ForestApp).GetField(name,Private).SetValue(app,value);
        static ForestWorld3D World=>Field<ForestWorld3D>("world3D");
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}
        static void Refresh()
        {
            foreach(var a in Field<System.Collections.Generic.List<Action>>("bindings"))a();
            World.Bind(app.State);Physics.SyncTransforms();World.RenderNow();Canvas.ForceUpdateCanvases();
        }
        static void Region(int region)
        {Call("CloseSheet");World.SetRegion(region);Field("shopBuilt",false);Call("ShowPage","小店");Refresh();}
        static void PanCorner(int corner)
        {
            var from=new Vector2((corner&1)==0?.2f:.8f,(corner&2)==0?.2f:.8f);
            for(int i=0;i<8;i++)World.Pan(from,Vector2.one-from);
            log.AppendLine("CAMERA region="+World.Region+" corner="+corner+" size="+World.CameraSize+" focus="+World.CameraFocus.ToString("F4"));
        }
        static void Shot(string name)
        {
            Refresh();Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            var canvas=Field<Canvas>("canvas");var safe=Field<RectTransform>("safe");var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler.enabled;
            var camera=new GameObject("Isolated scene life HUD camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(255,249,228,255);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            var rt=new RenderTexture(1080,1920,24);camera.targetTexture=rt;scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;canvas.scaleFactor=1;
            safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();
            var viewport=Field<RectTransform>("page").GetComponentInChildren<ForestWorldPointer>();if(viewport!=null){var rect=(RectTransform)viewport.transform;World.SetViewportAspect(rect.rect.width/rect.rect.height);}
            Refresh();foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>())m.Refresh();foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestWorldMarker>())m.Refresh();Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1080,1920,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1080,1920),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=enabled;Call("ApplySafeArea");Canvas.ForceUpdateCanvases();
            viewport=Field<RectTransform>("page").GetComponentInChildren<ForestWorldPointer>();if(viewport!=null){var rect=(RectTransform)viewport.transform;World.SetViewportAspect(rect.rect.width/rect.rect.height);}
        }
        static void Pick(Transform target,string kind,int index)
        {
            var camera=(Camera)typeof(ForestWorld3D).GetField("worldCamera",Private).GetValue(World);
            var collider=target.GetComponentsInChildren<BoxCollider>().First(c=>c.gameObject.activeInHierarchy && c.enabled);
            var at=camera.WorldToViewportPoint(collider.transform.TransformPoint(collider.center));
            string selected="";int selectedIndex=-1;var old=World.Selected;World.Selected=(k,i)=>{selected=k;selectedIndex=i;};
            World.Pick(new Vector2(at.x,at.y));World.Selected=old;
            Check(at.x>0 && at.x<1 && at.y>0 && at.y<1 && selected==kind && selectedIndex==index,"Actual ray target "+kind+index);
        }
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                var s=JsonUtility.FromJson<GameState>(File.ReadAllLines("Docs/Verification/3d-d26-life01/ordinary-held-before.save").Last());
                s.report.pending=false;s.dialogue=-1;s.forest.dialogue=-1;s.coins=2000000;
                var e=new Economy(s);while(s.plots.Length<12)e.BuyPlot();while(s.shelves.Length<4)e.BuyShelf();
                for(int i=0;i<12;i++){s.plots[i].config=i%4;s.plots[i].workItem=i%4;s.plots[i].remaining=i%2==0?1:10;}
                var holder=new GameObject("Inactive isolated scene fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                typeof(ForestApp).GetProperty("State").SetValue(app,s);typeof(ForestApp).GetProperty("Engine").SetValue(app,e);
                Call("ShowPage","小店");
                Check(Field<SaveStore>("store")==null,"Ordinary SaveStore never opens");string before=JsonUtility.ToJson(s);
                File.WriteAllText(Output+"/scope.txt","Prepared Editor native1080x1920 offscreen preview, not Android acceptance. Late layout expanded through normal rules in memory only. No ordinary SaveStore. Camera zoom/pan/reset and real object ray checks.\n");
                for(int region=0;region<8;region++)
                {
                    Region(region);var home=World.CameraFocus;float size=World.CameraSize;Shot("region-"+region+"-default");
                    World.Zoom(100);var far=World.CameraFocus;World.Zoom(100);
                    Check((far-World.CameraFocus).sqrMagnitude<.00001f,"Zoom cap does not pull region toward map origin "+region);
                    Shot("region-"+region+"-wide");World.Zoom(.0001f);PanCorner(0);Shot("region-"+region+"-near-boundary");
                    for(int corner=1;corner<4;corner++)
                    {
                        World.ResetCamera();World.Zoom(.0001f);PanCorner(corner);
                        Shot("region-"+region+"-near-boundary-"+corner);
                    }
                    World.ResetCamera();Check((home-World.CameraFocus).sqrMagnitude<.00001f && Mathf.Abs(size-World.CameraSize)<.0001f,"Exact region recenter "+region);
                    if(region==1)foreach(var t in (Transform[])typeof(ForestWorld3D).GetField("plots",Private).GetValue(World))Pick(t,"plot",Array.IndexOf((Transform[])typeof(ForestWorld3D).GetField("plots",Private).GetValue(World),t));
                    if(region==2){var machines=(Transform[])typeof(ForestWorld3D).GetField("machines",Private).GetValue(World);for(int i=0;i<2;i++)Pick(machines[i],"machine",i);}
                    if(region==3){var shelves=(Transform[])typeof(ForestWorld3D).GetField("shelves",Private).GetValue(World);for(int i=0;i<4;i++)Pick(shelves[i],"shelf",i);}
                }
                s.moments.choices[2]=0;Region(6);World.FocusMoment(2);Shot("leaf-bookmark-focused");World.ResetCamera();Shot("leaf-bookmark-default");s.moments.choices[2]=1;Refresh();World.FocusMoment(2);Shot("star-bookmark-focused");s.moments.choices[2]=0;
                // Restore only the deliberate preview arrangement mutation before invariance check.
                var expected=JsonUtility.FromJson<GameState>(before);s.moments.choices[2]=expected.moments.choices[2];
                Check(JsonUtility.ToJson(s)==before,"Visual camera and props do not mutate any saved game field");
                File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_LIFE_PREVIEW_OK "+Output);EditorApplication.Exit(0);
            }
            catch(Exception ex){File.WriteAllText(Output+"/checks.txt",log+"FAIL "+ex);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
