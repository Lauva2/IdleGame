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
    // Prepared Editor HUD checks. The app stays inactive, so Awake never opens
    // the player's SaveStore. Real UI handlers run against an in-memory state.
    [InitializeOnLoad]
    public static class ForestGuidancePreview
    {
        const string Pending = "ForestGuidancePreview.Pending";
        const string Output = "Builds/Previews/3d-d15-guidance";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static ForestApp app;
        static readonly StringBuilder checks = new StringBuilder();
        static ForestGuidancePreview()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                    EditorApplication.delayCall += Capture;
            };
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        static object Call(string method, params object[] args) => typeof(ForestApp).GetMethod(method, Private).Invoke(app, args);
        static T Field<T>(string name) => (T)typeof(ForestApp).GetField(name, Private).GetValue(app);
        static void Field(string name, object value) => typeof(ForestApp).GetField(name, Private).SetValue(app, value);
        static void Use(GameState state)
        {
            StateValidation.Check(state);
            typeof(ForestApp).GetProperty("State").SetValue(app, state);
            typeof(ForestApp).GetProperty("Engine").SetValue(app, new Economy(state));
            Field("shopBuilt", false); Field("shopPosition", 1f);
        }
        static ForestWorld3D World => Field<ForestWorld3D>("world3D");
        static void Check(bool valid, string label)
        { if (!valid) throw new Exception(label); checks.AppendLine("PASS " + label); }
        static void Click(string caption)
        {
            var button = UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b => b.interactable && b.GetComponentInChildren<Text>()?.text == caption);
            Check(button != null, "UI handler exists: " + caption); button.onClick.Invoke();
        }
        static void Shot(string name)
        {
            Field<Text>("coinsLabel").text = "金币  " + app.State.coins.ToString("N0");
            Field<Text>("reputationLabel").text = "声望  " + app.State.reputation;
            Field<Text>("goalLabel").text = app.Engine.Goal;
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("bindings")) binding();
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("modalBindings")) binding();
            World.Bind(app.State); World.RenderNow();
            // Static layout preview after transient feedback. No real-time claim.
            Field<Text>("toastLabel").transform.parent.gameObject.SetActive(false);
            CaptureHud(name);
        }
        static void CaptureHud(string name)
        {
            var canvas = Field<Canvas>("canvas"); var safe = Field<RectTransform>("safe");
            var scrolls = canvas.GetComponentsInChildren<ScrollRect>().Select(s => new { Scroll = s, Position = s.normalizedPosition }).ToArray();
            var cameraObject = new GameObject("Prepared HUD camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color32(255, 249, 228, 255);
            camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            var target = new RenderTexture(1080, 1920, 24); camera.targetTexture = target;
            var scaler = canvas.GetComponent<CanvasScaler>(); bool wasEnabled = scaler.enabled; scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.scaleFactor = 1;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            foreach (var scroll in scrolls) scroll.Scroll.normalizedPosition = scroll.Position;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(1080, 1920, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0); texture.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; camera.enabled = false; target.Release();
            UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(cameraObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; scaler.enabled = wasEnabled; Call("ApplySafeArea");
            Canvas.ForceUpdateCanvases();
            foreach (var scroll in scrolls) scroll.Scroll.normalizedPosition = scroll.Position;
        }
        static void PickFuture(int index)
        {
            Physics.SyncTransforms();
            var target = World.GetComponentsInChildren<ForestObject3D>().First(t => t.kind == "futurePlot" && t.index == index);
            var box = target.GetComponent<BoxCollider>();
            var uv = World.GetComponentInChildren<Camera>().WorldToViewportPoint(box.transform.TransformPoint(box.center));
            World.Pick(new Vector2(uv.x, uv.y));
        }
        static void Capture()
        {
            SessionState.SetBool(Pending, false);
            try
            {
                checks.Length = 0;
                var holder = new GameObject("Inactive prepared HUD app"); holder.SetActive(false);
                app = holder.AddComponent<ForestApp>();
                Check(Field<SaveStore>("store") == null, "Inactive app has not opened SaveStore");
                Field("recovering", true); Field("evidenceActive", true); Field("evidenceDirectory", Path.GetFullPath(Output));
                Field("font", Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));
                Field("round", Call("MakeRound"));
                Call("BuildShell");
                Use(new GameState { simpleAnimation = true });
                Call("FocusOpeningStep"); Call("ShowPage", "小店"); Call("ShowWelcome"); Shot("01-welcome");
                Click("接过钥匙"); Check(app.State.welcomeRead && World.Region == 1, "Welcome opens garden"); Shot("02-first-harvest-guide");
                Click("收获薄荷"); Check(app.State.tutorial == 1 && World.Region == 2 && app.State.crops[0] > 0, "Harvest routes to workshop"); Shot("03-workshop-guide");
                Click("前往坩埚"); Check(Field<string>("currentSheet") == "machine", "Workshop guide opens recipe sheet"); Shot("04-cauldron-recipe");
                Click("循环制作"); Check(app.State.tutorial == 2 && World.Region == 3 && app.State.machines[0].config == 0, "Recipe handler routes to shop"); Shot("05-shop-guide");
                Click("前往货架"); Check(Field<string>("currentSheet") == "shelf", "Shop guide opens shelf sheet");
                Click("选择售卖"); Check(app.State.tutorial == 3 && World.Region == 3 && app.State.shelves[0].config == 0, "Shelf handler arms automatic sales"); Shot("06-waiting-first-sale");
                app.Engine.Advance(16); Check(app.State.tutorial == 4 && app.State.sales > 0, "In-memory simulation reaches first sale"); Shot("07-first-sale-guide");
                Click("前往委托"); Check(Field<string>("currentPage") == "委托", "Completion guide opens orders"); Shot("08-first-order");
                var six = new GameState { welcomeRead = true, tutorial = 4, sideRoom = true, reputation = 150, coins = 6000, simpleAnimation = true };
                six.plots = Enumerable.Range(0, 6).Select(i => new Facility { config = i % 4, workItem = i % 4, remaining = 0 }).ToArray();
                Use(six); World.SetRegion(1); Call("ShowPage", "小店"); Shot("09-six-bed-next-expansion");
                Check(World.transform.Find("Handcrafted forest diorama/Twelve-bed woodland garden/Unbuilt garden bed 6/Next expansion supplies").gameObject.activeInHierarchy, "Only next bed has expansion supplies");
                Check(!World.transform.Find("Handcrafted forest diorama/Twelve-bed woodland garden/Unbuilt garden bed 7/Next expansion supplies").gameObject.activeInHierarchy, "Distant future bed remains quiet");
                PickFuture(6); Check(Field<string>("currentSheet") == "expansion", "Next bed ray opens expansion conditions"); Shot("10-expansion-conditions");
                Click("扩建地块"); Check(app.State.plots.Length == 7 && app.State.coins == 0, "Actual expansion UI buys only next bed at existing cost");
                World.SetRegion(1); Field("shopPosition", 1f); Call("ShowPage", "小店"); Shot("11-seven-bed-next-moved");
                Call("CloseSheet"); PickFuture(9); Check(Field<string>("currentSheet") == null && Field<Text>("toastLabel").text.Contains("第 8 块地"), "Far bed ray explains sequential expansion");
                Check(UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.GetComponentInChildren<Text>()?.text.StartsWith("远期田位") == true).All(b => !b.interactable), "Distant future buttons do not skip expansion order");
                Call("EvidenceScroll", false, 0f); Shot("12-future-bed-buttons");
                app.State.shelves = Enumerable.Range(0, 4).Select(i => new Facility { config = i }).ToArray();
                for (int i = 0; i < 6; i++) app.State.items[i] = 12;
                app.State.stories = 5;
                World.SetRegion(3); Field("shopPosition", 1f); Call("ShowPage", "小店"); Shot("13-old-shop-sign");
                app.State.decoration[0] = true; World.Bind(app.State); Shot("14-reward-shop-sign");
                Check(!holder.activeInHierarchy && Field<SaveStore>("store") == null && Field<bool>("evidenceActive") && Field<bool>("recovering"), "No active app, SaveStore or normal-player persistence");
                File.WriteAllText(Output + "/handler-checks.txt", checks.ToString());
                File.WriteAllText(Output + "/preview-scope.txt", "EDITOR OFFSCREEN PREPARED HUD PREVIEW, NOT ANDROID. Target=1080x1920, full simulated safe rectangle.\nHost Editor Screen=" + Screen.width + "x" + Screen.height + "; modal height still uses host Screen dimensions, so modal viewport/touch fit is NOT established by these offscreen images.\nInactive ForestApp; no Awake, SaveStore, audio, device install or ordinary player progress writes. UI handlers and future-bed rays exercised against memory.\nFirst sale uses 16 simulated seconds; not real-time play or continuous dynamic evidence. Transient toast hidden for static layout captures.\nSix-bed and stocked-shop states are prepared, not imported normal saves. No final art/functional/phone acceptance.\nUnity=" + Application.unityVersion + "\n");
                Debug.Log("FOREST_GUIDANCE_PREVIEW_OK " + Path.GetFullPath(Output)); EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
