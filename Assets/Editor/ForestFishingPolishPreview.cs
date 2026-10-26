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
    public static class ForestFishingPolishPreview
    {
        const string Pending="ForestFishingPolishPreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestFishingPolishPreview.Output","Builds/Previews/3d-d22-fishing-dev-r01");
        static ForestApp app;static string baseline;static readonly StringBuilder log=new StringBuilder();
        static string[] frozenFiles,frozenHashes;
        static string Hash(string file){using(var sha=SHA256.Create())using(var stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","");}
        static void Freeze()
        {
            frozenFiles=new[]{"Assets","Packages","ProjectSettings"}.SelectMany(p=>Directory.GetFiles(p,"*",SearchOption.AllDirectories)).OrderBy(p=>p).ToArray();frozenHashes=frozenFiles.Select(Hash).ToArray();
            File.WriteAllLines(Output+"/source-files.sha256",frozenFiles.Select((file,i)=>frozenHashes[i]+"  "+file.Replace('\\','/')));
        }
        static ForestFishingPolishPreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output="Builds/Previews/3d-d22-fishing-dev-r01";
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            string absolute=Path.GetFullPath(output),allowed=Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar;
            if(!absolute.StartsWith(allowed,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Preview must be inside local Builds/Previews");
            if(Directory.Exists(output))throw new IOException("Keep original preview; choose a new revision");
            Directory.CreateDirectory(output);SessionState.SetString("ForestFishingPolishPreview.Output",output);
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
            if(Field<string>("currentSheet")=="fishing")Call("FrameFishing",World,app.State.fishing.spot,true);
            World.RenderNow();foreach(var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>())m.Refresh();Canvas.ForceUpdateCanvases();
            if(Field<string>("currentSheet")=="fishing")CheckBasketFrame();c.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(1080,1920,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1080,1920),0,0);texture.Apply();File.WriteAllBytes(Output+"/"+name+".png",texture.EncodeToPNG());
            RenderTexture.active=previous;c.targetTexture=null;c.enabled=false;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(c.gameObject);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;scaler.enabled=enabled;Call("ApplySafeArea");Canvas.ForceUpdateCanvases();foreach(var s in scrolls)s.Scroll.normalizedPosition=s.Position;
        }
        static void LineConnection(bool mouth)
        {
            Refresh();var world=World;var type=typeof(ForestWorld3D);int spot=app.State.fishing.spot;
            var lines=(LineRenderer[])type.GetField("fishingLines",Private).GetValue(world);
            var tips=(Transform[])type.GetField("fishingRodTips",Private).GetValue(world);
            var docks=(Transform[])type.GetField("fishingDocks",Private).GetValue(world);
            Check(Vector3.Distance(docks[spot].TransformPoint(lines[spot].GetPosition(0)),tips[spot].position)<.001f,"Line stays on actual moving rod guide");
            if(mouth){var fish=(Transform)type.GetField("fishReceipt",Private).GetValue(world);var caught=app.State.fishing.recent[0];Check(Vector3.Distance(docks[spot].TransformPoint(lines[spot].GetPosition(12)),fish.TransformPoint(ForestFishingModels.MouthPosition(caught.species)))<.001f,"Reeled line stays on actual fish mouth");}
        }
        static void CheckBasketFrame()
        {
            var pointer=Field<RectTransform>("page").GetComponentInChildren<ForestWorldPointer>();var view=(RectTransform)pointer.transform;
            var corners=new Vector3[4];var paper=new Vector3[4];view.GetWorldCorners(corners);Field<RectTransform>("sheetPanel").GetWorldCorners(paper);
            var type=typeof(ForestWorld3D);var camera=(Camera)type.GetField("worldCamera",Private).GetValue(World);var baskets=(Transform[])type.GetField("fishingBaskets",Private).GetValue(World);
            var uv=camera.WorldToViewportPoint(baskets[app.State.fishing.spot].position+Vector3.up*.2f);float y=Mathf.Lerp(corners[0].y,corners[1].y,uv.y),margin=(corners[1].y-corners[0].y)*.01f;
            Check(y>Mathf.Max(corners[0].y,paper[1].y)+margin && y<corners[1].y-margin,"Basket framed inside exposed world above actual paper sheet");
        }
        static void Notice(string expected)
        {
            var s=Fresh();int seed=1;FishCatch candidate=null;
            for(;seed<10000;seed++){s.fishing=new FishingProgress{random=seed};var rules=new Economy(s).Fishing;rules.Start(0,0);candidate=s.fishing.candidate;var k=FishingCatalog.Species[candidate.species];if(candidate.lengthMm>k.MinLength+12 && candidate.lengthMm<k.MaxLength-12 && candidate.weightGrams>k.MinWeight+4 && candidate.weightGrams<k.MaxWeight-4)break;}
            s.fishing=new FishingProgress{random=seed};
            if(expected!="新发现")
            {
                bool longer=expected=="长度新纪录"||expected=="长度与重量新纪录",heavier=expected=="重量新纪录"||expected=="长度与重量新纪录";
                var old=new FishCatch{sequence=1,species=candidate.species,quality=0,lengthMm=candidate.lengthMm+(longer?-10:10),weightGrams=candidate.weightGrams+(heavier?-3:3),spot=0,bait=0};
                s.fishing.totalCaught=s.fishing.casts=1;s.fishing.recent=new[]{old};var e=s.fishing.entries[old.species];e.count=1;e.qualityCounts[0]=1;e.bestQuality=0;e.firstSequence=1;e.longestMm=old.lengthMm;e.heaviestGrams=old.weightGrams;
            }
            Use(s);Call("ShowFishing",0);Call("AdvanceFishing",(float)s.fishing.remaining+1.81f);
            Check(Field<string>("fishNotice")==expected,"Actual catch notice: "+expected);Check(Field<int>("fishNoticeSequence")==s.fishing.totalCaught,"Notice belongs to actual landed sequence");StateValidation.Check(s);
            Shot("notice-"+expected);LineConnection(true);
            var drops=(Transform[])typeof(ForestWorld3D).GetField("fishingDrops",Private).GetValue(World);
            Check(drops.All(d=>!d.gameObject.activeSelf || d.localScale.x<=.05f && d.localScale.y<=.09f && d.localScale.z<=.05f),"Splash remains small after presentation update");
            if(expected=="新发现"){Call("AdvanceFishing",.55f);Shot("motion-04-out-of-water");LineConnection(true);Call("AdvanceFishing",1.05f);Shot("motion-05-into-basket");Call("AdvanceFishing",.70f);Shot("motion-06-next-cast");LineConnection(false);}
        }
        static void LandSpecies(int species)
        {
            var s=Fresh();int spot=0;while((FishingCatalog.Species[species].Spots&(1<<spot))==0)spot++;s.forest.location=spot;
            int seed=1;for(;seed<100000;seed++){s.fishing=new FishingProgress{random=seed};new Economy(s).Fishing.Start(spot,0);if(s.fishing.candidate.species==species)break;}
            Check(seed<100000,"Legal seed exists for landing sculpture "+species);
            s.fishing=new FishingProgress{random=seed};Use(s);Call("ShowFishing",spot);Call("AdvanceFishing",(float)s.fishing.remaining+1.81f);Call("AdvanceFishing",1.70f);Refresh();
            var fish=(Transform)typeof(ForestWorld3D).GetField("fishReceipt",Private).GetValue(World);var baskets=(Transform[])typeof(ForestWorld3D).GetField("fishingBaskets",Private).GetValue(World);
            float min=float.PositiveInfinity;Bounds footprint=new Bounds();bool first=true;
            foreach(var renderer in fish.GetComponentsInChildren<MeshRenderer>())if(renderer.enabled)foreach(var vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices){var world=renderer.transform.TransformPoint(vertex);min=Mathf.Min(min,world.y);var local=baskets[spot].InverseTransformPoint(world);if(first){footprint=new Bounds(local,Vector3.zero);first=false;}else footprint.Encapsulate(local);}
            Check(min>=baskets[spot].position.y+.09f,"Settled sculpture remains above basket lining "+species);
            Check(Mathf.Abs(footprint.center.x)<.01f && Mathf.Abs(footprint.center.z)<.01f && footprint.extents.x<.63f,"Actual mesh is centred and fitted to basket "+species);Shot("landed-body-"+species.ToString("D2"));
        }
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                Freeze();
                File.WriteAllText(Output+"/scope.txt","ISOLATED PREPARED EDITOR VIEWS. No SaveStore/device. Rules advanced with simulated seconds; poses use persisted phase progress. Native offscreen1080x1920; Host Screen="+Screen.width+"x"+Screen.height+" affects sheet cap. Not Android adaptation, continuous dynamics, probability or final acceptance.\n");
                baseline=File.ReadAllLines("Docs/Verification/3d-d21-forest05/ordinary-final.save").Last();
                var holder=new GameObject("Inactive isolated polish fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();Check(Field<SaveStore>("store")==null,"No ordinary SaveStore opened");
                Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");Use(Fresh());
                Check(Resources.Load<Shader>("ForestWater").isSupported,"Painted depth shader is supported by actual Editor graphics device");
                for(int spot=0;spot<3;spot++)
                {
                    Call("ShowFishing",spot);Shot("habitat-"+spot+"-cast");Call("AdvanceFishing",.6f);Shot("habitat-"+spot+"-wait");LineConnection(false);
                    var floats=(Transform[])typeof(ForestWorld3D).GetField("fishingFloats",Private).GetValue(World);
                    Check(Vector3.Distance(floats[spot].localPosition,new Vector3(.2f,.19f,2.4f))<.001f,"Cast settles at actual water contact "+spot);
                    Check(World.FishingFocused && UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>().All(m=>!m.GetComponent<Button>().interactable),"Fishing context removes world marker interception "+spot);
                    World.Zoom(.5f);Check(World.CameraSize<5,"Fishing camera can zoom closer than map limit");World.ResetCamera();Check(World.FishingFocused,"Recenter retains fishing context");
                    Call("CloseSheet");Check(!app.State.fishing.running && !World.FishingFocused,"Leaving restores map context and pauses "+spot);Shot("left-habitat-"+spot);
                }
                Use(Fresh());Call("ShowFishing",0);Call("AdvanceFishing",(float)app.State.fishing.remaining+.01f);Shot("motion-01-bite");LineConnection(false);Call("AdvanceFishing",.85f);Shot("motion-02-reeling");LineConnection(false);
                foreach(string notice in new[]{"新发现","长度新纪录","重量新纪录","长度与重量新纪录","自动钓获"})Notice(notice);
                for(int species=0;species<12;species++)LandSpecies(species);
                var filled=Fresh();var engine=new Economy(filled);for(int spot=0;spot<3;spot++){filled.forest.location=spot;engine.Fishing.Start(spot,0);engine.Fishing.Advance(60000);engine.Fishing.Pause();}
                Use(filled);Check(app.Engine.Fishing.Discovered==12,"Prepared rule samples cover12 species");Call("ShowFishCollection");Shot("collection-all");
                for(int species=0;species<12;species++){Call("ShowFishSpecies",species);Shot("species-"+species.ToString("D2")+"-top");Call("EvidenceScroll",true,0f);Shot("species-"+species.ToString("D2")+"-bottom");}
                Field("fishFilter",2);Call("ShowFishCollection");Shot("recent-top");Call("EvidenceScroll",false,0f);Shot("recent-bottom");Call("ShowFishCatch",filled.fishing.recent[0]);Shot("individual");
                filled.simpleAnimation=true;Call("ShowFishing",2);Call("AdvanceFishing",(float)filled.fishing.remaining+1.81f);Shot("reduced-landing");
                var rods=(Transform[])typeof(ForestWorld3D).GetField("fishingRods",Private).GetValue(World);Check(Quaternion.Angle(rods[2].localRotation,Quaternion.identity)<.01f,"Reduced animation keeps rod stable");
                Call("ShowCredits");Shot("credits-version");Check(Field<RectTransform>("sheetContent").GetComponentsInChildren<Text>().Any(t=>t.text.Contains("版本 "+Application.version)),"Credits uses actual application version");
                StateValidation.Check(filled);Check(frozenFiles.Select((file,i)=>Hash(file)==frozenHashes[i]).All(v=>v),"Source inputs unchanged throughout isolated capture");File.WriteAllText(Output+"/checks.txt",log.ToString());File.WriteAllText(Output+"/prepared-final.json",JsonUtility.ToJson(filled,true));Debug.Log("FOREST_FISHING_POLISH_PREVIEW_OK "+Path.GetFullPath(Output));EditorApplication.Exit(0);
            }
            catch(Exception e){File.WriteAllText(Output+"/checks.txt",log+"\nFAILED "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
