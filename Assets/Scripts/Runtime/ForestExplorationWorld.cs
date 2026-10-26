using ForestShop.Core;
using UnityEngine;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    public sealed partial class ForestWorld3D
    {
        static readonly Vector3[] ForestCenters = { V(-45, 0, 38), V(-45, 0, 55), V(-60, 0, 69) };
        static readonly Vector3 GreenhouseOrigin = V(9, 0, 23);
        Transform forestScene, greenhouseOld, greenhouseNew, greenhouseOldRoof, greenhouseNewRoof;
        Transform bridgeOld, bridgeNew, vinesClosed, vinesOpen, pavilionOld, pavilionNew;
        readonly Transform[] forageAnchors = new Transform[3], nurseryAnchors = new Transform[4];
        readonly Transform[] forageFruit = new Transform[3], nurseryPlants = new Transform[8];
        sealed class ForestBurst { public Transform root, token, ring; public Transform[] variants; public float start; }
        readonly ForestBurst[] forestBursts = new ForestBurst[7];
        int ForestEffectCount { get { int count = 0; foreach (var b in forestBursts) if (b != null && b.root.gameObject.activeSelf) count++; return count; } }

        void BuildExplorationWorld()
        {
            var annex = Group(worldRoot, "Greenhouse beyond the garden", GreenhouseOrigin);
            greenhouseOld = Group(annex, "Unrepaired greenhouse"); greenhouseOldRoof = Group(greenhouseOld, "Old greenhouse roof");
            ForestExpansionModels.Greenhouse(greenhouseOld, greenhouseOldRoof, false);
            Hotspot(greenhouseOld, "forest-repair", 0, V(0, 1.5f, 0), V(9.2f, 3, 7.2f));
            greenhouseNew = Group(annex, "Restored greenhouse"); greenhouseNewRoof = Group(greenhouseNew, "Greenhouse removable roof");
            ForestExpansionModels.Greenhouse(greenhouseNew, greenhouseNewRoof, true);
            for (int i = 0; i < 4; i++)
            {
                var bed = nurseryAnchors[i] = Group(greenhouseNew, "Nursery bed " + (i + 1), V(i % 2 == 0 ? -2.6f : 2.6f, .25f, i < 2 ? -1.75f : .95f));
                var frame = Group(bed, "Nursery oak frame");
                Box(frame, "Nursery rich soil", V(0, .25f, 0), V(2.65f, .35f, 1.8f), Hex(0x745C44));
                for (int side = -1; side <= 1; side += 2)
                {
                    Box(frame, "Nursery side beam", V(side * 1.4f, .37f, 0), V(.14f, .5f, 2), Honey);
                    Box(frame, "Nursery end beam", V(0, .37f, side * .94f), V(2.9f, .5f, .14f), Wood);
                }
                Box(frame, "Copper bed number tag", V(1.1f, .66f, -.95f), V(.37f, .26f, .07f), Gold); Combine(frame);
                for (int crop = 0; crop < 2; crop++)
                {
                    var plants = nurseryPlants[i * 2 + crop] = Group(bed, "Cultivated discovery " + crop, V(0, .43f, 0));
                    for (int j = 0; j < 3; j++) { var model = ForestModels.Crop(plants, crop + 4); model.localPosition = V((j - 1) * .7f, 0, j % 2 * .33f); model.localScale = Vector3.one * .85f; }
                    Combine(plants);
                }
                Hotspot(bed, "nursery", i, V(0, .6f, 0), V(2.8f, 1.5f, 2));
            }
            var approach = Group(worldRoot, "Garden greenhouse approach");
            Path(approach, V(9, .12f, 13), V(9, .12f, 19.3f), 1.5f);
            Path(approach, V(9, .12f, 17), V(15, .12f, 17), 1.35f);
            Waypost(approach, V(14.4f, .12f, 16.3f), "forest-entry", 0, false);
            Hotspot(approach, "greenhouse", 0, V(9, 1, 19.3f), V(3, 2, 1.1f)); Combine(approach);

            forestScene = Group(worldRoot, "Connected living forest");
            var ground = Group(forestScene, "Forest floor and paths");
            Box(ground, "Continuous deep forest floor", V(-48, -.36f, 53), V(130, .8f, 140), Hex(0x7B9269));
            for (int i = 0; i < 3; i++)
            {
                var center = ForestCenters[i];
                Shape(ground, "Soft clearing " + i, Lathe(new[] { new Vector2(0,0), new Vector2(11.7f,0), new Vector2(12,.012f), new Vector2(11.5f,.025f), new Vector2(0,.025f) }, 80), center + V(0, .064f, 0), V(1,1,.88f), i == 1 ? Hex(0x91A787) : i == 2 ? Hex(0x879C75) : Hex(0x9BAE7E));
                Path(ground, center + V(0, .13f, -8), center + V(0, .13f, 7.7f), 1.7f);
                Path(ground, center + V(0, .13f, -1), center + V(-5, .13f, .5f), 1.2f);
                for (int j = 0; j < 15; j++)
                {
                    float a = j * 2.4f; var at = center + V(Mathf.Sin(a) * (6.4f + j % 3), .15f, Mathf.Cos(a) * (6.3f + j % 2));
                    if (Mathf.Abs(at.x - center.x) < 2) continue;
                    Ball(ground, "Mossy bank stone", at, V(.7f + j % 3 * .18f, .45f, .55f), j % 3 == 0 ? Hex(0xA4AB91) : Hex(0x7C8C70), V(8, j * 57, 12));
                    for (int leaf = 0; leaf < 3; leaf++) Ball(ground, "Forest floor fern", at + V(.45f, .15f, .35f), V(.18f, .55f, .10f), Moss, V(20, leaf * 65, 35));
                }
            }
            Path(ground, ForestCenters[1] + V(0, .12f, 6), ForestCenters[2] + V(0, .12f, -6), 1.6f);
            // A stream crosses the first footpath. Its bridge visibly gates travel.
            for (int i = 0; i < 18; i++)
            {
                float x = -70 + i * 2.9f, z = 47.5f + Mathf.Sin(i * .56f) * .65f;
                Box(ground, "Stream bank", V(x, .07f, z), V(3.8f, .13f, 4.7f), Hex(0xABB18F), V(0, Mathf.Cos(i * .56f) * -6, 0));
                Box(ground, "Quiet teal stream", V(x, .16f, z), V(3.6f, .05f, 3.35f), Hex(0x73AAA7), V(0, Mathf.Cos(i * .56f) * -6, 0));
                if (i % 2 == 0) Box(ground, "Water light stroke", V(x + .3f, .2f, z + .7f), V(1.1f, .02f, .05f), Hex(0xBFDED0), V(0, 8, 0));
            }
            Combine(ground);
            var trees = Group(forestScene, "Layered Blender forest shelter");
            string[] species = { "CanopyOak", "SilverBirch", "WoodlandLinden", "OpenRowan", "YoungBirch", "LowHazel" };
            for (int zone = 0; zone < 3; zone++) for (int i = 0; i < 20; i++)
            {
                float a = i * Mathf.PI * 2 / 20 + zone * .27f; float radius = 11.3f + i % 3 * 2.3f;
                var at = ForestCenters[zone] + V(Mathf.Sin(a) * radius, .1f, Mathf.Cos(a) * radius);
                if (Mathf.Abs(at.x + 45) < 3 && at.z < 63 || at.x < -47 && at.x > -63 && at.z > 59 && at.z < 65) continue;
                // Keep sightlines from the default front camera to every actual
                // discovery, including trees generated by the neighbouring zone.
                bool coversDiscovery = false;
                foreach (var center in ForestCenters)
                {
                    var node = center + V(-4.2f,0,-.8f);
                    if (Mathf.Abs(at.x-node.x) < 4.4f && at.z > node.z-9 && at.z < node.z+3) coversDiscovery = true;
                }
                foreach (var spot in FishingOrigins) if(Mathf.Abs(at.x-spot.x)<4 && at.z>spot.z-6 && at.z<spot.z+3) coversDiscovery=true;
                if (coversDiscovery || TreeCoversMoment(at) || at.x > -61 && at.x < -53 && at.z > 59 && at.z < 72) continue;
                float scale = .65f + i % 5 * .065f;
                Sculpture(trees, "Forest canopy " + zone + " " + i, species[(i * 7 + zone) % 6], at, Vector3.one * scale, Color.Lerp(Hex(0xA5B79C), Color.white, .75f)).localEulerAngles = V(0, i * 137.5f, 0);
            }
            Combine(trees);
            for (int node = 0; node < 3; node++) BuildForage(node);
            bridgeOld = Bridge(false); bridgeNew = Bridge(true);
            vinesClosed = VineGate(false); vinesOpen = VineGate(true);
            pavilionOld = Pavilion(false); pavilionNew = Pavilion(true);
            Waypost(forestScene, ForestCenters[0] + V(3.2f, .13f, -5.3f), "forest-home", 0, true);
            Waypost(forestScene, ForestCenters[0] + V(2.2f, .13f, 5), "forest-path", 1, false);
            Waypost(forestScene, ForestCenters[1] + V(2.5f, .13f, -5.5f), "forest-path", 0, true);
            Waypost(forestScene, ForestCenters[1] + V(-3.5f, .13f, 5.1f), "forest-path", 2, false);
            Waypost(forestScene, ForestCenters[2] + V(5.4f, .13f, -5.8f), "forest-path", 1, true);
            for (int i = 0; i < 7; i++)
            {
                var b = forestBursts[i] = new ForestBurst(); b.root = Group(worldRoot, "Discovery receipt " + i);
                b.token = Group(b.root, "Gathered material"); b.variants = new Transform[3];
                for (int j = 0; j < 3; j++) { b.variants[j] = ForestModels.Crop(b.token, j + 4); b.variants[j].localScale = Vector3.one * .72f; b.variants[j].gameObject.SetActive(false); }
                b.ring = Ring(b.root, "Discovery receipt halo", Vector3.zero, .62f, .04f, Cream); b.root.gameObject.SetActive(false);
            }
        }
        static void Path(Transform parent, Vector3 from, Vector3 to, float width)
        {
            float distance = Vector3.Distance(from, to); int count = Mathf.CeilToInt(distance / .65f);
            for (int i = 0; i <= count; i++)
            {
                var at = Vector3.Lerp(from, to, (float)i / count);
                Ball(parent, "Soft earthen footpath", at, V(width * 1.55f, .08f, width * 1.25f), Hex(0xB8AD87));
                if (i % 3 == 0) Box(parent, "Worn stepping stone", at + V(.13f * Mathf.Sin(i), .06f, 0), V(width * .7f, .09f, .46f), Hex(0xC5BC99), V(0, i * 47, 0));
            }
        }
        void Waypost(Transform parent, Vector3 at, string kind, int index, bool back)
        {
            var p = Group(parent, "Leaf waypost " + kind + index, at);
            Tube(p, "Waypost upright", V(0, 0, 0), V(0, 1.85f, 0), .10f, Wood);
            Box(p, "Deep green path sign", V(0, 1.55f, 0), V(1.45f, .65f, .16f), Deep);
            Tube(p, "Cream direction stroke", V(back ? .35f : -.35f, 1.55f, -.1f), V(back ? -.35f : .35f, 1.55f, -.1f), .035f, Cream);
            Tube(p, "Arrow tip upper", V(back ? -.35f : .35f, 1.55f, -.1f), V(0, 1.76f, -.1f), .035f, Cream);
            Tube(p, "Arrow tip lower", V(back ? -.35f : .35f, 1.55f, -.1f), V(0, 1.34f, -.1f), .035f, Cream);
            Hotspot(p, kind, index, V(0, 1.1f, 0), V(1.9f, 2.4f, 1.1f)); Combine(p);
        }
        void BuildForage(int node)
        {
            var p = forageAnchors[node] = Group(forestScene, "Discovery " + ForestRules.Discoveries[node], ForestCenters[node] + V(-4.2f, .18f, -.8f));
            if (node < 2)
            {
                Box(p, "Natural planting bank", V(0, .05f, 0), V(3.5f, .18f, 2.9f), node == 0 ? Hex(0x7E8A59) : Hex(0x849F8A));
                for (int i = 0; i < 4; i++) { var plant = ForestModels.Crop(p, node + 4); plant.localPosition = V((i % 2 - .5f) * 1.4f, .17f, (i / 2 - .5f) * 1.15f); plant.localScale = Vector3.one * (node == 0 ? 1.3f : 1.4f); }
                var seedling = ForestModels.Pot(p, V(.8f, .15f, -1.1f), .65f, false);
                var little = ForestModels.Crop(seedling, node + 4); little.localPosition = V(0, .4f, 0); little.localScale = Vector3.one * .65f;
            }
            else
            {
                Sculpture(p, "Original Blender resin pine", "ResinPine", V(0,0,.2f), Vector3.one, Color.white);
                Box(p, "Resin tap scar", V(0, 1.3f, -.34f), V(.48f, 1.0f, .10f), Honey);
                ForestModels.Pot(p, V(0, .13f, -.7f), .7f, false);
            }
            Combine(p);
            var fruit = forageFruit[node] = Group(p, "Available discovery marker", V(0, node == 2 ? 1.25f : 1.1f, -1.1f));
            var resource = ForestModels.Crop(fruit, node + 4); resource.localScale = Vector3.one * .75f;
            Hotspot(p, "forage", node, V(0, 1, 0), V(4.2f, node == 2 ? 5 : 2.8f, 3.5f));
        }
        Transform Bridge(bool repaired)
        {
            var p = Group(forestScene, repaired ? "Restored creek bridge" : "Broken creek bridge", V(-45, .25f, 47.9f));
            for (int side = -1; side <= 1; side += 2)
            {
                Box(p, "Bridge long beam", V(side * 1.4f, .17f, 0), V(.2f, .35f, 6.6f), DarkWood);
                for (int i = 0; i < 4; i++) Box(p, "Bridge rail post", V(side * 1.5f, .8f, -2.8f + i * 1.85f), V(.14f, 1.5f, .16f), Wood);
                if (repaired) Tube(p, "Bridge honey handrail", V(side * 1.5f, 1.5f, -3), V(side * 1.5f, 1.5f, 3), .07f, Honey);
            }
            for (int i = 0; i < 16; i++) if (repaired || i < 3 || i > 12)
                Box(p, "Bridge decking", V(0, .4f, -3 + i * .4f), V(3.2f, .13f, .37f), i % 3 == 0 ? Honey : Wood, V(0, repaired ? 0 : i % 2 * 8, 0));
            if (!repaired) for (int i = 0; i < 3; i++) Box(p, "Fallen bridge board", V(.2f + i * .4f, .05f, -.7f + i), V(2.7f, .12f, .32f), Wood, V(10, 20 + i * 25, 10));
            Hotspot(p, repaired ? "forest-path" : "forest-repair", 1, V(0, .8f, 0), V(3.5f, 2.2f, 6.7f)); Combine(p); return p;
        }
        Transform VineGate(bool open)
        {
            var p = Group(forestScene, open ? "Open leafy arch" : "Sleeping vine arch", V(-51.5f, .1f, 62)); p.localEulerAngles = V(0, -45, 0);
            for (int side = -1; side <= 1; side += 2)
            {
                Tube(p, "Arch timber", V(side * 2, 0, 0), V(side * 1.8f, 3.4f, 0), .16f, DarkWood);
                Tube(p, "Bent vine", V(side * 2, .1f, -.1f), V(side * 1.65f, 3.2f, -.1f), .09f, Moss);
                for (int i = 0; i < 7; i++) Ball(p, "Arch leaf", V(side * (1.85f - i * .035f), .5f + i * .42f, -.16f), V(.7f, .19f, .4f), i % 2 == 0 ? Leaf : Moss, V(0, 0, side * 25));
            }
            Tube(p, "Arch overhead timber", V(-2, 3.2f, 0), V(2, 3.2f, 0), .12f, Wood);
            if (!open) for (int i = 0; i < 5; i++)
            {
                Tube(p, "Blocking vine", V(-1.8f, .4f + i * .5f, -.2f), V(1.8f, 1.1f + i * .4f, -.2f), .07f, Deep);
                for (int j = 0; j < 4; j++) Ball(p, "Sleeping broad leaf", V(-1.3f + j * .8f, .8f + i * .5f, -.3f), V(.75f, .3f, .12f), Leaf, V(0, 0, j * 23));
            }
            else for (int i = 0; i < 7; i++) ForestModels.Flower(p, V(-1.8f + i * .6f, 3.4f, -.1f), .18f, Cream);
            Hotspot(p, open ? "forest-path" : "forest-repair", 2, V(0, 1.6f, 0), V(4.4f, 3.5f, 1.5f)); Combine(p); return p;
        }
        Transform Pavilion(bool restored)
        {
            var p = Group(forestScene, restored ? "Restored forest reading pavilion" : "Old forest reading pavilion", ForestCenters[2] + V(2.6f, .1f, 2.4f));
            Box(p, "Pavilion stone base", V(0, .2f, 0), V(5.7f, .4f, 4.8f), Hex(0xADB092));
            Box(p, "Pavilion open front terrace", V(0, .2f, -3.2f), V(5.7f, .4f, 2.4f), Hex(0xADB092));
            for (int i = 0; i < 14; i++) Box(p, "Pavilion floor plank", V(-2.55f + i * .39f, .46f, 0), V(.37f, .14f, 4.3f), restored ? Honey : Wood);
            for (int i = 0; i < 14; i++) if (restored || i % 4 != 1)
                Box(p, "Open terrace plank", V(-2.55f + i * .39f, .46f, -3.1f), V(.37f, .14f, 2.3f), restored ? Honey : Hex(0x877552));
            Box(p, "Pavilion welcoming step", V(0,.2f,-4.6f), V(2.8f,.2f,.7f), Hex(0xBCB79A));
            for (int side = -1; side <= 1; side += 2) for (int end = -1; end <= 1; end += 2)
                Box(p, "Pavilion pillar", V(side * 2.3f, 2.0f, end * 1.8f), V(.22f, 3.1f, .23f), restored ? Wood : DarkWood, V(0, 0, restored ? 0 : side * 3));
            for (int side = -1; side <= 1; side += 2)
            {
                for (int plank = 0; plank < 15; plank++) if (restored || side == -1 || plank < 4 || plank > 6)
                    Box(p, "Pavilion roof board", V(side * 1.45f, 4.0f, -2.28f + plank * .355f), V(3.25f, .2f, .34f), restored ? (plank % 3 == 0 ? Hex(0x476853) : Deep) : Hex(0x6E7960), V(0, 0, -side * 20));
                Tube(p, "Roof warm fascia", V(side * 2.95f, 3.43f, -2.45f), V(side * 2.95f, 3.43f, 2.85f), .09f, restored ? Honey : Wood);
                Box(p, "Reading bench seat", V(side * 1.8f, .95f, -3), V(.65f, .13f, 2.1f), restored ? Honey : Wood);
                for (int end = -1; end <= 1; end += 2) Box(p, "Reading bench leg", V(side * 1.8f, .7f, -3 + end * .75f), V(.48f,.5f,.16f), Wood);
                Box(p, "Reading bench back", V(side * 2.1f,1.34f,-3), V(.12f,.55f,2.1f), restored ? Honey : Wood);
            }
            Tube(p, "Pavilion roof ridge", V(0, 4.58f, -2.5f), V(0, 4.58f, 2.9f), .12f, Honey);
            Box(p, "Reading table", V(0, 1.35f, -2.8f), V(1.6f, .13f, 1.3f), restored ? Honey : Wood);
            Tube(p, "Table pedestal", V(0, .5f, -2.8f), V(0, 1.3f, -2.8f), .13f, Wood);
            if (restored)
            {
                Box(p, "Open story book", V(0, 1.48f, -2.55f), V(.85f, .07f, .65f), Cream, V(0, -15, 0));
                for (int i = 0; i < 3; i++) { var tea = ForestModels.Product(p, 6); tea.localPosition = V(-.5f + i * .5f, 1.46f, -3.15f); tea.localScale = Vector3.one * .4f; }
                ForestModels.Pot(p, V(-2.4f, .55f, -4.0f), .9f, true); ForestModels.Pot(p, V(2.4f, .55f, -4.0f), .9f, true);
            }
            else for (int i = 0; i < 3; i++) Box(p, "Stacked fallen pavilion boards", V(0, .66f + i * .15f, -.9f), V(3.2f, .14f, .35f), DarkWood, V(0, -15 + i * 23, 0));
            Hotspot(p, "forest-repair", 3, V(0, 1.7f, -.9f), V(5.8f, 4, 7)); Combine(p); return p;
        }
        void SetForestRegion(int region)
        {
            if (forestScene == null) return;
            bool forest = region >= 4 && region <= 6; forestScene.gameObject.SetActive(forest);
            greenhouseOldRoof.gameObject.SetActive(region != 7); greenhouseNewRoof.gameObject.SetActive(region != 7);
            if (region < 4) return;
            var focus = transform.position + (region == 7 ? GreenhouseOrigin + V(0, 1.3f, -.1f) : ForestCenters[Mathf.Clamp(region - 4, 0, 2)] + V(0, 1.3f, .7f));
            cameraFocus = focus; worldCamera.orthographicSize = region == 7 ? 7.2f : 11.8f;
            worldCamera.transform.position = focus + (region == 7 ? V(.5f, 22, -19) : V(1, 27, -25)); worldCamera.transform.LookAt(focus);
        }
        void ClampForestCamera(Vector3 local)
        {
            Vector3 bounded = Region == 7 ? V(Mathf.Clamp(local.x, 5, 13), local.y, Mathf.Clamp(local.z, 19, 27))
                : V(Mathf.Clamp(local.x, -75, -31), local.y, Mathf.Clamp(local.z, 27, 80));
            var move = bounded - local; cameraFocus += move; worldCamera.transform.position += move;
        }
        public Vector2 ForestMarker(int index)
        {
            if(index>=10)return MomentMarker(index-10);
            if(index>=7) return FishingMarker(index-7);
            var target = index < 3 ? forageAnchors[index] : nurseryAnchors[index - 3];
            var v = worldCamera.WorldToViewportPoint(target.position + V(0, index < 3 ? .1f : .18f, index < 3 ? -3.1f : -1.2f)); return new Vector2(v.x, v.y);
        }
        void BindForest()
        {
            if (greenhouseOld == null || state == null) return;
            var f = state.forest; greenhouseOld.gameObject.SetActive(!f.greenhouse); greenhouseNew.gameObject.SetActive(f.greenhouse);
            BindMoments();
            bridgeOld.gameObject.SetActive(!f.bridge); bridgeNew.gameObject.SetActive(f.bridge);
            vinesClosed.gameObject.SetActive(!f.vines); vinesOpen.gameObject.SetActive(f.vines);
            pavilionOld.gameObject.SetActive(!f.pavilion); pavilionNew.gameObject.SetActive(f.pavilion);
            for (int i = 0; i < 3; i++) forageFruit[i].gameObject.SetActive(f.available[i] > 0 && f.regrow[i] <= 0);
            for (int i = 0; i < 4; i++)
            {
                if (!f.greenhouse) continue; var bed = f.beds[i]; int crop = Mathf.Max(4, bed.workItem >= 0 ? bed.workItem : bed.config);
                float growth = bed.remaining > 0 ? Mathf.Clamp01(1 - (float)(bed.remaining / (Catalog.CropSeconds[crop] / Catalog.Speed[bed.level - 1]))) : 1;
                for (int j = 0; j < 2; j++) { var plant = nurseryPlants[i * 2 + j]; plant.gameObject.SetActive(crop == j + 4); plant.localScale = Vector3.one * Mathf.Lerp(.3f, 1, growth); }
            }
        }
        void ClearForestEffects() { foreach (var b in forestBursts) if (b != null) b.root.gameObject.SetActive(false); }
        void PresentForestProduction(ProductionNotice notice)
        {
            int index = notice.Kind == ProductionKind.Forage ? notice.Facility : 3 + notice.Facility;
            if (index < 0 || index >= forestBursts.Length || !worldCamera.enabled || state.report.pending) return;
            var anchor = index < 3 ? forageAnchors[index] : nurseryAnchors[index - 3];
            var position = anchor.position + V(0, 1.4f, -.3f); var uv = worldCamera.WorldToViewportPoint(position);
            if (uv.z <= 0 || uv.x < .03f || uv.x > .97f || uv.y < .08f || uv.y > .96f || !anchor.gameObject.activeInHierarchy) return;
            var b = forestBursts[index]; b.start = Time.unscaledTime; b.root.position = position; b.root.gameObject.SetActive(true);
            b.root.localScale = Vector3.one * Mathf.Clamp(worldCamera.orthographicSize / 7f, 1, 2.1f);
            for (int i = 0; i < 3; i++) b.variants[i].gameObject.SetActive(notice.Item == i + 4);
            b.root.localRotation = Quaternion.identity; b.token.localPosition = V(0, .25f, 0); b.ring.localScale = Vector3.one;
        }
        void UpdateForestEffects()
        {
            if (state == null) return;
            foreach (var b in forestBursts)
            {
                if (b == null || !b.root.gameObject.activeSelf) continue;
                float t = (Time.unscaledTime - b.start) / 1.85f;
                if (t >= 1 || state.report.pending || !worldCamera.enabled) { b.root.gameObject.SetActive(false); continue; }
                if (state.simpleAnimation) continue;
                b.token.localPosition = V(0, .25f + Mathf.Sin(t * Mathf.PI) * .55f, 0);
                b.ring.localScale = Vector3.one * (1 + t * .5f);
            }
        }
    }
}
