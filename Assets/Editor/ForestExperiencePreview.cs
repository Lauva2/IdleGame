using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ForestShop.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestShop.Editor
{
    // Isolated Editor development evidence. No ForestApp, SaveStore, device or
    // persistent player directory is created or read by this entry point.
    [InitializeOnLoad]
    public static class ForestExperiencePreview
    {
        const string Pending = "ForestExperiencePreview.Pending";
        static readonly string Output = Argument("-forestExperiencePreviewOutput") ?? "Builds/Previews/3d-d19-experience-r01-world";
        static readonly bool ShopOnly = Argument("-forestExperiencePreviewShop") == "true";
        const string Fixture = "Docs/Verification/3d-d18-forest04/Prepared/late.save";
        static ForestWorld3D world;
        static GameState state;
        static Economy economy;
        static StreamWriter frames, notices;
        static float started, nextShot;
        static double wallStarted;
        static int lastFrame, shot, region;
        static bool ending;

        static string Argument(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }

        static ForestExperiencePreview()
        {
            EditorApplication.playModeStateChanged += mode =>
            {
                if (mode == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                    EditorApplication.delayCall += Begin;
            };
        }

        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        static GameState Prepared()
        {
            string[] wrapper = File.ReadAllText(Fixture).Split(new[] { '\n' }, 4);
            if (wrapper.Length != 4 || wrapper[0].TrimEnd('\r') != "FORESTSHOP1")
                throw new InvalidDataException("Prepared development fixture wrapper is invalid.");
            var prepared = JsonUtility.FromJson<GameState>(wrapper[3]);
            StateValidation.Check(prepared);
            return prepared;
        }

        static void Begin()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            try
            {
                if (UnityEngine.Object.FindObjectOfType<ForestApp>() != null)
                    throw new InvalidOperationException("Preview refuses an active ForestApp.");
                Directory.CreateDirectory(Output + "/Live");
                Directory.CreateDirectory(Output + "/Prepared");
                Directory.CreateDirectory(Output + "/Poses");
                Directory.CreateDirectory(Output + "/Shopping");
                File.WriteAllText(Output + "/scope.txt",
                    "EDITOR DEVELOPMENT PREVIEW, NOT ANDROID OR FINAL ART ACCEPTANCE.\n" +
                    "Source fixture=" + Fixture + "; explicit prepared late structure.\n" +
                    "No ForestApp, SaveStore, ordinary player directory, device, audio or saved progression.\n" +
                    "Live: normal play-mode frames; once per Time.frameCount, Economy.Advance(Time.unscaledDeltaTime), Bind and real notices.\n" +
                    (ShopOnly ? "Live: 40 real seconds shop, new visit epoch; actual normal frame Advance and settlement notices.\n" : "0-20s garden,20-40s workshop,40-60s shop.\n") +
                    "PNG captures sample actual rendered time every >=0.5s; missed samples are not backfilled.\n" +
                    "Prepared: separate missing-material/full-capacity/simple-animation states. Simple animation observes 8 explicitly simulated core seconds.\n" +
                    "Poses: 5 residents, entrance phases8-13 and exit39-44 at3FPS input sampling. visitEpoch and initial facing explicitly prepared; NOT natural time or continuous footage.\n" +
                    "Poses use a separate prepared state and 8 simulated core seconds to arrange a sale; pose samples do not establish normal purchase timing.\n" +
                    "Shopping: explicit prepared visit epoch/facing and a labelled presentation-only sale notice; geometric key poses, NOT natural footage or economic sale proof.\n" +
                    "All PNGs are unchanged original World RenderTexture reads, no postprocessing. HUD, adaptation, normal progression and runtime performance are not established here.\n" +
                    "Unity=" + Application.unityVersion + "; host=" + Screen.width + "x" + Screen.height + "\n");
                state = Prepared(); state.simpleAnimation = false;
                world = new GameObject("Isolated d19 experience preview").AddComponent<ForestWorld3D>();
                world.Initialize(); world.Bind(state); world.SetRegion(ShopOnly ? 3 : 1); region = ShopOnly ? 3 : 1; world.RestartVisitSequence();
                economy = new Economy(state); economy.ProductionCompleted += Production;
                frames = new StreamWriter(Output + "/frames.csv", false, Encoding.UTF8);
                frames.WriteLine("file,frame,unscaledTime,elapsed,wallElapsed,region,harvests,crafts,sales,coins,activeProductionEffects");
                notices = new StreamWriter(Output + "/notices.csv", false, Encoding.UTF8);
                notices.WriteLine("frame,unscaledTime,elapsed,kind,facility,item,amount");
                started = Time.unscaledTime; wallStarted = EditorApplication.timeSinceStartup;
                lastFrame = Time.frameCount; nextShot = 0; shot = 0; ending = false;
                Application.targetFrameRate = 30;
                EditorApplication.update += Tick;
                Debug.Log("FOREST_EXPERIENCE_PREVIEW_STARTED " + Path.GetFullPath(Output));
            }
            catch (Exception error) { Fail(error); }
        }

        static string F(double number) => number.ToString("F6", CultureInfo.InvariantCulture);

        static void Production(ProductionNotice notice)
        {
            world.PresentProduction(notice);
            notices.WriteLine(Time.frameCount + "," + F(Time.unscaledTime) + "," + F(Time.unscaledTime - started) + "," +
                notice.Kind + "," + notice.Facility + "," + notice.Item + "," + notice.Amount);
        }

        static void Tick()
        {
            if (ending || !Application.isPlaying) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                economy.Advance(Time.unscaledDeltaTime);
                world.Bind(state);
                float elapsed = Time.unscaledTime - started;
                int nextRegion = ShopOnly ? 3 : elapsed < 20 ? 1 : elapsed < 40 ? 2 : 3;
                if (nextRegion != region) { region = nextRegion; world.SetRegion(region); }
                if (elapsed >= nextShot)
                {
                    string file = "Live/" + (++shot).ToString("D4") + "-region" + region + ".png";
                    Shot(file);
                    frames.WriteLine(file + "," + Time.frameCount + "," + F(Time.unscaledTime) + "," + F(elapsed) + "," +
                        F(EditorApplication.timeSinceStartup - wallStarted) + "," + region + "," + state.harvests + "," +
                        state.crafts + "," + state.sales + "," + state.coins + "," + world.ActiveProductionEffects);
                    frames.Flush(); notices.Flush();
                    nextShot = elapsed + .5f;
                }
                if (elapsed >= (ShopOnly ? 40 : 60)) Finish(elapsed);
            }
            catch (Exception error) { Fail(error); }
        }

        static void Shot(string relative)
        {
            world.RenderNow(); var previous = RenderTexture.active;
            Texture2D image = null;
            try
            {
                RenderTexture.active = world.View;
                image = new Texture2D(world.View.width, world.View.height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
                File.WriteAllBytes(Output + "/" + relative, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (image != null) UnityEngine.Object.DestroyImmediate(image);
            }
        }

        static void SetPrepared(GameState prepared)
        {
            StateValidation.Check(prepared);
            world.SetVisible(false); world.Bind(prepared); world.SetVisible(true);
        }

        static void StaticSamples()
        {
            var simple = Prepared(); simple.simpleAnimation = true; SetPrepared(simple);
            var simpleEngine = new Economy(simple); simpleEngine.ProductionCompleted += world.PresentProduction;
            simpleEngine.Advance(8); world.Bind(simple);
            for (int r = 1; r <= 3; r++) { world.SetRegion(r); Shot("Prepared/simple-region" + r + ".png"); }
            File.WriteAllText(Output + "/Prepared/simple-state.json", JsonUtility.ToJson(simple, true));

            foreach (bool full in new[] { false, true })
            {
                var blocked = Prepared(); int capacity = Catalog.Capacity[blocked.warehouse - 1];
                for (int c = 0; c < 4; c++) blocked.crops[c] = full ? capacity : 0;
                for (int i = 0; i < 6; i++) blocked.items[i] = full ? capacity : 0;
                foreach (var machine in blocked.machines) { machine.workItem = -1; machine.remaining = -1; machine.pending = -2; }
                foreach (var plot in blocked.plots) { plot.workItem = plot.config; plot.remaining = full ? 0 : Catalog.CropSeconds[plot.config]; }
                foreach (var shelf in blocked.shelves) shelf.remaining = 8;
                SetPrepared(blocked);
                string stem = full ? "full-capacity" : "missing-material";
                for (int r = 1; r <= 3; r++) { world.SetRegion(r); Shot("Prepared/" + stem + "-region" + r + ".png"); }
                File.WriteAllText(Output + "/Prepared/" + stem + "-state.json", JsonUtility.ToJson(blocked, true));
            }
        }

        static void SamplePoses()
        {
            var poses = Prepared(); poses.simpleAnimation = false; SetPrepared(poses); world.SetRegion(3);
            var saleEngine = new Economy(poses); saleEngine.ProductionCompleted += world.PresentProduction;
            saleEngine.Advance(8); world.Bind(poses); world.SetVisible(false); world.SetVisible(true);
            var epoch = typeof(ForestWorld3D).GetField("visitEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
            if (epoch == null) throw new MissingFieldException("ForestWorld3D.visitEpoch");
            var residents = world.GetComponentsInChildren<ForestResidentMotion>(true);
            using (var posesCsv = new StreamWriter(Output + "/poses.csv", false, Encoding.UTF8))
            {
                posesCsv.WriteLine("file,kind,resident,inputVisitPhase,inputSampleFPS,actualUnscaledTime");
                for (int resident = 0; resident < 5; resident++)
                    foreach (bool leaving in new[] { false, true })
                    {
                        foreach (var motion in residents) motion.transform.localRotation = Quaternion.Euler(0, leaving ? 0 : 180, 0);
                        for (int frame = 0; frame <= 15; frame++)
                        {
                            float phase = (leaving ? 39 : 8) + frame / 3f;
                            epoch.SetValue(world, Time.unscaledTime - resident * 52 - phase);
                            world.Bind(poses);
                            string file = "Poses/resident" + resident + "-" + (leaving ? "exit" : "enter") + "-" + frame.ToString("D3") + ".png";
                            Shot(file);
                            posesCsv.WriteLine(file + ",PREPARED_SAMPLED_POSE," + resident + "," + F(phase) + ",3," + F(Time.unscaledTime));
                        }
                    }
            }
            File.WriteAllText(Output + "/Prepared/poses-state.json", JsonUtility.ToJson(poses, true));
        }

        static void SampleShopping()
        {
            var prepared = Prepared(); SetPrepared(prepared); world.SetRegion(3);
            var epoch = typeof(ForestWorld3D).GetField("visitEpoch", BindingFlags.Instance | BindingFlags.NonPublic);
            var residents = world.GetComponentsInChildren<ForestResidentMotion>(true);
            float[] samples = { 21.2f, 22.2f, 28, 34.9f, 35.6f, 36.5f, 37.5f, 41 };
            for (int id = 0; id < 5; id++)
            {
                world.RestartVisitSequence(); int shelf = id % prepared.shelves.Length;
                world.PresentProduction(new ProductionNotice(ProductionKind.Sale, shelf, prepared.shelves[shelf].config, 1));
                for (int frame = 0; frame <= 225; frame++)
                {
                    float phase = 19 + frame * .1f;
                    foreach (var r in residents)
                        r.transform.localRotation = Quaternion.Euler(0, phase < 27 ? 180 : phase < 30 ? 0 : phase < 38 ? 90 : 0, 0);
                    epoch.SetValue(world, Time.unscaledTime - id * 52 - phase); world.Bind(prepared);
                    foreach (float sample in samples) if (Mathf.Abs(sample - phase) < .025f)
                        Shot("Shopping/resident" + id + "-phase" + sample.ToString("F1", CultureInfo.InvariantCulture) + ".png");
                }
            }
        }

        static void Finish(float elapsed)
        {
            ending = true; EditorApplication.update -= Tick;
            economy.ProductionCompleted -= Production; frames.Dispose(); notices.Dispose();
            File.WriteAllText(Output + "/live-final-state.json", JsonUtility.ToJson(state, true));
            StaticSamples(); SamplePoses(); SampleShopping();
            File.WriteAllText(Output + "/result.txt", "EDITOR DEVELOPMENT CAPTURE COMPLETE\nLive elapsed=" + F(elapsed) +
                "s; live shots=" + shot + "; prepared pose shots=160 at3FPS input sampling, not natural footage.\nNo ordinary persistence or device touched.\n");
            Debug.Log("FOREST_EXPERIENCE_PREVIEW_OK " + Path.GetFullPath(Output));
            EditorApplication.Exit(0);
        }

        static void Fail(Exception error)
        {
            ending = true; EditorApplication.update -= Tick;
            frames?.Dispose(); notices?.Dispose();
            Debug.LogException(error);
            try { File.WriteAllText(Output + "/error.txt", error.ToString()); } catch (IOException) { }
            EditorApplication.Exit(1);
        }
    }
}
