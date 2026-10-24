using System;
using System.Collections.Generic;

namespace ForestShop.Core
{
    // Pure deterministic rules: no Unity API, wall clock, rendering or random global state.
    public sealed class Economy
    {
        public readonly GameState State;
        public event Action<ProductionNotice> ProductionCompleted;
        private const double Epsilon = 0.0000001;
        public Economy(GameState state) { State = state ?? throw new ArgumentNullException(nameof(state)); }
        public int Capacity => Catalog.Capacity[State.warehouse - 1];
        public long TotalSold { get { long n = 0; foreach (int v in State.sold) n += v; return n; } }
        public bool CropUnlocked(int id) => id == 0 || (id == 1 ? State.machines.Length > 1 : State.reputation >= (id == 2 ? 25 : 45));
        public bool ItemUnlocked(int id) => id >= 0 && id < 6 && State.reputation >= Catalog.Unlock[id] && State.machines.Length > Catalog.Machine[id];
        public Order ActiveOrder { get { foreach (var o in State.orders) if (o.active) return o; return null; } }
        public int Reserved(int item) { var o = ActiveOrder; return o != null && o.reserve && o.item == item ? o.count : 0; }
        public int Incoming(int item) { int count = 0; foreach (var m in State.machines) if (m.workItem == item && m.remaining >= 0) count++; return count; }
        public string Goal
        {
            get
            {
                if (State.tutorial == 0) return "选择庭院，点按成熟的薄荷开始营业";
                if (State.tutorial == 1) return "打开魔法坩埚，选择循环制作醒神茶";
                if (State.tutorial == 2) return "打开货架，选择自动售卖醒神茶";
                if (State.tutorial == 3) return "等待第一笔销售，再到委托板看看老师的请求";
                if (State.stories == 0) return "为老师预留三杯茶，主动交付第一份委托";
                if (State.machines.Length == 1) return "积攒 260 金币，修复附魔工作台";
                if (State.stories < 5) return "推进熟客故事：" + Catalog.Stories[State.stories];
                if (!State.sideRoom) return "声望 100、销售额 15000 后，花 5000 金币修复侧屋";
                if (!State.chapterComplete) return "筹备茶会：六种商品售出、声望 150、销售额 60000";
                return "第一章已完成，邻居们依然期待你的奇物";
            }
        }

