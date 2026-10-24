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
    // Isolated, prepared Editor view. Never opens or changes the ordinary save.
    [InitializeOnLoad]
    public static class ForestFishingPreview
    {
        const string Pending="ForestFishingPreview.Pending", Output="Builds/Previews/3d-d21-fishing-dev-r02";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static ForestApp app; static readonly StringBuilder log=new StringBuilder();
        static ForestFishingPreview() { EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false)) EditorApplication.delayCall+=Capture;}; }
        public static void Run() {Directory.CreateDirectory(Output);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();}
        static object Call(string name,params object[] args)=>typeof(ForestApp).GetMethod(name,Private).Invoke(app,args);
        static T Field<T>(string name)=>(T)typeof(ForestApp).GetField(name,Private).GetValue(app);
        static void Field(string name,object value)=>typeof(ForestApp).GetField(name,Private).SetValue(app,value);
        static ForestWorld3D World=>Field<ForestWorld3D>("world3D");
        static void Check(bool ok,string name) {if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}
        static void Refresh()
        {
            Call("RefreshHeader");
            foreach(var a in Field<System.Collections.Generic.List<Action>>("bindings"))a();
            foreach(var a in Field<System.Collections.Generic.List<Action>>("modalBindings"))a();
            World.Bind(app.State);typeof(ForestWorld3D).GetMethod("UpdateFishingWorld",Private).Invoke(World,null);World.RenderNow();
        }
        static void Shot(string name)
        {
            Refresh();Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            var canvas=Field<Canvas>("canvas");var safe=Field<RectTransform>("safe");
            var scrolls=canvas.GetComponentsInChildren<ScrollRect>().Select(s=>new{Scroll=s,Position=s.normalizedPosition}).ToArray();
            var c=new GameObject("Isolated fishing HUD camera").AddComponent<Camera>();c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color32(255,249,228,255);c.orthographic=true;c.nearClipPlane=.1f;c.farClipPlane=100;
            var rt=new RenderTexture(1080,1920,24);c.targetTexture=rt;var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler.enabled;scaler.enabled=false;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=c;canvas.planeDistance=10;canvas.scaleFactor=1;
            safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();foreach(var s in scrolls)s.Scroll.normalizedPosition=s.Position;
            foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>())m.Refresh();
            Canvas.ForceUpdateCanvases();c.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1080,1920,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1080,1920),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;c.enabled=false;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(c.gameObject);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=enabled;Call("ApplySafeArea");Canvas.ForceUpdateCanvases();foreach(var s in scrolls)s.Scroll.normalizedPosition=s.Position;
        }
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                File.WriteAllText(Output+"/scope.txt","ISOLATED EDITOR PREVIEW. Prepared completed forest state, real fishing rules advanced with simulated seconds. No SaveStore or device. Host Screen="+Screen.width+"x"+Screen.height+"; offscreen1080x1920; sheet height uses host Screen. Not Android reachability, probability, dynamics or final acceptance.\n");
                var holder=new GameObject("Inactive isolated fishing preview");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Check(Field<SaveStore>("store")==null,"No SaveStore opened");
                Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                var state=JsonUtility.FromJson<GameState>(File.ReadAllText("Builds/Previews/3d-d21-forest-dev-r03/prepared-final.json"));StateMigration.Upgrade(state);state.forest.celebrationSeen=true;state.forest.dialogue=-1;state.report.pending=false;
                typeof(ForestApp).GetProperty("State").SetValue(app,state);typeof(ForestApp).GetProperty("Engine").SetValue(app,new Economy(state));
                for(int i=0;i<3;i++){Call("GoForest",i);Shot("01-spot-"+i);Call("ShowFishing",i);Check(state.fishing.running && state.fishing.spot==i,"Actual UI starts automatic fishing "+i);Shot("02-wait-"+i);Call("CloseSheet");Check(!state.fishing.running,"Leaving pauses "+i);}
                Call("ShowFishing",0);Call("AdvanceFishing",(float)state.fishing.remaining+.001f);Check(state.fishing.phase==2,"Automatic bite");Shot("03-bite");Call("AdvanceFishing",1.81f);Check(state.fishing.phase==3 && state.fishing.totalCaught==1,"Actual automatic landing once");Shot("04-first-catch");
                int count=state.fishing.totalCaught,casts=state.fishing.casts;Call("CloseSheet");app.Engine.Fishing.Advance(60);Check(state.fishing.totalCaught==count,"Paused clock does not catch");Call("ShowFishing",0);Call("AdvanceFishing",2.3f);Check(state.fishing.casts==casts+1 && state.fishing.totalCaught==count,"Next cast after receipt resumes without duplicate");
                for(int spot=0;spot<3;spot++){Call("GoForest",spot);app.Engine.Fishing.Start(spot,0);app.Engine.Fishing.Advance(60000);app.Engine.Fishing.Pause();}
                Check(app.Engine.Fishing.Discovered==12,"All12 real rule species reached in prepared simulated runs");Check(state.fishing.entries.Sum(e=>e.qualityCounts[3])>0,"Rare individual quality reached");
                Call("ShowFishCollection");Shot("05-all-fish-top");Call("EvidenceScroll",false,0f);Shot("06-all-fish-bottom");
                Field("fishFilter",1);Call("ShowFishCollection");Shot("07-discovered");Field("fishFilter",2);Call("ShowFishCollection");Shot("08-recent-top");Call("EvidenceScroll",false,0f);Shot("09-recent-bottom");
                for(int i=0;i<12;i++){Call("ShowFishSpecies",i);Shot("species-"+i.ToString("D2"));}
                Call("ShowFishSpecies",11);Call("EvidenceScroll",true,0f);Shot("10-species-statistics");Call("ShowFishCatch",state.fishing.recent[0]);Shot("11-individual-parameters");
                StateValidation.Check(state);File.WriteAllText(Output+"/prepared-final.json",JsonUtility.ToJson(state,true));File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_FISHING_PREVIEW_OK "+Path.GetFullPath(Output));EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText(Output+"/checks.txt",log+"\nFAILED "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
