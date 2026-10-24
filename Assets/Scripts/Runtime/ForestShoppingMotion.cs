using UnityEngine;
using ForestShop.Core;
using static ForestShop.ForestGeometry;

namespace ForestShop
{
    // A visual receipt of a real sale, never a second inventory or economy.
    // Each visit keeps one continuous product through shelf, hand and open bag.
    public sealed class ForestShoppingMotion
    {
        sealed class Visit
        {
            public Transform item, bag;
            public Transform[] products = new Transform[Catalog.Items.Length];
            public float lastPhase = -1, pickup = -1;
            public int shelf = -1, product = -1;
            public Vector3 source;
            public Quaternion rotation;
        }
        readonly Visit[] visits = new Visit[5];
        readonly float[] soldAt = { -1000, -1000, -1000, -1000 };
        readonly int[] soldItem = { -1, -1, -1, -1 };
        readonly Transform shop, checkout;
        readonly ForestResidentMotion[] residents;
        readonly Transform[] display;
        static readonly Vector3 ProductGrip = new Vector3(0, .02f, -.25f);
        static readonly Vector3 BagGrip = new Vector3(0, .32f, 0);
        static readonly Vector3 FlatBag = new Vector3(-3.98f, 5.225f, -1.72f);
        static readonly Vector3 CounterItem = new Vector3(-3.98f, 5.38f, -1.65f);
        static readonly Quaternion FlatRotation = Quaternion.Euler(-90, 0, 0);
        public int HeldProducts { get; private set; }

        public ForestShoppingMotion(Transform shop, Transform checkout, ForestResidentMotion[] residents, Transform[] display)
        {
            this.shop = shop; this.checkout = checkout; this.residents = residents; this.display = display;
            for (int i = 0; i < visits.Length; i++)
            {
                var v = visits[i] = new Visit();
                v.item = Group(shop, "Purchased product " + i); v.item.localScale = Vector3.one * .56f;
                for (int j = 0; j < Catalog.Items.Length; j++) { v.products[j] = ForestModels.Product(v.item, j); v.products[j].gameObject.SetActive(false); }
                v.bag = Group(shop, "Open paper shopping bag " + i); v.bag.localScale = Vector3.one * .72f;
                Box(v.bag, "Bag front", new Vector3(0, -.1f, -.215f), new Vector3(.5f, .5f, .025f), Cream);
                Box(v.bag, "Bag back", new Vector3(0, -.1f, .215f), new Vector3(.5f, .5f, .025f), Cream);
                for (int s = -1; s <= 1; s += 2)
                {
                    Box(v.bag, "Folded side", new Vector3(s * .25f, -.1f, 0), new Vector3(.025f, .5f, .43f), Hex(0xE6D6B1));
                    Ring(v.bag, "Paper handle", new Vector3(0, .17f, s * .16f), .15f, .018f, Wood, new Vector3(90, 0, 0));
                }
                Box(v.bag, "Bag base", new Vector3(0, -.35f, 0), new Vector3(.5f, .025f, .43f), Honey);
                Shape(v.bag, "Shop leaf seal", OakLeaf(), new Vector3(0, -.08f, -.232f), new Vector3(.26f, .31f, .2f), Leaf, new Vector3(90, 0, 0));
                Combine(v.bag); v.item.gameObject.SetActive(false); v.bag.gameObject.SetActive(false);
            }
        }

