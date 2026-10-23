using System;
using System.IO;
using UnityEngine;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        bool inputProbe;
        int probeFrames;
        float probeStart, probeMaximum;
        void DiagnosticsStart()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            inputProbe = File.Exists(Path.Combine(Application.persistentDataPath, "forest-input-probe.txt"));
            probeStart = Time.realtimeSinceStartup;
            if (inputProbe) { InstallLoopProbe(); Camera.onPreCull += ProbeCameraStart; Camera.onPostRender += ProbeCameraEnd; }
#endif
        }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction> loopProbes = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction>();
        static readonly System.Collections.Generic.Dictionary<int, long> cameraStarts = new System.Collections.Generic.Dictionary<int, long>();
        static void ProbeCameraStart(Camera camera) { cameraStarts[camera.GetInstanceID()] = System.Diagnostics.Stopwatch.GetTimestamp(); }
        static void ProbeCameraEnd(Camera camera)
        {
            if (!cameraStarts.TryGetValue(camera.GetInstanceID(), out long start)) return;
            double elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - start) / (double)System.Diagnostics.Stopwatch.Frequency;
            if (elapsed > .08) Debug.Log("FOREST_SLOW_CAMERA " + camera.name + " seconds=" + elapsed.ToString("F6"));
        }
        static void InstallLoopProbe()
        {
            var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            WrapLoop(ref loop);
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
        }
        static void WrapLoop(ref UnityEngine.LowLevel.PlayerLoopSystem parent)
        {
            if (parent.subSystemList == null) return;
            var children = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>();
            foreach (var original in parent.subSystemList)
            {
                var child = original; WrapLoop(ref child);
                string label = child.type == null ? "unknown" : child.type.FullName;
                long started = 0;
                UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction before = () => started = System.Diagnostics.Stopwatch.GetTimestamp();
                UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction after = () => {
                    double elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - started) / (double)System.Diagnostics.Stopwatch.Frequency;
                    if (elapsed > .08) Debug.Log("FOREST_SLOW_LOOP " + label + " seconds=" + elapsed.ToString("F6") + " frame=" + Time.frameCount);
                };
                loopProbes.Add(before); loopProbes.Add(after);
                children.Add(new UnityEngine.LowLevel.PlayerLoopSystem { updateDelegate = before });
                children.Add(child);
                children.Add(new UnityEngine.LowLevel.PlayerLoopSystem { updateDelegate = after });
            }
            parent.subSystemList = children.ToArray();
        }
#endif
        void DiagnosticsUpdate()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!inputProbe) return;
            probeFrames++; probeMaximum = Mathf.Max(probeMaximum, Time.unscaledDeltaTime);
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                if (touch.phase != TouchPhase.Stationary && touch.phase != TouchPhase.Moved)
                    Debug.Log("FOREST_INPUT " + touch.phase + " screen=" + touch.position + " frame=" + Time.frameCount);
            }
            if (Time.realtimeSinceStartup - probeStart >= 5)
            {
                Debug.Log("FOREST_FRAME_PROBE frames=" + probeFrames + " wall=" + (Time.realtimeSinceStartup - probeStart).ToString("F3") + " maxDelta=" + probeMaximum.ToString("F3") + " focus=" + Application.isFocused + " background=" + backgrounded + " evidence=" + evidenceActive);
                probeFrames = 0; probeMaximum = 0; probeStart = Time.realtimeSinceStartup;
                string modePath = Path.Combine(Application.persistentDataPath, "forest-render-probe.txt");
                string mode = File.Exists(modePath) ? File.ReadAllText(modePath).Trim() : "normal";
                if (world3D != null) world3D.SetVisible(currentPage == "小店" && mode != "no-world" && mode != "no-both");
                if (canvas != null) canvas.enabled = mode != "no-ui" && mode != "no-both";
                if (mode == "no-shadows") QualitySettings.shadows = ShadowQuality.Disable;
                if (mode == "no-sky") { RenderSettings.skybox = null; RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom; RenderSettings.reflectionIntensity = 0; }
                if (mode == "no-occlusion") foreach (var c in FindObjectsOfType<Camera>()) c.useOcclusionCulling = false;
                if (mode == "no-lights") foreach (var light in FindObjectsOfType<Light>()) light.enabled = false;
                if (mode == "no-msaa") foreach (var c in FindObjectsOfType<Camera>())
                {
                    c.allowMSAA = false;
                    if (c.targetTexture != null && c.targetTexture.antiAliasing > 1)
                    { var rt = c.targetTexture; rt.Release(); rt.antiAliasing = 1; rt.Create(); }
                }
                Debug.Log("FOREST_RENDER_PROBE mode=" + mode + " renderers=" + FindObjectsOfType<MeshRenderer>().Length);
            }
#endif
        }
        void DiagnosticsClick(string caption)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (inputProbe) Debug.Log("FOREST_CLICK " + caption + " frame=" + Time.frameCount);
#endif
        }
        long DiagnosticsStamp()
        {
            return inputProbe ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        }
        void DiagnosticsStage(string section, ref long stamp)
        {
            if (!inputProbe) return;
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            double elapsed = (now - stamp) / (double)System.Diagnostics.Stopwatch.Frequency;
            if (elapsed > .08) Debug.Log("FOREST_SLOW_SECTION " + section + " seconds=" + elapsed.ToString("F6") + " frame=" + Time.frameCount);
            stamp = now;
        }
    }
}
