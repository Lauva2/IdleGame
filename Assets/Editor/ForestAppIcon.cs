using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop.Editor
{
    public static class ForestAppIcon
    {
        const string Path = "Assets/Art/Application/ForestShop.png";
        public static void Configure()
        {
            if (!File.Exists(Path)) Generate();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path);
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android, IconKind.Application);
            var icons = new Texture2D[sizes.Length]; for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons, IconKind.Application);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon }, IconKind.Any);
        }
        static void Generate()
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            var host = new GameObject("Original app icon studio");
            var camera = host.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(56, 82, 58, 255); camera.orthographic = true;
            var target = new RenderTexture(512, 512, 24); camera.targetTexture = target;
            var ui = new GameObject("Icon canvas", typeof(RectTransform), typeof(Canvas)); ui.transform.SetParent(host.transform, false);
            var canvas = ui.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 5;
            var symbol = new GameObject("Original woodland shop emblem", typeof(RectTransform), typeof(CanvasRenderer), typeof(ForestSymbolGraphic)); symbol.transform.SetParent(ui.transform, false);
            var rect = symbol.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(390, 390);
            var graphic = symbol.GetComponent<ForestSymbolGraphic>(); graphic.Symbol = ForestSymbol.Home; graphic.Light = true;
            Canvas.ForceUpdateCanvases(); camera.Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(512, 512, TextureFormat.RGBA32, false); image.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); image.Apply();
            File.WriteAllBytes(Path, image.EncodeToPNG()); RenderTexture.active = previous;
            camera.targetTexture = null; target.Release(); Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(host);
            AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Path); importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false; importer.alphaIsTransparency = false; importer.SaveAndReimport();
        }
    }
}
