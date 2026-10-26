using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ForestShop.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop.Editor
{
    [InitializeOnLoad]
    public static class ForestMomentPreview
    {
        const string Pending="ForestMomentPreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestMomentPreview.Output","Builds/Previews/3d-d25-moments-dev-r02");
        static ForestApp app;static string baseline;static readonly StringBuilder log=new StringBuilder();
        static string Hash(string p){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","");}
        static ForestMomentPreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output="Builds/Previews/3d-d25-moments-dev-r01";
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            var absolute=Path.GetFullPath(output);if(!absolute.StartsWith(Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(absolute))throw new IOException("Use a new local preview directory");
            Directory.CreateDirectory(output);SessionState.SetString("ForestMomentPreview.Output",output);
            BuildTools.Prepare();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static object Call(string name,params object[] args)=>typeof(ForestApp).GetMethod(name,Private).Invoke(app,args);
        static T Field<T>(string name)=>(T)typeof(ForestApp).GetField(name,Private).GetValue(app);
        static void Field(string name,object value)=>typeof(ForestApp).GetField(name,Private).SetValue(app,value);
        static ForestWorld3D World=>Field<ForestWorld3D>("world3D");
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}
        static GameState Fresh(){var s=JsonUtility.FromJson<GameState>(baseline);StateMigration.Upgrade(s);s.report.pending=false;s.dialogue=-1;s.forest.dialogue=-1;return s;}
        static void Use(GameState s)
        {Call("CloseSheet");typeof(ForestApp).GetProperty("State").SetValue(app,s);typeof(ForestApp).GetProperty("Engine").SetValue(app,new Economy(s));Call("ShowPage","小店");}
        static void Refresh()
        {
            Call("RefreshHeader");foreach(var a in Field<System.Collections.Generic.List<Action>>("bindings"))a();foreach(var a in Field<System.Collections.Generic.List<Action>>("modalBindings"))a();
            World.Bind(app.State);Physics.SyncTransforms();World.RenderNow();
        }
        static void Pick(int id)
        {
            Refresh();int stage=app.State.moments.stages[id];string field=stage==0?"momentClues":stage<3?"momentObjects":"momentKeepsakes";
            var values=(Transform[])typeof(ForestWorld3D).GetField(field,Private).GetValue(World);
            var target=values[stage==3?id*2+app.State.moments.choices[id]:id];
            var camera=(Camera)typeof(ForestWorld3D).GetField("worldCamera",Private).GetValue(World);var uv=camera.WorldToViewportPoint(target.position+Vector3.up*.7f);
            Check(uv.z>0 && uv.x>.03f && uv.x<.97f && uv.y>.1f && uv.y<.96f,"Real object lies in view "+id+" phase "+stage);
            World.Pick(new Vector2(uv.x,uv.y));Refresh();
        }
        static void Choose(int id,int choice)
        {Field<RectTransform>("sheetContent").GetComponentsInChildren<Button>().Single(b=>b.name=="Moment choose "+id+" "+choice).onClick.Invoke();Refresh();}
        static void Shot(string name)
        {
            Refresh();Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            var canvas=Field<Canvas>("canvas");var safe=Field<RectTransform>("safe");var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler.enabled;
            var camera=new GameObject("Isolated moment HUD camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(255,249,228,255);camera.orthographic=true;camera.nearClipPlane=.1f;camera.farClipPlane=100;
            var rt=new RenderTexture(1080,1920,24);camera.targetTexture=rt;scaler.enabled=false;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;canvas.scaleFactor=1;
            safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;Canvas.ForceUpdateCanvases();
            var viewport=Field<RectTransform>("page").GetComponentInChildren<ForestWorldPointer>();if(viewport!=null){var rect=(RectTransform)viewport.transform;World.SetViewportAspect(rect.rect.width/rect.rect.height);}
            Refresh();foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>())m.Refresh();Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1080,1920,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1080,1920),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(camera.gameObject);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=enabled;Call("ApplySafeArea");Canvas.ForceUpdateCanvases();
            viewport=Field<RectTransform>("page").GetComponentInChildren<ForestWorldPointer>();if(viewport!=null){var rect=(RectTransform)viewport.transform;World.SetViewportAspect(rect.rect.width/rect.rect.height);}
        }
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                var files=new[]{"Assets","Packages","ProjectSettings"}.SelectMany(p=>Directory.GetFiles(p,"*",SearchOption.AllDirectories)).OrderBy(p=>p).ToArray();var hashes=files.Select(Hash).ToArray();
                File.WriteAllLines(Output+"/source-files.sha256",files.Select((p,i)=>hashes[i]+"  "+p.Replace('\\','/')));
                baseline=File.ReadAllLines("Docs/Verification/3d-d24-experience02/normal-restorable.save").Last();
                var holder=new GameObject("Inactive isolated moment fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                Check(Field<SaveStore>("store")==null,"Ordinary App SaveStore never opens");
                File.WriteAllText(Output+"/scope.txt","Isolated prepared Editor App with actual3D ray picks. No device/ordinary writes. Separate native codec saves only under this preview folder. Native1080x1920 offscreen, host dimensions affect sheet cap; not Android adaptation or final art acceptance.\n");
                var s=Fresh();Use(s);Check(s.version==4 && s.fishing.totalCaught==55 && app.Engine.Moments.Completed==0,"Actual v3 baseline migrated by Unity codec without giving events or losing fish");
                for(int id=0;id<3;id++)
                {
                    Call("GoMoment",id);World.FocusMoment(id);Refresh();string before=JsonUtility.ToJson(s);Shot("event-"+id+"-00-clue");
                    Pick(id);Check(s.moments.stages[id]==1,"First actual3D ray reveals object "+id);Shot("event-"+id+"-01-found");
                    Pick(id);Check(s.moments.stages[id]==2 && Field<string>("currentSheet")=="moment-choice","Second actual3D ray opens optional choice "+id);Shot("event-"+id+"-02-choice");
                    Call("CloseSheet");Check(s.moments.stages[id]==2,"Closing does not choose "+id);Pick(id);
                    Choose(id,0);Check(s.moments.stages[id]==3 && s.moments.choices[id]==0,"Third choice records first arrangement "+id);Shot("event-"+id+"-03-keepsake0");
                    Pick(id);Choose(id,1);Check(app.Engine.Moments.Completed==id+1 && s.moments.choices[id]==1,"Revisit swaps only arrangement "+id);Shot("event-"+id+"-04-keepsake1");
                    var expected=JsonUtility.FromJson<GameState>(before);expected.moments=s.moments;Check(JsonUtility.ToJson(expected)==JsonUtility.ToJson(s),"Every original saved field remains exact after event and swap "+id);StateValidation.Check(s);
                    string completed=JsonUtility.ToJson(s);Call("ShowMomentJournal");Shot("event-"+id+"-05-journal");Check(JsonUtility.ToJson(s)==completed,"Journal does not complete or award "+id);
                }
                Call("ShowJourneyHandbook");Shot("handbook-top");Call("EvidenceScroll",true,0f);Shot("handbook-actual-footer");
                string recorded=JsonUtility.ToJson(s);Field<RectTransform>("sheetContent").GetComponentsInChildren<Button>().Single(b=>b.name=="Open moment journal").onClick.Invoke();Check(Field<string>("currentSheet")=="moment-journal" && recorded==JsonUtility.ToJson(s),"Handbook opens the actual read-only keepsake record");
                for(int phase=0;phase<4;phase++)
                {
                    var p=Fresh();var e=new Economy(p);e.Forest.Visit(2);if(phase>0)e.Moments.Observe(2,0);if(phase>1)e.Moments.Observe(2,1);if(phase>2)e.Moments.Choose(2,1);
                    var store=new SaveStore(Output+"/Native-phase-"+phase,new UnityStateCodec());store.Save(p);var loaded=store.Load();Check(JsonUtility.ToJson(p)==JsonUtility.ToJson(loaded),"Native JsonUtility/SaveStore roundtrip phase "+phase);
                }
                Use(new GameState{welcomeRead=true});Call("ShowMomentJournal");Shot("locked-journal");Check(app.Engine.Moments.Completed==0,"New game remains locked and empty");
                Check(files.Select((p,i)=>Hash(p)==hashes[i]).All(v=>v),"All source inputs unchanged during isolated capture");
                File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_MOMENT_PREVIEW_OK "+Path.GetFullPath(Output));EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText(Output+"/checks.txt",log+"FAIL "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
