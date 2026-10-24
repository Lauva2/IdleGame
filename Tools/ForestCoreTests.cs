using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ForestShop.Core;

public static class ForestCoreTests
{
    static readonly TestCodec Codec = new TestCodec();
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static GameState Copy(GameState s) => Codec.Decode(Codec.Encode(s));
    static GameState EndOfChapter()
    {
        var s = new GameState { tutorial = 4, welcomeRead = true, coins = 20000, reputation = 150, stories = 5, sales = 60000, sideRoom = true, chapterComplete = true, chapterCelebrated = true, warehouse = 3 };
        s.plots = new[] { new Facility() }; s.machines = new[] { new Facility(), new Facility() };
        for (int i = 0; i < 4; i++) s.crops[i] = 30;
        for (int i = 0; i < 6; i++) s.sold[i] = 1;
        s.items[3] = 2; s.items[4] = 3;
        return s;
    }
    static GameState Nursery()
    {
        var s = EndOfChapter(); var e = new Economy(s);
        Check(e.Forest.Start() && e.Forest.Gather(0) && e.Forest.Repair(0), "nursery setup uses actual actions");
        return s;
    }
    static void Wrapped(string path, GameState state, int revision)
    {
        string payload = Codec.Encode(state), hash;
        using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-", "");
        File.WriteAllText(path, "FORESTSHOP1\n" + revision + "\n" + hash + "\n" + payload, new UTF8Encoding(false));
    }
    static GameState Legacy(GameState s)
    {
        s.version = 1; s.forest = null;
        Array.Resize(ref s.crops, 4); Array.Resize(ref s.items, 6); Array.Resize(ref s.sold, 6);
        Array.Resize(ref s.report.cropDelta, 4); Array.Resize(ref s.report.itemDelta, 6); return s;
    }
    static Order ForestOrder(GameState s)
    { foreach (var o in s.orders) if (o.forestStory) return o; throw new Exception("forest request missing"); }
    public static void Run(Action<string, Action> test)
    {
        test("v1 actual Android save migrates without altering original progress or file", () => {
            string original = "Docs/Verification/3d-d20-shopping03/ordinary-final-confirmed.save";
            byte[] bytes = File.ReadAllBytes(original); var before = Codec.Decode(Encoding.UTF8.GetString(bytes).Split(new[] { '\n' }, 4)[3]);
            string dir = Path.Combine("Tools/TestOutput", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "forest-shop.save"), bytes);
            var after = new SaveStore(dir, Codec).Load();
            Check(after.version == 3 && after.crops.Length == 7 && after.items.Length == 9 && after.sold.Length == 9, "new shape missing");
            Check(before.coins == after.coins && before.sales == after.sales && before.utcTicks == after.utcTicks && after.stories == before.stories, "ordinary progress changed on migration");
            for (int i = 0; i < 4; i++) Check(before.crops[i] == after.crops[i], "old crop changed");
            for (int i = 0; i < 6; i++) Check(before.items[i] == after.items[i] && before.sold[i] == after.sold[i], "old item changed");
            for (int i = 0; i < before.machines.Length; i++) Check(before.machines[i].remaining == after.machines[i].remaining && before.machines[i].workItem == after.machines[i].workItem, "queue reset");
            Check(!after.forest.started && after.forest.discovered == 0 && after.crops[4] == 0 && after.items[6] == 0, "migration granted new resources");
            Check(Convert.ToBase64String(File.ReadAllBytes(original)) == Convert.ToBase64String(bytes), "original evidence changed"); StateValidation.Check(after);
        });
        test("legacy pending report preserves earned income and cannot award again", () => {
            var old = Legacy(EndOfChapter()); old.report.pending = true; old.report.coins = 123; old.report.cropDelta[0] = -2; old.utcTicks = DateTime.UtcNow.Ticks;
            long coins = old.coins, stamp = old.utcTicks; StateMigration.Upgrade(old); OfflineSettlement.Resume(old, stamp + TimeSpan.FromHours(8).Ticks);
            Check(old.version == 3 && old.report.pending && old.report.coins == 123 && old.report.cropDelta[0] == -2 && old.report.itemDelta.Length == 9 && old.coins == coins && old.utcTicks == stamp, "pending report replayed or changed");
        });
        test("malformed old shape is rejected before migration and valid backup wins", () => {
            string dir = Path.Combine("Tools/TestOutput", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            var good = Legacy(EndOfChapter()); Wrapped(Path.Combine(dir, "forest-shop.save.bak"), good, 1);
            var bad = Legacy(EndOfChapter()); bad.crops = new int[3]; Wrapped(Path.Combine(dir, "forest-shop.save"), bad, 2);
            var store = new SaveStore(dir, Codec); var s = store.Load();
            Check(s.version == 3 && s.coins == good.coins && store.RecoveryMessage != null, "bad v1 was silently repaired instead of backup");
        });
        test("forest remains dormant until chapter completion and explicit entry", () => {
            var s = new GameState(); var e = new Economy(s);
            Check(!e.Forest.Start() && !e.Forest.Gather(0) && !e.Forest.Repair(0), "opened before chapter"); e.Advance(28800);
            Check(s.forest.discovered == 0 && !s.forest.started && s.crops[4] == 0 && !e.ItemUnlocked(6), "time auto-explored forest");
            s.chapterComplete = true; Check(e.Forest.Start() && !e.Forest.Start(), "entry not idempotent");
        });
        test("gather retains leftovers at a full warehouse and recovery only replenishes node", () => {
            var s = EndOfChapter(); s.warehouse = 1; var e = new Economy(s); e.Forest.Start(); s.crops[4] = 19;
            Check(e.Forest.Gather(0) && s.crops[4] == 20 && s.forest.available[0] == 2 && s.forest.regrow[0] == 0, "partial gather discarded remainder");
            string full = Codec.Encode(s); Check(!e.Forest.Gather(0) && Codec.Encode(s) == full, "full gather consumed node");
            s.crops[4] = 0; Check(e.Forest.Gather(0) && s.forest.available[0] == 0 && !e.Forest.Gather(0), "cooldown or duplicate collection wrong");
            int gathered = s.forest.gathered[0]; e.Advance(90);
            Check(s.forest.available[0] == 3 && s.crops[4] == 2 && s.forest.gathered[0] == gathered, "cooldown auto-collected inventory");
        });
        test("locked paths and repair costs are atomic and respect active reservations", () => {
            var s = Nursery(); var e = new Economy(s); s.forest.requests = 1;
            s.orders = new[] { new Order { id = 900, item = 4, count = 1, resident = 0, active = true } };
            Check(!e.Forest.Visit(1) && !e.Forest.Gather(1) && !e.Forest.CanRepair(1), "locked path or reserved warm potion consumed");
            long coins = s.coins; Check(!e.Forest.Repair(1) && s.coins == coins && s.items[3] == 2, "failed repair consumed stock");
            e.CancelOrder(); Check(e.Forest.Repair(1) && !e.Forest.Repair(1) && s.coins == coins - 500 && s.items[3] == 0 && s.items[4] == 0, "repair charged twice");
            Check(e.Forest.Visit(1) && e.Forest.Gather(1) && !e.Forest.Gather(0), "remote node gathered without travel"); StateValidation.Check(s);
        });
        test("forest request shares one active slot, pauses and survives refresh", () => {
            var s = Nursery(); var e = new Economy(s); var request = ForestOrder(s);
            Check(e.AcceptOrder(request.id) && e.Reserved(6) == 2, "reservation missing");
            foreach (var o in s.orders) if (!o.forestStory) Check(!e.AcceptOrder(o.id), "second accepted order");
            Check(e.CancelOrder() && !request.cancelled && e.RefreshOrders(), "forest request lost on pause");
            Check(ForestOrder(s).id == request.id && e.AcceptOrder(request.id), "refresh replaced persistent request");
            s.items[6] = 2; long before = s.coins;
            Check(e.DeliverOrder() && !e.DeliverOrder() && s.coins == before + 150 && s.stories == 5 && s.forest.requests == 1, "reward repeated or old stories changed");
        });
        test("nursery mature stock waits without loss and commits deferred species after room frees", () => {
            var s = Nursery(); s.forest.requests = 1; s.orders = new Order[0]; s.forest.bridge = true; s.forest.discovered = 3;
            for (int i = 1; i < 4; i++) s.forest.beds[i] = new Facility();
            var bed = s.forest.beds[0]; bed.workItem = bed.config = 4; bed.remaining = 0; s.crops[4] = 80;
            var e = new Economy(s); Check(e.Forest.ConfigureBed(0, 5), "deferred species rejected"); e.Advance(10);
            Check(bed.workItem == 4 && bed.remaining == 0 && bed.pending == 5, "mature stock lost on switch");
            Check(e.ConfigureMachine(0, 6), "tea could not free source capacity");
            Check(s.crops[4] == 79 && bed.config == 5 && bed.workItem == 5 && bed.remaining == 60, "same-time harvest/switch didn't recover"); StateValidation.Check(s);
        });
        test("new nursery and recipes preserve deterministic eight-hour online/offline results", () => {
            var a = Nursery(); a.forest.requests = 1; a.orders = new Order[0]; a.forest.bridge = true; a.forest.discovered = 3;
            var ea = new Economy(a); ea.Forest.ConfigureBed(0, 5); ea.Forest.ConfigureBed(1, 5);
            a.plots = new[] { new Facility { config = 0 }, new Facility { config = 3 } };
            ea.ConfigureMachine(0, 6); ea.ConfigureMachine(1, 7); ea.ConfigureShelf(0, 6);
            a.shelves = new[] { a.shelves[0], new Facility { config = 7 } };
            var b = Copy(a); var eb = new Economy(b); int beforeGather = a.forest.gathered[0];
            ea.Advance(28800); for (int i = 0; i < 28800; i++) eb.Advance(1);
            Check(a.coins == b.coins && a.sales == b.sales && a.harvests == b.harvests && a.crafts == b.crafts, "new economy totals disagree");
            for (int i = 0; i < 7; i++) Check(a.crops[i] == b.crops[i], "new crop inventory differs");
            for (int i = 0; i < 9; i++) Check(a.items[i] == b.items[i] && a.sold[i] == b.sold[i], "new output inventory differs");
            Check(a.forest.gathered[0] == beforeGather && a.forest.available[0] == b.forest.available[0], "time granted exploration"); StateValidation.Check(a); StateValidation.Check(b);
        });
        test("complete expansion follows real harvest/craft actions without new-resource gifts", () => {
            var s = EndOfChapter(); var e = new Economy(s); Check(e.Forest.Start() && e.Forest.Gather(0) && e.Forest.Repair(0), "initial chain");
            e.ConfigureMachine(0, 6); e.ConfigureShelf(0, 6); Check(e.AcceptOrder(ForestOrder(s).id), "first forest request"); e.Advance(150);
            Check(s.items[6] >= 2 && e.DeliverOrder(), "berry request unreachable"); s.forest.dialogue = -1;
            Check(e.Forest.Repair(1) && e.Forest.Visit(1) && e.Forest.Gather(1), "bridge/creek unreachable");
            e.Forest.ConfigureBed(0, 5); e.Forest.ConfigureBed(1, 5); e.ConfigureMachine(1, 7); Check(e.AcceptOrder(ForestOrder(s).id), "second request missing"); e.Advance(180);
            Check(s.items[7] >= 2 && e.DeliverOrder(), "spray request unreachable"); s.forest.dialogue = -1; e.Advance(180);
            Check(e.Forest.Repair(2) && e.Forest.Visit(2) && e.Forest.Gather(2), "vines/resin unreachable");
            e.ConfigureMachine(1, 8); Check(e.AcceptOrder(ForestOrder(s).id), "third request missing"); e.Advance(240);
            Check(e.DeliverOrder(), "balm request unreachable"); s.forest.dialogue = -1;
            Check(e.Forest.Gather(2), "resin recovery unreachable"); e.Advance(160);
            Check(e.Forest.Repair(3) && !e.Forest.Repair(3) && s.forest.pavilion && s.forest.requests == 3 && s.stories == 5, "pavilion or original chapter corrupted");
            StateValidation.Check(s); Console.WriteLine("  expansion complete with normal new-resource actions, simulated seconds=" + s.playedSeconds);
        });
    }
}
