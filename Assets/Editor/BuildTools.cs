using System;
using System.IO;
using ForestShop;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ForestShop.Editor
{
    public static class BuildTools
    {
        const string Scene = "Assets/Scenes/ForestShop.unity";
        [MenuItem("Forest Shop/Prepare Project")]
        public static void Prepare()
        {
            StartupBranding.Configure();
            ForestAppIcon.Configure();
            ForestLaunchCard.Configure();
            PlayerSettings.companyName = "Forest Atelier";
            PlayerSettings.productName = "林间奇物铺";
            PlayerSettings.bundleVersion = "0.2.6";
            PlayerSettings.Android.bundleVersionCode = 8;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.forestatelier.curioshop");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false; PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.defaultScreenWidth = 540; PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.runInBackground = false;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)32;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.Mono2x);
            PlayerSettings.Android.forceInternetPermission = false;
            PlayerSettings.Android.androidIsGame = true;
            // Use the player's normal 30 FPS limiter rather than the optional
            // Android pacing filter; verify timing on each acceptance device.
            PlayerSettings.Android.optimizedFramePacing = false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
            PlayerSettings.stripEngineCode = true;
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.waitForManagedDebugger = false;
            EditorUserBuildSettings.allowDebugging = false;
            EditorUserBuildSettings.connectProfiler = false;
            QualitySettings.vSyncCount = 0; QualitySettings.antiAliasing = 0;
            if (!File.Exists(Scene))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Forest Shop", typeof(ForestApp));
                EditorSceneManager.SaveScene(scene, Scene);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(Scene, true) };
            foreach (string path in Directory.Exists("Assets/Resources/Art") ? Directory.GetFiles("Assets/Resources/Art", "*.png") : new string[0])
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (importer == null) continue;
                importer.textureType = TextureImporterType.Default; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None; importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log("FOREST_PREPARE_OK " + Application.unityVersion);
        }
        [MenuItem("Forest Shop/Build Windows Test Player")]
        public static void Windows()
        {
            Prepare(); Directory.CreateDirectory("Builds/Windows/dev01");
            Build(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = "Builds/Windows/dev01/ForestShop.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development });
        }
        [MenuItem("Forest Shop/Build Android Test APK")]
        public static void Android() => BuildAndroid(false);

        [MenuItem("Forest Shop/Build Android Release APK")]
        public static void AndroidRelease() => BuildAndroid(true);

        static void BuildAndroid(bool release)
        {
            Prepare();
            if (release)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
                PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
            }
            string output = release ? "Builds/Android/release01/ForestShop-release01.apk" : "Builds/Android/dev01/ForestShop-dev01.apk";
            var arguments = Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (arguments[i] != "-forestBuildOutput") continue;
                string candidatePath = Path.GetFullPath(arguments[i + 1]);
                string outputRoot = Path.GetFullPath("Builds/Android") + Path.DirectorySeparatorChar;
                if (!candidatePath.StartsWith(outputRoot, StringComparison.OrdinalIgnoreCase) || !candidatePath.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("Android candidate output must be an APK inside Builds/Android.");
                output = candidatePath;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            string tools = "C:/Program Files/Unity/Hub/Editor/2022.3.8f1c1/Editor/Data/PlaybackEngines/AndroidPlayer/";
            string sdk = Directory.Exists("Tools/AndroidSdk") ? Path.GetFullPath("Tools/AndroidSdk") : tools + "SDK";
            EditorPrefs.SetString("AndroidSdkRoot", sdk); EditorPrefs.SetBool("SdkUseEmbedded", false);
            EditorPrefs.SetString("AndroidNdkRootR16b", tools + "NDK"); EditorPrefs.SetString("AndroidNdkRoot", tools + "NDK"); EditorPrefs.SetBool("NdkUseEmbedded", false);
            EditorPrefs.SetString("JdkPath", tools + "OpenJDK"); EditorPrefs.SetBool("JdkUseEmbedded", false);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var external = assembly.GetType("UnityEditor.Android.AndroidExternalToolsSettings");
                if (external == null) continue;
                external.GetProperty("sdkRootPath")?.SetValue(null, sdk);
                external.GetProperty("ndkRootPath")?.SetValue(null, tools + "NDK");
                external.GetProperty("jdkRootPath")?.SetValue(null, tools + "OpenJDK");
                Debug.Log("FOREST_ANDROID_TOOLS " + external.GetProperty("ndkRootPath")?.GetValue(null));
            }
            EditorUserBuildSettings.buildAppBundle = false;
            Build(new BuildPlayerOptions { scenes = new[] { Scene }, locationPathName = output, target = BuildTarget.Android, options = release ? BuildOptions.None : BuildOptions.Development });
        }
        static void Build(BuildPlayerOptions options)
        {
            var report = BuildPipeline.BuildPlayer(options);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/" + options.target + "-summary.txt", "Result: " + report.summary.result + "\nErrors: " + report.summary.totalErrors + "\nWarnings: " + report.summary.totalWarnings + "\nOutput: " + report.summary.outputPath + "\nBytes: " + report.summary.totalSize + "\nDuration: " + report.summary.totalTime);
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("FOREST_BUILD_OK " + report.summary.outputPath);
        }
    }
}
