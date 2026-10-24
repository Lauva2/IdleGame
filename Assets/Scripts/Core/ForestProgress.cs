using System;

namespace ForestShop.Core
{
    [Serializable]
    public sealed class ForestProgress
    {
        public bool started, greenhouse, bridge, vines, pavilion, celebrationSeen;
        public int discovered, visited, location, requests;
        public int dialogue = -1;
        public Facility[] beds = new Facility[0];
        public int[] available = { 3, 3, 3 };
        public int[] gathered = new int[3];
        public double[] regrow = new double[3];
    }

    // Player actions and deterministic timers only. No clock, rendering, or gifts
    // on loading. Discovery records unlock renewable species rather than seeds
    // that can be lost when the ordinary material warehouse is full.
    public sealed class ForestRules
    {
        public static readonly string[] Zones = { "林缘小径", "溪边湿地", "苔石林地" };
        public static readonly string[] Discoveries = { "野莓苗", "露铃草种苗", "松脂" };
        public static readonly string[] RequestTitles = { "园丁的林间茶歇", "邮差的露水路标", "老师的旧亭心愿" };
        public static readonly int[] RequestResidents = { 1, 3, 0 };
        public static readonly int[] RequestCount = { 2, 2, 2 };
        public static readonly int[] RequestCoins = { 150, 220, 300 };
        public static readonly string[] RequestBefore = {
            "这株野莓很适合温室。等它结出果子，可以请我喝两杯莓香茶吗？木桥那边也藏着好种苗呢。",
            "溪边的露铃草能留住水光。两瓶清露喷雾，可以让旧路标在林荫里重新清楚起来。",
            "苔石林地里有座旧亭，是邻居们从前读书的地方。请做两盒护木香膏，我们一起把它照顾好。" };
        public static readonly string[] RequestAfter = {
            "茶里有刚熟的野莓香。准备些避雨药水和暖炉药剂，我们就能修好溪边的小木桥了。",
            "露水让路标亮起来了！再备两瓶喷雾，藤蔓也会舒展叶子，为你让出通向苔石林地的路。",
            "护木的香气，像小时候读过的故事。再备好修缮物资，林间旧亭很快就能重新迎接邻居。" };
        public static readonly int[] Yield = { 3, 3, 3 };
        public static readonly double[] RegrowSeconds = { 90, 120, 180 };
        public static readonly long[] RepairCoins = { 800, 500, 0, 1200 };
        public static readonly int[][] RepairItems = {
            new[] { 0, 0, 0, 0, 2, 0, 0, 0, 0 },
            new[] { 0, 0, 0, 2, 1, 0, 0, 0, 0 },
            new[] { 0, 0, 0, 0, 0, 0, 0, 2, 0 },
            new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2 } };
        readonly Economy economy;
        GameState S => economy.State;
        ForestProgress F => S.forest;
        public ForestRules(Economy owner) { economy = owner; }
        public bool Discovered(int node) => node >= 0 && node < 3 && (F.discovered & (1 << node)) != 0;
        public bool CanVisit(int zone) => F.started && zone >= 0 && zone < 3 && (zone == 0 || zone == 1 && F.bridge || zone == 2 && F.vines);
        public bool CanGrow(int crop) => F.greenhouse && (crop == 4 && Discovered(0) || crop == 5 && Discovered(1));
        public bool RecipeUnlocked(int item) => F.greenhouse && item >= 6 && item < 9 && Discovered(item - 6);
        public bool RequestReady => F.started && F.requests < 3 && economy.ItemUnlocked(6 + F.requests);
        public string Goal
        {
            get
            {
                if (!S.chapterComplete) return "开张茶会之后，和园丁一起走进森林";
                if (!F.started) return "庭院后的林径开放了，去看看园丁的新发现";
                if (!Discovered(0)) return "观察林缘的野莓苗，采集第一份森林发现";
                if (!F.greenhouse) return "用暖炉药剂修复温室，带回的野莓可以长期培育";
                if (F.requests == 0) return "温室培育野莓，制作两杯莓香茶交给园丁";
                if (!F.bridge) return "备好避雨药水与暖炉药剂，修好溪边木桥";
                if (!Discovered(1)) return "沿木桥走到溪边，寻找露铃草种苗";
                if (F.requests == 1) return "培育露铃草，给邮差准备两瓶清露喷雾";
                if (!F.vines) return "用两瓶清露喷雾唤醒藤蔓，打开苔石小径";
                if (!Discovered(2)) return "观察苔石林地的松树，收集温润的松脂";
                if (F.requests == 2) return "制作两盒护木香膏，完成老师的旧亭心愿";
                if (!F.pavilion) return "备好金币和两盒护木香膏，修复林间旧亭";
                return "林间旧亭已修复，森林与小店一起慢慢生长";
            }
        }
        public bool Start()
        {
            if (!S.chapterComplete || F.started) return false;
            F.started = true; F.visited = 1; F.location = 0; economy.Advance(0); return true;
        }
        public bool Visit(int zone)
        { if (!CanVisit(zone)) return false; F.location = zone; F.visited |= 1 << zone; return true; }
        public bool Gather(int node)
        {
            if (!CanVisit(node) || F.location != node || F.regrow[node] > .0000001 || F.available[node] <= 0) return false;
            int crop = node + 4, count = Math.Min(F.available[node], economy.Capacity - S.crops[crop]);
            if (count <= 0) return false;
            S.crops[crop] += count; S.harvests += count;
            F.available[node] -= count; F.gathered[node] += count; F.discovered |= 1 << node;
            if (F.available[node] == 0) F.regrow[node] = RegrowSeconds[node];
            economy.Notify(new ProductionNotice(ProductionKind.Forage, node, crop, count));
            economy.Advance(0); return true;
        }
        public bool IsRepaired(int id) => id == 0 ? F.greenhouse : id == 1 ? F.bridge : id == 2 ? F.vines : id == 3 && F.pavilion;
        public bool RepairPrerequisite(int id)
        {
            if (!F.started || id < 0 || id > 3 || IsRepaired(id)) return false;
            return id == 0 ? Discovered(0) : id == 1 ? F.greenhouse && F.requests >= 1 : id == 2 ? F.bridge && F.requests >= 2 : F.vines && F.requests >= 3;
        }
        public bool CanRepair(int id)
        {
            if (!RepairPrerequisite(id) || S.coins < RepairCoins[id]) return false;
            for (int i = 0; i < Catalog.Items.Length; i++)
                if (S.items[i] - economy.Reserved(i) < RepairItems[id][i]) return false;
            return true;
        }
        public bool Repair(int id)
        {
            if (!CanRepair(id)) return false;
            S.coins -= RepairCoins[id];
            for (int i = 0; i < Catalog.Items.Length; i++) S.items[i] -= RepairItems[id][i];
            if (id == 0)
            {
                F.greenhouse = true; F.beds = new Facility[4];
                for (int i = 0; i < 4; i++) F.beds[i] = new Facility { config = 4 };
            }
            else if (id == 1) F.bridge = true;
            else if (id == 2) F.vines = true;
            else F.pavilion = true;
            economy.Advance(0); return true;
        }
        public bool ConfigureBed(int index, int crop)
        {
            if (!F.greenhouse || index < 0 || index >= F.beds.Length || !CanGrow(crop)) return false;
            var bed = F.beds[index];
            if (bed.remaining >= 0 && bed.workItem >= 0) bed.pending = crop == bed.config ? -2 : crop;
            else { bed.config = crop; bed.pending = -2; }
            economy.Advance(0); return true;
        }
        internal void Tick(double seconds)
        {
            if (!F.started) return;
            for (int i = 0; i < 3; i++)
                if (F.regrow[i] > 0)
                {
                    F.regrow[i] = Math.Max(0, F.regrow[i] - seconds);
                    if (F.regrow[i] <= .0000001) { F.regrow[i] = 0; F.available[i] = Yield[i]; }
                }
        }
    }
}