        public void RecordSale(ProductionNotice notice)
        {
            if (notice.Kind != ProductionKind.Sale || notice.Facility < 0 || notice.Facility >= 4) return;
            soldAt[notice.Facility] = Time.unscaledTime; soldItem[notice.Facility] = notice.Item;
        }
        public void Clear()
        {
            foreach (var v in visits) { v.pickup = v.lastPhase = -1; v.product = v.shelf = -1; v.item.gameObject.SetActive(false); v.bag.gameObject.SetActive(false); }
            for (int i = 0; i < 4; i++) { soldAt[i] = -1000; soldItem[i] = -1; }
            foreach (var t in display) if (t != null) t.gameObject.SetActive(true);
        }
        public void BeginFrame()
        {
            HeldProducts = 0;
            foreach (var t in display) if (t != null) t.gameObject.SetActive(true);
        }
        static float Ease(float from, float to, float phase) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, phase));
        Vector3 CheckoutPoint(Vector3 legacyShopPoint) => checkout.TransformPoint(legacyShopPoint - new Vector3(-3.7f, 4.64f, -1.2f));
        Vector3 Grip(Transform item, Vector3 position, Quaternion rotation, Vector3 local)
            => position + rotation * Vector3.Scale(local, item.lossyScale);
        void ProductAt(Visit v, Vector3 position, Quaternion rotation)
            => v.item.SetPositionAndRotation(position, rotation);
        void ProductInBag(Visit v)
        {
            v.item.position = v.bag.TransformPoint(new Vector3(0, -.335f, 0));
            v.item.rotation = v.bag.rotation;
        }

        public void Pose(int index, float phase, int shelf, bool active, bool simple)
        {
            var v = visits[index]; var motion = residents[index];
            if (phase < v.lastPhase) { v.pickup = -1; v.product = v.shelf = -1; }
            v.lastPhase = phase;
            v.item.gameObject.SetActive(false); v.bag.gameObject.SetActive(active && !simple);
            if (!active || simple) { v.pickup = -1; return; }
            v.bag.SetPositionAndRotation(CheckoutPoint(FlatBag), checkout.rotation * FlatRotation);
            float start = Time.unscaledTime - phase;
            if (v.pickup < 0 && phase >= 20.5f && phase < 25 && soldAt[shelf] >= start && soldItem[shelf] >= 0)
            {
                v.pickup = Mathf.Max(21.2f, phase + .55f); v.product = soldItem[shelf]; v.shelf = shelf;
                var source = display[shelf * Catalog.Items.Length + v.product];
                v.source = source.position; v.rotation = source.rotation;
                for (int j = 0; j < Catalog.Items.Length; j++) v.products[j].gameObject.SetActive(j == v.product);
            }
            if (v.pickup < 0) return;
            if (phase < v.pickup)
            {
                motion.AimRightHand(Grip(v.item, v.source, v.rotation, ProductGrip), Ease(v.pickup - .55f, v.pickup, phase));
                return;
            }
            display[v.shelf * Catalog.Items.Length + v.product].gameObject.SetActive(false);
            v.item.gameObject.SetActive(true); HeldProducts++;
            Vector3 carry = motion.HandTarget(new Vector3(.45f, .83f, -.43f));
            Quaternion carried = motion.transform.rotation * Quaternion.Euler(0, 180, 0);
            if (phase < 34)
            {
                float t = Ease(v.pickup, v.pickup + .9f, phase);
                Vector3 sourceGrip = Grip(v.item, v.source, v.rotation, ProductGrip);
                motion.AimRightHand(Vector3.Lerp(sourceGrip, carry, t), 1);
                Quaternion rotation = Quaternion.Slerp(v.rotation, carried, t);
                ProductAt(v, motion.RightHand - rotation * Vector3.Scale(ProductGrip, v.item.lossyScale), rotation);
            }
            else if (phase < 35.2f)
            {
                motion.LeanOverCounter(Ease(33.8f, 34.6f, phase));
                Vector3 table = CheckoutPoint(CounterItem);
                Quaternion rotation = Quaternion.Slerp(carried, checkout.rotation, Ease(34, 34.6f, phase));
                motion.AimRightHand(Vector3.Lerp(carry, Grip(v.item, table, rotation, ProductGrip), Ease(34, 35, phase)), 1);
                ProductAt(v, motion.RightHand - rotation * Vector3.Scale(ProductGrip, v.item.lossyScale), rotation);
            }
            else if (phase < 36.2f)
            {
                motion.LeanOverCounter(1);
                float t = Ease(35.2f, 36.2f, phase);
                Vector3 mouth = v.bag.TransformPoint(new Vector3(0, .14f, 0)) + Vector3.up * .09f;
                Vector3 inside = v.bag.TransformPoint(new Vector3(0, -.335f, 0));
                Vector3 position = t < .4f ? Vector3.Lerp(CheckoutPoint(CounterItem), mouth, t / .4f) : Vector3.Lerp(mouth, inside, (t - .4f) / .6f);
                Quaternion rotation = Quaternion.Slerp(checkout.rotation, v.bag.rotation, Mathf.Clamp01(t / .4f));
                Vector3 hand = Grip(v.item, position, rotation, ProductGrip);
                Vector3 next = v.bag.TransformPoint(BagGrip);
                motion.AimRightHand(Vector3.Lerp(hand, next, Mathf.Clamp01((t - .4f) / .35f)), 1);
                ProductAt(v, position, rotation);
            }
            else
            {
                motion.LeanOverCounter(1 - Ease(36.5f, 37.6f, phase));
                float lift = Ease(36.2f, 36.9f, phase), settle = Ease(36.9f, 37.6f, phase);
                Vector3 sourceGrip = v.bag.TransformPoint(BagGrip);
                Vector3 raised = motion.HandTarget(new Vector3(.5f, 1.36f, -.12f));
                Vector3 held = motion.HandTarget(new Vector3(.52f, .92f, -.12f));
                motion.AimRightHand(Vector3.Lerp(Vector3.Lerp(sourceGrip, raised, lift), held, settle), 1);
                Quaternion rotation = Quaternion.Slerp(checkout.rotation * FlatRotation, checkout.rotation, lift);
                if (phase >= 38) rotation = Quaternion.Slerp(checkout.rotation, shop.rotation, Ease(38, 39.5f, phase));
                v.bag.SetPositionAndRotation(motion.RightHand - rotation * Vector3.Scale(BagGrip, v.bag.lossyScale), rotation);
                ProductInBag(v);
            }
        }
    }
}
