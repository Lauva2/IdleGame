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
        static readonly string Output = PreviewOutput();
        static string PreviewOutput()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-forestPreviewOutput")
            {
                string output = Path.GetFullPath(args[i + 1]);
                if (!output.StartsWith(Path.GetFullPath("Builds/Previews") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Preview output must be inside Builds/Previews");
                return output;
            }
            return "Builds/Previews/3d-d19-experience-hud";
        }
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
        static void ClickName(string name)
        {
            var button = UnityEngine.Object.FindObjectsOfType<Button>().FirstOrDefault(b => b.interactable && b.name == name);
            Check(button != null, "Named UI handler exists: " + name); button.onClick.Invoke();
        }
        static void Shot(string name)
        {
            Call("RefreshHeader");
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("bindings")) binding();
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("modalBindings")) binding();
            foreach (var visual in UnityEngine.Object.FindObjectsOfType<ForestButtonVisual>())
                typeof(ForestButtonVisual).GetMethod("LateUpdate", Private).Invoke(visual, null);
            World.Bind(app.State); World.RenderNow();
            foreach (var marker in UnityEngine.Object.FindObjectsOfType<ForestWorldMarker>()) marker.Refresh();
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
        static Text GuideDetail => UnityEngine.Object.FindObjectsOfType<Text>().First(t => t.name == "Opening guide detail");
        static void RefreshBindings()
        {
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("bindings")) binding();
            foreach (var binding in Field<System.Collections.Generic.List<Action>>("modalBindings")) binding();
        }
        static void CheckWaitingGuide()
        {
            var waiting = new GameState { welcomeRead = true, tutorial = 3, harvests = 1, simpleAnimation = true };
            waiting.plots[0].config = waiting.plots[0].workItem = 0; waiting.plots[0].remaining = 12.1;
            waiting.machines[0].config = waiting.machines[0].workItem = 0; waiting.machines[0].remaining = 9.2;
            waiting.shelves[0].config = 0; waiting.shelves[0].remaining = 5.2;
            Use(waiting); World.SetRegion(3); Call("ShowPage", "小店"); RefreshBindings();
            Check(GuideDetail.text.Contains("加工中") && GuideDetail.text.Contains("10秒"), "Waiting guide describes the actual unfinished machine cycle");
            ClickName("Opening action");
            Check(World.Region == 2 && Field<string>("currentSheet") == "machine", "Waiting guide opens the unfinished machine rather than an empty shelf");
            Shot("21-waiting-machine-details"); Call("CloseSheet");
            waiting.items[0] = 1; Call("ShowPage", "小店"); RefreshBindings();
            Check(GuideDetail.text.Contains("醒神茶 1份") && GuideDetail.text.Contains("6秒"), "Ready stock guide reads the actual inventory and shelf interval");
            ClickName("Opening action");
            Check(World.Region == 3 && Field<string>("currentSheet") == "shelf", "Ready stock guide opens its sales shelf");
            Shot("22-waiting-sales-details"); Call("CloseSheet");
            waiting.items[0] = 0; waiting.machines[0].workItem = -1; waiting.machines[0].remaining = -1;
            Call("ShowPage", "小店"); RefreshBindings();
            Check(GuideDetail.text.Contains("薄荷生长中") && GuideDetail.text.Contains("13秒"), "Missing ingredient guide uses actual crop growth instead of a made-up income timer");
            ClickName("Opening action");
            Check(World.Region == 1 && Field<string>("currentSheet") == "plot", "Missing ingredient guide returns to the garden plot");
            Shot("23-waiting-crop-details"); Call("CloseSheet");
            waiting.items[0] = 1;
            waiting.orders = new[] { new Order { id = 100, item = 0, count = 3, resident = 0, active = true, reserve = true } };
            waiting.nextOrderId = 101;
            Call("ShowPage", "小店"); RefreshBindings();
            Check(GuideDetail.text.Contains("已预留"), "Reserved tutorial stock is explained instead of promising an impossible sale");
            Shot("24-waiting-reserved-stock"); ClickName("Opening action");
            Check(Field<string>("currentPage") == "委托" && waiting.tutorial == 3 && waiting.sales == 0, "Guide navigation cannot manufacture the first sale or complete the tutorial");
        }
        static void CheckCompactSheets()
        {
            Call("ShowResident", 0);
            var inner = Field<RectTransform>("sheetContent");
            var paragraphs = inner.GetComponentsInChildren<Text>().Where(t => t.transform.parent == inner).ToArray();
            Check(paragraphs.Length == 3 && paragraphs.All(t => t.rectTransform.rect.height >= t.preferredHeight), "Actual resident paragraphs fit their complete measured Chinese text");
            Check(paragraphs[0].rectTransform.rect.height < 200, "Short resident prose no longer reserves a 240px blank block");
            for (int i = 1; i < paragraphs.Length; i++)
            {
                var previous = paragraphs[i - 1].rectTransform; var next = paragraphs[i].rectTransform;
                Check(-next.anchoredPosition.y >= -previous.anchoredPosition.y + previous.rect.height + 12, "Resident paragraph flow has no overlap " + i);
            }
            string text = string.Join("", paragraphs.Select(t => t.text)).Replace("\n", "");
            Check(text.Contains(Catalog.Before[0].Replace("\n", "")) && text.Contains(Catalog.After[0].Replace("\n", "")), "Compact resident layout retains both authored story passages");
            Shot("25-compact-resident-story"); Call("CloseSheet");
            Call("ShowStoryDialogue", 1);
            var button = Field<RectTransform>("modal").GetComponentsInChildren<Button>().First(b => b.GetComponentInChildren<Text>()?.text == "继续营业");
            var row = (RectTransform)button.transform.parent;
            var touch = button.transform.Find("Touch target");
            var touchBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(row, touch);
            Check(touchBounds.max.y <= row.rect.yMax + .1f && touchBounds.min.y >= row.rect.yMin - .1f, "Compact action row keeps its full 132px touch area inside the card");
            Shot("26-compact-reward-dialogue"); Call("CloseSheet");
            Call("ShowCredits"); inner = Field<RectTransform>("sheetContent");
            string license = Resources.Load<TextAsset>("Fonts/OFL").text;
            var legal = inner.GetComponentsInChildren<Text>().Single(t => t.text.EndsWith(license, StringComparison.Ordinal));
            Check(legal.rectTransform.rect.height >= legal.preferredHeight && inner.rect.height >= -legal.rectTransform.anchoredPosition.y + legal.rectTransform.rect.height, "Compact credits retain the complete OFL text and reachable final line");
            Call("EvidenceScroll", true, 0f); Shot("27-compact-credits-final-line"); Call("CloseSheet");
        }
        static void SymbolBoard()
        {
            var board = (RectTransform)Call("Panel", Field<RectTransform>("safe"), "Icon contact sheet", (Color)new Color32(255, 249, 228, 255), 0f, 0f, 1080f, 1920f);
            Call("Label", board, "林间奇物铺 · 原创界面图标", 38, (Color)new Color32(72, 105, 79, 255), 36f, 24f, 1000f, 80f, TextAnchor.MiddleLeft);
            Call("Label", board, "64px / 深绿底64px / 32px · Editor 原生绘制", 25, (Color)new Color32(112, 105, 82, 255), 36f, 104f, 1000f, 54f, TextAnchor.MiddleLeft);
            string[] names = { "金币", "声望", "设置", "小院", "庭院", "工坊", "店面", "委托", "图鉴", "仓库", "扩建", "装饰", "收获", "货架", "计时", "循环", "完成", "锁定", "预留", "缺货", "说明", "方向", "关闭", "茶会" };
            for (int i = 0; i < names.Length; i++)
            {
                float x = 30 + i % 4 * 260, y = 190 + i / 4 * 276;
                Call("Symbol", board, (ForestSymbol)i, x + 8, y + 12, 64f, false);
                var swatch = (RectTransform)Call("Panel", board, "Selected swatch", (Color)new Color32(72, 105, 79, 255), x + 88, y, 88f, 88f);
                Call("Symbol", swatch, (ForestSymbol)i, 12f, 12f, 64f, true);
                Call("Symbol", board, (ForestSymbol)i, x + 201, y + 28, 32f, false);
                Call("Label", board, names[i], 29, (Color)new Color32(65, 58, 43, 255), x, y + 106, 244f, 54f, TextAnchor.MiddleCenter);
            }
            CaptureHud("18-symbol-contact-sheet"); board.gameObject.SetActive(false); UnityEngine.Object.Destroy(board.gameObject);
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
                ClickName("Opening action"); Check(app.State.tutorial == 1 && World.Region == 2 && app.State.crops[0] > 0, "Harvest routes to workshop"); Shot("03-workshop-guide");
                ClickName("Opening action"); Check(Field<string>("currentSheet") == "machine", "Workshop guide opens recipe sheet"); Shot("04-cauldron-recipe");
                Click("循环制作"); Check(app.State.tutorial == 2 && World.Region == 3 && app.State.machines[0].config == 0, "Recipe handler routes to shop"); Shot("05-shop-guide");
                ClickName("Opening action"); Check(Field<string>("currentSheet") == "shelf", "Shop guide opens shelf sheet");
                Click("选择售卖"); Check(app.State.tutorial == 3 && World.Region == 3 && app.State.shelves[0].config == 0, "Shelf handler arms automatic sales"); Shot("06-waiting-first-sale");
                app.Engine.Advance(16); Check(app.State.tutorial == 4 && app.State.sales > 0, "In-memory simulation reaches first sale"); Shot("07-first-sale-guide");
                ClickName("Opening action"); Check(Field<string>("currentPage") == "委托", "Completion guide opens orders"); Shot("08-first-order");
                CheckWaitingGuide();
                var six = new GameState { welcomeRead = true, tutorial = 4, sideRoom = true, reputation = 150, coins = 6000, simpleAnimation = true };
                six.plots = Enumerable.Range(0, 6).Select(i => new Facility { config = i % 4, workItem = i % 4, remaining = 0 }).ToArray();
                Use(six); World.SetRegion(1); Call("ShowPage", "小店"); Shot("09-six-bed-next-expansion");
                Check(World.transform.Find("Handcrafted forest diorama/Twelve-bed woodland garden/Unbuilt garden bed 6/Next expansion supplies").gameObject.activeInHierarchy, "Only next bed has expansion supplies");
                Check(!World.transform.Find("Handcrafted forest diorama/Twelve-bed woodland garden/Unbuilt garden bed 7/Next expansion supplies").gameObject.activeInHierarchy, "Distant future bed remains quiet");
                PickFuture(6); Check(Field<string>("currentSheet") == "expansion", "Next bed ray opens expansion conditions"); Shot("10-expansion-conditions");
                Click("扩建地块"); Check(app.State.plots.Length == 7 && app.State.coins == 0, "Actual expansion UI buys only next bed at existing cost");
                World.SetRegion(1); Field("shopPosition", 1f); Call("ShowPage", "小店"); Shot("11-seven-bed-next-moved");
                Call("CloseSheet"); PickFuture(9); Check(Field<string>("currentSheet") == null && Field<Text>("toastLabel").text.Contains("第 8 块地"), "Far bed ray explains sequential expansion");
                var distant = UnityEngine.Object.FindObjectsOfType<Button>().Where(b => b.name.StartsWith("Plot card ") && int.Parse(b.name.Substring(10)) > app.State.plots.Length).ToArray();
                Check(distant.Length == 4 && distant.All(b => !b.interactable), "Four distant future buttons do not skip expansion order");
                Call("EvidenceScroll", false, 0f); Shot("12-future-bed-buttons");
                app.State.shelves = Enumerable.Range(0, 4).Select(i => new Facility { config = i }).ToArray();
                for (int i = 0; i < 6; i++) app.State.items[i] = 12;
                app.State.stories = 5;
                World.SetRegion(3); Field("shopPosition", 1f); Call("ShowPage", "小店"); Shot("13-old-shop-sign");
                app.State.decoration[0] = true; World.Bind(app.State); Shot("14-reward-shop-sign");
                ClickName("Goal details"); Check(Field<string>("currentSheet") == "goal", "Header information icon opens full chapter requirements"); Shot("15-goal-details");
                Call("CloseSheet"); World.SetRegion(1); Field("shopPosition", 1f); Call("ShowPage", "小店");
                ClickName("Garden details"); Check(Field<string>("currentSheet") == "garden-help", "Garden information icon opens full cost and ordering explanation"); Shot("16-garden-details");
                Call("CloseSheet"); World.SetRegion(3); Field("shopPosition", 0f); Call("ShowPage", "小店"); Shot("17-shop-cards-footer");
                SymbolBoard();
                app.State.items[0] = 0; app.State.shelves[1].config = -1;
                app.State.shelves = app.State.shelves.Take(3).ToArray();
                app.State.orders = new[] { new Order { id = 999, active = true, reserve = true, item = 2, count = 12, resident = 0 } };
                Check((ForestSymbol)Call("ShelfSymbol", 0) == ForestSymbol.Empty && (ForestSymbol)Call("ShelfSymbol", 1) == ForestSymbol.Settings && (ForestSymbol)Call("ShelfSymbol", 2) == ForestSymbol.Reserved && (ForestSymbol)Call("ShelfSymbol", 3) == ForestSymbol.Lock, "Empty, unconfigured, reserved and locked shelf meanings remain distinct");
                Field("shopPosition", 1f); Call("ShowPage", "小店"); Shot("19-shelf-state-icons");
                ClickName("Shelf details"); Check(Field<string>("currentSheet") == "shelf-help", "In-scene information button opens written shelf icon meanings"); Shot("20-shelf-help"); Call("CloseSheet");
                var liveSymbols = UnityEngine.Object.FindObjectsOfType<ForestSymbolGraphic>();
                Check(liveSymbols.Length > 20 && liveSymbols.All(g => !g.raycastTarget && g.GetComponent<CanvasRenderer>() != null), "Live symbols have renderers and never intercept UI or scene rays");
                var navigation = Field<System.Collections.Generic.List<Button>>("navigationButtons");
                Check(navigation.Count == 3 && navigation.Count(b => ((ForestSymbolGraphic)b.GetComponent<ForestButtonVisual>().icon).Light) == 1 && navigation[0].GetComponentInChildren<ForestSymbolGraphic>().Light, "Exactly current bottom navigation has selected symbol palette");
                // A fitted overview can report normalized position zero. A region jump
                // must show the new area's navigation and scene, not reuse that footer.
                foreach (string region in new[] { "庭院", "工坊", "店面" })
                {
                    Field("shopPosition", 0f); Field("shopBuilt", true);
                    Click(region); Canvas.ForceUpdateCanvases();
                    Check(Field<RectTransform>("content").parent.GetComponent<ScrollRect>().verticalNormalizedPosition > .99f, "Region jump resets inherited footer position: " + region);
                }
                CheckCompactSheets();
                Check(!holder.activeInHierarchy && Field<SaveStore>("store") == null && Field<bool>("evidenceActive") && Field<bool>("recovering"), "No active app, SaveStore or normal-player persistence");
                File.WriteAllText(Output + "/handler-checks.txt", checks.ToString());
                File.WriteAllText(Output + "/preview-scope.txt", "EDITOR OFFSCREEN PREPARED HUD PREVIEW, NOT ANDROID. Target=1080x1920, full simulated safe rectangle.\nHost Editor Screen=" + Screen.width + "x" + Screen.height + "; modal height still uses host Screen dimensions, so modal viewport/touch fit is NOT established by these offscreen images.\nInactive ForestApp; no Awake, SaveStore, audio, device install or ordinary player progress writes. UI handlers and future-bed rays exercised against memory.\nFirst sale uses 16 simulated seconds; not real-time play or continuous dynamic evidence. Transient toast hidden for static layout captures.\nSix-bed and stocked-shop states are prepared, not imported normal saves. No final art/functional/phone acceptance.\nUnity=" + Application.unityVersion + "\n");
                Debug.Log("FOREST_GUIDANCE_PREVIEW_OK " + Path.GetFullPath(Output)); EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
