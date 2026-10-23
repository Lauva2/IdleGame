using System;
using System.IO;
using System.Text;
using ForestShop.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop.Editor
{
    [InitializeOnLoad]
    public static class ForestMapPreview
    {
        const string Pending = "ForestMapPreview.Pending";
        const string Output = "Builds/Previews/3d-d18-map";
        static ForestMapPreview()
        {
            EditorApplication.playModeStateChanged += c => { if (c == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false)) EditorApplication.delayCall += Capture; };
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Pending, true); EditorApplication.EnterPlaymode();
        }
        static void Shot(ForestWorld3D world, string name)
        {
            world.RenderNow(); var old = RenderTexture.active; RenderTexture.active = world.View;
            var image = new Texture2D(world.View.width, world.View.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, image.width, image.height), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            RenderTexture.active = old; UnityEngine.Object.Destroy(image);
        }
        static void Capture()
        {
            if (!SessionState.GetBool(Pending, false)) return; SessionState.SetBool(Pending, false);
            try
            {
                var world = new GameObject("Prepared map preview").AddComponent<ForestWorld3D>(); world.Initialize();
                var state = new GameState { tutorial = 4, welcomeRead = true, reputation = 150, stories = 5, sideRoom = true, simpleAnimation = true };
                state.plots = new Facility[12]; for (int i = 0; i < 12; i++) state.plots[i] = new Facility { config = i % 4, workItem = i % 4, remaining = 0 };
                state.machines = new[] { new Facility { config = 0 }, new Facility { config = 1 } };
                state.shelves = new[] { new Facility { config = 0 }, new Facility { config = 1 }, new Facility { config = 2 }, new Facility { config = 5 } };
                for (int i = 0; i < 6; i++) state.items[i] = 12;
                world.Bind(state);
                var results = new StringBuilder("EDITOR PREPARED STATE, NOT ANDROID OR LIVE PLAYER DATA\n");
                string[] names = { "01-clearing", "02-garden", "03-workshop", "04-front-shop" };
                for (int region = 0; region < 4; region++)
                {
                    world.SetRegion(region); Shot(world, names[region]);
                    float size = world.CameraSize; Vector3 focus = world.CameraFocus;
                    world.Zoom(.8f); world.Pan(new Vector2(.5f, .5f), new Vector2(.58f, .52f));
                    if (world.CameraSize >= size || Vector3.Distance(focus, world.CameraFocus) < .1f) throw new Exception("Camera zoom/pan failed " + region);
                    Shot(world, names[region] + "-moved");
                    world.ResetCamera();
                    if (Mathf.Abs(world.CameraSize - size) > .001f || Vector3.Distance(focus, world.CameraFocus) > .001f) throw new Exception("Camera reset failed " + region);
                    results.AppendLine("PASS zoom/pan/reset region=" + region);
                }
                for (int region = 1; region < 4; region++)
                {
                    world.SetRegion(region); world.Zoom(.9f); world.Pan(new Vector2(.5f, .5f), new Vector2(.53f, .52f)); Physics.SyncTransforms();
                    var camera = world.GetComponentInChildren<Camera>();
                    string kind = region == 1 ? "plot" : region == 2 ? "machine" : "shelf";
                    foreach (var target in world.GetComponentsInChildren<ForestObject3D>())
                    {
                        if (target.kind != kind) continue;
                        var box = target.GetComponent<BoxCollider>(); var uv = camera.WorldToViewportPoint(box.transform.TransformPoint(box.center));
                        if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) continue;
                        string hit = null; int index = -1; world.Selected = (k, n) => { hit = k; index = n; }; world.Pick(uv);
                        if (hit != kind || index != target.index) throw new Exception("Moved camera picking failed " + kind + target.index + ": " + hit + index);
                        results.AppendLine("PASS moved camera ray " + kind + target.index);
                    }
                }
                world.SetRegion(0); world.Zoom(100); world.Pan(Vector2.zero, Vector2.one * 100);
                if (world.CameraSize > 27 || Mathf.Abs(world.CameraFocus.x) > 4 || Mathf.Abs(world.CameraFocus.z) > 6) throw new Exception("Map clamp failed");
                Shot(world, "05-bounded-zoom-out");
                world.Zoom(.0001f); if (world.CameraSize < 5) throw new Exception("Minimum zoom failed");
                results.AppendLine("PASS zoom bounds, pan bounds");
                File.WriteAllText(Output + "/camera-checks.txt", results.ToString());
                Debug.Log("FOREST_MAP_PREVIEW_OK " + Path.GetFullPath(Output)); EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
