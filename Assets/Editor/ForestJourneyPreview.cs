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
    // Isolated prepared views only. No SaveStore, no device, no ordinary progress.
    [InitializeOnLoad]
    public static class ForestJourneyPreview
    {
        const string Pending="ForestJourneyPreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestJourneyPreview.Output","Builds/Previews/3d-d23-journey-dev-r01");
        static ForestApp app;static string baseline;static readonly StringBuilder log=new StringBuilder();
        static string[] frozenFiles,frozenHashes;
        static string Hash(string file){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
        static void Freeze()
        {
            frozenFiles=new[]{"Assets","Packages","ProjectSettings"}.SelectMany(p=>Directory.GetFiles(p,"*",SearchOption.AllDirectories)).OrderBy(p=>p).ToArray();frozenHashes=frozenFiles.Select(Hash).ToArray();
            File.WriteAllLines(Output+"/source-files.sha256",frozenFiles.Select((file,i)=>frozenHashes[i]+"  "+file.Replace('\\','/')));
        }
        static ForestJourneyPreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output="Builds/Previews/3d-d23-journey-dev-r01";
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            string absolute=Path.GetFullPath(output),allowed=Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar;
            if(!absolute.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Preview must be inside local Builds/Previews");
            if(Directory.Exists(output))throw new IOException("Keep original preview; choose a new revision");
            Directory.CreateDirectory(output);SessionState.SetString("ForestJourneyPreview.Output",output);
            BuildTools.Prepare();EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static object Call(string name,params object[] args)=>typeof(ForestApp).GetMethod(name,Private).Invoke(app,args);
        static T Field<T>(string name)=>(T)typeof(ForestApp).GetField(name,Private).GetValue(app);
        static void Field(string name,object value)=>typeof(ForestApp).GetField(name,Private).SetValue(app,value);
        static ForestWorld3D World=>Field<ForestWorld3D>("world3D");
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}
        static void Use(GameState state)
        {Call("CloseSheet");typeof(ForestApp).GetProperty("State").SetValue(app,state);typeof(ForestApp).GetProperty("Engine").SetValue(app,new Economy(state));}
        static GameState Fresh()
        {var s=JsonUtility.FromJson<GameState>(baseline);s.report.pending=false;s.fishing=new FishingProgress();s.forest.location=0;s.simpleAnimation=false;return s;}
        static void Refresh()
        {
            Call("RefreshHeader");foreach(var a in Field<System.Collections.Generic.List<Action>>("bindings"))a();
            foreach(var a in Field<System.Collections.Generic.List<Action>>("modalBindings"))a();
            World.Bind(app.State);typeof(ForestWorld3D).GetMethod("UpdateFishingWorld",Private).Invoke(World,null);World.RenderNow();
        }
        static void Shot(string name)
        {
            Refresh();Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            var canvas=Field<Canvas>("canvas");var safe=Field<RectTransform>("safe");
            var scrolls=canvas.GetComponentsInChildren<ScrollRect>().Select(s=>new{Scroll=s,Position=s.normalizedPosition}).ToArray();
            var c=new GameObject("Isolated polish HUD camera").AddComponent<Camera>();c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color32(255,249,228,255);c.orthographic=true;c.nearClipPlane=.1f;c.farClipPlane=100;
            var rt=new RenderTexture(1080,1920,24);c.targetTexture=rt;var scaler=canvas.GetComponent<CanvasScaler>();bool enabled=scaler.enabled;scaler.enabled=false;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=c;canvas.planeDistance=10;canvas.scaleFactor=1;safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
            Canvas.ForceUpdateCanvases();foreach(var s in scrolls)s.Scroll.normalizedPosition=s.Position;
            if(Field<string>("currentSheet")=="fishing")Call("FrameFishing",World,app.State.fishing.spot);
            World.RenderNow();foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>())m.Refresh();Canvas.ForceUpdateCanvases();
            c.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1080,1920,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1080,1920),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;c.enabled=false;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(c.gameObject);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=enabled;Call("ApplySafeArea");Canvas.ForceUpdateCanvases();foreach(var s in scrolls)s.Scroll.normalizedPosition=s.Position;
        }
        static object[] Steps() => ((System.Array)Call("JourneySteps",true)).Cast<object>().ToArray();
        static string TextOf(object step,string field)=>(string)step.GetType().GetField(field).GetValue(step);
        static int IndexOf(object step)=>(int)step.GetType().GetField("Index").GetValue(step);
        static GameState Stage(int step)
        {
            var s=Fresh();s.orders=new Order[0];s.dialogue=-1;s.warehouse=4;
            var f=s.forest;f.started=step>=1;f.greenhouse=step>=3;f.bridge=step>=5;f.vines=step>=8;f.pavilion=step>=11;f.celebrationSeen=f.pavilion;
            f.discovered=step>=9?7:step>=6?3:step>=2?1:0;f.visited=step>=8?7:step>=5?3:step>=1?1:0;f.location=0;f.requests=step>=10?3:step>=7?2:step>=4?1:0;f.dialogue=-1;
            f.beds=f.greenhouse?Enumerable.Range(0,4).Select(i=>new Facility{config=4,workItem=4,remaining=20}).ToArray():new Facility[0];
            f.gathered=Enumerable.Range(0,3).Select(i=>(f.discovered&(1<<i))!=0?3:0).ToArray();f.available=new[]{3,3,3};f.regrow=new double[3];
            for(int i=0;i<7;i++)s.crops[i]=i<4?30:0;for(int i=6;i<9;i++){s.items[i]=0;s.sold[i]=0;}
            s.machines=new[]{new Facility{config=4,workItem=4,remaining=12},new Facility{config=5,workItem=5,remaining=20}};
            s.shelves=new[]{new Facility{config=0,remaining=8},new Facility{config=1,remaining=8}};
            return s;
        }
        static void ActiveForest(GameState s,int item)
        {s.orders=new[]{new Order{id=301,forestStory=true,resident=ForestRules.RequestResidents[item-6],item=item,count=2,active=true,reserve=true}};s.nextOrderId=302;}
        static void Plan(GameState s,string target,string name)
        {
            Use(s);StateValidation.Check(s);string before=JsonUtility.ToJson(s);var steps=Steps();
            Check(steps.Length==2 && TextOf(steps[0],"Target")==target,name+" recommends "+target);
            Call("ShowPage","小店");Call("ShowJourneyHandbook");Refresh();
            Check(JsonUtility.ToJson(s)==before,name+" handbook reads without changing state");
            Shot(name+"-handbook");Call("EvidenceScroll",true,0f);Shot(name+"-footer");
        }
        static void Jump(object step,string target)
        {
            var s=app.State;long coins=s.coins;int[] crops=(int[])s.crops.Clone(),items=(int[])s.items.Clone();int discoveries=s.forest.discovered,requests=s.forest.requests,fish=s.fishing.totalCaught;
            Call("GoJourney",step);
            Check(s.coins==coins && crops.SequenceEqual(s.crops) && items.SequenceEqual(s.items) && discoveries==s.forest.discovered && requests==s.forest.requests && fish==s.fishing.totalCaught,"Navigation never grants/collects/configures or delivers "+target);
            Check(Field<string>("currentSheet")==target,"Navigation opens actual "+target);
        }
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                Freeze();
                File.WriteAllText(Output+"/scope.txt","ISOLATED PREPARED EDITOR VIEWS. No SaveStore/device/ordinary progression. Core advances use simulated seconds; completed world receipts use explicitly chosen visual clocks. Native offscreen1080x1920, Host Screen="+Screen.width+"x"+Screen.height+" affects sheet cap. Not Android adaptation or continuous dynamics.\n");
                baseline=File.ReadAllLines("Docs/Verification/3d-d22-fishing02/ordinary-after-region-fit-final.save").Last();
                var holder=new GameObject("Inactive isolated journey fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Check(Field<SaveStore>("store")==null,"No ordinary SaveStore opened");
                Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                var opening=new GameState{welcomeRead=true};Use(opening);Call("ShowPage","小店");Plan(opening,"opening","01-opening");var first=Steps()[0];Call("GoJourney",first);Check(opening.tutorial==0 && opening.crops[0]==0,"Opening navigation does not harvest");
                Check(app.Engine.HarvestFirst() && app.Engine.ConfigureMachine(0,0) && app.Engine.ConfigureShelf(0,0),"Existing core opening actions unchanged");app.Engine.Advance(40);Plan(opening,"orders","02-first-sale");
                int[] stages={0,1,2,3,4,5,6,7,8,9,10,11};string[] targets={"forest-entry","forage","forest-repair","orders","forest-repair","forage","orders","forest-repair","forage","orders","forest-repair","fish-species"};
                for(int i=0;i<stages.Length;i++){Plan(Stage(stages[i]),targets[i],"stage-"+i.ToString("D2"));if(targets[i]=="forage"){Jump(Steps()[0],"forage");Shot("stage-"+i.ToString("D2")+"-destination");}}
                var supply=Stage(3);ActiveForest(supply,6);Plan(supply,"nursery","supply-berry");Jump(Steps()[0],"nursery");
                supply=Stage(6);ActiveForest(supply,7);Plan(supply,"nursery","supply-dew");
                supply=Stage(9);ActiveForest(supply,8);Plan(supply,"forage","supply-resin");
                supply=Stage(3);ActiveForest(supply,6);supply.crops[4]=8;Plan(supply,"machine","supply-ready");Jump(Steps()[0],"machine");
                supply=Stage(3);ActiveForest(supply,6);supply.machines[0]=new Facility{config=6,workItem=6,remaining=20};Plan(supply,"machine","supply-working");Check(TextOf(Steps()[0],"Title").Contains("制作中"),"Committed recipe materials are not mistaken for a missing input");
                supply=Stage(3);ActiveForest(supply,6);supply.items[6]=2;Plan(supply,"orders","supply-deliver");
                var empty=Stage(5);Use(empty);app.Engine.Fishing.Start(0,0);app.Engine.Fishing.Advance(45);app.Engine.Fishing.Pause();Call("ShowPage","小店");var next=Steps()[1];int species=IndexOf(next);var kind=FishingCatalog.Species[species];
                Check(TextOf(next,"Target")=="fish-species" && empty.fishing.entries[species].count==0 && app.Engine.Fishing.BaitUnlocked(kind.Bait) && Enumerable.Range(0,3).Any(i=>(kind.Spots&(1<<i))!=0 && app.Engine.Fishing.SpotUnlocked(i)),"Next clue prefers an unfound fish with an accessible habitat and bait");
                Call("ShowFishCollection");Shot("fish-partial-progress");Call("ShowFishSpecies",species);Shot("fish-clue-top");Call("EvidenceScroll",true,0f);Shot("fish-clue-footer");
                var unknown=Field<RectTransform>("sheetContent").GetComponentsInChildren<Button>().First(b=>b.name=="Go to species fishing spot");unknown.onClick.Invoke();Check(Field<string>("currentSheet")=="fishing","Clue navigates to an accessible actual fishing spot");Call("CloseSheet");
                var filled=Stage(11);var rules=new Economy(filled);for(int spot=0;spot<3;spot++){filled.forest.location=spot;rules.Fishing.Start(spot,0);rules.Fishing.Advance(60000);rules.Fishing.Pause();}
                Plan(filled,"fish-collection","all-species");Check(app.Engine.Fishing.Discovered==12,"Prepared all-species collection is complete");Call("ShowFishCollection");Shot("fish-complete-progress");Call("ShowChapterProgress");Shot("chapter-progress");Call("EvidenceScroll",true,0f);Shot("chapter-progress-footer");
                var busy=Stage(11);Use(busy);Call("ShowPage","小店");World.SetRegion(2);Call("ShowPage","小店");Call("ConnectProductionPresentation");Call("RefreshProductionReceipts");busy.plots[0].remaining=.1;busy.machines[0].remaining=.1;busy.shelves[0].remaining=.2;long h=busy.harvests,c=busy.crafts,m=busy.sales;app.Engine.Advance(.3);Call("RefreshProductionReceipts");
                var counts=Field<long[]>("receiptCounts");Check(counts[0]==busy.harvests-h && counts[1]==busy.crafts-c && counts[2]==busy.sales-m,"HUD counts match actual Core settlement deltas");
                var effects=typeof(ForestWorld3D).GetField("productionEffects",Private).GetValue(World);foreach(string list in new[]{"plots","machines","shelves"})foreach(object burst in (System.Array)effects.GetType().GetField(list,Private).GetValue(effects))burst.GetType().GetField("started").SetValue(burst,Time.unscaledTime-.7f);Shot("actual-craft-receipt-chosen-visual-clock");
                busy.report.pending=true;Call("RefreshProductionReceipts");Check(!Field<RectTransform>("productionReceipt").gameObject.activeSelf,"Pending offline report does not replay receipt");busy.report.pending=false;busy.sales+=12;Call("RefreshProductionReceipts");Check(Field<long[]>("receiptCounts")[2]==12,"A new foreground receipt excludes old offline income");
                Use(Stage(11));Call("RefreshProductionReceipts");Check(!Field<RectTransform>("productionReceipt").gameObject.activeSelf,"Switching prepared state cannot replay historical counters");
                Check(frozenFiles.Select((file,i)=>Hash(file)==frozenHashes[i]).All(v=>v),"Source inputs unchanged throughout isolated capture");File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_JOURNEY_PREVIEW_OK "+Path.GetFullPath(Output));EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText(Output+"/checks.txt",log+"\nFAILED "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
