using System;
using System.Collections.Generic;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed class ForestObject3D : MonoBehaviour { public string kind; public int index; }

    public sealed class ForestWorldPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public ForestWorld3D world; Vector2 start;
        public void OnPointerDown(PointerEventData e) { start = e.position; }
        public void OnPointerUp(PointerEventData e)
        {
            if (Vector2.Distance(start, e.position) > 18) return;
            var rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out var local))
                world.Pick(new Vector2((local.x - rect.rect.x) / rect.rect.width, (local.y - rect.rect.y) / rect.rect.height));
        }
    }

    public sealed class ForestWorld3D : MonoBehaviour
    {
        public RenderTexture View { get; private set; }
        public int Region { get; private set; }
        public Action<string, int> Selected;
        public Action DoorEntered;
        Camera worldCamera, portraitCamera;
        Transform worldRoot, portraitRoot, sideClosed, sideOpen, lowerRoom, upperRoom, canopy, roof, oak, landscape, woodland;
        Transform shopArea, workshopArea, gardenArea, workshopRoof, shopRoofCap, workshopRoofCap;
        static readonly Vector3 ShopOrigin = V(.5f, -3.726f, 5.3f);
        static readonly Vector3 WorkshopOrigin = V(-7.2f, 0, -1.6f);
        static readonly Vector3 GardenOrigin = V(3, 0, -9.1f);
        RenderTexture fullView, closeView;
        readonly Transform[] plots = new Transform[Catalog.MaxPlots], cropModels = new Transform[Catalog.MaxPlots * 4], shelves = new Transform[4], goods = new Transform[24], machines = new Transform[2], visitors = new Transform[5];
        readonly Transform[] emptyPlots = new Transform[Catalog.MaxPlots], emptyShelves = new Transform[4];
        readonly ForestResidentMotion[] residentMotion = new ForestResidentMotion[5];
        readonly Transform[] shoppingBags = new Transform[5];
        readonly Transform[] passageCurtains = new Transform[2];
        Transform spoon, curtainPlain, curtainLeaves, matPlain, matBread, signOld, signNew, potPlain, potFlowers;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        GameState state;
        long lastSale;
        float saleAt = -100;
        float visitEpoch;
        float lastDoorAt = -10;
        static Vector3 V(float x, float y, float z) => new Vector3(x, y, z);

        public void Initialize()
        {
            gameObject.name = "Forest 3D stage"; transform.position = V(1000, 0, 0); gameObject.layer = 10;
            worldRoot = Group(transform, "Handcrafted forest diorama");
            worldCamera = CameraFor("World camera", 10, transform); worldCamera.backgroundColor = Hex(0xDCE3CD);
            fullView = new RenderTexture(864, 1056, 24) { name = "Live 3D forest panorama", antiAliasing = 2 }; fullView.Create();
            closeView = new RenderTexture(864, 1024, 24) { name = "Live 3D forest detail", antiAliasing = 2 }; closeView.Create();
            View = fullView; worldCamera.targetTexture = View;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex(0x929D88); RenderSettings.ambientEquatorColor = Hex(0x827E6C); RenderSettings.ambientGroundColor = Hex(0x5F6151); RenderSettings.ambientIntensity = .75f;
            QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.VeryHigh; QualitySettings.shadowDistance = 65; QualitySettings.shadowCascades = 2;
            var sun = Group(transform, "Afternoon sun").gameObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.color = Hex(0xFFF3DE); sun.intensity = .84f; sun.transform.localEulerAngles = V(42, -35, 0); sun.shadows = LightShadows.Soft; sun.shadowStrength = .32f; sun.shadowBias = .045f; sun.cullingMask = 1 << 10;
            sun.shadowCustomResolution = 2048;
            Shader.SetGlobalFloat("_ForestShadowTexelSize", 1f / 2048);
            var fill = Group(transform, "Forest reflected light").gameObject.AddComponent<Light>(); fill.type = LightType.Directional; fill.color = Hex(0xC4DDC7); fill.intensity = .18f; fill.transform.localEulerAngles = V(25, 140, 0); fill.cullingMask = 1 << 10;
            BuildArchitecture(); BuildObjects(); ArrangeForestCourtyard();
            portraitRoot = Group(null, "3D portrait studio", V(2000, 0, 0)); portraitRoot.gameObject.layer = 11;
            portraitCamera = CameraFor("Portrait camera", 11, portraitRoot); portraitCamera.enabled = false; portraitCamera.backgroundColor = Color.clear;
            portraitCamera.transform.localPosition = V(2.7f, 2.2f, -7); portraitCamera.transform.LookAt(portraitRoot.position + V(0, 1.05f, 0)); portraitCamera.orthographicSize = 1.36f;
            var light = Group(portraitRoot, "Portrait softbox").gameObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .88f; light.color = Hex(0xFFF1DB); light.transform.localEulerAngles = V(35, -25, 0); light.cullingMask = 1 << 11;
            SetRegion(0);
        }
        Camera CameraFor(string name, int layer, Transform parent)
        {
            var c = Group(parent, name).gameObject.AddComponent<Camera>(); c.orthographic = true; c.nearClipPlane = .1f; c.farClipPlane = 100; c.cullingMask = 1 << layer; c.clearFlags = CameraClearFlags.SolidColor; c.allowHDR = false; c.allowMSAA = true; return c;
        }
        public void SetVisible(bool visible) { if (worldCamera != null) worldCamera.enabled = visible; }
        public void RestartVisitSequence() { visitEpoch = Time.unscaledTime; }
        public void RenderNow() { if (worldCamera != null) worldCamera.Render(); }
        public Vector2 PlotMarker(int index)
        { Vector3 v = worldCamera.WorldToViewportPoint(plots[index].TransformPoint(V(0, .25f, -.96f))); return new Vector2(v.x, v.y); }
        public Vector2 ShelfMarker(int index)
        { Vector3 v = worldCamera.WorldToViewportPoint(shelves[index].TransformPoint(V(0, 1.85f, 0))); return new Vector2(v.x, v.y); }
        public void SetRegion(int region)
        {
            Region = region;
            View = region == 0 ? fullView : closeView; worldCamera.targetTexture = View;
            Vector3 focus = region == 1 ? gardenArea.TransformPoint(V(0, .4f, .05f))
                : region == 2 ? workshopArea.TransformPoint(V(0, 2.4f, -.2f))
                : region == 3 ? shopArea.TransformPoint(V(.55f, 5.7f, .1f)) : transform.position + V(-.8f, 2.2f, -2.7f);
            worldCamera.orthographicSize = region == 0 ? 16.4f : region == 1 ? 7.1f : region == 2 ? 6.25f : 6.85f;
            worldCamera.transform.position = focus + (region == 0 ? V(3.5f, 25, -33) : region == 1 ? V(.7f, 18, -15) : V(2, 7, -18));
            worldCamera.transform.LookAt(focus);
            workshopArea.gameObject.SetActive(region == 0 || region == 2);
            shopArea.gameObject.SetActive(region == 0 || region == 3);
            gardenArea.gameObject.SetActive(region == 0 || region == 1);
            if (lowerRoom != null) lowerRoom.gameObject.SetActive(region == 0 || region == 2);
            if (upperRoom != null) upperRoom.gameObject.SetActive(region == 0 || region == 3);
            if (canopy != null) canopy.gameObject.SetActive(region == 0);
            if (oak != null) oak.gameObject.SetActive(region == 0);
            if (landscape != null) landscape.gameObject.SetActive(true);
            if (woodland != null) woodland.gameObject.SetActive(region == 0);
            if (roof != null) roof.gameObject.SetActive(region == 0);
            if (workshopRoof != null) workshopRoof.gameObject.SetActive(region == 0);
            if (shopRoofCap != null) shopRoofCap.gameObject.SetActive(region == 3);
            if (workshopRoofCap != null) workshopRoofCap.gameObject.SetActive(region == 2);
            if (state != null) Bind(state);
            if (region == 2 || region == 3) FitArea(region == 2 ? workshopArea : shopArea);
        }
        void FitArea(Transform area)
        {
            Vector3 right = worldCamera.transform.right, up = worldCamera.transform.up;
            Vector3 origin = worldCamera.transform.position;
            float left = float.PositiveInfinity, bottom = float.PositiveInfinity, rightEdge = float.NegativeInfinity, top = float.NegativeInfinity;
            foreach (var r in area.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue; Bounds b = r.bounds;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = b.center + Vector3.Scale(b.extents, V(x, y, z)) - origin;
                    float px = Vector3.Dot(point, right), py = Vector3.Dot(point, up);
                    left = Mathf.Min(left, px); rightEdge = Mathf.Max(rightEdge, px); bottom = Mathf.Min(bottom, py); top = Mathf.Max(top, py);
                }
            }
            if (float.IsInfinity(left)) return;
            worldCamera.transform.position += right * ((left + rightEdge) * .5f) + up * ((bottom + top) * .5f);
            float aspect = (float)View.width / View.height;
            worldCamera.orthographicSize = Mathf.Max((top - bottom) * .5f, (rightEdge - left) * .5f / aspect) * 1.08f;
        }
        void BuildArchitecture()
        {
            var s = landscape = Group(worldRoot, "Forest garden ground");
            Shape(s, "Broad forest courtyard clearing", Lathe(new[] { new Vector2(0, -.7f), new Vector2(5.6f, -.7f), new Vector2(6.7f, -.3f), new Vector2(6.9f, 0), new Vector2(6.7f, .12f), new Vector2(0, .12f) }, 64), V(-1.7f, 0, -3), V(1.96f, 1, 2.45f), Hex(0x90A477));
            oak = Group(worldRoot, "Living oak trunk and boughs");
            Sculpture(oak, "Continuous curved oak with spreading roots", "OakTrunk", Vector3.zero, Vector3.one, Color.white);
            canopy = Group(worldRoot, "Oak leaf canopy");
            canopy.localScale = V(1, .73f, 1); canopy.localPosition = V(0, 2.295f, 0);
            Sculpture(canopy, "Asymmetric connected oak crown", "OakCrown", Vector3.zero, Vector3.one, Color.white);
            LeafRelief(canopy, Vector3.zero, Vector3.one, Color.white);
            woodland = Group(worldRoot, "Woodland edge behind the shop");
            // Different age, lean and crown proportion establish a small grove.
            Vector3[] treeAt = { V(-10, .05f, 6.5f), V(8.4f, .05f, 6.8f), V(-6.8f, .05f, 9.5f), V(5.5f, .05f, 10.8f) };
            Vector3[] treeScale = { V(.32f, .35f, .34f), V(.35f, .32f, .35f), V(.28f, .44f, .30f), V(.26f, .40f, .28f) };
            for (int i = 0; i < treeAt.Length; i++)
            {
                var tree = Group(woodland, "Woodland oak " + i, treeAt[i]); tree.localScale = treeScale[i]; tree.localEulerAngles = V(0, -18 + i * 14, i == 0 ? -8 : i == 1 ? 5 : -3);
                Sculpture(tree, "Young oak trunk", "WoodlandTrunk", Vector3.zero, Vector3.one, i < 2 ? Hex(0xCBD4BB) : Hex(0xD4DDC8));
                Sculpture(tree, "Woodland crown", "WoodlandCrown", V(0, -9.1f, 0), V(1.12f, 1.6f, i == 1 ? 1 : 1.1f), i < 2 ? Hex(0xB8C7AE) : Hex(0xD1DDC7));
                LeafRelief(tree, V(0, -9.1f, 0), V(1.12f, 1.6f, i == 1 ? 1 : 1.1f), i < 2 ? Hex(0xB8C7AE) : Hex(0xD1DDC7));
            }
            for (int floor = 0; floor < 2; floor++)
            {
                var room = Group(worldRoot, floor == 0 ? "Workshop cutaway" : "Shop cutaway"); if (floor == 0) lowerRoom = room; else upperRoom = room;
                float y = 1.1f + floor * 3.45f;
                Box(room, "Floor shadow beam", V(0, y - .18f, -.05f), V(8.9f, .4f, 4.7f), DarkWood);
                for (int plank = 0; plank < 17; plank++) Box(room, "Honey oak floorboard", V(-4.16f + plank * .52f, y, -.03f), V(.505f, .15f, 4.6f), plank % 3 == 0 ? Honey : Hex(0xBE8C58));
                if (floor == 0) Box(room, "Cream rear wall", V(0, y + 1.55f, 2.18f), V(8.5f, 3, .2f), Cream);
                else
                {
                    Box(room, "Shop rear wall beside passage", V(.58f, y + 1.55f, 2.18f), V(7.34f, 3, .2f), Cream);
                    Box(room, "Passage lintel wall", V(-3.7f, y + 2.7f, 2.18f), V(1.15f, .7f, .2f), Cream);
                    Box(room, "Passage floor continues behind curtain", V(-3.7f, y, 3.75f), V(1.5f, .15f, 3.4f), Wood);
                    Box(room, "Shaded passage rear", V(-3.7f, y + 1.2f, 5.4f), V(1.5f, 2.4f, .15f), DarkWood);
                    for (int side = -1; side <= 1; side += 2)
                        Box(room, "Passage side wall", V(-3.7f + side * .74f, y + 1.2f, 3.75f), V(.16f, 2.4f, 3.4f), Cream);
                    // Moving panels stay outside the static room mesh batch.
                    for (int side = 0; side < 2; side++)
                    {
                        var panel = passageCurtains[side] = Group(worldRoot, "Parting linen panel " + side, V(-3.7f, y + 2.08f, 2.02f));
                        for (int fold = 0; fold < 5; fold++)
                            Ball(panel, "Soft hanging linen fold", V((side == 0 ? -1 : 1) * (.055f + fold * .116f), -1.04f, 0), V(.16f, 2.06f, .12f), fold % 2 == 0 ? Deep : Leaf);
                        Combine(panel);
                    }
                    Tube(room, "Passage curtain rail", V(-4.27f, y + 2.16f, 2), V(-3.08f, y + 2.16f, 2), .055f, Honey);
                    Box(room, "Doorway threshold", V(-3.7f, y + .09f, 2.07f), V(1.2f, .09f, .45f), Honey);
                }
                for (int i = -2; i <= 2; i++)
                    if (floor == 0 || i != -2) Box(room, "Timber wall upright", V(i * 1.95f, y + 1.55f, 2.02f), V(.13f, 3.2f, .19f), Wood);
                Box(room, "Rear horizontal timber", V(0, y + 2.87f, 2), V(8.6f, .18f, .22f), Wood);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(room, "Side cutaway wall", V(side * 4.3f, y + .56f, .4f), V(.17f, 1.1f, 3.7f), Cream);
                    Box(room, "Front support post", V(side * 4.27f, y + 1.5f, -1.9f), V(.23f, 3.2f, .25f), DarkWood);
                    Tube(room, "Curved corner brace", V(side * 4.25f, y + 2.85f, -1.9f), V(side * 3.4f, y + 3.25f, -1.9f), .12f, Wood);
                }
                Window(room, V(floor == 0 ? -2.5f : 0, y + 1.7f, 1.86f));
                Box(room, "Floor fascia", V(0, y - .22f, -2.32f), V(9.1f, .23f, .21f), Honey);
                for (int i = 0; i < 12; i++)
                {
                    float x = -4.2f + i * .76f;
                    Ball(room, "Trailing ivy leaf", V(x, y - .39f - (i % 3) * .11f, -2.39f), V(.24f, .37f, .08f), i % 2 == 0 ? Leaf : Moss, V(0, 0, i % 2 == 0 ? 28 : -28));
                }
                if (floor == 0) Box(room, "Front ceiling beam", V(0, y + 3.1f, -2.12f), V(8.7f, .17f, .19f), Wood);
                Combine(room);
            }
            // Scalloped green tiled canopy over the open dollhouse frontage.
            roof = Group(worldRoot, "Moss glazed roof");
            for (int row = 0; row < 5; row++) for (int col = 0; col < 18; col++)
                Box(roof, "Overlapping leaf roof tile", V(-4.45f + col * .52f + row % 2 * .13f, 8.6f - row * .17f + Mathf.Sin(col * 1.7f) * .012f, 1.72f - row * .66f), V(.57f, .11f, .85f), (row * 13 + col * 7) % 5 == 0 ? Hex(0x53745A) : (row + col * 3) % 4 == 0 ? Hex(0x486B54) : Deep, V(-14, 0, col % 3 - 1));
            Tube(roof, "Roof copper ridge", V(-4.7f, 8.72f, 2), V(4.7f, 8.72f, 2), .11f, Copper); Combine(roof);
            CourtyardPaths(s);
            for (int i = 0; i < 8; i++)
            {
                var a = V(-4.65f + i * 1.32f, 7.65f + Mathf.Abs(i - 3.5f) * .09f, -2.08f);
                if (i < 7) Tube(upperRoom, "Tea party light cord", a, V(a.x + 1.32f, 7.65f + Mathf.Abs(i + 1 - 3.5f) * .09f, a.z), .018f, DarkWood);
                Ball(upperRoom, "Warm lantern", a + V(0, -.16f, 0), V(.15f, .21f, .15f), Gold);
            }
            ForestModels.Pot(lowerRoom, V(-3.7f, 1.18f, -1.65f), 1.15f, false);
            ForestModels.Pot(upperRoom, V(3.7f, 4.63f, 1.1f), 1.1f, false);
            Box(lowerRoom, "Warehouse chest", V(3.65f, 1.59f, -.84f), V(1.03f, .78f, .7f), Wood);
            Box(lowerRoom, "Chest lid", V(3.65f, 2.03f, -.84f), V(1.09f, .16f, .76f), Honey);
            Box(lowerRoom, "Brass chest latch", V(3.65f, 1.84f, -1.21f), V(.13f, .2f, .035f), Gold);
            Hotspot(lowerRoom, "warehouse", 0, V(3.65f, 1.65f, -.84f), V(1.4f, 1.1f, 1.1f));
            Combine(s);
            sideClosed = Annex(false); sideOpen = Annex(true);
            curtainPlain = Curtains(false); curtainLeaves = Curtains(true);
            matPlain = Rug(false); matBread = Rug(true);
            signOld = Sign(false); signNew = Sign(true);
            potPlain = Planter(false); potFlowers = Planter(true);
            Hotspot(worldRoot, "decoration", 0, V(4.6f, .65f, -2.2f), V(1.1f, 1.4f, 1.1f));
        }
        void CourtyardPaths(Transform ground)
        {
            foreach (var origin in new[] { V(ShopOrigin.x, 0, ShopOrigin.z), WorkshopOrigin })
                for (int step = 0; step < 4; step++)
                    Box(ground, "Independent porch step", origin + V(-.65f, .2f + step * .22f, -2.65f + step * .3f), V(2.7f - step * .15f, .22f, .48f), Hex(0xAEAE98));
            StonePath(ground, V(-.2f, .14f, 2.4f), V(-3.2f, .14f, .9f), 8);
            StonePath(ground, V(-3.2f, .14f, .9f), V(-7.8f, .14f, -4.7f), 14);
            StonePath(ground, V(-6.9f, .14f, -4.8f), V(-2.4f, .14f, -6.4f), 10);
            StonePath(ground, V(-2.4f, .14f, -4.5f), V(-2.4f, .14f, -14.3f), 21);
            StonePath(ground, V(-2.4f, .14f, -6.4f), V(6.8f, .14f, -6.4f), 19);
            StonePath(ground, V(-2.4f, .14f, -11.8f), V(6.8f, .14f, -11.8f), 19);
            for (int i = 0; i < 34; i++)
            {
                float a = i * 2.39996f; var at = V(-1.7f + Mathf.Sin(a) * 12.1f, .14f, -3 + Mathf.Cos(a) * 14.7f);
                if (at.x > -2 && at.x < 9 && at.z > -15.5f && at.z < -3.4f) continue;
                Ball(ground, "Clearing edge shrub", at + V(0, .2f, 0), V(.85f + i % 3 * .14f, .65f, .85f), i % 2 == 0 ? Leaf : Moss);
                if (i % 3 == 0) ForestModels.Flower(ground, at + V(.16f, .61f, -.08f), .11f, Cream);
                for (int j = 0; j < 3; j++) Shape(ground, "Forest floor fern", OakLeaf(), at + V((j - 1) * .14f, .05f, -.5f), V(.3f, .4f, .7f), Deep, V(-35, i * 27 + j * 55, 0));
            }
        }
        void StonePath(Transform parent, Vector3 from, Vector3 to, int count)
        {
            for (int i = 0; i <= count; i++)
            {
                Vector3 at = Vector3.Lerp(from, to, (float)i / count);
                at.x += Mathf.Sin(i * 2.1f) * .06f;
                Ball(parent, "Courtyard stepping stone", at, V(.68f, .10f, .46f), i % 3 == 0 ? Hex(0xCBC6AA) : Hex(0xB6B89C), V(0, i * 23, 0));
            }
        }
        void MoveToArea(Transform child, Transform area, float lift = 0)
        {
            child.SetParent(area, false);
            child.localPosition += V(0, lift, 0);
        }
        void ArrangeForestCourtyard()
        {
            // Keep facility-local coordinates and animation paths together when
            // moving each business into its own building. Save indices stay put.
            workshopArea = Group(worldRoot, "Independent magic workshop", WorkshopOrigin);
            shopArea = Group(worldRoot, "Treehouse village shop", ShopOrigin);
            workshopArea.localScale = shopArea.localScale = Vector3.one * 1.08f;
            gardenArea = Group(worldRoot, "Twelve-bed woodland garden", GardenOrigin);
            MoveToArea(lowerRoom, workshopArea);
            foreach (var t in machines) MoveToArea(t, workshopArea);
            foreach (var t in new[] { curtainPlain, curtainLeaves, matPlain, matBread }) MoveToArea(t, workshopArea);
            MoveToArea(upperRoom, shopArea); MoveToArea(roof, shopArea);
            foreach (var t in shelves) MoveToArea(t, shopArea);
            foreach (var t in emptyShelves) MoveToArea(t, shopArea);
            foreach (var t in visitors) MoveToArea(t, shopArea);
            foreach (var t in passageCurtains) MoveToArea(t, shopArea);
            foreach (var t in new[] { sideClosed, sideOpen, signOld, signNew, potPlain, potFlowers }) MoveToArea(t, shopArea, 3.45f);
            MoveToArea(worldRoot.Find("Touch decoration0"), shopArea, 3.45f);
            for (int i = 0; i < Catalog.MaxPlots; i++) { MoveToArea(plots[i], gardenArea); MoveToArea(emptyPlots[i], gardenArea); }
            var livingTree = Group(worldRoot, "Shop's sheltering oak", V(ShopOrigin.x, 0, ShopOrigin.z + .8f));
            livingTree.localScale = Vector3.one * .72f;
            MoveToArea(oak, livingTree); MoveToArea(canopy, livingTree);
            workshopRoof = Group(workshopArea, "Workshop honey tiled roof");
            for (int row = 0; row < 5; row++) for (int col = 0; col < 18; col++)
                Box(workshopRoof, "Warm clay roof tile", V(-4.45f + col * .52f + row % 2 * .13f, 5.15f - row * .17f, 1.72f - row * .66f), V(.57f, .11f, .85f), (row + col) % 4 == 0 ? Hex(0xAB7651) : Hex(0x99734E), V(-14, 0, col % 3 - 1));
            Tube(workshopRoof, "Workshop roof ridge", V(-4.7f, 5.27f, 2), V(4.7f, 5.27f, 2), .10f, Copper); Combine(workshopRoof);
            shopRoofCap = RoofCap(shopArea, "Shop cutaway rear roof", 8.6f, false);
            workshopRoofCap = RoofCap(workshopArea, "Workshop cutaway rear roof", 5.15f, true);
            var supplies = Group(workshopArea, "Workshop outdoor supplies");
            ForestModels.Pot(supplies, V(-4.8f, .16f, -1.8f), 1.25f, false);
            for (int i = 0; i < 4; i++) Box(supplies, "Stacked supply crate", V(4.8f, .4f + i / 2 * .58f, -1.5f + i % 2 * .7f), V(.75f, .56f, .63f), i % 2 == 0 ? Honey : Wood);
            Combine(supplies);
            GardenBorders();
            Physics.SyncTransforms();
        }
        Transform RoofCap(Transform parent, string name, float height, bool clay)
        {
            var cap = Group(parent, name);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 18; col++)
                Box(cap, "Rear eave tile", V(-4.45f + col * .52f + row % 2 * .13f, height - row * .17f, 1.72f - row * .66f), V(.57f, .11f, .85f), clay ? (col % 4 == 0 ? Hex(0xAB7651) : Hex(0x99734E)) : (col % 4 == 0 ? Hex(0x53745A) : Deep), V(-14, 0, col % 3 - 1));
            Box(cap, "Timber eave apron joins wall to rear roof", V(0, height - .57f, 1.97f), V(8.6f, 1.12f, .19f), Wood);
            Tube(cap, "Rear roof ridge", V(-4.7f, height + .12f, 2), V(4.7f, height + .12f, 2), .10f, Copper); Combine(cap); return cap;
        }
        void GardenBorders()
        {
            var border = Group(gardenArea, "Garden hedges and low oak fence");
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 9; i++)
                {
                    float z = -5.4f + i * 1.35f;
                    // Openings line up with the two cross paths.
                    if (side < 0 && (i == 2 || i == 6)) continue;
                    Box(border, "Low garden post", V(side * 4.75f, .45f, z), V(.11f, .65f, .11f), Wood);
                    if (i < 8 && !(side < 0 && (i == 1 || i == 5)))
                        for (int rail = 0; rail < 2; rail++) Box(border, "Garden rail", V(side * 4.75f, .32f + rail * .24f, z + .675f), V(.07f, .08f, 1.35f), Honey);
                    Ball(border, "Herb border", V(side * 5.15f, .28f, z), V(.55f, .4f, .65f), i % 2 == 0 ? Leaf : Moss);
                    if (i % 3 == 0) ForestModels.Flower(border, V(side * 5.16f, .54f, z), .09f, Cream);
                }
            var tools = Group(border, "Garden potting bench", V(5.2f, .13f, 2.6f));
            Box(tools, "Potting surface", V(0, .85f, 0), V(.8f, .12f, 1.8f), Honey);
            for (int side = -1; side <= 1; side += 2) Box(tools, "Bench legs", V(0, .42f, side * .65f), V(.55f, .84f, .12f), DarkWood);
            ForestModels.Pot(tools, V(0, .94f, .45f), .55f, false);
            Ball(tools, "Watering can", V(0, 1.1f, -.4f), V(.37f, .4f, .4f), Blue);
            Tube(tools, "Watering spout", V(0, 1.1f, -.55f), V(0, 1.28f, -.94f), .045f, Copper);
            Ring(tools, "Watering handle", V(0, 1.28f, -.24f), .19f, .025f, Copper, V(0, 90, 0));
            Combine(border);
        }
        void LeafRelief(Transform parent, Vector3 at, Vector3 scale, Color tint)
        {
            var leaves = Sculpture(parent, "Shallow crown leaf relief", "OakLeafRelief", at, scale, tint).GetComponent<MeshRenderer>();
            // Millimetre-scale painted relief uses its curved surface lighting;
            // shadow-map self sampling creates dotted seams at this scale.
            leaves.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            leaves.receiveShadows = false;
        }
        void Window(Transform p, Vector3 at)
        {
            var w = Group(p, "Round woodland window", at);
            Ball(w, "Window blue glass", Vector3.zero, V(1.73f, 1.73f, .1f), Blue);
            Ring(w, "Oak window frame", V(0, 0, -.08f), .88f, .11f, Honey, V(90, 0, 0));
            Tube(w, "Window mullion", V(-.8f, 0, -.14f), V(.8f, 0, -.14f), .035f, Cream);
            Tube(w, "Window mullion", V(0, -.8f, -.14f), V(0, .8f, -.14f), .035f, Cream);
            Box(w, "Window sill", V(0, -.9f, -.05f), V(2, .14f, .47f), Honey);
        }
        Transform Annex(bool restored)
        {
            var a = Group(worldRoot, restored ? "Restored side room" : "Boarded side room", V(5, 1.3f, .9f));
            Box(a, "Annex wall", V(0, 1.08f, 0), V(1.45f, 2.2f, 1.75f), restored ? Cream : DarkWood);
            for (int i = 0; i < 4; i++) Box(a, "Annex shingle", V(0, 2.32f + i * .12f, -.6f + i * .4f), V(1.8f, .14f, .62f), restored ? Deep : Wood, V(-16, 0, 0));
            if (restored) { Window(a, V(0, 1.1f, -.91f)); ForestModels.Pot(a, V(0, .15f, -1.1f), .75f, true); }
            else for (int i = 0; i < 3; i++) Box(a, "Repair board", V(0, .7f + i * .45f, -.94f), V(1.37f, .17f, .1f), Honey, V(0, 0, (i - 1) * 16));
            Hotspot(a, "expansion", 0, V(0, 1.1f, 0), V(1.6f, 2.4f, 1.9f)); Combine(a); return a;
        }
        Transform Curtains(bool leaves, Transform parent = null)
        {
            var p = Group(parent ?? worldRoot, leaves ? "Leaf patterned curtains" : "Linen curtains", V(-2.5f, 2.8f, 1.6f));
            Tube(p, "Curtain rail", V(-1.2f, 1.05f, 0), V(1.2f, 1.05f, 0), .045f, Honey);
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 5; i++)
            {
                float x = side * (.8f + i * .065f);
                Ball(p, "Gathered fabric fold", V(x, .05f, -.03f), V(.15f, 1.8f, .1f), leaves ? Moss : Cream, V(0, 0, -side * (i - 2) * 2));
                if (leaves && i % 2 == 0) for (int j = 0; j < 4; j++) Ball(p, "Woven leaf pattern", V(x, -.55f + j * .36f, -.09f), V(.065f, .13f, .025f), Deep, V(0, 0, side * 25));
            }
            Combine(p); return p;
        }
        Transform Rug(bool bread, Transform parent = null)
        {
            var p = Group(parent ?? worldRoot, bread ? "Bread rug" : "Jute mat", V(0, 1.2f, -.95f));
            Ball(p, "Woven rug", Vector3.zero, V(2.6f, .04f, 1.2f), bread ? Hex(0xBC8A52) : Hex(0xB2A17B));
            for (int i = 0; i < 11; i++) Tube(p, "Rug weave", V(-1.1f, .025f, -.45f + i * .09f), V(1.1f, .025f, -.45f + i * .09f), .009f, Cream);
            if (bread)
            {
                Ball(p, "Golden loaf woven motif", V(0, .045f, 0), V(1.3f, .025f, .65f), Honey);
                for (int i = 0; i < 3; i++) Box(p, "Cream loaf scoring", V(-.3f + i * .3f, .065f, 0), V(.065f, .013f, .36f), Cream, V(0, 25, 0));
            }
            Combine(p); return p;
        }
        Transform Sign(bool fresh, Transform parent = null)
        {
            var p = Group(parent ?? worldRoot, fresh ? "Tea party oak sign" : "Old wooden sign", V(0, 3.97f, -2.4f));
            Box(p, "Carved wooden sign", Vector3.zero, V(2.4f, .49f, .15f), fresh ? Honey : Wood);
            for (int side = -1; side <= 1; side += 2)
            { Tube(p, "Sign chain", V(side * .8f, .1f, .03f), V(side * .8f, .5f, .03f), .022f, Gold); Ball(p, "Leaf carving", V(side * .94f, 0, -.09f), V(.2f, .3f, .035f), fresh ? Deep : Cream, V(0, 0, side * 25)); }
            if (fresh)
            {
                Box(p, "Forest green enamel inset", V(0, 0, -.10f), V(1.83f, .40f, .05f), Deep);
                Ball(p, "Tea cup carved body", V(-.10f, -.015f, -.15f), V(.43f, .24f, .055f), Cream);
                Tube(p, "Tea cup lip", V(-.30f, .095f, -.17f), V(.10f, .095f, -.17f), .022f, Gold);
                Ring(p, "Tea cup handle", V(.14f, .015f, -.15f), .09f, .023f, Cream, V(90, 0, 0));
                Tube(p, "Tea steam", V(-.14f, .13f, -.15f), V(-.10f, .21f, -.15f), .018f, Gold);
                for (int side = -1; side <= 1; side += 2)
                    for (int j = 0; j < 3; j++) Ball(p, "Celebration leaf wreath", V(side * (.65f + j * .10f), -.17f + j * .14f, -.16f), V(.15f, .23f, .04f), j == 1 ? Gold : Moss, V(0, 0, side * 40));
            }
            else Ring(p, "Little shop emblem", V(0, .015f, -.12f), .15f, .026f, Cream, V(90, 0, 0));
            Combine(p); return p;
        }
        Transform Planter(bool flowers, Transform parent = null)
        {
            var p = ForestModels.Pot(parent ?? worldRoot, V(4.6f, .13f, -2.2f), flowers ? 1.65f : 1.4f, flowers);
            if (flowers)
            {
                Ring(p, "Cream planter band", V(0, .36f, 0), .323f, .035f, Cream);
                for (int i = 0; i < 5; i++)
                {
                    float x = -.34f + i * .17f, y = .77f + Mathf.Sin(i * 1.7f) * .15f;
                    Tube(p, "Spreading flower stem", V(0, .46f, 0), V(x, y, -.12f), .018f, Deep);
                    ForestModels.Flower(p, V(x, y, -.15f), .13f, i % 2 == 0 ? Gold : Cream);
                    Ball(p, "Trailing planter leaf", V(x, .62f, -.13f), V(.23f, .11f, .13f), Leaf, V(0, 0, i * 25));
                }
                Combine(p);
            }
            return p;
        }
        void BuildObjects()
        {
            for (int i = 0; i < Catalog.MaxPlots; i++)
            {
                Vector3 at = V(-3.15f + i % 3 * 3.15f, .13f, 4.05f - i / 3 * 2.7f);
                plots[i] = Group(worldRoot, "Garden bed " + i, at);
                var bed = Group(plots[i], "Raised oak frame");
                bed.localScale = V(1.28f, 1, 1.25f);
                Box(bed, "Rich soil", V(0, .14f, 0), V(1.7f, .25f, 1.42f), Hex(0x725A40));
                for (int side = -1; side <= 1; side += 2)
                { Box(bed, "Bed side", V(side * .88f, .24f, 0), V(.12f, .35f, 1.55f), Honey); Box(bed, "Bed edge", V(0, .24f, side * .73f), V(1.8f, .35f, .12f), Wood); }
                Combine(bed);
                for (int crop = 0; crop < 4; crop++) { var g = ForestModels.Crop(plots[i], crop); g.localPosition = V(0, .29f, 0); g.localScale = Vector3.one * 1.6f; cropModels[i * 4 + crop] = g; }
                Hotspot(plots[i], "plot", i, V(0, .7f, 0), V(2.4f, 1.65f, 2));
                emptyPlots[i] = Group(worldRoot, "Unbuilt garden bed " + i, at);
                for (int j = 0; j < 3; j++) Box(emptyPlots[i], "Stacked timber", V((j - 1) * .2f, .14f + j * .1f, 0), V(1.3f, .12f, .25f), Honey, V(0, -j * 13, 0));
                for (int corner = 0; corner < 4; corner++) Box(emptyPlots[i], "Future bed corner stake", V((corner % 2 == 0 ? -1 : 1) * 1.12f, .3f, (corner < 2 ? -1 : 1) * .91f), V(.07f, .34f, .07f), Wood);
                Hotspot(emptyPlots[i], "expansion", 0, V(0, .4f, 0), V(2.4f, 1, 2)); Combine(emptyPlots[i]);
            }
            machines[0] = ForestModels.Cauldron(worldRoot); machines[0].localPosition = V(-1.65f, 1.19f, .2f); machines[0].localScale = Vector3.one * 1.05f;
            spoon = machines[0].Find("Stirring spoon"); Hotspot(machines[0], "machine", 0, V(0, .9f, 0), V(2.4f, 2.2f, 2.2f));
            machines[1] = ForestModels.Bench(worldRoot); machines[1].localPosition = V(1.35f, 1.19f, .3f); Hotspot(machines[1], "machine", 1, V(0, .8f, 0), V(2.3f, 1.8f, 1.3f));
            for (int i = 0; i < 4; i++)
            {
                var at = V(-2.7f + i * 1.8f, 4.64f, .6f);
                shelves[i] = ForestModels.Shelf(worldRoot); shelves[i].localPosition = at;
                Hotspot(shelves[i], "shelf", i, V(0, .8f, 0), V(1.4f, 1.8f, 1));
                for (int j = 0; j < 6; j++)
                { var p = Group(shelves[i], "Stock " + j); for (int copy = 0; copy < 3; copy++) { var item = ForestModels.Product(p, j); item.localPosition = V((copy % 2 - .5f) * .45f, copy < 2 ? .23f : .8f, -.06f); item.localScale = Vector3.one * .56f; } goods[i * 6 + j] = p; }
                emptyShelves[i] = Group(worldRoot, "Sealed display " + i, at);
                ForestModels.Shelf(emptyShelves[i]);
                for (int fold = 0; fold < 9; fold++) Ball(emptyShelves[i], "Linen dust cover fold", V(-.5f + fold * .125f, .94f, -.17f), V(.17f, 1.28f, .12f), fold % 2 == 0 ? Hex(0xC8BA96) : Hex(0xBCAD87));
                Box(emptyShelves[i], "Draped cover top", V(0, 1.67f, .13f), V(1.25f, .07f, .65f), Hex(0xC8BA96));
                Tube(emptyShelves[i], "Sealed display cord", V(-.56f, 1.52f, -.27f), V(.55f, .39f, -.27f), .024f, Wood);
                Tube(emptyShelves[i], "Sealed display cord", V(.56f, 1.52f, -.27f), V(-.55f, .39f, -.27f), .024f, Wood);
                Ring(emptyShelves[i], "Brass lock shackle", V(0, 1.05f, -.31f), .1f, .023f, Gold, V(90, 0, 0));
                Box(emptyShelves[i], "Brass lock", V(0, .94f, -.33f), V(.24f, .21f, .06f), Gold);
                Ball(emptyShelves[i], "Lock keyhole", V(0, .94f, -.37f), V(.035f, .07f, .01f), DarkWood);
                Hotspot(emptyShelves[i], "expansion", 0, V(0, .8f, 0), V(1.4f, 1.8f, 1)); Combine(emptyShelves[i]);
            }
            for (int i = 0; i < 5; i++)
            {
                visitors[i] = ForestModels.Resident(worldRoot, i); visitors[i].localPosition = V(-2.8f + i * 1.3f, 4.64f, -1.15f); visitors[i].localScale = Vector3.one * .62f; residentMotion[i] = visitors[i].GetComponent<ForestResidentMotion>();
                var bag = shoppingBags[i] = Group(residentMotion[i].rightArm, "Purchased goods bag", V(0, -.53f, -.19f));
                bag.localScale = Vector3.one * 1.35f;
                Box(bag, "Paper bag", V(0, -.1f, 0), V(.31f, .38f, .23f), Cream);
                Ring(bag, "Paper handle", V(0, .12f, 0), .11f, .018f, Wood, V(90, 0, 0));
                Shape(bag, "Shop leaf seal", OakLeaf(), V(0, -.08f, -.124f), V(.22f, .3f, .2f), Leaf, V(90, 0, 0)); Combine(bag);
            }
            var checkout = Group(upperRoom, "Little checkout desk", V(-3.15f, 4.64f, -.52f));
            Box(checkout, "Rounded counter body", V(0, .39f, 0), V(1.05f, .78f, .78f), Wood);
            Box(checkout, "Countertop", V(0, .82f, 0), V(1.18f, .13f, .91f), Honey);
            Box(checkout, "Sales ledger", V(-.2f, .93f, 0), V(.38f, .08f, .36f), Deep);
            Box(checkout, "Ledger pages", V(-.2f, .94f, -.04f), V(.3f, .04f, .27f), Cream);
            Shape(checkout, "Counter bell", Lathe(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.15f, .05f), new Vector2(.1f, .14f), new Vector2(0, .17f) }), V(.26f, .89f, -.08f), Vector3.one, Gold);
            Combine(checkout);
        }
        void Hotspot(Transform parent, string kind, int index, Vector3 center, Vector3 size)
        {
            var h = Group(parent, "Touch " + kind + index); var target = h.gameObject.AddComponent<ForestObject3D>(); target.kind = kind; target.index = index; var box = h.gameObject.AddComponent<BoxCollider>(); box.center = center; box.size = size;
        }
        public void Pick(Vector2 uv)
        {
            Ray ray = worldCamera.ViewportPointToRay(uv);
            if (Physics.Raycast(ray, out var hit, 100, 1 << 10)) { var target = hit.collider.GetComponent<ForestObject3D>(); if (target != null) Selected?.Invoke(target.kind, target.index); }
        }
        public void Bind(GameState current)
        {
            if (state != current) { state = current; lastSale = state.sales; saleAt = -100; }
            bool workshop = Region == 0 || Region == 2;
            bool shop = Region == 0 || Region == 3;
            sideClosed.gameObject.SetActive(!state.sideRoom && shop); sideOpen.gameObject.SetActive(state.sideRoom && shop);
            curtainPlain.gameObject.SetActive(workshop && !state.decoration[1]); curtainLeaves.gameObject.SetActive(workshop && state.decoration[1]); matPlain.gameObject.SetActive(workshop && !state.decoration[2]); matBread.gameObject.SetActive(workshop && state.decoration[2]);
            signOld.gameObject.SetActive(shop && !state.decoration[0]); signNew.gameObject.SetActive(shop && state.decoration[0]); potPlain.gameObject.SetActive(shop && !state.decoration[3]); potFlowers.gameObject.SetActive(shop && state.decoration[3]);
            UpdateResidents();
            for (int i = 0; i < Catalog.MaxPlots; i++)
            {
                bool built = i < state.plots.Length; bool shown = Region == 0 || Region == 1; plots[i].gameObject.SetActive(built && shown); emptyPlots[i].gameObject.SetActive(!built && shown);
                if (!built) continue; var f = state.plots[i]; int crop = Mathf.Max(0, f.workItem >= 0 ? f.workItem : f.config);
                for (int j = 0; j < 4; j++) cropModels[i * 4 + j].gameObject.SetActive(crop == j);
                float growth = f.remaining > 0 ? Mathf.Clamp01(1 - (float)(f.remaining / (Catalog.CropSeconds[crop] / Catalog.Speed[f.level - 1]))) : 1;
                cropModels[i * 4 + crop].localScale = Vector3.one * Mathf.Lerp(.52f, 1.6f, growth);
            }
            machines[0].gameObject.SetActive(workshop); machines[1].gameObject.SetActive(state.machines.Length > 1 && workshop);
            for (int i = 0; i < 4; i++)
            {
                bool built = i < state.shelves.Length; bool shown = Region == 0 || Region == 3; shelves[i].gameObject.SetActive(built && shown); emptyShelves[i].gameObject.SetActive(!built && shown);
                for (int j = 0; j < 6; j++) goods[i * 6 + j].gameObject.SetActive(built && state.shelves[i].config == j && state.items[j] > 0);
            }
            if (state.sales > lastSale) { lastSale = state.sales; saleAt = Time.unscaledTime; }
        }
        void Update()
        {
            if (state == null) return;
            if (spoon != null && state.machines.Length > 0 && state.machines[0].remaining > 0 && !state.simpleAnimation) spoon.localEulerAngles = V(-18 + Mathf.Sin(Time.unscaledTime * 1.5f) * 7, Time.unscaledTime * 26, -28);
            UpdateResidents();
        }
        void UpdateResidents()
        {
            if (state == null) return;
            float doorOpening = 0;
            for (int i = 0; i < 5; i++)
            {
                // A single visitor uses the narrow shop aisle at a time. Every
                // known resident takes a turn, avoiding intersecting waiting poses.
                int knownCount = Mathf.Min(5, state.stories + 1);
                float cycle = knownCount * 34, phase = (Time.unscaledTime - visitEpoch + cycle - i * 34) % cycle;
                bool simple = state.simpleAnimation, known = i < Mathf.Min(5, state.stories + 1);
                visitors[i].gameObject.SetActive(known && (Region == 0 || Region == 3) && (simple || phase < 33.7f));
                if (!known) continue;
                float shelfX = -2.7f + i % state.shelves.Length * 1.8f, lane = -1.5f;
                Vector3 entrance = V(-3.7f, 4.64f, 4.85f), entryLane = V(-3.7f, 4.64f, lane), browse = V(shelfX, 4.64f, -.15f), counter = V(-2.5f, 4.64f, lane);
                if (!simple)
                {
                    float entering = Mathf.SmoothStep(0, 1, phase / .4f) * (1 - Mathf.SmoothStep(0, 1, (phase - 1.6f) / .9f));
                    float leaving = Mathf.SmoothStep(0, 1, (phase - 30.15f) / .5f) * (1 - Mathf.SmoothStep(0, 1, (phase - 32.1f) / .9f));
                    doorOpening = Mathf.Max(doorOpening, entering, leaving);
                    if ((Region == 0 || Region == 3) && phase >= 1.35f && phase < 1.7f && Time.unscaledTime - lastDoorAt > 2)
                    { lastDoorAt = Time.unscaledTime; DoorEntered?.Invoke(); }
                }
                Vector3 at; float yaw; bool walking;
                if (phase < 3) { at = Vector3.Lerp(entrance, entryLane, phase / 3); yaw = 0; walking = true; }
                else if (phase < 7) { at = Vector3.Lerp(entryLane, V(shelfX, 4.64f, lane), (phase - 3) / 4); yaw = -90; walking = true; }
                else if (phase < 10) { at = Vector3.Lerp(V(shelfX, 4.64f, lane), browse, (phase - 7) / 3); yaw = 180; walking = true; }
                else if (phase < 17) { at = browse; yaw = 180 + Mathf.Sin(phase * .6f) * 8; walking = false; }
                else if (phase < 21) { at = Vector3.Lerp(browse, V(shelfX, 4.64f, lane), (phase - 17) / 4); yaw = 0; walking = true; }
                else if (phase < 25) { at = Vector3.Lerp(V(shelfX, 4.64f, lane), counter, (phase - 21) / 4); yaw = 90; walking = true; }
                else if (phase < 29) { at = counter; yaw = Mathf.Lerp(155, 0, Mathf.Clamp01((phase - 26) / 1.2f)); walking = false; }
                else if (phase < 30) { at = Vector3.Lerp(counter, entryLane, phase - 29); yaw = 90; walking = true; }
                else { at = Vector3.Lerp(entryLane, entrance, Mathf.Clamp01((phase - 30) / 2)); yaw = 180; walking = true; }
                if (simple) { at = V(-2.8f + i * 1.3f, 4.64f, -1.15f); yaw = -8; walking = false; }
                float cadence = i == 0 ? 6.4f : i == 1 ? 7.1f : i == 2 ? 8.2f : i == 3 ? 9 : 5.8f;
                float stride = walking ? Mathf.Sin(Time.unscaledTime * cadence + i) : 0;
                float browseGesture = !simple && phase >= 11 && phase < 16 ? Mathf.Sin((phase - 11) * Mathf.PI / 5) : 0;
                float thankYou = !simple && phase >= 27 && phase < 28.4f ? Mathf.Sin((phase - 27) * Mathf.PI / 1.4f) : 0;
                visitors[i].localPosition = at + V(0, Mathf.Abs(stride) * (i == 4 ? .015f : .024f), 0);
                visitors[i].localRotation = simple ? Quaternion.Euler(0, yaw, 0)
                    : Quaternion.RotateTowards(visitors[i].localRotation, Quaternion.Euler(thankYou * 5, yaw, browseGesture * (i == 0 ? -3 : 2)), 240 * Time.unscaledDeltaTime);
                bool visitHasSale = saleAt >= Time.unscaledTime - phase;
                float reach = 0;
                if (!simple && visitHasSale && phase >= 25)
                    reach = phase < 26 ? Mathf.SmoothStep(0, 1, phase - 25) : phase < 27 ? Mathf.SmoothStep(1, .35f, phase - 26) : .35f;
                residentMotion[i].Pose(stride, reach, browseGesture, !simple);
                shoppingBags[i].gameObject.SetActive(!simple && phase >= 26 && phase < 33.7f && visitHasSale);
            }
            for (int side = 0; side < 2; side++) if (passageCurtains[side] != null)
            {
                float direction = side == 0 ? -1 : 1;
                passageCurtains[side].gameObject.SetActive(Region == 0 || Region == 3);
                passageCurtains[side].localPosition = V(-3.7f + direction * doorOpening * .52f, 6.63f, 2.02f);
                passageCurtains[side].localEulerAngles = V(0, 0, direction * doorOpening * 7);
            }
        }
        public Sprite Icon(string name, int cell)
        {
            string key = name + cell; if (icons.TryGetValue(key, out var sprite)) return sprite;
            Transform model = Group(portraitRoot, "Portrait model");
            portraitCamera.orthographicSize = 1.36f;
            if (name == "celebration") ForestModels.TeaParty(model);
            else if (name == "residents") ForestModels.Resident(model, cell % 5);
            else if (name == "decorations")
            {
                int slot = cell / 2; bool unlocked = cell % 2 == 1;
                Transform p = slot == 0 ? Sign(unlocked, model) : slot == 1 ? Curtains(unlocked, model) : slot == 2 ? Rug(unlocked, model) : Planter(unlocked, model);
                p.localPosition = Vector3.zero;
                if (slot == 2) p.localEulerAngles = V(-48, 0, 0);
            }
            else if (name == "inventory")
            {
                Transform p;
                if (cell < 4) p = ForestModels.Crop(model, cell);
                else if (cell < 10) p = ForestModels.Product(model, cell - 4);
                else if (cell == 10) p = ForestModels.Cauldron(model);
                else if (cell == 11) p = ForestModels.Bench(model);
                else if (cell == 12) p = ForestModels.Shelf(model);
                else if (cell == 13) { p = Group(model, "Gold coins"); for (int i = 0; i < 5; i++) Shape(p, "Coin", Lathe(new[] { new Vector2(0, 0), new Vector2(.33f, 0), new Vector2(.33f, .09f), new Vector2(0, .09f) }), V((i % 2) * .24f, i * .09f, 0), Vector3.one, Gold); }
                else if (cell == 14) { p = Group(model, "Leaf shop sign"); Box(p, "Sign", V(0, .4f, 0), V(1.3f, .6f, .12f), Honey); Ball(p, "Leaf emblem", V(0, .4f, -.09f), V(.21f, .39f, .05f), Leaf, V(0, 0, -25)); }
                else p = ForestModels.Pot(model, Vector3.zero, 1, true);
                float scale = cell < 10 || cell >= 13 ? 2.2f : 1.05f; p.localScale *= scale; p.localPosition += V(0, cell < 10 ? .2f : 0, 0);
            }
            else if (name == "treehouse" || name == "treehouse-restored")
            {
                model.gameObject.SetActive(false);
                int oldRegion = Region; SetRegion(0);
                var rtScene = new RenderTexture(540, 660, 24); worldCamera.targetTexture = rtScene; worldCamera.Render(); sprite = ReadSprite(rtScene); SetRegion(oldRegion); rtScene.Release(); Destroy(rtScene); Destroy(model.gameObject); icons[key] = sprite; return sprite;
            }
            else { Box(model, "Woven linen", V(0, .9f, 0), V(1.5f, 1.7f, .08f), cell % 2 == 0 ? Cream : Moss); }
            // Frame the actual sculpture, including long ears and all carried accessories.
            Bounds bounds = new Bounds(model.position, Vector3.zero); bool first = true;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                if (renderer.enabled) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            portraitCamera.transform.position = bounds.center + V(2.7f, 1.15f, -7);
            portraitCamera.transform.LookAt(bounds.center);
            float extent = 0, portraitAspect = name == "celebration" ? 1.7f : .8f;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = Quaternion.Inverse(portraitCamera.transform.rotation) * Vector3.Scale(bounds.extents, V(x, y, z));
                extent = Mathf.Max(extent, Mathf.Abs(corner.y), Mathf.Abs(corner.x) / portraitAspect);
            }
            portraitCamera.orthographicSize = Mathf.Max(.2f, extent * 1.12f);
            var rt = new RenderTexture(name == "celebration" ? 850 : 320, name == "celebration" ? 500 : 400, 24) { antiAliasing = 2 }; portraitCamera.targetTexture = rt; portraitCamera.Render(); sprite = ReadSprite(rt);
            portraitCamera.targetTexture = null; rt.Release(); Destroy(rt); model.gameObject.SetActive(false); Destroy(model.gameObject); icons[key] = sprite; return sprite;
        }
        Sprite ReadSprite(RenderTexture rt)
        {
            var previous = RenderTexture.active; RenderTexture.active = rt; var t = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            t.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); t.Apply(); RenderTexture.active = previous;
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f), 100);
        }
        void OnDestroy()
        {
            if (fullView != null) { fullView.Release(); Destroy(fullView); }
            if (closeView != null) { closeView.Release(); Destroy(closeView); }
            if (portraitRoot != null) Destroy(portraitRoot.gameObject);
            foreach (var icon in icons.Values) { if (icon != null) { Destroy(icon.texture); Destroy(icon); } }
            ForestGeometry.Release();
        }
    }
}
