using System;
using System.IO;
using System.Reflection;
using ForestShop.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestShop.Editor
{
    // Local development previews only. Never opens SaveStore or the player's save.
    [InitializeOnLoad]
    public static class ForestCourtyardPreview
    {
        const string Pending = "ForestCourtyardPreview.Pending";
        const string Output = "Builds/Previews/3d-d13-courtyard";
        static ForestCourtyardPreview()
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
            StartupBranding.Configure(Output + "/startup-branding.json");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
        static void Capture()
        {
            if (!SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            try
            {
                var world = new GameObject("Courtyard development preview").AddComponent<ForestWorld3D>();
                world.Initialize();
                var state = new GameState { tutorial = 4, welcomeRead = true, reputation = 150, stories = 5, sideRoom = true, simpleAnimation = true };
                state.plots = new Facility[Catalog.MaxPlots];
                for (int i = 0; i < state.plots.Length; i++) state.plots[i] = new Facility { config = i % 4, workItem = i % 4, remaining = 0, level = 3 };
                state.machines = new[] { new Facility { config = 0, workItem = 0, remaining = 8 }, new Facility { config = 1, workItem = 1, remaining = 8 } };
                state.shelves = new[] { new Facility { config = 0 }, new Facility { config = 1 }, new Facility { config = 2 }, new Facility { config = 5 } };
                for (int i = 0; i < 6; i++) state.items[i] = 12;
                for (int i = 0; i < 4; i++) state.decoration[i] = true;
                StateValidation.Check(state); world.Bind(state);
                string[] names = { "01-courtyard-overview", "02-twelve-bed-garden", "03-independent-workshop", "04-treehouse-shop" };
                for (int i = 0; i < names.Length; i++) { world.SetRegion(i); Shot(world, names[i]); }
                world.SetRegion(1); Physics.SyncTransforms();
                var camera = world.GetComponentInChildren<Camera>();
                var targets = new System.Text.StringBuilder();
                for (int i = 0; i < Catalog.MaxPlots; i++)
                {
                    var bed = world.transform.Find("Handcrafted forest diorama/Twelve-bed woodland garden/Garden bed " + i);
                    Vector3 uv = camera.WorldToViewportPoint(bed.TransformPoint(new Vector3(0, .7f, 0)));
                    if (uv.x < .02f || uv.x > .98f || uv.y < .02f || uv.y > .98f) throw new Exception("Garden bed outside detail camera: " + i + " " + uv);
                    int hit = -1; world.Selected = (kind, index) => { if (kind == "plot") hit = index; };
                    world.Pick(new Vector2(uv.x, uv.y));
                    if (hit != i) throw new Exception("Garden ray target mismatch: wanted " + i + ", got " + hit);
                    targets.AppendLine(i + "," + uv.x.ToString("F4") + "," + uv.y.ToString("F4") + ",PASS");
                }
                File.WriteAllText(Output + "/garden-ray-targets.csv", "plotIndex,viewportX,viewportY,result\n" + targets);
                world.Bind(new GameState { welcomeRead = true }); world.SetRegion(0); Shot(world, "05-new-game-clearing");
                string old = "Docs/Verification/3d-d12-audio02/normal-final.save";
                if (File.Exists(old))
                {
                    string[] lines = File.ReadAllLines(old); var legacy = JsonUtility.FromJson<GameState>(lines[lines.Length - 1]);
                    StateValidation.Check(legacy); legacy.simpleAnimation = true;
                    world.Bind(legacy); world.SetRegion(1); Shot(world, "06-six-bed-old-save-layout");
                }
                File.WriteAllText(Output + "/preview-scope.txt", "EDITOR PLAY-MODE 3D PREVIEW, NOT ANDROID EVIDENCE.\nPrepared 12-bed state and read-only old-save copy; no SaveStore, no device install, no player progress writes.\nSimple animation enabled for static layout inspection. MuMu, final art and long-duration acceptance deferred by user.\nUnity=" + Application.unityVersion + "\n");
                Debug.Log("FOREST_COURTYARD_PREVIEW_OK " + Path.GetFullPath(Output));
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
        static void Shot(ForestWorld3D world, string name)
        {
            world.RenderNow(); var old = RenderTexture.active; RenderTexture.active = world.View;
            var image = new Texture2D(world.View.width, world.View.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = old; UnityEngine.Object.Destroy(image);
        }
    }
    public static class StartupBranding
    {
        [Serializable] class Result { public string unity; public bool professionalLicense; public bool splashEnabled; public bool unityLogoEnabled; public string status; }
        public static void Configure(string output = null)
        {
            var probe = typeof(UnityEditorInternal.InternalEditorUtility).GetMethod("HasPro", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            bool pro = probe != null && (bool)probe.Invoke(null, null);
            if (pro)
            {
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                AssetDatabase.SaveAssets();
            }
            var result = new Result { unity = Application.unityVersion, professionalLicense = pro,
                splashEnabled = PlayerSettings.SplashScreen.show, unityLogoEnabled = PlayerSettings.SplashScreen.showUnityLogo,
                status = pro ? "Disabled through supported PlayerSettings API; verify next Android startup." : "Unity 2022.3 Personal cannot disable the built-in splash; engine/license decision required." };
            if (output != null) File.WriteAllText(output, JsonUtility.ToJson(result, true));
            Debug.Log("FOREST_STARTUP_BRANDING " + JsonUtility.ToJson(result));
        }
    }
}
