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
    public static class ForestSupplyPreview
    {
        const string Pending="ForestSupplyPreview.Pending";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static string Output=>SessionState.GetString("ForestSupplyPreview.Output","");
        static ForestApp app;static readonly StringBuilder log=new StringBuilder();
        static ForestSupplyPreview(){EditorApplication.playModeStateChanged+=m=>{if(m==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending,false))EditorApplication.delayCall+=Capture;};}
        public static void Run()
        {
            var args=Environment.GetCommandLineArgs();string output=null;
            SessionState.SetBool("ForestSupplyPreview.Details",args.Contains("-forestPreviewDetails"));
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-forestPreviewOutput")output=args[i+1];
            var absolute=Path.GetFullPath(output??throw new ArgumentException("Explicit preview output required"));
            if(!absolute.StartsWith(Path.GetFullPath("Builds/Previews")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase) || Directory.Exists(absolute))throw new IOException("Use a new local preview directory");
            Directory.CreateDirectory(absolute);SessionState.SetString("ForestSupplyPreview.Output",absolute);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);SessionState.SetBool(Pending,true);EditorApplication.EnterPlaymode();
        }
        static object Call(string name,params object[] args)=>typeof(ForestApp).GetMethod(name,Private).Invoke(app,args);
        static T Field<T>(string name)=>(T)typeof(ForestApp).GetField(name,Private).GetValue(app);
        static void Field(string name,object value)=>typeof(ForestApp).GetField(name,Private).SetValue(app,value);
        static ForestWorld3D World=>Field<ForestWorld3D>("world3D");
        static void Check(bool ok,string name){if(!ok)throw new Exception(name);log.AppendLine("PASS "+name);}
        static void Refresh()
        {
            Call("RefreshHeader");foreach(var a in Field<System.Collections.Generic.List<Action>>("bindings"))a();
            foreach(var a in Field<System.Collections.Generic.List<Action>>("modalBindings"))a();
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

        static void Use(GameState s)
        {
            Call("CloseSheet");typeof(ForestApp).GetProperty("State").SetValue(app,s);typeof(ForestApp).GetProperty("Engine").SetValue(app,new Economy(s));
            Field("shopBuilt",false);Call("ShowPage","小店");Region(2);
        }
        static void Click(string caption)
        {
            var button=Field<RectTransform>("sheetContent").GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text==caption);
            button.onClick.Invoke();
        }
        static GameState Base()=>new GameState{tutorial=4,welcomeRead=true,stories=2,reputation=30,machines=new[]{new Facility{config=0},new Facility{config=2}},plots=new[]{new Facility{config=0,workItem=0,remaining=8},new Facility{config=1}},simpleAnimation=true};
        static void Capture()
        {
            SessionState.SetBool(Pending,false);
            try
            {
                var holder=new GameObject("Inactive isolated supply fixture");holder.SetActive(false);app=holder.AddComponent<ForestApp>();
                Field("recovering",true);Field("evidenceActive",true);Field("font",Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));Field("round",Call("MakeRound"));Call("BuildShell");
                if(SessionState.GetBool("ForestSupplyPreview.Details",false))
                {
                    var detail=Base();detail.orders=new[]{new Order{id=1,item=2,count=5,active=true}};detail.machines[1]=new Facility{config=1,workItem=1,remaining=12,pending=2};Use(detail);
                    string original=JsonUtility.ToJson(detail);Call("OpenObject","machine",1);
                    Check(Field<RectTransform>("sheetContent").GetComponentsInChildren<Text>().Any(t=>t.text.Contains("寻物粉 · 查看材料来源")),"Queued recipe supply entry names its actual destination");
                    Click("备货路线");Shot("01-pending-text-and-materials-top");
                    Call("EvidenceScroll",true,0f);Shot("02-pending-materials-and-footer");Click("看配方");
                    Check(Field<string>("currentSheet")=="machine" && World.Region==2,"Scrolled footer opens actual recipe panel");
                    Check(JsonUtility.ToJson(detail)==original && Field<SaveStore>("store")==null,"Focused guide checks preserve every saved field without ordinary store");
                    File.WriteAllText(Output+"/scope.txt","Two focused Editor native1080x1920 views only: revised pending text and actual bottom scroll. Isolated prepared state, no Android fit or final art acceptance. No ordinary SaveStore.\n");
                    File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_SUPPLY_PREVIEW_OK "+Output);EditorApplication.Exit(0);return;
                }
                var s=Base();s.orders=new[]{new Order{id=1,item=0,count=3,active=true}};Use(s);string before=JsonUtility.ToJson(s);
                Check(Field<SaveStore>("store")==null,"No ordinary SaveStore opened");
                Shot("01-missing-mint-current-goal");
                Field<RectTransform>("safe").Find("Current goal").GetComponent<Button>().onClick.Invoke();
                Check(Field<string>("currentSheet")=="plot" && World.Region==1,"Top goal actual button opens growing ingredient plot");
                Check(JsonUtility.ToJson(s)==before,"Top goal navigation changes no saved fields");
                Call("ShowSupplyGuide",0);Shot("02-tea-supply-route");
                s.items[0]=3;Refresh();Check(Field<RectTransform>("sheetContent").GetComponentsInChildren<Text>().Any(t=>t.text=="去交付"),"Open guide updates its action when prepared stock becomes ready");s.items[0]=0;Refresh();
                Click("去庭院");Check(Field<string>("currentSheet")=="plot","Supply route button opens actual plot");Call("CloseSheet");
                Call("ShowPage","委托");
                var orderButton=Field<RectTransform>("page").GetComponentsInChildren<Button>().First(b=>b.GetComponentInChildren<Text>()?.text=="备货路线");
                orderButton.onClick.Invoke();Check(Field<string>("currentSheet")=="supply","Active order route button opens supply sheet");
                Check(JsonUtility.ToJson(s)==before,"Order guide reads and navigation do not configure or reward");
                s=Base();s.orders=new[]{new Order{id=1,item=2,count=5,active=true}};s.machines[1]=new Facility{config=1,workItem=1,remaining=12,pending=2};Use(s);
                before=JsonUtility.ToJson(s);Call("ShowSupplyGuide",2);Shot("03-pending-recipe-and-two-materials");Call("CloseSheet");Check(JsonUtility.ToJson(s)==before,"Pending recipe guide leaves queued/current work intact");
                s=Base();s.items[0]=3;s.orders=new[]{new Order{id=1,item=0,count=3,active=true,reserve=true}};Use(s);before=JsonUtility.ToJson(s);
                Call("ShowSupplyGuide",0);Shot("04-ready-reserved-order");Click("去交付");Check(Field<string>("currentPage")=="委托" && s.orders[0].active,"Delivery shortcut opens board without auto-delivering");Check(JsonUtility.ToJson(s)==before,"Delivery navigation consumes no items and grants no reward");
                s=JsonUtility.FromJson<GameState>(File.ReadAllLines("Docs/Verification/3d-d27-life03/ordinary-final.save").Last());
                s.report.pending=false;s.orders=new[]{new Order{id=1,item=7,count=2,active=true,forestStory=true}};s.items[7]=0;s.crops[3]=1;s.crops[5]=0;s.machines[1]=new Facility{config=7};s.forest.beds[0]=new Facility{config=5,workItem=5,remaining=17};Use(s);before=JsonUtility.ToJson(s);
                Call("ShowSupplyGuide",7);Shot("05-greenhouse-dew-route");Click("去温室");Check(Field<string>("currentSheet")=="nursery" && World.Region==7,"Dew route opens actual greenhouse nursery");Check(JsonUtility.ToJson(s)==before,"Greenhouse guidance does not start or finish crops");
                s.orders=new[]{new Order{id=2,item=8,count=2,active=true,forestStory=true}};s.items[8]=0;s.crops[2]=1;s.crops[6]=0;s.machines[1]=new Facility{config=8};s.forest.regrow[2]=42;Use(s);before=JsonUtility.ToJson(s);
                Call("ShowSupplyGuide",8);Shot("06-resin-cooldown-route");Call("CloseSheet");Check(JsonUtility.ToJson(s)==before,"Resin route reads do not gather or shorten cooldown");
                s=Base();s.stories=5;s.sideRoom=true;s.reputation=150;s.sales=60000;s.sold=new[]{0,1,1,1,1,1,0,0,0};s.items[0]=1;Use(s);before=JsonUtility.ToJson(s);
                var step=Call("NextJourneyStep");Check((string)step.GetType().GetField("Target").GetValue(step)=="shelf","First-unsold goal names the real available product");
                Field<RectTransform>("safe").Find("Current goal").GetComponent<Button>().onClick.Invoke();Shot("07-first-sale-shelf-destination");Check(Field<string>("currentSheet")=="shelf" && World.Region==3,"First-sale goal opens an actual shelf");Check(JsonUtility.ToJson(s)==before,"First-sale navigation does not sell");
                File.WriteAllText(Output+"/scope.txt","Prepared local Editor previews only; inactive ForestApp, no ordinary SaveStore, ticking, Android install or final art acceptance. Actual button handlers tested with isolated states. Host Screen limits sheet height; no physical or Android fit claim.\n");
                File.WriteAllText(Output+"/checks.txt",log.ToString());Debug.Log("FOREST_SUPPLY_PREVIEW_OK "+Output);EditorApplication.Exit(0);
            }
            catch(Exception ex){File.WriteAllText(Output+"/checks.txt",log+"FAIL "+ex);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
