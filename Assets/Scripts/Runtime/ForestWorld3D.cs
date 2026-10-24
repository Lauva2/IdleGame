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

    // Marker geometry follows the same camera as the world every frame,
    // including with reduced animation enabled. Off-screen labels stay hidden.
    public sealed class ForestWorldMarker : MonoBehaviour
    {
        public ForestWorld3D world; public int index; public bool shelf;
        public void Refresh()
        {
            var rect = (RectTransform)transform; var parent = (RectTransform)rect.parent;
            Vector2 at = shelf ? world.ShelfMarker(index) : world.PlotMarker(index);
            bool visible = at.x > .03f && at.x < .97f && at.y > .035f && at.y < .97f;
            // Keep the full badge above the fixed camera controls and gesture legend.
            if (at.y < .125f && at.x > .65f) visible = false;
            foreach (var g in GetComponentsInChildren<Graphic>()) g.enabled = visible;
            rect.anchoredPosition = new Vector2(at.x * parent.rect.width - rect.rect.width / 2, -(1 - at.y) * parent.rect.height + rect.rect.height / 2);
        }
        void LateUpdate() { Refresh(); }
    }

    public sealed class ForestWorldPointer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public ForestWorld3D world;
        Vector2 start; bool dragged, pinched; float pinchDistance;
        readonly HashSet<int> fingers = new HashSet<int>();
        Vector2 UV(Vector2 screen, Camera camera)
        {
            var rect = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, screen, camera, out var p);
            return new Vector2((p.x - rect.rect.x) / rect.rect.width, (p.y - rect.rect.y) / rect.rect.height);
        }
        public void OnPointerDown(PointerEventData e) { start = e.position; if (fingers.Count == 0) { dragged = pinched = false; } fingers.Add(e.pointerId); }
        public void OnPointerUp(PointerEventData e)
        {
            fingers.Remove(e.pointerId);
            if (dragged || pinched || Vector2.Distance(start, e.position) > 18) return;
            world.Pick(UV(e.position, e.pressEventCamera));
        }
        public void OnBeginDrag(PointerEventData e) { dragged = true; }
        public void OnDrag(PointerEventData e)
        { if (!pinched && Input.touchCount < 2) world.Pan(UV(e.position - e.delta, e.pressEventCamera), UV(e.position, e.pressEventCamera)); }
        public void OnEndDrag(PointerEventData e) { }
        public void OnScroll(PointerEventData e) { world.Zoom(Mathf.Pow(.86f, e.scrollDelta.y)); }
        void Update()
        {
            if (Input.touchCount < 2) { pinchDistance = 0; return; }
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            if (!fingers.Contains(a.fingerId) || !fingers.Contains(b.fingerId)) return;
            pinched = true; float distance = Vector2.Distance(a.position, b.position);
            if (pinchDistance > 8 && distance > 8) world.Zoom(pinchDistance / distance);
            pinchDistance = distance;
        }
        void OnDisable() { fingers.Clear(); pinchDistance = 0; }
    }

    public sealed partial class ForestWorld3D : MonoBehaviour
    {
        public RenderTexture View { get; private set; }
        public int Region { get; private set; }
        public Action<string, int> Selected;
        public Action DoorEntered;
        Camera worldCamera, portraitCamera;
        Transform worldRoot, portraitRoot, sideClosed, sideOpen, lowerRoom, upperRoom, canopy, roof, oak, landscape, woodland;
        Transform shopArea, workshopArea, gardenArea, workshopRoof, shopRoofCap, workshopRoofCap, workshopFrontBeam;
        static readonly Vector3 ShopOrigin = V(0, -3.726f, -10);
        static readonly Vector3 WorkshopOrigin = V(-10, 0, 9);
        static readonly Vector3 GardenOrigin = V(9, 0, 8);
        const float ShopDoorX = -5, ShopLaneZ = -2.6f;
        static float ShelfX(int index) => -2.8f + index * 2.35f;
        RenderTexture fullView, closeView;
        readonly Transform[] plots = new Transform[Catalog.MaxPlots], cropModels = new Transform[Catalog.MaxPlots * 4], shelves = new Transform[4], goods = new Transform[4 * Catalog.Items.Length], machines = new Transform[2], visitors = new Transform[5];
        readonly Transform[] emptyPlots = new Transform[Catalog.MaxPlots], emptyShelves = new Transform[4];
        readonly Transform[] nextPlotDetails = new Transform[Catalog.MaxPlots];
        readonly ForestResidentMotion[] residentMotion = new ForestResidentMotion[5];
        readonly Transform[] displayProducts = new Transform[4 * Catalog.Items.Length];
        readonly Transform[] passageCurtains = new Transform[2];
        Transform spoon, checkout, curtainPlain, curtainLeaves, matPlain, matBread, signOld, signNew, potPlain, potFlowers;
        readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
        GameState state;
        ForestProductionEffects productionEffects;
        ForestShoppingMotion shopping;
        float stirringPhase;
        float visitEpoch;
        float lastDoorAt = -10;
        Vector3 cameraFocus;
        public float CameraSize => worldCamera.orthographicSize;
        public Vector3 CameraFocus => cameraFocus - transform.position;
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
            BuildArchitecture(); BuildObjects(); ArrangeForestCourtyard(); BuildExplorationWorld(); BuildFishingWorld();
            productionEffects = new ForestProductionEffects(worldRoot, plots, machines, shelves);
            shopping = new ForestShoppingMotion(shopArea, checkout, residentMotion, displayProducts);
            portraitRoot = Group(null, "3D portrait studio", V(2000, 0, 0)); portraitRoot.gameObject.layer = 11;
            portraitCamera = CameraFor("Portrait camera", 11, portraitRoot); portraitCamera.enabled = false; portraitCamera.backgroundColor = Color.clear;
            portraitCamera.transform.localPosition = V(2.7f, 2.2f, -7); portraitCamera.transform.LookAt(portraitRoot.position + V(0, 1.05f, 0)); portraitCamera.orthographicSize = 1.36f;
            var light = Group(portraitRoot, "Portrait softbox").gameObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = .88f; light.color = Hex(0xFFF1DB); light.transform.localEulerAngles = V(35, -25, 0); light.cullingMask = 1 << 11;
            SetRegion(0);
        }
        Camera CameraFor(string name, int layer, Transform parent)
        {
            var c = Group(parent, name).gameObject.AddComponent<Camera>(); c.orthographic = true; c.nearClipPlane = .1f; c.farClipPlane = 180; c.cullingMask = 1 << layer; c.clearFlags = CameraClearFlags.SolidColor; c.allowHDR = false; c.allowMSAA = true; return c;
        }
        public void SetVisible(bool visible) { if (worldCamera != null) worldCamera.enabled = visible; if (!visible) { productionEffects?.Clear(); ClearForestEffects(); ClearFishingReceipt(); } }
        public int ActiveProductionEffects => (productionEffects == null ? 0 : productionEffects.ActiveCount) + ForestEffectCount;
        public void PresentProduction(ProductionNotice notice)
        {
            shopping?.RecordSale(notice);
            if (state == null) return;
            if (ActiveProductionEffects >= 5) return;
            if (notice.Kind == ProductionKind.Forage || notice.Kind == ProductionKind.Nursery) PresentForestProduction(notice);
            else productionEffects?.Trigger(notice, worldCamera, state.simpleAnimation);
        }
        public void RestartVisitSequence() { visitEpoch = Time.unscaledTime; shopping?.Clear(); }
        public void RenderNow() { if (worldCamera != null) worldCamera.Render(); }
        public void SetViewportAspect(float aspect) { worldCamera.aspect = aspect; }
        public Vector2 PlotMarker(int index)
        { Vector3 v = worldCamera.WorldToViewportPoint(plots[index].TransformPoint(V(0, .25f, -.96f))); return new Vector2(v.x, v.y); }
        public Vector2 ShelfMarker(int index)
        { Vector3 v = worldCamera.WorldToViewportPoint(shelves[index].TransformPoint(V(0, 1.85f, 0))); return new Vector2(v.x, v.y); }
        public void SetRegion(int region)
        {
            productionEffects?.Clear(); ClearForestEffects(); ClearFishingReceipt();
            Region = region;
            View = region == 0 ? fullView : closeView; worldCamera.targetTexture = View;
            worldCamera.ResetAspect();
            Vector3 focus = region == 1 ? gardenArea.TransformPoint(V(0, .4f, .05f))
                : region == 2 ? workshopArea.TransformPoint(V(0, 2.4f, -.2f))
                : region == 3 ? shopArea.TransformPoint(V(.55f, 5.7f, .1f)) : transform.position + V(-.5f, 1.8f, 0);
            cameraFocus = focus;
            worldCamera.orthographicSize = region == 0 ? 22.5f : region == 1 ? 7.1f : region == 2 ? 6.25f : 6.85f;
            worldCamera.transform.position = focus + (region == 0 ? V(2, 36, -38) : region == 1 ? V(.7f, 18, -15) : V(2, 25, -23));
            worldCamera.transform.LookAt(focus);
            workshopArea.gameObject.SetActive(true);
            shopArea.gameObject.SetActive(true);
            gardenArea.gameObject.SetActive(true);
            if (lowerRoom != null) lowerRoom.gameObject.SetActive(true);
            if (upperRoom != null) upperRoom.gameObject.SetActive(true);
            if (canopy != null) canopy.gameObject.SetActive(region == 0);
            if (oak != null) oak.gameObject.SetActive(region == 0);
            if (landscape != null) landscape.gameObject.SetActive(true);
            if (woodland != null) woodland.gameObject.SetActive(region < 4 || region == 7);
            if (roof != null) roof.gameObject.SetActive(region != 3);
            if (workshopRoof != null) workshopRoof.gameObject.SetActive(region != 2);
            if (shopRoofCap != null) shopRoofCap.gameObject.SetActive(region == 3);
            if (workshopRoofCap != null) workshopRoofCap.gameObject.SetActive(region == 2);
            if (workshopFrontBeam != null) workshopFrontBeam.gameObject.SetActive(region == 0);
            SetForestRegion(region);
            if (state != null) Bind(state);
            if (region == 2 || region == 3) FitArea(region == 2 ? workshopArea : shopArea);
        }
        public void ResetCamera() { SetRegion(Region); }
        public void Zoom(float factor)
        {
            worldCamera.orthographicSize = Mathf.Clamp(worldCamera.orthographicSize * factor, 5, 27);
            ClampCamera();
        }
        Vector3 GroundPoint(Vector2 uv)
        {
            var ray = worldCamera.ViewportPointToRay(uv);
            var plane = new Plane(Vector3.up, transform.position);
            return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : cameraFocus;
        }
        public void Pan(Vector2 previous, Vector2 current)
        {
            Vector3 move = GroundPoint(previous) - GroundPoint(current);
            worldCamera.transform.position += move; cameraFocus += move; ClampCamera();
        }
        void ClampCamera()
        {
            float range = Mathf.Max(2, 28 - CameraSize);
            var local = CameraFocus;
            if (Region >= 4) { ClampForestCamera(local); return; }
            Vector3 bounded = V(Mathf.Clamp(local.x, -range - 1, range + 1), local.y, Mathf.Clamp(local.z, -range - 2, range + 3));
            Vector3 move = bounded - local; cameraFocus += move; worldCamera.transform.position += move;
        }
        void FitArea(Transform area)
        {
            Vector3 right = worldCamera.transform.right, up = worldCamera.transform.up;
            Vector3 origin = worldCamera.transform.position;
            float left = float.PositiveInfinity, bottom = float.PositiveInfinity, rightEdge = float.NegativeInfinity, top = float.NegativeInfinity;
            foreach (var r in area.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled || r.GetComponentInParent<ForestResidentMotion>() != null) continue; Bounds b = r.bounds;
                for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 point = b.center + Vector3.Scale(b.extents, V(x, y, z)) - origin;
                    float px = Vector3.Dot(point, right), py = Vector3.Dot(point, up);
                    left = Mathf.Min(left, px); rightEdge = Mathf.Max(rightEdge, px); bottom = Mathf.Min(bottom, py); top = Mathf.Max(top, py);
                }
            }
            if (float.IsInfinity(left)) return;
            Vector3 shift = right * ((left + rightEdge) * .5f) + up * ((bottom + top) * .5f);
            worldCamera.transform.position += shift; cameraFocus += shift;
            float aspect = (float)View.width / View.height;
            worldCamera.orthographicSize = Mathf.Max((top - bottom) * .5f, (rightEdge - left) * .5f / aspect) * 1.08f;
        }
        void BuildArchitecture()
        {
            var s = landscape = Group(worldRoot, "Forest garden ground");
            Box(s, "Continuous woodland terrain", V(0, -.34f, 0), V(180, .8f, 180), Hex(0x7E9466));
            Shape(s, "Sunlit clearing blends into forest floor", Lathe(new[] { new Vector2(0, 0), new Vector2(20, 0), new Vector2(24, .025f), new Vector2(24.5f, .045f), new Vector2(22, .07f), new Vector2(0, .07f) }, 96), V(0, .03f, 1), V(1, 1, 1.12f), Hex(0x91A575));
            oak = Group(worldRoot, "Living oak trunk and boughs");
            Sculpture(oak, "Blender sheltering oak", "CanopyOak", Vector3.zero, Vector3.one * 1.25f, Color.white);
            canopy = Group(worldRoot, "Oak leaf canopy");
            woodland = Group(worldRoot, "Surrounding layered woodland");
            string[] trees = { "CanopyOak", "SilverBirch", "WoodlandLinden", "OpenRowan", "YoungBirch", "LowHazel" };
            // Closed forest edge, staggered groves beyond it, one opening for the road.
            for (int row = 0; row < 3; row++) for (int i = 0; i < 32 + row * 8; i++)
            {
                float a = (i + row * .43f) * Mathf.PI * 2 / (32 + row * 8) + Mathf.Sin(i * 3.7f) * .036f;
                float clump = Mathf.Sin(a * 5 + row * .7f) * 1.7f + Mathf.Cos(i * 1.6f) * .7f;
                var at = V(Mathf.Sin(a) * (24 + row * 7 + clump), .08f, 2 + Mathf.Cos(a) * (27 + row * 8 + clump));
                if (at.z < -19 && Mathf.Abs(at.x + 5.4f) < 3.8f) continue;
                if (at.x > 2 && at.x < 17 && at.z > 16 && at.z < 30) continue;
                float scale = .81f + (i * 7 % 11) * .045f + row * .05f;
                Sculpture(woodland, "Blender grove " + row + " tree " + i, trees[(i * 7 + i / 3 + row * 3) % 6], at, V(scale * (1 + .13f * Mathf.Sin(i)), scale, scale * (1 + .12f * Mathf.Cos(i))),
                    Color.Lerp(Hex(0xC8D7BD), Color.white, .55f + .45f * Mathf.Sin(a * 3 + row))).localEulerAngles = V(0, i * 137.5f, 0);
            }
            Combine(woodland);
            for (int floor = 0; floor < 2; floor++)
            {
                var room = Group(worldRoot, floor == 0 ? "Workshop cutaway" : "Shop cutaway"); if (floor == 0) lowerRoom = room; else upperRoom = room;
                float y = 1.1f + floor * 3.45f;
                Box(room, "Floor shadow beam", V(0, y - .18f, -.3f), V(12.5f, .4f, 7.3f), DarkWood);
                for (int plank = 0; plank < 24; plank++) Box(room, "Honey oak floorboard", V(-5.98f + plank * .52f, y, -.3f), V(.505f, .15f, 7.2f), plank % 3 == 0 ? Honey : Hex(0xBE8C58));
                if (floor == 0) Box(room, "Cream rear wall", V(0, y + 1.55f, 3.18f), V(12.1f, 3, .2f), Cream);
                else
                {
                    Box(room, "Shop cream rear wall", V(0, y + 1.55f, 3.18f), V(12.1f, 3, .2f), Cream);
                    for (int side = -1; side <= 1; side += 2)
                        Box(room, "Front door oak jamb", V(ShopDoorX + side * .72f, y + 1.2f, -3.63f), V(.14f, 2.4f, .2f), Wood);
                    Box(room, "Front door lintel", V(ShopDoorX, y + 2.47f, -3.63f), V(1.58f, .18f, .26f), Honey);
                    // Moving panels stay outside the static room mesh batch.
                    for (int side = 0; side < 2; side++)
                    {
                        var panel = passageCurtains[side] = Group(worldRoot, "Parting linen panel " + side, V(ShopDoorX, y + 2.08f, -3.64f));
                        for (int fold = 0; fold < 5; fold++)
                            Ball(panel, "Soft hanging linen fold", V((side == 0 ? -1 : 1) * (.055f + fold * .116f), -1.04f, 0), V(.16f, 2.06f, .12f), fold % 2 == 0 ? Deep : Leaf);
                        Combine(panel);
                    }
                    Tube(room, "Entrance curtain rail", V(ShopDoorX - .65f, y + 2.16f, -3.64f), V(ShopDoorX + .65f, y + 2.16f, -3.64f), .045f, Honey);
                    Box(room, "Doorway threshold", V(ShopDoorX, y + .09f, -3.68f), V(1.3f, .09f, .45f), Honey);
                }
                for (int i = -3; i <= 3; i++)
                    Box(room, "Timber wall upright", V(i * 2, y + 1.55f, 3.02f), V(.13f, 3.2f, .19f), Wood);
                Box(room, "Rear horizontal timber", V(0, y + 2.87f, 3), V(12.3f, .18f, .22f), Wood);
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(room, "Side cutaway wall", V(side * 6.1f, y + .56f, -.15f), V(.17f, 1.1f, 6.6f), Cream);
                    Box(room, "Front support post", V(side * 6.07f, y + 1.5f, -3.48f), V(.23f, 3.2f, .25f), DarkWood);
                    Tube(room, "Curved corner brace", V(side * 6.05f, y + 2.85f, -3.48f), V(side * 5.2f, y + 3.25f, -3.48f), .12f, Wood);
                }
                Window(room, V(floor == 0 ? -3.6f : 1, y + 1.7f, 2.86f));
                Box(room, "Floor fascia", V(0, y - .22f, -3.92f), V(12.65f, .23f, .21f), Honey);
                for (int i = 0; i < 16; i++)
                {
                    float x = -5.95f + i * .79f;
                    Ball(room, "Trailing ivy leaf", V(x, y - .39f - (i % 3) * .11f, -3.99f), V(.24f, .37f, .08f), i % 2 == 0 ? Leaf : Moss, V(0, 0, i % 2 == 0 ? 28 : -28));
                }
                if (floor == 0) workshopFrontBeam = Box(worldRoot, "Front ceiling beam", V(0, y + 3.1f, -3.68f), V(12.3f, .17f, .19f), Wood);
                Combine(room);
            }
            // Scalloped green tiled canopy over the open dollhouse frontage.
            roof = Group(worldRoot, "Moss glazed roof");
            for (int row = 0; row < 4; row++) for (int col = 0; col < 25; col++)
                Box(roof, "Overlapping leaf roof tile", V(-6.4f + col * .52f + row % 2 * .13f, 8.6f - row * .11f + Mathf.Sin(col * 1.7f) * .012f, 3.2f - row * .85f), V(.57f, .11f, .98f), (row * 13 + col * 7) % 5 == 0 ? Hex(0x53745A) : (row + col * 3) % 4 == 0 ? Hex(0x486B54) : Deep, V(-8, 0, col % 3 - 1));
            Tube(roof, "Roof copper ridge", V(-6.65f, 8.72f, 3.5f), V(6.65f, 8.72f, 3.5f), .11f, Copper); Combine(roof);
            CourtyardPaths(s);
            for (int i = 0; i < 8; i++)
            {
                var a = V(-6.0f + i * 1.7f, 7.65f + Mathf.Abs(i - 3.5f) * .09f, -3.48f);
                if (i < 7) Tube(upperRoom, "Tea party light cord", a, V(a.x + 1.7f, 7.65f + Mathf.Abs(i + 1 - 3.5f) * .09f, a.z), .018f, DarkWood);
                Ball(upperRoom, "Warm lantern", a + V(0, -.16f, 0), V(.15f, .21f, .15f), Gold);
            }
            ForestModels.Pot(lowerRoom, V(-5.4f, 1.18f, -3.1f), 1.15f, false);
            ForestModels.Pot(upperRoom, V(5.4f, 4.63f, 2.1f), 1.1f, false);
            Box(lowerRoom, "Warehouse chest", V(5.15f, 1.59f, .25f), V(1.03f, .78f, .7f), Wood);
            Box(lowerRoom, "Chest lid", V(5.15f, 2.03f, .25f), V(1.09f, .16f, .76f), Honey);
            Box(lowerRoom, "Brass chest latch", V(5.15f, 1.84f, -.12f), V(.13f, .2f, .035f), Gold);
            Hotspot(lowerRoom, "warehouse", 0, V(5.15f, 1.65f, .25f), V(1.4f, 1.1f, 1.1f));
            WorkshopStorage();
            Combine(s);
            sideClosed = Annex(false); sideOpen = Annex(true);
            curtainPlain = Curtains(false); curtainLeaves = Curtains(true);
            matPlain = Rug(false); matBread = Rug(true);
            signOld = Sign(false); signNew = Sign(true);
            potPlain = Planter(false); potFlowers = Planter(true);
            Hotspot(worldRoot, "decoration", 0, V(6.45f, .65f, -3.7f), V(1.1f, 1.4f, 1.1f));
        }
        void WorkshopStorage()
        {
            var cabinet = Group(lowerRoom, "Material preparation cabinet", V(.1f, 1.19f, 2.5f));
            Box(cabinet, "Cabinet body", V(0, .55f, 0), V(1.8f, 1.1f, .7f), Wood);
            Box(cabinet, "Preparation top", V(0, 1.14f, -.03f), V(1.94f, .15f, .85f), Honey);
            for (int i = 0; i < 2; i++)
            {
                Box(cabinet, "Supply drawer", V(0, .3f + i * .47f, -.37f), V(1.64f, .39f, .06f), Honey);
                Ring(cabinet, "Drawer pull", V(0, .31f + i * .47f, -.43f), .075f, .018f, Gold, V(90, 0, 0));
            }
            for (int i = 0; i < 3; i++)
            {
                var jar = ForestModels.Product(cabinet, i == 0 ? 0 : 2);
                jar.localPosition = V(-.58f + i * .58f, 1.23f, -.08f); jar.localScale = Vector3.one * .65f;
            }
            Combine(cabinet);
            var tools = Group(lowerRoom, "Wall preparation tools", V(2.5f, 3.15f, 2.88f));
            Box(tools, "Tool rail", Vector3.zero, V(2.3f, .13f, .1f), Honey);
            for (int i = 0; i < 3; i++)
            {
                float x = -.75f + i * .75f;
                Tube(tools, "Hanging wooden spoon", V(x, -.1f, -.08f), V(x, -.65f, -.08f), .045f, Wood);
                Ball(tools, "Spoon bowl", V(x, -.75f, -.08f), V(.22f, .3f, .07f), Honey);
            }
            Combine(tools);
        }
        void CourtyardPaths(Transform ground)
        {
            // Low contrast painted washes, scattered meadow islands and quiet open paths.
            var meadow = Group(ground, "Meadow light and moss washes");
            for (int i = 0; i < 24; i++)
            {
                float a = i * 2.39996f, r = 4 + Mathf.Sqrt(i / 23f) * 15;
                var at = V(Mathf.Sin(a) * r, .112f + i * .0001f, 2 + Mathf.Cos(a) * r);
                MeadowPatch(meadow, at, new Vector2(2.1f + i % 4 * .7f, 1.5f + i % 3 * .6f), i % 3 == 0 ? Hex(0x9CAC7C) : Hex(0x889F70), i * 1.7f);
            }
            for (int step = 0; step < 5; step++)
            {
                Box(ground, "Shop entrance stone stair", V(-5.4f, .18f + step * .23f, -15.8f + step * .45f), V(2.5f, .25f, .53f), Hex(0xB9B69B));
                Box(ground, "Workshop porch stair", WorkshopOrigin + V(-.65f, .18f + step * .23f, -5.8f + step * .42f), V(3.1f, .25f, .52f), Hex(0xB9B69B));
            }
            // The broad public road ends at the shop's front doorway.
            DirtPath(ground, V(-5.4f, .105f, -48), V(-5.4f, .105f, -16), 3.0f);
            // Private paths go around the shop and branch in the clearing behind it.
            DirtPath(ground, V(-5.4f, .105f, -16), V(-9.2f, .105f, -15.8f), 1.4f);
            DirtPath(ground, V(-9.2f, .105f, -15.8f), V(-9.2f, .105f, -.5f), 1.4f);
            StonePath(ground, V(-9.2f, .14f, -.5f), V(-10.65f, .14f, 3.3f), 9);
            StonePath(ground, V(-9.2f, .14f, -.5f), V(3.7f, .14f, -.5f), 24);
            StonePath(ground, V(3.7f, .14f, -.5f), V(3.7f, .14f, 10.7f), 22);
            StonePath(ground, V(3.7f, .14f, 5.3f), V(12.8f, .14f, 5.3f), 17);
            StonePath(ground, V(3.7f, .14f, 10.7f), V(12.8f, .14f, 10.7f), 17);
            // A small communal rest area gives the clearing a deliberate centre.
            var rest = Group(ground, "Clearing rest place", V(-1.1f, .12f, 3.1f));
            for (int i = 0; i < 3; i++) Box(rest, "Bench seat plank", V(0, .72f, -.22f + i * .22f), V(2.5f, .12f, .2f), Honey);
            for (int side = -1; side <= 1; side += 2)
            {
                Box(rest, "Bench leg", V(side * .92f, .38f, 0), V(.17f, .7f, .52f), Wood);
                Box(rest, "Bench upright", V(side * 1.04f, 1.07f, .29f), V(.13f, 1.3f, .13f), Wood, V(-8, 0, 0));
            }
            for (int i = 0; i < 2; i++) Box(rest, "Bench back rail", V(0, 1.12f + i * .26f, .35f), V(2.5f, .18f, .12f), Honey, V(-8, 0, 0));
            ForestModels.Pot(rest, V(-1.8f, .02f, -.2f), .9f, true);
            for (int i = 0; i < 76; i++)
            {
                float a = i * 2.39996f; float bank = 20.8f + Mathf.Sin(i * 2.1f) * 1.5f + i % 3 * .65f;
                var at = V(Mathf.Sin(a) * bank, .13f, 2 + Mathf.Cos(a) * (bank + 2.2f));
                if (at.x > 3.5f && at.x < 14.5f && at.z > 18 && at.z < 28) continue;
                if (at.z < -17 && Mathf.Abs(at.x + 5.4f) < 3.1f) continue;
                if (i % 5 == 2) continue;
                Ball(ground, "Fern bank moss", at + V(0, .12f, 0), V(.5f + i % 4 * .24f, .22f + i % 3 * .07f, .6f + i % 3 * .19f), i % 2 == 0 ? Leaf : Moss);
                if (i % 3 == 0) ForestModels.Flower(ground, at + V(.16f, .61f, -.08f), .11f, Cream);
                for (int j = 0; j < 3; j++) Shape(ground, "Forest floor fern", OakLeaf(), at + V((j - 1) * .14f, .05f, -.5f), V(.3f, .4f, .7f), Deep, V(-35, i * 27 + j * 55, 0));
            }
            foreach (var at in new[] { V(-16, .14f, 1), V(15.5f, .14f, -2), V(2, .14f, 17), V(-4, .14f, 14), V(7, .14f, -3) })
                for (int j = 0; j < 7; j++)
                {
                    Vector3 p = at + V(Mathf.Sin(j * 2.4f) * (j % 3) * .5f, 0, Mathf.Cos(j * 2.4f) * .7f);
                    for (int k = 0; k < 3; k++) Ball(ground, "Meadow clover leaf", p + V((k - 1) * .09f, .08f, 0), V(.20f, .06f, .24f), j % 2 == 0 ? Moss : Leaf, V(0, k * 120, 0));
                    if (j % 3 == 0) ForestModels.Flower(ground, p + V(0, .25f, 0), .075f, Cream);
                }
            for (int side = -1; side <= 1; side += 2)
            {
                var post = V(-5.4f + side * 2.15f, .12f, -23.5f);
                Box(ground, "Road lantern post", post + V(0, .9f, 0), V(.14f, 1.8f, .14f), Wood);
                Box(ground, "Lantern brass cap", post + V(0, 1.95f, 0), V(.46f, .10f, .43f), Copper);
                Ball(ground, "Lantern warm glass", post + V(0, 1.72f, 0), V(.31f, .40f, .31f), Gold);
                ForestModels.Pot(ground, post + V(side * .45f, 0, .16f), .75f, true);
            }
            Combine(ground);
        }
        void DirtPath(Transform parent, Vector3 from, Vector3 to, float width)
        {
            Vector3 d = to - from; var p = Box(parent, "Warm worn woodland path", (from + to) * .5f, V(width, .035f, d.magnitude + width * .6f), Hex(0xB3A47A));
            p.localRotation = Quaternion.LookRotation(d);
            int count = Mathf.CeilToInt(d.magnitude / 1.25f);
            for (int i = 0; i <= count; i++)
                Ball(parent, "Embedded path paver", Vector3.Lerp(from, to, (float)i / count) + V(Mathf.Sin(i * 2) * .22f, .035f, 0), V(.75f, .055f, .58f), Hex(0xC5BC98), V(0, i * 31, 0));
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
            // Furniture and residents retain their proportions; the room shell
            // and individual positions provide the extra floor space.
            workshopArea = Group(worldRoot, "Independent magic workshop", WorkshopOrigin);
            shopArea = Group(worldRoot, "Treehouse village shop", ShopOrigin);
            workshopArea.localScale = shopArea.localScale = Vector3.one * 1.08f;
            gardenArea = Group(worldRoot, "Twelve-bed woodland garden", GardenOrigin);
            MoveToArea(lowerRoom, workshopArea);
            MoveToArea(workshopFrontBeam, workshopArea);
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
            var livingTree = Group(worldRoot, "Shop's sheltering oak", V(-12.6f, .1f, -8.2f));
            livingTree.localScale = Vector3.one * 1.05f;
            MoveToArea(oak, livingTree); MoveToArea(canopy, livingTree);
            workshopRoof = Group(workshopArea, "Workshop honey tiled roof");
            for (int row = 0; row < 4; row++) for (int col = 0; col < 25; col++)
                Box(workshopRoof, "Warm clay roof tile", V(-6.4f + col * .52f + row % 2 * .13f, 5.15f - row * .11f, 3.2f - row * .85f), V(.57f, .11f, .98f), (row + col) % 4 == 0 ? Hex(0xAB7651) : Hex(0x99734E), V(-8, 0, col % 3 - 1));
            Tube(workshopRoof, "Workshop roof ridge", V(-6.65f, 5.27f, 3.5f), V(6.65f, 5.27f, 3.5f), .10f, Copper); Combine(workshopRoof);
            shopRoofCap = RoofCap(shopArea, "Shop cutaway rear roof", 8.6f, false);
            workshopRoofCap = RoofCap(workshopArea, "Workshop cutaway rear roof", 5.15f, true);
            var supplies = Group(workshopArea, "Workshop outdoor supplies");
            ForestModels.Pot(supplies, V(-6.65f, .16f, -3.1f), 1.25f, false);
            for (int i = 0; i < 4; i++) Box(supplies, "Stacked supply crate", V(6.5f, .4f + i / 2 * .58f, -2.8f + i % 2 * .7f), V(.75f, .56f, .63f), i % 2 == 0 ? Honey : Wood);
            Combine(supplies);
            GardenBorders();
            Physics.SyncTransforms();
        }
        Transform RoofCap(Transform parent, string name, float height, bool clay)
        {
            var cap = Group(parent, name);
            for (int row = 0; row < 2; row++) for (int col = 0; col < 25; col++)
                Box(cap, "Rear eave tile", V(-6.4f + col * .52f + row % 2 * .13f, height - row * .11f, 3.2f - row * .85f), V(.57f, .11f, .98f), clay ? (col % 4 == 0 ? Hex(0xAB7651) : Hex(0x99734E)) : (col % 4 == 0 ? Hex(0x53745A) : Deep), V(-8, 0, col % 3 - 1));
            Box(cap, "Timber eave apron joins wall to rear roof", V(0, height - .57f, 3.17f), V(12.3f, 1.12f, .19f), Wood);
            Tube(cap, "Rear roof ridge", V(-6.65f, height + .12f, 3.5f), V(6.65f, height + .12f, 3.5f), .10f, Copper); Combine(cap); return cap;
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
            var a = Group(worldRoot, restored ? "Restored side room" : "Boarded side room", V(6.85f, 1.3f, 1.9f));
            Box(a, "Annex wall", V(0, 1.08f, 0), V(1.45f, 2.2f, 1.75f), restored ? Cream : DarkWood);
            for (int i = 0; i < 4; i++) Box(a, "Annex shingle", V(0, 2.32f + i * .12f, -.6f + i * .4f), V(1.8f, .14f, .62f), restored ? Deep : Wood, V(-16, 0, 0));
            if (restored) { Window(a, V(0, 1.1f, -.91f)); ForestModels.Pot(a, V(0, .15f, -1.1f), .75f, true); }
            else for (int i = 0; i < 3; i++) Box(a, "Repair board", V(0, .7f + i * .45f, -.94f), V(1.37f, .17f, .1f), Honey, V(0, 0, (i - 1) * 16));
            Hotspot(a, "expansion", 0, V(0, 1.1f, 0), V(1.6f, 2.4f, 1.9f)); Combine(a); return a;
        }
        Transform Curtains(bool leaves, Transform parent = null)
        {
            var p = Group(parent ?? worldRoot, leaves ? "Leaf patterned curtains" : "Linen curtains", V(-3.6f, 2.8f, 2.6f));
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
            var p = Group(parent ?? worldRoot, bread ? "Bread rug" : "Jute mat", V(0, 1.2f, -2.55f));
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
            var p = Group(parent ?? worldRoot, fresh ? "Tea party oak sign" : "Old wooden sign", V(-2.3f, 3.45f, 2.88f));
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
            var p = ForestModels.Pot(parent ?? worldRoot, V(6.45f, .13f, -3.7f), flowers ? 1.65f : 1.4f, flowers);
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
                for (int corner = 0; corner < 4; corner++) Box(emptyPlots[i], "Quiet future bed stake", V((corner % 2 == 0 ? -1 : 1) * 1.12f, .2f, (corner < 2 ? -1 : 1) * .91f), V(.07f, .18f, .07f), Hex(0x86916B));
                Hotspot(emptyPlots[i], "futurePlot", i, V(0, .4f, 0), V(2.4f, 1, 2)); Combine(emptyPlots[i]);
                var next = nextPlotDetails[i] = Group(emptyPlots[i], "Next expansion supplies");
                for (int j = 0; j < 3; j++) Box(next, "Stacked timber for next bed", V((j - 1) * .2f, .14f + j * .1f, 0), V(1.3f, .12f, .25f), Honey, V(0, -j * 13, 0));
                for (int side = -1; side <= 1; side += 2)
                    Box(next, "Next bed marked edge", V(side * 1.12f, .16f, 0), V(.09f, .1f, 1.9f), Honey);
                Box(next, "Expansion notice stake", V(0, .48f, .58f), V(.08f, .72f, .09f), Wood);
                Box(next, "Expansion notice", V(0, .8f, .55f), V(.65f, .45f, .1f), Cream);
                Box(next, "Expansion notice plus horizontal", V(0, .8f, .485f), V(.33f, .065f, .025f), Deep);
                Box(next, "Expansion notice plus vertical", V(0, .8f, .48f), V(.065f, .31f, .025f), Deep);
                Combine(next);
            }
            machines[0] = ForestModels.Cauldron(worldRoot); machines[0].localPosition = V(-2.8f, 1.19f, .4f); machines[0].localScale = Vector3.one * 1.05f;
            spoon = machines[0].Find("Stirring spoon"); Hotspot(machines[0], "machine", 0, V(0, .9f, 0), V(2.4f, 2.2f, 2.2f));
            machines[1] = ForestModels.Bench(worldRoot); machines[1].localPosition = V(2.0f, 1.19f, .8f); Hotspot(machines[1], "machine", 1, V(0, .8f, 0), V(2.3f, 1.8f, 1.3f));
            for (int i = 0; i < 4; i++)
            {
                var at = V(ShelfX(i), 4.64f, 1.5f);
                shelves[i] = ForestModels.Shelf(worldRoot); shelves[i].localPosition = at;
                Hotspot(shelves[i], "shelf", i, V(0, .8f, 0), V(1.4f, 1.8f, 1));
                for (int j = 0; j < Catalog.Items.Length; j++)
                { var p = Group(shelves[i], "Stock " + j); for (int copy = 0; copy < 3; copy++) { var item = ForestModels.Product(p, j); item.localPosition = V((copy % 2 - .5f) * .45f, copy < 2 ? .23f : .8f, -.06f); item.localScale = Vector3.one * .56f; if (copy == 2) displayProducts[i * Catalog.Items.Length + j] = item; } goods[i * Catalog.Items.Length + j] = p; }
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
                visitors[i] = ForestModels.Resident(worldRoot, i); visitors[i].localPosition = V(-4.8f + i * 2.4f, 4.64f, ShopLaneZ); visitors[i].localScale = Vector3.one * .62f; residentMotion[i] = visitors[i].GetComponent<ForestResidentMotion>();
            }
            checkout = Group(upperRoom, "Little checkout desk", V(-3.7f, 4.64f, -1.2f));
            checkout.localRotation = Quaternion.Euler(0, -90, 0);
            Box(checkout, "Rounded counter body", V(0, .31f, 0), V(1.05f, .62f, .78f), Wood);
            Box(checkout, "Countertop", V(0, .66f, 0), V(1.18f, .13f, 1.1f), Honey);
            Box(checkout, "Sales ledger", V(.22f, .77f, .12f), V(.38f, .08f, .36f), Deep);
            Box(checkout, "Ledger pages", V(.22f, .78f, .08f), V(.3f, .04f, .27f), Cream);
            Shape(checkout, "Counter bell", Lathe(new[] { new Vector2(0, 0), new Vector2(.15f, 0), new Vector2(.15f, .05f), new Vector2(.1f, .14f), new Vector2(0, .17f) }), V(.26f, .73f, -.08f), Vector3.one, Gold);
            Box(checkout, "Lower collection tray", V(-.28f, .40f, -.38f), V(.70f, .06f, .74f), Honey);
            for (int side = -1; side <= 1; side += 2)
                Tube(checkout, "Tray bracket", V(-.28f + side * .24f, .06f, -.04f), V(-.28f + side * .24f, .36f, -.62f), .04f, DarkWood);
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
            if (state != current) { state = current; stirringPhase = 0; shopping?.Clear(); productionEffects?.Clear(); }
            bool workshop = true;
            bool shop = true;
            sideClosed.gameObject.SetActive(!state.sideRoom && shop); sideOpen.gameObject.SetActive(state.sideRoom && shop);
            curtainPlain.gameObject.SetActive(workshop && !state.decoration[1]); curtainLeaves.gameObject.SetActive(workshop && state.decoration[1]); matPlain.gameObject.SetActive(workshop && !state.decoration[2]); matBread.gameObject.SetActive(workshop && state.decoration[2]);
            signOld.gameObject.SetActive(shop && !state.decoration[0]); signNew.gameObject.SetActive(shop && state.decoration[0]); potPlain.gameObject.SetActive(shop && !state.decoration[3]); potFlowers.gameObject.SetActive(shop && state.decoration[3]);
            UpdateResidents();
            for (int i = 0; i < Catalog.MaxPlots; i++)
            {
                bool built = i < state.plots.Length; plots[i].gameObject.SetActive(built); emptyPlots[i].gameObject.SetActive(!built);
                nextPlotDetails[i].gameObject.SetActive(!built && i == state.plots.Length);
                if (!built) continue; var f = state.plots[i]; int crop = Mathf.Max(0, f.workItem >= 0 ? f.workItem : f.config);
                for (int j = 0; j < 4; j++) cropModels[i * 4 + j].gameObject.SetActive(crop == j);
                float growth = f.remaining > 0 ? Mathf.Clamp01(1 - (float)(f.remaining / (Catalog.CropSeconds[crop] / Catalog.Speed[f.level - 1]))) : 1;
                cropModels[i * 4 + crop].localScale = Vector3.one * Mathf.Lerp(.52f, 1.6f, growth);
            }
            machines[0].gameObject.SetActive(workshop); machines[1].gameObject.SetActive(state.machines.Length > 1 && workshop);
            for (int i = 0; i < 4; i++)
            {
                bool built = i < state.shelves.Length; shelves[i].gameObject.SetActive(built); emptyShelves[i].gameObject.SetActive(!built);
                for (int j = 0; j < Catalog.Items.Length; j++) goods[i * Catalog.Items.Length + j].gameObject.SetActive(built && state.shelves[i].config == j && state.items[j] > 0);
            }
            productionEffects?.Refresh(state);
            BindForest();
        }
        void Update()
        {
            if (state == null) return;
            productionEffects?.Refresh(state);
            UpdateForestEffects();
            UpdateFishingWorld();
            bool stirring = state.machines.Length > 0 && state.machines[0].remaining > 0 && state.machines[0].workItem >= 0 && !state.report.pending && !state.simpleAnimation;
            if (stirring) stirringPhase += Time.unscaledDeltaTime;
            if (spoon != null) spoon.localEulerAngles = stirring ? V(-18 + Mathf.Sin(stirringPhase * 1.5f) * 7, stirringPhase * 26, -28) : V(-18, 0, -28);
            UpdateResidents();
        }
        void UpdateResidents()
        {
            if (state == null) return;
            shopping?.BeginFrame();
            float doorOpening = 0;
            for (int i = 0; i < 5; i++)
            {
                // A single visitor uses the entrance aisle at a time. Every
                // known resident takes a turn, avoiding intersecting waiting poses.
                int knownCount = Mathf.Min(5, state.stories + 1);
                float cycle = knownCount * 52, phase = (Time.unscaledTime - visitEpoch + cycle - i * 52) % cycle;
                bool simple = state.simpleAnimation, known = i < Mathf.Min(5, state.stories + 1);
                visitors[i].gameObject.SetActive(known && (simple || phase < 51.8f));
                if (!known) continue;
                float shelfX = ShelfX(i % state.shelves.Length), lane = ShopLaneZ;
                Vector3 outside = V(ShopDoorX, 3.6f, -24), foot = V(ShopDoorX, 3.6f, -5.4f), entrance = V(ShopDoorX, 4.64f, -3.6f);
                Vector3 entryLane = V(ShopDoorX, 4.64f, lane), shelfLane = V(shelfX, 4.64f, lane), browse = V(shelfX, 4.64f, 1);
                Vector3 counter = shopArea.InverseTransformPoint(checkout.TransformPoint(V(0, 0, -1.05f)));
                if (!simple)
                {
                    float entering = Mathf.SmoothStep(0, 1, (phase - 9) / .7f) * (1 - Mathf.SmoothStep(0, 1, (phase - 12.5f) / .6f));
                    float leaving = Mathf.SmoothStep(0, 1, (phase - 38.4f) / .6f) * (1 - Mathf.SmoothStep(0, 1, (phase - 41.7f) / .7f));
                    doorOpening = Mathf.Max(doorOpening, entering, leaving);
                    if (phase >= 10.7f && phase < 11.1f && Time.unscaledTime - lastDoorAt > 2)
                    { lastDoorAt = Time.unscaledTime; DoorEntered?.Invoke(); }
                }
                Vector3 at; float yaw; bool walking;
                if (phase < 8) { at = Vector3.Lerp(outside, foot, phase / 8); yaw = 180; walking = true; }
                else if (phase < 11) { at = Vector3.Lerp(foot, entrance, (phase - 8) / 3); yaw = 180; walking = true; }
                else if (phase < 13) { at = Vector3.Lerp(entrance, entryLane, (phase - 11) / 2); yaw = 180; walking = true; }
                else if (phase < 17) { at = Vector3.Lerp(entryLane, shelfLane, (phase - 13) / 4); yaw = -90; walking = true; }
                else if (phase < 20) { at = Vector3.Lerp(shelfLane, browse, (phase - 17) / 3); yaw = 180; walking = true; }
                else if (phase < 27) { at = browse; yaw = 180 + Mathf.Sin(phase * .6f) * 8; walking = false; }
                else if (phase < 30) { at = Vector3.Lerp(browse, shelfLane, (phase - 27) / 3); yaw = 0; walking = true; }
                else if (phase < 34) { at = Vector3.Lerp(shelfLane, counter, (phase - 30) / 4); yaw = Quaternion.LookRotation(shelfLane - counter).eulerAngles.y; walking = true; }
                else if (phase < 38) { at = counter; yaw = 90; walking = false; }
                else if (phase < 39) { at = Vector3.Lerp(counter, entryLane, phase - 38); yaw = Quaternion.LookRotation(counter - entryLane).eulerAngles.y; walking = true; }
                else if (phase < 41) { at = Vector3.Lerp(entryLane, entrance, (phase - 39) / 2); yaw = 0; walking = true; }
                else if (phase < 44) { at = Vector3.Lerp(entrance, foot, (phase - 41) / 3); yaw = 0; walking = true; }
                else { at = Vector3.Lerp(foot, outside, Mathf.Clamp01((phase - 44) / 8)); yaw = 0; walking = true; }
                if (simple) { at = V(-4.8f + i * 2.4f, 4.64f, ShopLaneZ); yaw = -8; walking = false; }
                float browseGesture = !simple && phase >= 21 && phase < 26 ? Mathf.Sin((phase - 21) * Mathf.PI / 5) : 0;
                Quaternion facing = simple ? Quaternion.Euler(0, yaw, 0)
                    : Quaternion.RotateTowards(visitors[i].localRotation, Quaternion.Euler(0, yaw, 0), 240 * Time.unscaledDeltaTime);
                residentMotion[i].PoseRoute(at, facing, phase, walking, 0, browseGesture, !simple);
                shopping?.Pose(i, phase, i % state.shelves.Length, known && phase < 51.8f, simple);
            }
            for (int side = 0; side < 2; side++) if (passageCurtains[side] != null)
            {
                float direction = side == 0 ? -1 : 1;
                passageCurtains[side].gameObject.SetActive(true);
                passageCurtains[side].localPosition = V(ShopDoorX + direction * doorOpening * .52f, 6.63f, -3.64f);
                passageCurtains[side].localEulerAngles = V(0, 0, direction * doorOpening * 7);
            }
        }
        public Sprite Icon(string name, int cell)
        {
            string key = name + cell; if (icons.TryGetValue(key, out var sprite)) return sprite;
            Transform model = Group(portraitRoot, "Portrait model");
            portraitCamera.orthographicSize = 1.36f;
            if (name == "fish") ForestFishingModels.Fish(model,cell/4,cell%4);
            else if (name == "fishing-bait") ForestFishingModels.Bait(model,cell);
            else if (name == "celebration") ForestModels.TeaParty(model);
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
                if (cell >= 16 && cell <= 18) p = ForestModels.Crop(model, cell - 12);
                else if (cell >= 19 && cell <= 21) p = ForestModels.Product(model, cell - 13);
                else if (cell < 4) p = ForestModels.Crop(model, cell);
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
                int oldRegion = Region; Vector3 oldPosition = worldCamera.transform.position, oldFocus = cameraFocus; Quaternion oldRotation = worldCamera.transform.rotation; float oldSize = CameraSize;
                SetRegion(0);
                var rtScene = new RenderTexture(540, 660, 24); worldCamera.targetTexture = rtScene; worldCamera.Render(); sprite = ReadSprite(rtScene); SetRegion(oldRegion);
                worldCamera.transform.SetPositionAndRotation(oldPosition, oldRotation); worldCamera.orthographicSize = oldSize; cameraFocus = oldFocus;
                rtScene.Release(); Destroy(rtScene); Destroy(model.gameObject); icons[key] = sprite; return sprite;
            }
            else { Box(model, "Woven linen", V(0, .9f, 0), V(1.5f, 1.7f, .08f), cell % 2 == 0 ? Cream : Moss); }
            // Frame the actual sculpture, including long ears and all carried accessories.
            Bounds bounds = new Bounds(model.position, Vector3.zero); bool first = true;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>())
                if (renderer.enabled) { if (first) { bounds = renderer.bounds; first = false; } else bounds.Encapsulate(renderer.bounds); }
            portraitCamera.transform.position = bounds.center + V(2.7f, 1.15f, -7);
            portraitCamera.transform.LookAt(bounds.center);
            float extent = 0, portraitAspect = name == "celebration" ? 1.7f : name=="fish"?1.6f:.8f;
            for (int x = -1; x <= 1; x += 2) for (int y = -1; y <= 1; y += 2) for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = Quaternion.Inverse(portraitCamera.transform.rotation) * Vector3.Scale(bounds.extents, V(x, y, z));
                extent = Mathf.Max(extent, Mathf.Abs(corner.y), Mathf.Abs(corner.x) / portraitAspect);
            }
            portraitCamera.orthographicSize = Mathf.Max(.2f, extent * 1.12f);
            var rt = new RenderTexture(name == "celebration" ? 850 : name=="fish"?480:320, name == "celebration" ? 500 : name=="fish"?300:400, 24) { antiAliasing = 2 }; portraitCamera.targetTexture = rt; portraitCamera.Render(); sprite = ReadSprite(rt);
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
            ForestFishingModels.Release(); ForestExpansionModels.Release(); ForestGeometry.Release();
        }
    }
}
