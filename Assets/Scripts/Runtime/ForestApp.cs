using System;
using System.Collections.Generic;
using System.IO;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed class UnityStateCodec : IStateCodec
    {
        public string Encode(GameState s) => JsonUtility.ToJson(s);
        public GameState Decode(string json) => JsonUtility.FromJson<GameState>(json);
    }

    public sealed partial class ForestApp : MonoBehaviour
    {
        public GameState State { get; private set; }
        public Economy Engine { get; private set; }
        SaveStore store;
        Font font;
        Canvas canvas;
        RectTransform safe, page, content, modal;
        Text coinsLabel, reputationLabel, goalLabel, toastLabel, productionLabel;
        readonly List<Action> bindings = new List<Action>();
        readonly List<Action> modalBindings = new List<Action>();
        readonly List<Action> animationBindings = new List<Action>();
        readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Color Ink = new Color32(65, 58, 43, 255);
        static readonly Color Muted = new Color32(112, 105, 82, 255);
        static readonly Color Paper = new Color32(255, 249, 228, 255);
        static readonly Color Sage = new Color32(72, 105, 79, 255);
        static readonly Color Sand = new Color32(238, 222, 179, 255);
        Sprite round;
        string currentPage = "小店", currentSheet;
        int selectedIndex;
        float saveTimer, toastTimer;
        bool backgrounded, recovering, quitting;
        bool booting, changingPage;
        ForestStartupCover startupCover;
        bool resumeFishingAfterPause;
        long previousSales;
        long previousHarvests, previousCrafts;
        ForestAudio audioController;
        Economy presentationEngine;
        readonly Queue<float> frameTimes = new Queue<float>();
        float performanceTimer;
        int performanceSamples;
        double performanceSum;
        readonly List<float> performanceFrames = new List<float>();

        void Awake()
        {
            DiagnosticsStart();
            Application.targetFrameRate = 30;
            if (FindObjectOfType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            Screen.orientation = ScreenOrientation.Portrait;
            font = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (font == null) throw new InvalidOperationException("Required licensed Chinese font is missing");
            round = MakeRound();
            BuildShell();
            booting = true;
            startupCover = ForestStartupCover.Create(font);
            StartCoroutine(OpenShop());
        }

        System.Collections.IEnumerator OpenShop()
        {
            // Let the first branded frame render before synchronous save/world
            // preparation. No artificial minimum wait or invented percentage.
            Debug.Log("FOREST_STARTUP shell " + Time.realtimeSinceStartup.ToString("F3"));
            yield return null;
            startupCover.Stage("正在读取小店进度");
            Canvas.ForceUpdateCanvases();
            store = new SaveStore(Application.persistentDataPath, new UnityStateCodec());
            try
            {
                State = store.Load() ?? new GameState { utcTicks = DateTime.UtcNow.Ticks };
                Engine = new Economy(State);
                Engine.Fishing.Pause();
                OfflineSettlement.Resume(State, DateTime.UtcNow.Ticks);
                Save();
            }
            catch (Exception e)
            {
                recovering = true;
                Debug.LogError("Save recovery required: " + e.Message);
                ShowRecovery(e.Message);
                booting = false; Destroy(startupCover.gameObject);
                yield break;
            }
            previousSales = State.sales;
            previousHarvests = State.harvests; previousCrafts = State.crafts;
            receiptState = State;
            Debug.Log("FOREST_STARTUP save-ready " + Time.realtimeSinceStartup.ToString("F3"));
            startupCover.Stage("正在布置林间小院");
            yield return null;
            audioController = gameObject.AddComponent<ForestAudio>(); audioController.Apply(State);
            FocusOpeningStep();
            ShowPage("小店");
            if (State.report.pending) ShowReport();
            else if (!State.welcomeRead) ShowWelcome();
            RefreshHeader();
            if (!string.IsNullOrEmpty(store.RecoveryMessage)) Toast(store.RecoveryMessage);
            Debug.Log("FOREST_STARTUP world-ready " + Time.realtimeSinceStartup.ToString("F3"));
            yield return null;
            booting = false; Destroy(startupCover.gameObject);
            Debug.Log("FOREST_STARTUP interactive " + Time.realtimeSinceStartup.ToString("F3"));
            EvidenceStart();
        }

        void Update()
        {
            DiagnosticsUpdate();
            if (booting || recovering || State == null) return;
            long diagnosticStamp = DiagnosticsStamp();
            if (!backgrounded && !State.report.pending && !quitting)
            {
                ConnectProductionPresentation();
                Engine.Advance(Time.unscaledDeltaTime);
                AdvanceFishing(Time.unscaledDeltaTime);
                DiagnosticsStage("economy", ref diagnosticStamp);
                saveTimer += Time.unscaledDeltaTime;
                if (saveTimer >= 10) { saveTimer = 0; Save(); }
                DiagnosticsStage("save", ref diagnosticStamp);
            }
            RefreshProductionReceipts();
            RefreshHeader();
            DiagnosticsStage("labels-audio", ref diagnosticStamp);
            if (world3D != null) world3D.Bind(State);
            DiagnosticsStage("world-bind", ref diagnosticStamp);
            foreach (var binding in bindings) binding();
            foreach (var binding in modalBindings) binding();
            if (!State.simpleAnimation) foreach (var binding in animationBindings) binding();
            DiagnosticsStage("ui-bindings", ref diagnosticStamp);
            if (toastTimer > 0) { toastTimer -= Time.unscaledDeltaTime; if (toastTimer <= 0) toastLabel.transform.parent.gameObject.SetActive(false); }
            if (Input.GetKeyDown(KeyCode.Escape))
            { if (State.report.pending) { } else if (modal != null) CloseSheet(); else if (currentPage != "小店") ShowPage("小店"); else if (world3D != null && world3D.Region >= 4) GoForestHome(); else ShowExit(); }
            if (modal == null && !State.report.pending)
            {
                if (State.dialogue >= 0) ShowStoryDialogue(State.dialogue);
                else if (State.chapterComplete && !State.chapterCelebrated) ShowCelebration();
                else if (State.forest.dialogue >= 0) ShowForestDialogue();
                else if (State.forest.pavilion && !State.forest.celebrationSeen) ShowForestCelebration();
            }
            performanceTimer += Time.unscaledDeltaTime;
            // Long stalls are part of the sample. Never discard them to improve FPS.
            if (!backgrounded && !evidenceActive && Time.unscaledDeltaTime > 0) { performanceFrames.Add(Time.unscaledDeltaTime * 1000); performanceSum += Time.unscaledDeltaTime; performanceSamples++; }
            if (performanceTimer >= 300) { FlushPerformance(); performanceTimer = 0; }
            EvidenceUpdate();
            DiagnosticsStage("update-end", ref diagnosticStamp);
        }

        void OnApplicationPause(bool paused)
        {
            if (State == null || recovering || evidenceActive) return;
            if (paused) { resumeFishingAfterPause=State.fishing.running; Engine.Fishing.Pause(); backgrounded = true; Save(); }
            else if (backgrounded)
            {
                backgrounded = false;
                OfflineSettlement.Resume(State, DateTime.UtcNow.Ticks); Save();
                if (State.report.pending) ShowReport();
                else if(resumeFishingAfterPause && currentSheet=="fishing") { Engine.Fishing.Start(State.fishing.spot,State.fishing.bait); Save(); }
                resumeFishingAfterPause=false;
            }
        }
        void OnApplicationQuit() { if (!recovering && State != null) { Engine?.Fishing.Pause(); Save(); FlushPerformance(); } }
        void Save()
        {
            if (State == null || recovering || evidenceActive) return;
            try { OfflineSettlement.SaveOnlineTime(State, DateTime.UtcNow.Ticks); store.Save(State); }
            catch (Exception e) { Debug.LogError("Save failed: " + e); Toast("进度保存失败，请检查设备存储空间。", 6); }
        }
        void FlushPerformance()
        {
            if (performanceFrames.Count == 0) return;
            performanceFrames.Sort();
            string line = DateTime.UtcNow.ToString("o") + "," + performanceSamples + "," + performanceSum.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) + "," + (performanceSamples / Math.Max(.001, performanceSum)).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + performanceFrames[(int)((performanceFrames.Count - 1) * .95)].ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() + "," + performanceFrames[performanceFrames.Count - 1].ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            try { File.AppendAllText(Path.Combine(Application.persistentDataPath, "performance-v2.csv"), line + "\n"); } catch (IOException) { }
            performanceFrames.Clear(); performanceSum = 0; performanceSamples = 0;
        }
        bool Act(Func<bool> action, string success, bool rebuild = true)
        {
            ConnectProductionPresentation();
            int previousTutorial = State.tutorial;
            bool ok = action();
            if (ok)
            {
                Save(); if (audioController != null) audioController.Cue("confirm"); Toast(success);
                if (rebuild)
                {
                    if (previousTutorial == 0 && State.tutorial == 1 && !State.simpleAnimation && isActiveAndEnabled)
                        StartCoroutine(FinishFirstHarvestFocus(State, world3D == null ? -1 : world3D.Region));
                    else if (State.tutorial != previousTutorial && State.tutorial < 4)
                    { FocusOpeningStep(); ShowPage("小店"); }
                    else { string sheet = currentSheet; int index = selectedIndex; ShowPage(currentPage); if (sheet != null) OpenObject(sheet, index); }
                }
            }
            else Toast("条件尚未满足，请查看所需金币、声望或库存。", 3);
            return ok;
        }

        System.Collections.IEnumerator FinishFirstHarvestFocus(GameState harvestedState, int sourceRegion)
        {
            // The crop is already saved. Briefly keep its local receipt visible;
            // explicit player navigation always takes precedence over this handoff.
            yield return new WaitForSecondsRealtime(.85f);
            if (State != harvestedState || State.tutorial != 1 || currentPage != "小店" || modal != null || backgrounded || State.report.pending || world3D == null || world3D.Region != sourceRegion) yield break;
            FocusOpeningStep(); ShowPage("小店");
        }

        void ConnectProductionPresentation()
        {
            if (presentationEngine == Engine) return;
            if (presentationEngine != null) presentationEngine.ProductionCompleted -= PresentProduction;
            presentationEngine = Engine;
            if (presentationEngine != null) presentationEngine.ProductionCompleted += PresentProduction;
        }
        void PresentProduction(ProductionNotice notice)
        {
            // A failed decorative effect must not interrupt deterministic settlement.
            try { if (!backgrounded && !State.report.pending && world3D != null) world3D.PresentProduction(notice); }
            catch (Exception error) { Debug.LogException(error); }
        }

        void BuildShell()
        {
            var c = new GameObject("Forest Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = c.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = c.GetComponent<CanvasScaler>(); scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scale.referenceResolution = new Vector2(1080, 1920); scale.matchWidthOrHeight = 0;
            Fill(c.GetComponent<RectTransform>());
            var bg = Panel(c.transform, "Backdrop", Paper); Fill(bg);
            safe = Rect(c.transform, "Safe area", 0, 0, 1080, 1920); ApplySafeArea();
            if (FindObjectOfType<EventSystem>() == null) new GameObject("Touch Input", typeof(EventSystem), typeof(StandaloneInputModule));
            var header = Panel(safe, "Resources", Paper); AnchorBar(header, true, 100);
            Symbol(header, ForestSymbol.Coin, 30, 20, 60);
            coinsLabel = Label(header, "", 36, Sage, 104, 15, 420, 70);
            Symbol(header, ForestSymbol.Reputation, 560, 20, 60);
            reputationLabel = Label(header, "", 34, Ink, 630, 15, 230, 70);
            IconButton(header, "", ForestSymbol.Settings, 950, 6, 100, 88, ShowSettings).name = "Settings";
            var goal = Panel(safe, "Current goal", Sand); AnchorBar(goal, true, 88); goal.anchoredPosition = new Vector2(0, -100);
            goalSymbol = Symbol(goal, ForestSymbol.Tea, 30, 13, 60);
            goalLabel = Label(goal, "", 31, Ink, 108, 8, 648, 72);
            Symbol(goal,ForestSymbol.Arrow,766,24,40);
            var goalAction=goal.gameObject.AddComponent<Button>();goalAction.transition=UnityEngine.UI.Selectable.Transition.None;
            goalAction.onClick.AddListener(()=>{if(State!=null && !State.report.pending)GoJourney(JourneySteps(true)[0]);});
            IconButton(goal, "手册", ForestSymbol.Collection, 838, 5, 220, 78, ShowJourneyHandbook).name = "Journey handbook";
            var productionBar = Panel(safe, "Production updates", new Color32(226, 234, 211, 255)); AnchorBar(productionBar, true, 56); productionBar.anchoredPosition = new Vector2(0, -188);
            productionFlow = Rect(productionBar, "Automatic production flow", 300, 0, 480, 56);
            ForestSymbol[] flow = { ForestSymbol.Garden, ForestSymbol.Arrow, ForestSymbol.Workshop, ForestSymbol.Arrow, ForestSymbol.Shop, ForestSymbol.Loop };
            for (int i = 0; i < flow.Length; i++) Symbol(productionFlow, flow[i], i * 80, 3, 50);
            BuildProductionReceiptHud(productionBar);
            var bottom = Panel(safe, "Navigation", Paper); AnchorBar(bottom, false, 124);
            string[] nav = { "小店", "委托", "图鉴" };
            ForestSymbol[] navIcons = { ForestSymbol.Home, ForestSymbol.Orders, ForestSymbol.Collection };
            for (int i = 0; i < nav.Length; i++) { string n = nav[i]; navigationButtons.Add(IconButton(bottom, n, navIcons[i], 24 + i * 350, 18, 330, 84, () => ShowPage(n))); }
            var toast = Panel(safe, "Feedback", Sage); toast.anchorMin = toast.anchorMax = new Vector2(.5f, 0); toast.pivot = new Vector2(.5f, 0); toast.anchoredPosition = new Vector2(0, 140); toast.sizeDelta = new Vector2(930, 90);
            toastLabel = Label(toast, "", 30, Paper, 20, 6, 890, 76, TextAnchor.MiddleCenter); toast.gameObject.SetActive(false);
        }
        void ApplySafeArea()
        {
            var a = Screen.safeArea;
            safe.anchorMin = new Vector2(a.xMin / Math.Max(1, Screen.width), a.yMin / Math.Max(1, Screen.height));
            safe.anchorMax = new Vector2(a.xMax / Math.Max(1, Screen.width), a.yMax / Math.Max(1, Screen.height));
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }
        void Toast(string text, float duration = 2.2f)
        { toastLabel.text = text; toastLabel.transform.parent.gameObject.SetActive(true); toastLabel.transform.parent.SetAsLastSibling(); toastTimer = duration; }

        RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var o = new GameObject(name, typeof(RectTransform)); var r = o.GetComponent<RectTransform>(); r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height); return r;
        }
        static void Fill(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
        static void AnchorBar(RectTransform r, bool top, float height)
        { r.anchorMin = new Vector2(0, top ? 1 : 0); r.anchorMax = new Vector2(1, top ? 1 : 0); r.pivot = new Vector2(.5f, top ? 1 : 0); r.anchoredPosition = Vector2.zero; r.sizeDelta = new Vector2(0, height); }
        RectTransform Panel(Transform parent, string name, Color color, float x = 0, float y = 0, float width = 1080, float height = 100)
        { var r = Rect(parent, name, x, y, width, height); var im = r.gameObject.AddComponent<Image>(); im.sprite = round; im.type = Image.Type.Sliced; im.color = color; return r; }
        Text Label(Transform parent, string value, int size, Color color, float x, float y, float width, float height, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var r = Rect(parent, "Text", x, y, width, height); var t = r.gameObject.AddComponent<Text>(); t.font = font; t.text = value; t.fontSize = size; t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false; t.lineSpacing = 1.12f; return t;
        }
        Button Button(Transform parent, string caption, float x, float y, float width, float height, Action action, bool enabled = true, bool primary = false)
        {
            var r = Panel(parent, caption, primary ? Sage : Sand, x, y, width, height); var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = r.GetComponent<Image>(); b.interactable = enabled;
            var colors = b.colors; colors.highlightedColor = new Color(.93f, .95f, .86f); colors.pressedColor = new Color(.78f, .84f, .72f); colors.disabledColor = Color.white; b.colors = colors;
            b.onClick.AddListener(() => { DiagnosticsClick(caption); if (audioController != null) audioController.Cue("tap"); action(); });
            var text = Label(r, caption, 30, primary ? Paper : Ink, 12, 5, width - 24, height - 10, TextAnchor.MiddleCenter);
            var visual = r.gameObject.AddComponent<ForestButtonVisual>(); visual.button = b; visual.background = r.GetComponent<Image>(); visual.caption = text; visual.normalBackground = primary ? Sage : Sand; visual.normalText = primary ? Paper : Ink;
            if (height < 132)
            {
                var touch = Panel(r, "Touch target", Color.clear, 0, -(132 - height) / 2, width, 132);
                touch.GetComponent<Image>().raycastTarget = true;
            }
            return b;
        }
        Image Picture(Transform parent, string asset, int cell, int columns, int rows, float x, float y, float width, float height)
        {
            var r = Rect(parent, asset, x, y, width, height); var image = r.gameObject.AddComponent<Image>();
            image.sprite = Art(asset, cell, columns, rows); image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        Sprite Art(string name, int cell = 0, int columns = 1, int rows = 1)
        {
            return World().Icon(name, cell);
        }
        Sprite MakeRound()
        {
            var t = new Texture2D(48, 48, TextureFormat.RGBA32, false); t.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
            { float dx = Math.Max(0, Math.Abs(x - 23.5f) - 11.5f), dy = Math.Max(0, Math.Abs(y - 23.5f) - 11.5f); t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(12 - Mathf.Sqrt(dx * dx + dy * dy)))); }
            t.Apply(); return Sprite.Create(t, new Rect(0, 0, 48, 48), new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
        }
        RectTransform Scroll(Transform parent, float top, float bottom, float height, string name)
        {
            var view = Panel(parent, name, Color.clear); Fill(view); view.offsetMin = new Vector2(0, bottom); view.offsetMax = new Vector2(0, -top);
            view.gameObject.AddComponent<RectMask2D>(); var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 65;
            var inner = Rect(view, "Content", 0, 0, 1080, height); inner.anchorMin = new Vector2(0, 1); inner.anchorMax = new Vector2(1, 1); inner.sizeDelta = new Vector2(0, height);
            scroll.viewport = view; scroll.content = inner; return inner;
        }
        void ShowPage(string name)
        {
            changingPage = true; CloseSheet(); changingPage = false;
            currentPage = name; bindings.Clear(); animationBindings.Clear();
            if (State != null) JourneySteps(true);
            RefreshNavigation(name);
            if (world3D != null) world3D.SetVisible(name == "小店");
            if (page != null) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Rect(safe, "Page " + name, 0, 0, 1080, 1500); Fill(page); page.offsetMin = new Vector2(0, 124); page.offsetMax = new Vector2(0, -244);
            if (name == "小店") BuildShop(); else if (name == "委托") BuildOrders(); else BuildCollection();
            toastLabel.transform.parent.SetAsLastSibling();
        }
    }
}
