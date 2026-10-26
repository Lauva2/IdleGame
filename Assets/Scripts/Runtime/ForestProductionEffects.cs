using ForestShop.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // Bounded, reusable presentation. Only actual settlement notices start bursts;
    // binding a save or switching a camera never replays historical production.
    public sealed class ForestProductionEffects
    {
        sealed class Burst
        {
            public Transform root, token, store, halo;
            public Transform[] variants, motes;
            public float started = -100;
            public ProductionKind kind;
            public float scale = 1;
        }
        readonly Burst[] plots = new Burst[Catalog.MaxPlots], machines = new Burst[2], shelves = new Burst[4];
        readonly Transform[] steam = new Transform[6];
        readonly Transform guide;
        readonly Transform[] plotAnchors, machineAnchors, shelfAnchors;
        const float Duration = 1.7f;
        public int ActiveCount { get; private set; }

        public ForestProductionEffects(Transform root, Transform[] plotAnchors, Transform[] machineAnchors, Transform[] shelfAnchors)
        {
            this.plotAnchors = plotAnchors; this.machineAnchors = machineAnchors; this.shelfAnchors = shelfAnchors;
            var effects = Group(root, "Local settlement feedback");
            for (int i = 0; i < plots.Length; i++) plots[i] = Make(effects, "Harvest " + i, ProductionKind.Harvest);
            for (int i = 0; i < machines.Length; i++) machines[i] = Make(effects, "Craft " + i, ProductionKind.Craft);
            for (int i = 0; i < shelves.Length; i++) shelves[i] = Make(effects, "Sale " + i, ProductionKind.Sale);
            for (int i = 0; i < 6; i++)
            {
                steam[i] = Ball(machineAnchors[i / 3], "Working vapour " + i, Vector3.zero, Vector3.one * .12f, Cream);
                NoShadow(steam[i]); steam[i].gameObject.SetActive(false);
            }
            guide = Group(effects, "Next opening facility");
            Ring(guide, "Warm target outline", Vector3.zero, 1.05f, .035f, Gold);
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI / 2;
                Ball(guide, "Target leaf", new Vector3(Mathf.Cos(a) * 1.18f, .015f, Mathf.Sin(a) * 1.18f), new Vector3(.19f, .025f, .09f), Cream, new Vector3(0, -i * 90, 0));
            }
            NoShadow(guide); guide.gameObject.SetActive(false);
        }

        static void NoShadow(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            { r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false; }
        }
        static Burst Make(Transform parent, string name, ProductionKind kind)
        {
            var b = new Burst { root = Group(parent, name), kind = kind };
            b.token = Group(b.root, "Settled item");
            if (kind == ProductionKind.Sale)
            {
                b.variants = new Transform[0];
                Ball(b.token, "Earned coin", Vector3.zero, new Vector3(.60f, .60f, .13f), Gold);
                Ring(b.token, "Coin rim", Vector3.zero, .26f, .022f, Cream, new Vector3(90, 0, 0));
            }
            else
            {
                int count = kind == ProductionKind.Harvest ? 4 : Catalog.Items.Length;
                b.variants = new Transform[count];
                for (int i = 0; i < count; i++)
                {
                    b.variants[i] = kind == ProductionKind.Harvest ? ForestModels.Crop(b.token, i) : ForestModels.Product(b.token, i);
                    b.variants[i].localScale = Vector3.one * (kind == ProductionKind.Craft ? 1.04f : .54f); b.variants[i].gameObject.SetActive(false);
                }
            }
            b.store = Group(b.root, kind == ProductionKind.Sale ? "Receipt leaf" : "Into shared stock");
            if (kind != ProductionKind.Sale)
            {
                Box(b.store, "Small stock basket", Vector3.zero, new Vector3(.48f, .26f, .33f), Honey);
                for (int i = 0; i < 3; i++) Box(b.store, "Basket weave", new Vector3(-.15f + i * .15f, .01f, -.18f), new Vector3(.025f, .24f, .025f), Cream);
            }
            else Shape(b.store, "Receipt seal", OakLeaf(), Vector3.zero, Vector3.one * .32f, Leaf, new Vector3(-30, 20, 0));
            b.halo = Ring(b.root, "Quiet completion ring", Vector3.zero, .50f, .035f, kind == ProductionKind.Sale ? Gold : Cream);
            if (kind == ProductionKind.Sale) b.halo.localRotation = Quaternion.Euler(90, 0, 0);
            b.motes = new Transform[3];
            for (int i = 0; i < 3; i++) b.motes[i] = Ball(b.root, "Completion seed " + i, Vector3.zero, Vector3.one * .07f, i == 1 ? Gold : Cream);
            NoShadow(b.root); b.root.gameObject.SetActive(false); return b;
        }

        public void Clear()
        {
            foreach (var b in plots) b.root.gameObject.SetActive(false);
            foreach (var b in machines) b.root.gameObject.SetActive(false);
            foreach (var b in shelves) b.root.gameObject.SetActive(false);
            ActiveCount = 0;
        }
        public void Trigger(ProductionNotice notice, Camera camera, bool simple)
        {
            if (notice.Kind == ProductionKind.Forage || notice.Kind == ProductionKind.Nursery) return;
            var list = notice.Kind == ProductionKind.Harvest ? plots : notice.Kind == ProductionKind.Craft ? machines : shelves;
            var anchors = notice.Kind == ProductionKind.Harvest ? plotAnchors : notice.Kind == ProductionKind.Craft ? machineAnchors : shelfAnchors;
            if (notice.Facility < 0 || notice.Facility >= list.Length || !camera.enabled) return;
            var anchor = anchors[notice.Facility];
            Vector3 position = anchor.position + Vector3.up * (notice.Kind == ProductionKind.Harvest ? .85f : notice.Kind == ProductionKind.Craft ? 2.15f : 1.85f);
            if (notice.Kind == ProductionKind.Sale)
            {
                // Same anchor as the real numbered badge. Offset in viewport
                // space so zoom or camera tilt cannot put the coin behind it.
                Vector3 marker = camera.WorldToViewportPoint(anchor.TransformPoint(new Vector3(0, 1.85f, 0)));
                if (marker.z < 0 || marker.x < .035f || marker.x > .965f || marker.y < .13f || marker.y > .93f) return;
                // Keep receipts above the badge/product, with a side fallback
                // near the top edge. Never pull an off-screen source into view.
                if (marker.y <= .82f) marker.y += .075f;
                else marker.x += marker.x < .5f ? .11f : -.11f;
                position = camera.ViewportToWorldPoint(marker);
            }
            Vector3 uv = camera.WorldToViewportPoint(position);
            if (uv.z < 0 || uv.x < .035f || uv.x > .965f || uv.y < .13f || uv.y > .93f) return;
            var b = list[notice.Facility];
            if (b.root.gameObject.activeSelf && Time.unscaledTime - b.started < .6f) return;
            if (!b.root.gameObject.activeSelf && ActiveCount >= 5) return;
            b.root.position = position;
            // Sales have a small, stable screen footprint even at closest zoom.
            // Harvest/craft retain their item silhouette, but also shrink nearby.
            b.scale = notice.Kind == ProductionKind.Sale ? camera.orthographicSize * .052f : Mathf.Clamp(camera.orthographicSize / 7f, .45f, 1.8f);
            b.root.rotation = notice.Kind == ProductionKind.Sale ? camera.transform.rotation : Quaternion.Euler(0, camera.transform.eulerAngles.y, 0);
            b.started = Time.unscaledTime;
            for (int i = 0; i < b.variants.Length; i++) b.variants[i].gameObject.SetActive(i == notice.Item);
            if (!b.root.gameObject.activeSelf) ActiveCount++;
            b.root.gameObject.SetActive(true); Pose(b, 0, simple);
        }

        static void Pose(Burst b, float t, bool simple)
        {
            float envelope = Mathf.SmoothStep(0, 1, t / .12f) * (1 - Mathf.SmoothStep(0, 1, (t - .72f) / .28f));
            b.root.localScale = Vector3.one * (envelope * b.scale);
            b.token.localPosition = simple ? new Vector3(-.22f, .35f, 0) : Vector3.Lerp(new Vector3(-.30f, 0, 0), new Vector3(.28f, .35f, 0), Mathf.SmoothStep(0, 1, t)) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * .48f);
            b.token.localEulerAngles = simple ? Vector3.zero : new Vector3(0, Mathf.Sin(t * Mathf.PI) * 25, -Mathf.Sin(t * Mathf.PI) * 10);
            b.store.localPosition = new Vector3(.30f, .27f, 0);
            b.store.localScale = Vector3.one * (simple ? .7f : Mathf.Lerp(.65f, 1, Mathf.Sin(t * Mathf.PI)));
            b.halo.localPosition = new Vector3(0, -.05f, 0);
            b.halo.localScale = Vector3.one * (simple ? .6f : .5f + .6f * t);
            for (int i = 0; i < b.motes.Length; i++)
            {
                b.motes[i].gameObject.SetActive(!simple);
                float a = i * Mathf.PI * 2 / 3;
                b.motes[i].localPosition = new Vector3(Mathf.Cos(a) * (.2f + t * .28f), .15f + Mathf.Sin(t * Mathf.PI) * .32f, Mathf.Sin(a) * .15f);
            }
        }
        void Tick(Burst b, bool simple)
        {
            if (!b.root.gameObject.activeSelf) return;
            float t = (Time.unscaledTime - b.started) / (b.kind == ProductionKind.Craft ? 1.95f : Duration);
            if (t >= 1) { b.root.gameObject.SetActive(false); ActiveCount--; return; }
            Pose(b, t, simple);
        }
        public void Refresh(GameState state)
        {
            foreach (var b in plots) Tick(b, state.simpleAnimation);
            foreach (var b in machines) Tick(b, state.simpleAnimation);
            foreach (var b in shelves) Tick(b, state.simpleAnimation);
            for (int i = 0; i < steam.Length; i++)
            {
                int machine = i / 3; bool working = machine < state.machines.Length && state.machines[machine].remaining > 0 && state.machines[machine].workItem >= 0;
                steam[i].gameObject.SetActive(working && !state.simpleAnimation);
                if (!working || state.simpleAnimation) continue;
                float t = (Time.unscaledTime * .28f + i * .32f) % 1;
                steam[i].localPosition = new Vector3(Mathf.Sin(i * 2 + t * 4) * .11f, (machine == 0 ? 1.65f : 1.15f) + t * .45f, machine == 0 ? 0 : -.12f);
                steam[i].localScale = Vector3.one * (.06f + .10f * Mathf.Sin(t * Mathf.PI));
            }
            guide.gameObject.SetActive(state.welcomeRead && state.tutorial < 4);
            if (!guide.gameObject.activeSelf) return;
            Transform target = state.tutorial == 0 ? plotAnchors[0] : state.tutorial == 1 ? machineAnchors[0] : shelfAnchors[0];
            if (state.tutorial == 3 && state.items[0] == 0) target = state.machines[0].workItem >= 0 || state.crops[0] > 0 ? machineAnchors[0] : plotAnchors[0];
            guide.position = target.position + Vector3.up * .035f;
            guide.localScale = Vector3.one * (state.simpleAnimation ? 1 : 1 + .035f * Mathf.Sin(Time.unscaledTime * 2));
        }
    }
}
