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
    public static class ForestResidentPreview
    {
        const string Pending="ForestResidentPreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestResidentPreview.Output","");
        static ForestApp app;static readonly StringBuilder log=new StringBuilder();
        static ForestResidentPreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output=null;
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            var absolute=Path.GetFullPath(output??throw new ArgumentException("Explicit preview output required"));
            if(!absolute.StartsWith(Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(absolute))throw new IOException("Use a new local preview directory");
            Directory.CreateDirectory(absolute);SessionState.SetString("ForestResidentPreview.Output",absolute);
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
                var s=JsonUtility.FromJson<GameState>(File.ReadAllLines("Docs/Verification/3d-d27-life03/ProtectedOrdinary/before.save").Last());
                s.report.pending=false;s.dialogue=-1;s.forest.dialogue=-1;s.stories=5;s.simpleAnimation=true;
                var e=new Economy(s);
                var holder=new GameObject("Inactive isolated resident fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                typeof(ForestApp).GetProperty("State").SetValue(app,s);typeof(ForestApp).GetProperty("Engine").SetValue(app,e);
                Call("ShowPage","小店");Region(3);
                Check(Field<SaveStore>("store")==null,"Ordinary SaveStore never opens");
                string before=JsonUtility.ToJson(s);
                var visitors=(Transform[])typeof(ForestWorld3D).GetField("visitors",Private).GetValue(World);
                for(int i=0;i<5;i++)
                {
                    Pick(visitors[i],"resident",i);
                    for(int repeat=0;repeat<3;repeat++){Call("ShowResidentNote",i);Check(Field<string>("currentSheet")=="resident-note","Short greeting opens "+i);Call("CloseSheet");}
                    Call("ShowResidentNote",i);Shot("note-"+i);Call("CloseSheet");
                }
                Check(JsonUtility.ToJson(s)==before,"Fifteen greeting open/close cycles leave every saved field unchanged");
                Shot("simple-five-residents");
                var tufts=(System.Collections.Generic.List<Transform>)typeof(ForestWorld3D).GetField("breezeTufts",Private).GetValue(World);
                typeof(ForestWorld3D).GetMethod("UpdateAmbientLife",Private).Invoke(World,null);
                Check(tufts.Count==9 && tufts.All(t=>Quaternion.Angle(t.localRotation,Quaternion.identity)<.001f),"Nine grass patches return exactly to rest in simplified mode");
                s.stories=0;Refresh();Call("ShowResidentNote",4);
                Check(Field<string>("currentSheet")==null && !visitors[4].gameObject.activeSelf,"Unmet resident cannot open a greeting or remain pickable");
                s.stories=5;s.simpleAnimation=false;
                for(int i=0;i<5;i++)
                {
                    typeof(ForestWorld3D).GetField("visitEpoch",Private).SetValue(World,Time.unscaledTime-i*52-23);
                    visitors[i].localRotation=Quaternion.Euler(0,180,0);Refresh();Shot("resident-"+i+"-browse");
                }
                for(int r=0;r<8;r++){Region(r);typeof(ForestWorld3D).GetMethod("UpdateAmbientLife",Private).Invoke(World,null);Shot("region-"+r);}
                s.simpleAnimation=true;s.stories=5;
                Check(JsonUtility.ToJson(s)==before,"Visual route, greeting and ambient presentation leave every saved field unchanged");
                File.WriteAllText(Output+"/scope.txt","Prepared Editor native1080x1920 offscreen preview, not Android acceptance. Inactive ForestApp, no ordinary SaveStore, tick, install or runtime operation claim. Five known residents, actual physics rays, repeated short greeting UI and simplified grass rest checks. Route static snapshots do not establish dynamic foot or sale continuity. Host Screen limits modal height; Android fit still required.\n");
                File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_RESIDENT_PREVIEW_OK "+Output);EditorApplication.Exit(0);
            }
            catch(Exception ex){File.WriteAllText(Output+"/checks.txt",log+"FAIL "+ex);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
