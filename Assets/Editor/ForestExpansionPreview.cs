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
    // Isolated development preview. Explicit prepared chapter-end state;
    // no ForestApp.Awake, SaveStore, persistent player save, or device access.
    [InitializeOnLoad]
    public static class ForestExpansionPreview
    {
        const string Pending = "ForestExpansionPreview.Pending";
        const string Output = "Builds/Previews/3d-d21-forest-dev-r03";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static ForestApp app;
        static readonly StringBuilder log = new StringBuilder();
        static ForestExpansionPreview()
        {
            EditorApplication.playModeStateChanged += mode => { if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) EditorApplication.delayCall += Capture; };
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        static object Call(string name, params object[] args) => typeof(ForestApp).GetMethod(name, Private).Invoke(app, args);
        static T Field<T>(string name) => (T)typeof(ForestApp).GetField(name, Private).GetValue(app);
        static void Field(string name, object value) => typeof(ForestApp).GetField(name, Private).SetValue(app, value);
        static ForestWorld3D World => Field<ForestWorld3D>("world3D");
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); log.AppendLine("PASS " + message); }
        static void Click(string caption, string row = null)
        {
            var button = UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b => b.interactable && b.GetComponentInChildren<Text>()?.text == caption && (row == null || b.transform.parent.name == row));
            Check(button != null, "UI button " + caption + " / " + row); button.onClick.Invoke();
        }
        static void Pick(string kind, int index)
        {
            Physics.SyncTransforms(); string selected = null; int selectedIndex = -1;
            var target = World.GetComponentsInChildren<ForestObject3D>().First(t => t.kind == kind && t.index == index);
            var box = target.GetComponent<BoxCollider>(); var camera = World.GetComponentsInChildren<Camera>().First(c => c.name == "World camera");
            var at = camera.WorldToViewportPoint(box.transform.TransformPoint(box.center));
            var actual = World.Selected; World.Selected = (k, i) => { selected = k; selectedIndex = i; actual(k, i); };
            World.Pick(new Vector2(at.x, at.y)); World.Selected = actual;
            Check(selected == kind && selectedIndex == index, "Actual 3D ray " + kind + index);
        }
        static void Refresh()
        {
            Call("RefreshHeader");
            foreach (var action in Field<System.Collections.Generic.List<Action>>("bindings")) action();
            foreach (var action in Field<System.Collections.Generic.List<Action>>("modalBindings")) action();
            foreach (var visual in UnityEngine.Object.FindObjectsOfType<ForestButtonVisual>()) typeof(ForestButtonVisual).GetMethod("LateUpdate", Private).Invoke(visual, null);
            World.Bind(app.State); World.RenderNow();
        }
        static void Shot(string name)
        {
            Refresh(); Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            var canvas = Field<Canvas>("canvas"); var safe = Field<RectTransform>("safe");
            var scrolls = canvas.GetComponentsInChildren<ScrollRect>().Select(s => new { Scroll = s, Position = s.normalizedPosition }).ToArray();
            var c = new GameObject("Prepared expansion HUD camera").AddComponent<Camera>(); c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = new Color32(255,249,228,255); c.orthographic = true; c.nearClipPlane = .1f; c.farClipPlane = 100;
            var rt = new RenderTexture(1080,1920,24); c.targetTexture = rt;
            var scaler = canvas.GetComponent<CanvasScaler>(); bool enabled = scaler.enabled; scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = c; canvas.planeDistance = 10; canvas.scaleFactor = 1;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases(); foreach (var s in scrolls) s.Scroll.normalizedPosition = s.Position;
            foreach (var m in UnityEngine.Object.FindObjectsOfType<ForestExploreMarker>()) m.Refresh();
            Canvas.ForceUpdateCanvases(); c.Render(); var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(1080,1920,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1080,1920),0,0); texture.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
            RenderTexture.active = previous; c.targetTexture = null; c.enabled = false; rt.Release(); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(c.gameObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; scaler.enabled = enabled; Call("ApplySafeArea");
            Canvas.ForceUpdateCanvases(); foreach (var s in scrolls) s.Scroll.normalizedPosition = s.Position;
        }
        static void Request(int item)
        {
            app.Engine.Advance(0); Call("ShowPage", "委托"); Shot("request-" + item + "-before");
            var order = app.State.orders.Single(o => o.forestStory && o.item == item);
            Check(app.Engine.ActiveOrder == null, "No concurrent prepared order");
            // Select the actual forest card, not an ordinary request for the same good.
            var card = Field<RectTransform>("content").Find("Order " + order.id);
            card.GetComponentsInChildren<Button>().Single(b => b.GetComponentInChildren<Text>().text == "接受委托").onClick.Invoke();
            Check(order.active && order.reserve, "Forest request accepted and reserved");
            app.Engine.Advance(200);
            Call("ShowPage", "委托"); Refresh(); Click("交付委托");
            Check(app.State.forest.requests == item - 5 && app.State.stories == 5, "New request leaves chapter story count intact");
            Call("ShowForestDialogue"); Shot("request-" + item + "-after"); Call("CloseSheet");
        }
        static void Capture()
        {
            SessionState.SetBool(Pending, false); log.Length = 0;
            try
            {
                File.WriteAllText(Output + "/scope.txt", "ISOLATED EDITOR DEVELOPMENT PREVIEW. Prepared chapter-end economy, existing raw materials/gold, no new discoveries initially. UI/ray handlers plus explicitly simulated seconds. No ordinary save, device, natural time or final art acceptance.\nHost Editor Screen=" + Screen.width + "x" + Screen.height + "; offscreen target=1080x1920. Sheets still use host Screen for height; button/safe-area reachability is NOT established by these offscreen images.\n");
                var holder = new GameObject("Inactive isolated forest expansion UI"); holder.SetActive(false); app = holder.AddComponent<ForestApp>();
                Check(Field<SaveStore>("store") == null, "No SaveStore opened"); Field("recovering", true); Field("evidenceActive", true);
                Field("font", Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular")); Field("round", Call("MakeRound")); Call("BuildShell");
                var state = new GameState { welcomeRead = true, tutorial = 4, stories = 5, reputation = 180, sales = 60000, coins = 30000, warehouse = 4, sideRoom = true, chapterComplete = true, chapterCelebrated = true, simpleAnimation = false };
                state.plots = Enumerable.Range(0,12).Select(i => new Facility { config = i%4, level = 2 }).ToArray();
                state.machines = new[] { new Facility(), new Facility() }; state.shelves = Enumerable.Range(0,4).Select(i => new Facility()).ToArray();
                for(int i=0;i<4;i++) state.crops[i] = 60; for(int i=0;i<6;i++) { state.items[i] = 12; state.sold[i] = 1; }
                StateValidation.Check(state); typeof(ForestApp).GetProperty("State").SetValue(app,state); typeof(ForestApp).GetProperty("Engine").SetValue(app,new Economy(state));
                Call("ShowPage", "小店"); Shot("01-courtyard-entrance"); Call("OpenObject", "forest-entry", 0); Shot("02-invitation"); Click("走进林径");
                Check(World.Region == 4 && state.forest.started, "Entrance handler starts exploration"); Shot("03-forest-edge"); Pick("forage",0); Shot("04-berry-discovery"); Click("采集"); Check(state.crops[4] == 3 && state.forest.discovered == 1, "Berry gather actual UI"); Shot("05-gather-receipt");
                Call("GoGreenhouse"); Shot("06-old-greenhouse"); Call("OpenObject","forest-repair",0); Shot("07-greenhouse-requirements"); Click("修复"); Check(state.forest.beds.Length == 4,"Four greenhouse slots restored"); Shot("08-greenhouse-cutaway");
                Call("OpenObject","nursery",0); Shot("09-nursery-choice"); Call("CloseSheet"); Call("OpenObject","machine",0); Call("EvidenceScroll",true,0f); Shot("10-new-tea-recipe"); Click("循环制作",Catalog.Items[6]); Call("CloseSheet"); Request(6);
                Call("GoForest",0); Pick("forest-repair",1); Shot("11-broken-bridge-requirements"); Click("修复"); Check(state.forest.bridge,"Bridge repaired by goods"); Shot("12-restored-bridge"); Call("GoForest",1); Shot("13-creek-wetland"); Pick("forage",1); Shot("14-dew-discovery"); Click("采集");
                Call("GoGreenhouse"); for(int i=2;i<4;i++) { Call("OpenObject","nursery",i); Click("培育种苗",Catalog.Crops[5]); Call("CloseSheet"); } app.Engine.Advance(65); Shot("15-two-species-greenhouse");
                Call("OpenObject","machine",1); Click("循环制作",Catalog.Items[7]); Call("CloseSheet"); Request(7);
                Call("GoForest",1); Call("OpenObject","forest-repair",2); Shot("16-vine-requirements"); Click("修复"); Shot("17-open-vine-path"); Call("GoForest",2); Shot("18-mossy-grove"); Pick("forage",2); Shot("19-resin-discovery"); Click("采集");
                Call("OpenObject","machine",1); Click("循环制作",Catalog.Items[8]); Call("CloseSheet"); Request(8);
                Call("GoForest",2); Pick("forage",2); Click("采集"); Pick("forest-repair",3); Shot("20-pavilion-requirements"); app.Engine.Advance(200); Refresh(); Click("修复"); Check(state.forest.pavilion,"Pavilion closes expansion loop"); Shot("21-restored-pavilion");
                Call("ShowForestCelebration"); Shot("22-forest-ending"); Click("看看旧亭"); Call("ShowForestJournal"); Shot("23-discovery-journal");
                StateValidation.Check(state); File.WriteAllText(Output + "/prepared-final.json",JsonUtility.ToJson(state,true));
                File.WriteAllText(Output + "/checks.txt",log.ToString()); Debug.Log("FOREST_EXPANSION_PREVIEW_OK " + Path.GetFullPath(Output)); EditorApplication.Exit(0);
            }
            catch(Exception e) { File.WriteAllText(Output + "/checks.txt",log + "\nFAILED " + e); Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
