using System;
using System.Collections;
using System.IO;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        string evidenceDirectory;
        float evidenceStarted;
        bool evidenceActive;
        bool evidenceVideoOnly;
        void EvidenceStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-forestEvidence") evidenceDirectory = args[i + 1];
            string request = Path.Combine(Application.persistentDataPath, "forest-evidence-request.txt");
            if (File.Exists(request)) { evidenceDirectory = Path.Combine(Application.persistentDataPath, "Verification3D"); File.Delete(request); }
            string videoRequest = Path.Combine(Application.persistentDataPath, "forest-video-request.txt");
            if (File.Exists(videoRequest)) { evidenceVideoOnly = true; evidenceDirectory = Path.Combine(Application.persistentDataPath, "Verification3DVideo"); File.Delete(videoRequest); }
            if (!string.IsNullOrEmpty(evidenceDirectory))
            {
                Application.runInBackground = true; Directory.CreateDirectory(evidenceDirectory);
                evidenceActive = true; evidenceStarted = Time.realtimeSinceStartup; StartCoroutine(CaptureRun());
            }
#endif
        }
        void EvidenceUpdate() { }
        void EvidenceScroll(bool sheet, float value)
        {
            var c = sheet ? sheetContent : content;
            Canvas.ForceUpdateCanvases(); c.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = value;
        }
        IEnumerator Shot(string name, int width = 1080, int height = 1920)
        {
            // A separate continuous recording run does no PNG readback or encoding.
            // The native Android recorder observes normal rendered frames.
            if (evidenceVideoOnly) { yield return new WaitForSecondsRealtime(.15f); yield break; }
            coinsLabel.text = "金币  " + State.coins.ToString("N0"); reputationLabel.text = "声望  " + State.reputation; goalLabel.text = Engine.Goal;
            foreach (var b in bindings) b(); foreach (var b in modalBindings) b();
            if (world3D != null) { world3D.Bind(State); world3D.RenderNow(); }
            Canvas.ForceUpdateCanvases();
            // Allow panels scheduled for destruction to leave the canvas before capture.
            yield return null;
            yield return new WaitForEndOfFrame();
            if (Application.platform == RuntimePlatform.Android)
            {
                // Actual framebuffer, never an alternate-size mock device render.
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(evidenceDirectory, name + ".png"), texture.EncodeToPNG()); Destroy(texture);
            }
            else CaptureOffscreen(name, width, height);
            yield return new WaitForSecondsRealtime(.12f);
        }
        IEnumerator CaptureRun()
        {
            // Prepared test states never replace the player's normal save.
            var normal = State; var normalEngine = Engine; recovering = true;
            State = new GameState(); Engine = new Economy(State); shopBuilt = false; shopPosition = 1;
            World().SetRegion(0); ShowPage("小店"); ShowWelcome(); yield return Shot("01-welcome"); EvidenceScroll(true, 0); yield return Shot("01b-welcome-bottom");
            CloseSheet(); World().SetRegion(1); shopPosition = 1; ShowPage("小店"); yield return Shot("02-mature-mint");
            Engine.HarvestFirst(); Engine.ConfigureMachine(0, 0); Engine.ConfigureShelf(0, 0); Engine.Advance(16);
            World().SetRegion(2); ShowPage("小店"); yield return Shot("03-first-workshop"); ShowPage("委托"); yield return Shot("04-teaching-order");
            var late = new GameState { tutorial = 4, welcomeRead = true, coins = 25000, reputation = 150, stories = 5, sales = 60000, sideRoom = true, chapterComplete = true, chapterCelebrated = true, dialogue = -1 };
            late.plots = new Facility[6]; for (int i = 0; i < 6; i++) late.plots[i] = new Facility { config = i % 4, level = 3 };
            late.machines = new[] { new Facility { config = 4, level = 3 }, new Facility { config = 5, level = 3 } };
            late.shelves = new[] { new Facility { config = 0, remaining = 8 }, new Facility { config = 1, remaining = 8 }, new Facility { config = 4, remaining = 8 }, new Facility { config = 5, remaining = 8 } };
            for (int i = 0; i < 4; i++) late.crops[i] = 12;
            for (int i = 0; i < 6; i++) { late.items[i] = 12; late.sold[i] = 20; }
            for (int i = 0; i < 5; i++) late.friendship[i] = 60;
            late.decoration = new[] { true, true, true, true }; late.warehouse = 3;
            State = late; Engine = new Economy(State); Engine.Advance(0);
            for (int region = 0; region < 4; region++)
            { World().SetRegion(region); shopPosition = 1; ShowPage("小店"); yield return Shot("05-region-" + region); EvidenceScroll(false, 0); yield return Shot("05-region-" + region + "-status"); }
            OpenObject("machine", 0); yield return Shot("06-cauldron-recipes"); EvidenceScroll(true, 0); yield return Shot("06b-cauldron-recipes-bottom");
            OpenObject("machine", 1); yield return Shot("07-bench-recipes"); EvidenceScroll(true, 0); yield return Shot("07b-bench-recipes-bottom");
            ShowPage("委托"); Engine.AcceptOrder(State.orders[0].id); ShowPage("委托"); yield return Shot("08-orders-ready");
            var active = Engine.ActiveOrder; if (active != null) State.items[active.item] = 0;
            ShowPage("委托"); yield return Shot("08b-orders-shortage");
            if (active != null) State.items[active.item] = active.count;
            World().SetRegion(3); shopPosition = 0; ShowPage("小店"); yield return Shot("08c-sale-reservation-status");
            ShowPage("图鉴"); collectionTab = 0; ShowPage("图鉴"); yield return Shot("09-items"); EvidenceScroll(false, 0); yield return Shot("09b-items-bottom");
            collectionTab = 1; ShowPage("图鉴"); yield return Shot("10-residents"); EvidenceScroll(false, 0); yield return Shot("10b-residents-bottom");
            ShowStoryDialogue(3); yield return Shot("11-dialogue"); EvidenceScroll(true, 0); yield return Shot("11b-dialogue-bottom");
            World().SetRegion(0); ShowPortrait(4); yield return Shot("12-memorial");
            for (int resident = 0; resident < 5; resident++)
            {
                ShowStoryDialogue(resident); yield return Shot("11c-story-" + resident); EvidenceScroll(true, 0); yield return Shot("11d-story-bottom-" + resident);
                World().SetRegion(0); ShowPortrait(resident); yield return Shot("12b-memorial-" + resident);
            }
            CloseSheet(); State.report = new OfflineReport { pending = true, away = 36000, effective = 28800, coins = 8256, cropDelta = new[] { -2, 4, 8, 0 }, itemDelta = new[] { 5, 7, 0, 0, 1, 3 }, hint = "委托商品已预留。\n部分原料库存已满，生产等待。" };
            ShowReport(); yield return Shot("13-offline-prepared"); EvidenceScroll(true, 0); yield return Shot("13b-offline-prepared-bottom");
            State.report = new OfflineReport { pending = true, away = 60, effective = 60, coins = 0 }; ShowReport(); yield return Shot("13c-offline-zero-prepared"); State.report.pending = false;
            ShowSettings(); yield return Shot("14-settings"); EvidenceScroll(true, 0); yield return Shot("14b-settings-bottom");
            CloseSheet(); State.chapterCelebrated = false; ShowCelebration(); yield return Shot("15-chapter-celebration"); EvidenceScroll(true, 0); yield return Shot("15b-chapter-bottom"); CloseSheet();
            State.sideRoom = false; World().SetRegion(0); shopPosition = 1; ShowPage("小店"); yield return Shot("16-annex-before");
            State.sideRoom = true; ShowPage("小店"); yield return Shot("16b-annex-after");
            for (int i = 0; i < 4; i++) State.decoration[i] = false;
            ShowPage("小店"); yield return Shot("17-basic-decorations");
            for (int i = 0; i < 4; i++) State.decoration[i] = true;
            ShowPage("小店"); yield return Shot("17b-unlocked-decorations");
            for (int slot = 0; slot < 4; slot++)
            {
                for (int i = 0; i < 4; i++) State.decoration[i] = false;
                OpenObject("decoration", 0); yield return Shot("17d-decoration-selection-before-" + slot);
                var toggles = new System.Collections.Generic.List<Button>();
                foreach (var button in modal.GetComponentsInChildren<Button>())
                    if (button.GetComponentInChildren<Text>()?.text == "切换外观") toggles.Add(button);
                toggles[slot].onClick.Invoke(); yield return Shot("17e-decoration-selection-after-" + slot);
                CloseSheet();
                ShowPage("小店"); yield return Shot("17c-decoration-only-" + slot);
                World().SetRegion(slot == 3 ? 0 : 2); shopPosition = 1; ShowPage("小店"); yield return Shot("17f-decoration-detail-" + slot);
                // Isolated prepared-state round trip, never the player's store.
                var testStore = new SaveStore(Path.Combine(evidenceDirectory, "PreparedDecorationPersistence"), new UnityStateCodec());
                testStore.Save(State); State = testStore.Load(); Engine = new Economy(State);
                if (!State.decoration[slot]) throw new InvalidDataException("Prepared decoration round trip failed");
                ShowPage("小店"); yield return Shot("17g-decoration-reloaded-" + slot);
                File.AppendAllText(Path.Combine(evidenceDirectory, "decoration-roundtrip.txt"), "Prepared state, slot " + slot + ": UI handler -> isolated SaveStore -> Load -> selected=true. Not normal progression or application restart.\n");
                World().SetRegion(0);
            }
            late = State;
            for (int i = 0; i < 4; i++) State.decoration[i] = true;
            State = new GameState { welcomeRead = true }; Engine = new Economy(State); OpenObject("expansion", 0); yield return Shot("18-expansion-locked");
            collectionTab = 0; ShowPage("图鉴"); yield return Shot("18b-collection-locked"); EvidenceScroll(false, 0); yield return Shot("18c-collection-locked-bottom");
            State = late; Engine = new Economy(State); shopPosition = 1; World().SetRegion(0); ShowPage("小店");
            if (Application.platform != RuntimePlatform.Android)
            { yield return Shot("19-long-offscreen", 1080, 2340); yield return Shot("20-narrow-offscreen", 720, 1600); EvidenceScroll(false, 0); yield return Shot("20b-narrow-bottom-offscreen", 720, 1600); }
            // Real-time Update drives production and feedback during the following 65 seconds.
            previousSales = State.sales; previousHarvests = State.harvests; previousCrafts = State.crafts; recovering = false;
            World().SetRegion(3); shopPosition = 1; ShowPage("小店");
            World().RestartVisitSequence();
            File.WriteAllText(Path.Combine(evidenceDirectory, "customer-ready.txt"), "Actual real-time shop visit loop begins; prepared progression; no economy fast-forward.");
            float customerBegin = Time.realtimeSinceStartup; int customerFrame = 0;
            while (Time.realtimeSinceStartup - customerBegin < 172)
            {
                yield return Shot("customer-" + customerFrame.ToString("D4"), 720, 1280);
                File.AppendAllText(Path.Combine(evidenceDirectory, "customer-frames.csv"), customerFrame + "," + Time.realtimeSinceStartup.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," + State.playedSeconds.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
                customerFrame++; yield return new WaitForSecondsRealtime(.35f);
            }
            float begin = Time.realtimeSinceStartup; int frame = 0;
            while (Time.realtimeSinceStartup - begin < 65)
            {
                if (frame % 18 == 0) { World().SetRegion((frame / 18) % 4); shopPosition = 1; ShowPage("小店"); }
                if (frame % 18 == 8) { if (World().Region == 2) OpenObject("machine", 0); else EvidenceScroll(false, 0); }
                if (frame % 18 == 14) CloseSheet();
                yield return Shot("dynamic-" + frame.ToString("D4"), 720, 1280);
                File.AppendAllText(Path.Combine(evidenceDirectory, "frames.csv"), frame + "," + Time.realtimeSinceStartup.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "," + State.playedSeconds.ToString("F4", System.Globalization.CultureInfo.InvariantCulture) + "\n");
                frame++; yield return new WaitForSecondsRealtime(.35f);
            }
            File.WriteAllText(Path.Combine(evidenceDirectory, "metadata.txt"), "Candidate=3d-d10; DEVELOPMENT TEST PREPARATION\nRuntime=" + Application.platform + "\nUnity=" + Application.unityVersion + "\nScreen=" + Screen.width + "x" + Screen.height + "; safe=" + Screen.safeArea + "\nVideoOnly=" + evidenceVideoOnly + "\nAndroid shots use the actual application framebuffer. Windows shots use actual UI rendered offscreen at named sizes, and are not device screenshots.\nLate progression and sample offline reports are explicitly prepared, not evidence of normal progression or actual away earnings.\nDynamic production uses normal real-time Update; navigation is automated. Timestamps and simulated elapsed play time are in frames.csv.\nWall runtime=" + (Time.realtimeSinceStartup - evidenceStarted) + "s\nFinal acceptance, physical device, sound and performance must be separately verified.");
            State = normal; Engine = normalEngine; recovering = false; evidenceActive = false;
            Debug.Log("FOREST_EVIDENCE_COMPLETE " + evidenceDirectory); Application.Quit();
        }
        void CaptureOffscreen(string name, int width, int height)
        {
            coinsLabel.text = "金币  " + State.coins.ToString("N0");
            reputationLabel.text = "声望  " + State.reputation;
            goalLabel.text = Engine.Goal;
            foreach (var b in bindings) b(); foreach (var b in modalBindings) b();
            var cameraObject = new GameObject("Evidence camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Paper; camera.orthographic = true; camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            var target = new RenderTexture(width, height, 24); camera.targetTexture = target;
            var scale = canvas.GetComponent<CanvasScaler>(); bool wasEnabled = scale.enabled; scale.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 10; canvas.scaleFactor = width / 1080f;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false); texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            File.WriteAllBytes(Path.Combine(evidenceDirectory, name + ".png"), texture.EncodeToPNG());
            RenderTexture.active = previous; camera.targetTexture = null; target.Release(); Destroy(target); Destroy(texture); Destroy(cameraObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null; scale.enabled = wasEnabled; ApplySafeArea();
        }
    }
}


