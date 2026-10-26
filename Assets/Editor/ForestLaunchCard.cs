using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop.Editor
{
    public static class ForestLaunchCard
    {
        const string ImagePath = "Assets/Art/Application/ForestLaunch.png";
        public static void Configure()
        {
            if (!File.Exists(ImagePath)) Generate();
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            settings.FindProperty("androidSplashScreen").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(ImagePath);
            settings.FindProperty("AndroidSplashScreenScale").intValue = 2; // supported Scale to Fill; artwork stays in the central safe composition
            settings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.SplashScreen.backgroundColor = new Color32(255,249,228,255);
        }
        public static void Generate()
        {
            var cover = ForestStartupCover.Create(Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular"));
            var host = new GameObject("Native launch card studio",typeof(Camera)); var camera = host.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color32(255,249,228,255); camera.orthographic=true;
            var target = new RenderTexture(1080,1920,24); camera.targetTexture=target;
            var canvas=cover.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=5;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(1080,1920,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();
            File.WriteAllBytes(ImagePath,image.EncodeToPNG());RenderTexture.active=previous;
            camera.targetTexture=null;target.Release();Object.DestroyImmediate(image);Object.DestroyImmediate(target);Object.DestroyImmediate(cover.gameObject);Object.DestroyImmediate(host);
            AssetDatabase.ImportAsset(ImagePath,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(ImagePath);importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.SaveAndReimport();
        }
    }
}