        public void Advance(double seconds)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0 || seconds > 28800.001) throw new ArgumentOutOfRangeException(nameof(seconds));
            double left = seconds;
            int guard = 0;
            while (true)
            {
                Resolve();
                if (left <= Epsilon) break;
                if (++guard > 1000000) throw new InvalidOperationException("Simulation event limit exceeded");
                double step = left;
                FindNext(State.plots, ref step);
                FindNext(State.machines, ref step);
                FindNext(State.shelves, ref step);
                Tick(State.plots, step); Tick(State.machines, step); Tick(State.shelves, step);
                State.playedSeconds += step;
                State.refreshSeconds = Math.Max(0, State.refreshSeconds - step);
                left = Math.Max(0, left - step);
            }
        }

        private static void FindNext(Facility[] fs, ref double step)
        { foreach (var f in fs) if (f.remaining > Epsilon) step = Math.Min(step, f.remaining); }
        private static void Tick(Facility[] fs, double step)
        { foreach (var f in fs) if (f.remaining > Epsilon) f.remaining = Math.Max(0, f.remaining - step); }
        private static void ApplyPending(Facility f) { if (f.pending != -2) { f.config = f.pending; f.pending = -2; } }

        private void Resolve()
        {
            // Input consumption can free a full crop slot at this same timestamp.
            // Settle again before advancing time so frame-sized and offline calls
            // resume waiting harvests and planting at the identical instant.
            // Each extra pass starts at least one positive-duration machine cycle,
            // so the number of machines bounds the additional passes.
            for (int pass = 0; pass <= State.machines.Length; pass++)
                if (!ResolveProduction()) break;
            UpdateOrders();
            bool allSold = true;
            foreach (var n in State.sold) if (n < 1) allSold = false;
            if (allSold && State.stories == 5 && State.sideRoom && State.reputation >= 150 && State.sales >= 60000)
                State.chapterComplete = true;
        }

        private bool ResolveProduction()
        {
            bool consumed = false;
            // Fixed ordering at identical timestamps: all deposits, sales, then starts.
            if (State.tutorial > 0)
                for (int index = 0; index < State.plots.Length; index++)
                {
                    var p = State.plots[index];
                    if (p.remaining >= 0 && p.remaining <= Epsilon && p.workItem >= 0 && State.crops[p.workItem] < Capacity)
                    {
                        int crop = p.workItem;
                        State.crops[crop]++; State.harvests++; p.remaining = -1; p.workItem = -1; ApplyPending(p);
                        ProductionCompleted?.Invoke(new ProductionNotice(ProductionKind.Harvest, index, crop, 1));
                    }
                }
            for (int index = 0; index < State.machines.Length; index++)
            {
                var m = State.machines[index];
                if (m.remaining >= 0 && m.remaining <= Epsilon && m.workItem >= 0)
                {
                    int item = m.workItem;
                    State.items[item]++; State.crafts++; m.remaining = -1; m.workItem = -1; ApplyPending(m);
                    ProductionCompleted?.Invoke(new ProductionNotice(ProductionKind.Craft, index, item, 1));
                }
            }
            for (int index = 0; index < State.shelves.Length; index++)
            {
                var shelf = State.shelves[index];
                if (shelf.remaining < 0) shelf.remaining = 8 / Catalog.Speed[shelf.level - 1];
                if (shelf.remaining > Epsilon) continue;
                int item = shelf.config;
                if (item >= 0 && State.items[item] > Reserved(item))
                {
                    State.items[item]--; State.sold[item]++;
                    State.coins += Catalog.Price[item]; State.sales += Catalog.Price[item];
                    if (State.tutorial == 3) State.tutorial = 4;
                    ProductionCompleted?.Invoke(new ProductionNotice(ProductionKind.Sale, index, item, Catalog.Price[item]));
                }
                ApplyPending(shelf);
                shelf.remaining = 8 / Catalog.Speed[shelf.level - 1];
            }
            if (State.tutorial > 0)
                foreach (var p in State.plots)
                    if (p.remaining < 0)
                    {
                        ApplyPending(p);
                        if (p.config >= 0 && State.crops[p.config] < Capacity)
                        { p.workItem = p.config; p.remaining = Catalog.CropSeconds[p.config] / Catalog.Speed[p.level - 1]; }
                    }
            for (int index = 0; index < State.machines.Length; index++)
            {
                var m = State.machines[index];
                if (m.remaining >= 0) continue;
                ApplyPending(m);
                int item = m.config;
                if (item < 0 || State.items[item] + Incoming(item) >= Capacity) continue;
                bool enough = true;
                for (int c = 0; c < 4; c++) if (State.crops[c] < Catalog.Materials[item][c]) enough = false;
                if (!enough) continue;
                for (int c = 0; c < 4; c++) State.crops[c] -= Catalog.Materials[item][c];
                m.workItem = item; m.remaining = Catalog.ItemSeconds[item] / Catalog.Speed[m.level - 1];
                consumed = true;
            }
            return consumed;
        }

        public bool HarvestFirst()
        {
            if (State.tutorial != 0) return false;
            State.tutorial = 1; Advance(0); return true;
        }
        public bool ConfigurePlot(int index, int crop)
        {
            if (index < 0 || index >= State.plots.Length || crop < 0 || crop > 3 || !CropUnlocked(crop) || State.tutorial == 0) return false;
            Configure(State.plots[index], crop); Advance(0); return true;
        }
        public bool ConfigureMachine(int index, int item)
        {
            if (index < 0 || index >= State.machines.Length || !ItemUnlocked(item) || Catalog.Machine[item] != index || State.tutorial == 0) return false;
            Configure(State.machines[index], item);
            if (State.tutorial == 1 && index == 0) State.tutorial = 2;
            Advance(0); return true;
        }
        public bool ConfigureShelf(int index, int item)
        {
            if (index < 0 || index >= State.shelves.Length || !ItemUnlocked(item) || State.tutorial < 2) return false;
            // Shelves hold no product and switch their sale selection immediately; current interval is retained.
            State.shelves[index].config = item;
            if (State.tutorial == 2) State.tutorial = 3;
            Advance(0); return true;
        }
        private static void Configure(Facility f, int item)
        {
            if (f.remaining >= 0 && f.workItem >= 0) f.pending = item == f.config ? -2 : item;
            else { f.config = item; f.pending = -2; }
        }
        public void CancelSwitch(Facility f) { f.pending = -2; }
        public bool Upgrade(Facility f)
        {
            if (f == null || f.level >= 5 || State.coins < Catalog.Upgrade[f.level]) return false;
            State.coins -= Catalog.Upgrade[f.level]; f.level++; return true;
        }
        public bool UpgradeWarehouse()
        {
            if (State.warehouse >= 4 || State.coins < Catalog.WarehouseCost[State.warehouse]) return false;
            State.coins -= Catalog.WarehouseCost[State.warehouse]; State.warehouse++; Advance(0); return true;
        }
        public string PlotRequirement
        {
            get
            {
                int n = State.plots.Length;
                if (n >= Catalog.MaxPlots) return "十二块田地已全部开放";
                return n == 1 ? "累计售出 3 件商品" : n >= 4 ? "先修复侧屋" : "声望达到 " + Catalog.PlotRep[n];
            }
        }
        public bool BuyPlot()
        {
            int n = State.plots.Length;
            if (n >= Catalog.MaxPlots || (n == 1 && TotalSold < 3) || (n >= 4 && !State.sideRoom) || State.reputation < Catalog.PlotRep[n] || State.coins < Catalog.PlotCost[n]) return false;
            State.coins -= Catalog.PlotCost[n]; var p = new Facility { config = 0 };
            var plots = State.plots; Array.Resize(ref plots, n + 1); plots[n] = p; State.plots = plots; Advance(0); return true;
        }
        public bool RepairWorkbench()
        {
            if (State.machines.Length > 1 || State.reputation < 10 || State.coins < 260) return false;
            State.coins -= 260; State.machines = new[] { State.machines[0], new Facility() }; UpdateOrders(); return true;
        }
        public bool BuyShelf()
        {
            int n = State.shelves.Length;
            if (n >= 4 || State.machines.Length < 2 || (n == 2 && State.reputation < 60) || (n == 3 && !State.sideRoom) || State.coins < Catalog.ShelfCost[n]) return false;
            State.coins -= Catalog.ShelfCost[n]; var shelves = State.shelves; Array.Resize(ref shelves, n + 1);
            shelves[n] = new Facility { remaining = 8 }; State.shelves = shelves; return true;
        }
        public bool RepairSideRoom()
        {
            if (State.sideRoom || State.reputation < 100 || State.sales < 15000 || State.coins < 5000) return false;
            State.coins -= 5000; State.sideRoom = true; Advance(0); return true;
        }
        public bool DecorationUnlocked(int index) => (index == 0 || index == 3) ? State.chapterComplete : (index == 1 ? State.stories >= 2 : State.stories >= 5);
        public bool ToggleDecoration(int index)
        { if (index < 0 || index > 3 || !DecorationUnlocked(index)) return false; State.decoration[index] = !State.decoration[index]; return true; }

        private uint NextRandom() { State.random = State.random * 1664525u + 1013904223u; return State.random; }
        private void UpdateOrders()
        {
            if (State.tutorial < 4) return;
            var orders = new List<Order>(State.orders);
            if (State.stories < 5 && ItemUnlocked(Catalog.StoryItem[State.stories]))
            {
                bool exists = false;
                foreach (var o in orders) if (o.story) exists = true;
                if (!exists)
                {
                    if (orders.Count >= 3)
                    {
                        for (int i = orders.Count - 1; i >= 0; i--) if (!orders[i].active && !orders[i].story) { orders.RemoveAt(i); break; }
                    }
                    orders.Insert(0, new Order { id = State.nextOrderId++, story = true, resident = State.stories, item = Catalog.StoryItem[State.stories], count = Catalog.StoryCount[State.stories] });
                }
            }
            if (State.stories > 0)
                while (orders.Count < 3)
                {
                    var options = new List<int>(); for (int i = 0; i < 6; i++) if (ItemUnlocked(i)) options.Add(i);
                    var o = new Order { id = State.nextOrderId++, resident = (int)(NextRandom() % (uint)Math.Min(5, State.stories + 1)) };
                    // Only residents who have appeared: a pending story introduces its resident.
                    bool introduced = State.stories < 5 && ItemUnlocked(Catalog.StoryItem[State.stories]);
                    if (!introduced && o.resident == State.stories) o.resident = 0;
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        o.item = options[(int)(NextRandom() % (uint)options.Count)]; o.count = 3 + (int)(NextRandom() % 6);
                        bool duplicate = false;
                        foreach (var other in orders) if (other.item == o.item && other.count == o.count) duplicate = true;
                        if (!duplicate) break;
                    }
                    orders.Add(o);
                }
            State.orders = orders.ToArray();
        }
        public int OrderCoins(Order o) => o.story ? Catalog.StoryCoins[o.resident] : (int)Math.Floor(o.count * Catalog.Price[o.item] * 1.35 + 0.5);
        public int OrderReputation(Order o) => o.story ? Catalog.StoryRep[o.resident] : 6;
        public bool AcceptOrder(int id)
        {
            if (ActiveOrder != null) return false;
            foreach (var o in State.orders) if (o.id == id && !o.cancelled) { o.active = true; o.reserve = true; return true; }
            return false;
        }
        public bool CancelOrder()
        {
            var o = ActiveOrder; if (o == null) return false;
            o.active = false;
            // Cancelled normal slots remain until the next explicit refresh; story may be resumed unchanged.
            if (!o.story) o.cancelled = true;
            return true;
        }
        public bool DeliverOrder()
        {
            var o = ActiveOrder;
            if (o == null || State.items[o.item] < o.count) return false;
            State.items[o.item] -= o.count;
            State.coins += OrderCoins(o); State.reputation += OrderReputation(o);
            State.friendship[o.resident] = Math.Min(100, State.friendship[o.resident] + (o.story ? 10 : 3));
            if (o.story) { State.stories++; State.dialogue = o.resident; }
            var orders = new List<Order>(State.orders); orders.Remove(o); State.orders = orders.ToArray();
            Advance(0); return true;
        }
        public bool RefreshOrders()
        {
            if (State.stories == 0 || State.refreshSeconds > 0) return false;
            var keep = new List<Order>(); var avoid = new List<Order>();
            foreach (var o in State.orders) { if (o.story || o.active) keep.Add(o); else avoid.Add(o); }
            State.orders = keep.ToArray(); UpdateOrders();
            foreach (var o in State.orders)
                if (!o.story && !o.active)
                    for (int attempt = 0; attempt < 30; attempt++)
                    {
                        bool same = false; foreach (var prev in avoid) if (prev.item == o.item && prev.count == o.count) same = true;
                        if (!same) break;
                        o.count = 3 + (int)(NextRandom() % 6);
                    }
            State.refreshSeconds = 1800; return true;
        }

        public string PlotStatus(Facility p)
        {
            if (State.tutorial == 0) return "薄荷成熟 · 点按收获";
            if (p.remaining == 0) return "库存已满 · 等待入库";
            if (p.remaining > 0) return "生长中 · " + Math.Ceiling(p.remaining) + "秒";
            return "库存已满 · 等待种植";
        }
        public string MachineStatus(Facility m)
        {
            if (m.remaining > 0) return "加工中 · " + Math.Ceiling(m.remaining) + "秒";
            if (m.config < 0) return "请选择循环配方";
            if (State.items[m.config] + Incoming(m.config) >= Capacity) return "商品库存已满";
            string s = "缺材料：";
            for (int i = 0; i < 4; i++) if (State.crops[i] < Catalog.Materials[m.config][i]) s += Catalog.Crops[i] + " ";
            return s;
        }
    }
}
