using System;
using System.IO;
using System.Web.Script.Serialization;
using ForestShop.Core;

public sealed class TestCodec : IStateCodec
{
    private readonly JavaScriptSerializer json = new JavaScriptSerializer();
    public string Encode(GameState s) => json.Serialize(s);
    public GameState Decode(string text) => json.Deserialize<GameState>(text);
}

public static class CoreTests
{
    static int passed;
    static readonly TestCodec codec = new TestCodec();
    static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Test(string name, Action a) { a(); passed++; Console.WriteLine("PASS " + name); }
    static GameState Clone(GameState s) => codec.Decode(codec.Encode(s));
    static GameState Running()
    {
        var s = new GameState(); var e = new Economy(s);
        e.HarvestFirst(); e.ConfigureMachine(0, 0); e.ConfigureShelf(0, 0); return s;
    }
    public static int Main(string[] args)
    {
        try
        {
            Test("first harvest and tutorial actions are idempotent", () => {
                var s = new GameState(); var e = new Economy(s); e.Advance(60);
                Check(s.crops[0] == 0, "tutorial harvested without input"); Check(e.HarvestFirst() && !e.HarvestFirst(), "harvest duplicated");
                Check(s.crops[0] == 1, "missing first crop"); Check(e.ConfigureMachine(0, 0), "recipe failed");
                Check(s.crops[0] == 0 && s.machines[0].remaining == 15, "material not consumed once");
                e.ConfigureShelf(0, 0); e.Advance(20); Check(s.coins == 62 && s.sold[0] == 1 && s.tutorial == 4, "first sale incorrect");
            });
            Test("presentation notices match settlements without changing saved state", () => {
                var a = Running(); var b = Clone(a); var observed = new Economy(a); var plain = new Economy(b);
                long harvests = a.harvests, crafts = a.crafts, sales = a.sales; int h = 0, c = 0, sold = 0;
                observed.ProductionCompleted += n => {
                    if (n.Kind == ProductionKind.Harvest) { Check(n.Facility < a.plots.Length && n.Item < 4 && n.Amount == 1, "harvest identity"); h++; }
                    else if (n.Kind == ProductionKind.Craft) { Check(n.Facility < a.machines.Length && n.Item < 6 && n.Amount == 1, "craft identity"); c++; }
                    else { Check(n.Facility < a.shelves.Length && n.Amount == Catalog.Price[n.Item], "sale identity"); sold += n.Amount; }
                };
                observed.Advance(600); plain.Advance(600);
                Check(codec.Encode(a) == codec.Encode(b), "observer altered saved economy");
                Check(h == a.harvests - harvests && c == a.crafts - crafts && sold == a.sales - sales, "notice totals differ from actual settlement");
            });
            Test("blocked and reserved production never emits false completion", () => {
                var s = new GameState { tutorial = 4, welcomeRead = true }; var e = new Economy(s); int harvest = 0, sale = 0, craft = 0;
                e.ProductionCompleted += n => { if (n.Kind == ProductionKind.Harvest) harvest++; else if (n.Kind == ProductionKind.Sale) sale++; else craft++; };
                s.crops[0] = 20; s.plots[0].remaining = 0; s.plots[0].workItem = 0; s.machines[0].config = -1; s.shelves[0].config = -1;
                e.Advance(1); Check(harvest == 0 && craft == 0 && sale == 0, "full warehouse cue");
                s.crops[0]--; e.Advance(0); Check(harvest == 1, "unblocked actual deposit cue missing");
                s.plots[0].config = s.plots[0].workItem = -1; s.plots[0].remaining = -1; s.crops[0] = 0;
                s.machines[0].config = 0; e.Advance(10); Check(craft == 0, "missing material cue");
                s.machines[0].config = -1; s.items[0] = 8; s.shelves[0].config = 0; s.shelves[0].remaining = 0;
                s.orders = new[] { new Order { id = 100, resident = 0, item = 0, count = 8, active = true, reserve = true } };
                e.Advance(0); Check(sale == 0, "reserved stock emitted sale");
                s.items[0]++; e.Advance(8); Check(sale == 1 && s.items[0] == 8, "surplus sale identity");
            });
            Test("first manual harvest notifies once", () => {
                var s = new GameState(); var e = new Economy(s); int count = 0;
                e.ProductionCompleted += n => { Check(n.Kind == ProductionKind.Harvest && n.Item == 0 && n.Facility == 0, "first harvest notice"); count++; };
                Check(e.HarvestFirst() && !e.HarvestFirst() && count == 1, "first harvest notification duplicated or absent");
            });
            foreach (int seconds in new[] { 60, 3600, 28800 })
                Test("online/offline deterministic equivalence " + seconds + " seconds", () => {
                    var a = Running(); var b = Clone(a); var ea = new Economy(a); var eb = new Economy(b);
                    for (int i = 0; i < seconds; i++) ea.Advance(1); eb.Advance(seconds);
                    Check(codec.Encode(a) == codec.Encode(b), "online/offline mismatch"); StateValidation.Check(a);
                });
            Test("full crop warehouse retains mature harvest and resumes", () => {
                var s = Running(); s.machines[0].config = -1; s.machines[0].workItem = -1; s.machines[0].remaining = -1;
                s.crops[0] = 19; new Economy(s).Advance(20); Check(s.crops[0] == 20 && s.plots[0].remaining < 0, "full crop didn't pause");
                s.crops[0]--; new Economy(s).Advance(0); Check(s.plots[0].remaining == 20, "didn't recover");
                s.crops[0] = 20; new Economy(s).Advance(20); Check(s.plots[0].remaining == 0, "mature harvest lost");
                s.crops[0]--; new Economy(s).Advance(0); Check(s.crops[0] == 20 && s.plots[0].remaining < 0, "pending harvest not deposited");
            });
            Test("machine reserves output capacity before consuming input", () => {
                var s = Running(); s.items[0] = 19; s.shelves[0].config = -1; var e = new Economy(s);
                Check(e.Incoming(0) == 1, "no incoming reservation"); e.Advance(15); Check(s.items[0] == 20 && s.machines[0].remaining < 0, "capacity overflow");
                s.items[0]--; e.Advance(5); Check(s.machines[0].remaining == 15, "didn't resume after room/input"); StateValidation.Check(s);
            });
            Test("same timestamp deposits precede sale and production starts", () => {
                var s = Running(); s.machines[0].remaining = 8; s.plots[0].remaining = 8; var e = new Economy(s); e.Advance(8);
                Check(s.coins == 62 && s.items[0] == 0 && s.crops[0] == 0 && s.machines[0].remaining == 15, "event order differs");
            });
            Test("full crop warehouse resumes on the exact machine-consumption event", () => {
                var a = Running(); a.tutorial = 4; a.crops[0] = 20;
                a.plots[0].remaining = -1; a.plots[0].workItem = -1;
                a.machines[0].remaining = 5; a.shelves[0].remaining = 8;
                var b = Clone(a); var ea = new Economy(a); var eb = new Economy(b);
                ea.Advance(6); for (int i = 0; i < 180; i++) eb.Advance(1d / 30);
                Check(Math.Abs(a.plots[0].remaining - 19) < 1e-7, "crop restart was delayed until the next event");
                Check(Math.Abs(a.plots[0].remaining - b.plots[0].remaining) < 1e-7, "frame-sized and offline advances disagree after full warehouse resumes");
                StateValidation.Check(a); StateValidation.Check(b);
            });
            Test("full upgraded multi-chain state matches fractional frame advances", () => {
                var a = Running(); a.tutorial = 4; a.reputation = 150; a.stories = 5;
                a.crops = new[] { 20, 20, 20, 3, 0, 0, 0 };
                a.plots = new[] { new Facility { config = 0, level = 3 }, new Facility { config = 1, level = 3 }, new Facility { config = 2, level = 3 }, new Facility { config = 1, level = 3 } };
                a.machines = new[] { new Facility { config = 4, level = 3, workItem = 4, remaining = 5 }, new Facility { config = 2, level = 3, workItem = 2, remaining = 7 } };
                a.shelves = new[] { new Facility { config = 4, remaining = 8 }, new Facility { config = 2, remaining = 8 } };
                var b = Clone(a); var ea = new Economy(a); var eb = new Economy(b);
                ea.Advance(3600); for (int i = 0; i < 108000; i++) eb.Advance(1d / 30);
                Check(a.coins == b.coins && a.sales == b.sales && a.harvests == b.harvests && a.crafts == b.crafts, "fractional-frame totals differ");
                for (int i = 0; i < 4; i++) Check(a.crops[i] == b.crops[i], "fractional-frame crop stock differs");
                for (int i = 0; i < 6; i++) Check(a.items[i] == b.items[i] && a.sold[i] == b.sold[i], "fractional-frame goods differ");
                var left = new System.Collections.Generic.List<Facility>(a.plots); left.AddRange(a.machines); left.AddRange(a.shelves);
                var right = new System.Collections.Generic.List<Facility>(b.plots); right.AddRange(b.machines); right.AddRange(b.shelves);
                for (int i = 0; i < left.Count; i++) Check(left[i].config == right[i].config && left[i].workItem == right[i].workItem && Math.Abs(left[i].remaining - right[i].remaining) < 1e-5, "fractional-frame facility differs: " + i);
                StateValidation.Check(a); StateValidation.Check(b);
            });
            Test("upgrade preserves active cycle", () => {
                var s = Running(); s.coins = 1000; var e = new Economy(s); double before = s.plots[0].remaining;
                Check(e.Upgrade(s.plots[0]), "upgrade failed"); Check(s.plots[0].remaining == before, "upgrade reset current cycle");
                e.Advance(20); Check(s.plots[0].remaining == 16, "new speed not applied next cycle");
            });
            Test("deferred crop/recipe switch and cancellation", () => {
                var s = Running(); s.reputation = 100; s.machines = new[] { s.machines[0], new Facility() }; var e = new Economy(s);
                e.ConfigurePlot(0, 1); Check(s.plots[0].config == 0 && s.plots[0].pending == 1, "crop switched mid growth");
                e.CancelSwitch(s.plots[0]); Check(s.plots[0].pending == -2, "cancel failed"); e.ConfigurePlot(0, 1);
                e.ConfigureMachine(0, 4); Check(s.machines[0].workItem == 0 && s.machines[0].pending == 4, "recipe switched mid processing");
                e.Advance(20); Check(s.plots[0].workItem == 1 && s.machines[0].config == 4, "switch did not commit");
                Check(!e.ConfigureMachine(0, 1) && !e.ConfigureMachine(1, 0), "wrong device accepted recipe");
            });
            Test("reservation only blocks requested product, releases atomically", () => {
                var s = Running(); var e = new Economy(s); e.Advance(16); Check(e.AcceptOrder(s.orders[0].id), "story missing");
                s.items[0] = 5; e.Advance(16); Check(s.items[0] >= 3, "reserved stock sold");
                long coins = s.coins; Check(e.DeliverOrder() && !e.DeliverOrder(), "delivery duplicated");
                Check(s.coins == coins + 50 && s.reputation == 10 && s.stories == 1 && s.friendship[0] == 10, "reward incorrect");
                Check(e.Reserved(0) == 0 && e.ActiveOrder == null, "reservation not released");
            });
            Test("only one accepted order and story pause persists", () => {
                var s = Running(); var e = new Economy(s); e.Advance(16); int id = s.orders[0].id;
                Check(e.AcceptOrder(id) && !e.AcceptOrder(id), "multiple acceptance"); Check(e.CancelOrder(), "pause failed");
                Check(s.orders[0].id == id && !s.orders[0].cancelled && e.AcceptOrder(id), "story lost on pause");
            });
            Test("offline cap and pending report prevent repeat awards", () => {
                var s = Running(); s.utcTicks = new DateTime(2026, 10, 20, 0, 0, 0, DateTimeKind.Utc).Ticks; long now = s.utcTicks + TimeSpan.FromHours(24).Ticks;
                OfflineSettlement.Resume(s, now); long coins = s.coins; double play = s.playedSeconds;
                Check(s.report.pending && s.report.effective == 28800 && s.report.away == 86400 && play == 28800, "cap wrong");
                OfflineSettlement.Resume(s, now + TimeSpan.FromHours(1).Ticks); Check(s.coins == coins && s.playedSeconds == play, "report repeated rewards");
                OfflineSettlement.CloseReport(s, now + TimeSpan.FromHours(1).Ticks); Check(s.coins == coins, "close awarded again");
                OfflineSettlement.Resume(s, s.utcTicks); Check(s.playedSeconds == play, "discarded time recovered");
            });
            Test("rollback keeps high water UTC and no negative/repeated income", () => {
                var s = Running(); s.utcTicks = DateTime.UtcNow.Ticks; long baseline = s.utcTicks;
                OfflineSettlement.Resume(s, baseline - TimeSpan.FromHours(1).Ticks); Check(s.coins == 50 && s.utcTicks == baseline, "rollback changed baseline");
                new Economy(s).Advance(60); OfflineSettlement.SaveOnlineTime(s, baseline - 100); Check(s.utcTicks == baseline, "save lowered high water");
                OfflineSettlement.Resume(s, baseline); Check(s.playedSeconds == 60, "repeated interval");
            });
            Test("checksummed saves restore valid backup and retain bad files", () => {
                string dir = Path.Combine("Tools", "TestOutput", Guid.NewGuid().ToString("N")); var store = new SaveStore(dir, codec); var s = Running();
                store.Save(s); new Economy(s).Advance(60); store.Save(s);
                Check(new SaveStore(dir, codec).Load().coins == s.coins, "save roundtrip failed");
                File.WriteAllText(Path.Combine(dir, "forest-shop.save"), "damaged"); var recover = new SaveStore(dir, codec); var restored = recover.Load();
                Check(restored.coins == 50 && recover.RecoveryMessage != null, "backup not restored"); recover.Save(restored);
                Check(new SaveStore(dir, codec).Load().coins == 50, "restored save failed");
                File.WriteAllText(Path.Combine(dir, "forest-shop.save"), "bad"); File.WriteAllText(Path.Combine(dir, "forest-shop.save.bak"), "bad");
                bool threw = false; try { new SaveStore(dir, codec).Load(); } catch (InvalidDataException) { threw = true; }
                Check(threw, "silently reset corrupted saves");
            });
            Test("verified temporary save survives interrupted final rename", () => {
                string dir = Path.Combine("Tools", "TestOutput", Guid.NewGuid().ToString("N")); var store = new SaveStore(dir, codec); var s = Running(); store.Save(s);
                File.Move(Path.Combine(dir, "forest-shop.save"), Path.Combine(dir, "forest-shop.save.tmp"));
                Check(new SaveStore(dir, codec).Load() != null, "temporary recovery failed");
            });
            Test("multi-input and reserved-stock online/offline equivalence at upgraded speeds", () => {
                var a = Running(); a.reputation = 150; a.stories = 5; a.warehouse = 3;
                a.plots = new[] { new Facility { config = 0, level = 3 }, new Facility { config = 1, level = 2 }, new Facility { config = 2, level = 4 }, new Facility { config = 3, level = 5 } };
                a.machines = new[] { new Facility { config = 4, level = 4 }, new Facility { config = 5, level = 3 } };
                a.shelves = new[] { new Facility { config = 4, level = 3, remaining = 8 }, new Facility { config = 5, level = 4, remaining = 8 } };
                a.orders = new[] { new Order { id = 99, item = 5, count = 8, resident = 2, active = true, reserve = true } };
                for (int i = 0; i < 4; i++) a.crops[i] = 17;
                a.items[4] = 79; a.items[5] = 7;
                var b = Clone(a); var ea = new Economy(a); var eb = new Economy(b);
                for (int i = 0; i < 28800; i++) ea.Advance(1); eb.Advance(28800);
                Check(a.coins == b.coins && a.sales == b.sales, "multi-chain income mismatch");
                for (int i = 0; i < 4; i++) Check(a.crops[i] == b.crops[i], "multi-chain crop mismatch");
                for (int i = 0; i < 6; i++) Check(a.items[i] == b.items[i] && a.sold[i] == b.sold[i], "multi-chain item mismatch");
                for (int i = 0; i < 2; i++) Check(Math.Abs(a.machines[i].remaining - b.machines[i].remaining) < .00001, "cycle precision mismatch");
                Check(a.items[5] >= 8, "reserved goods sold"); StateValidation.Check(a); StateValidation.Check(b);
            });
            Test("cancelled normal order preserved until explicit refresh; active/story never overwritten", () => {
                var s = Running(); var e = new Economy(s); e.Advance(16); e.AcceptOrder(s.orders[0].id); s.items[0] = 3; e.DeliverOrder();
                int id = s.orders[0].id; int oldItem = s.orders[0].item, oldCount = s.orders[0].count;
                e.AcceptOrder(id); e.CancelOrder(); Check(!e.AcceptOrder(id), "cancelled slot accepted");
                e.Advance(60); bool exists = false; foreach (var o in s.orders) if (o.id == id && o.cancelled) exists = true;
                Check(exists && e.RefreshOrders(), "cancelled slot changed without refresh");
                foreach (var o in s.orders) Check(o.id != id && !(o.item == oldItem && o.count == oldCount), "just-cancelled combination repeated");
                Check(!e.RefreshOrders(), "refresh cooldown bypassed");
            });
            Test("one-hour and zero-income offline reports, close is not reward", () => {
                var s = Running(); s.utcTicks = DateTime.UtcNow.Ticks; OfflineSettlement.Resume(s, s.utcTicks + TimeSpan.FromHours(1).Ticks);
                Check(s.report.effective == 3600 && s.report.coins > 0, "one-hour settlement incorrect");
                var empty = new GameState { utcTicks = DateTime.UtcNow.Ticks }; long before = empty.coins;
                OfflineSettlement.Resume(empty, empty.utcTicks + TimeSpan.FromMinutes(1).Ticks);
                Check(empty.report.pending && empty.report.coins == 0 && empty.coins == before, "zero-income report missing");
            });
            Test("six-recipe and expansion gates cannot be bought early or twice", () => {
                var s = Running(); var e = new Economy(s); s.coins = 100000;
                Check(!e.BuyPlot() && !e.RepairWorkbench() && !e.BuyShelf() && !e.RepairSideRoom(), "gate bypass");
                Check(!e.ConfigureMachine(0, 5) && !e.ConfigurePlot(0, 1), "locked config accepted");
                s.reputation = 10; Check(e.RepairWorkbench() && !e.RepairWorkbench(), "repair repeated");
                s.sold[0] = 3; Check(e.BuyPlot(), "second plot blocked");
                s.reputation = 100; s.sales = 15000; Check(e.RepairSideRoom() && !e.RepairSideRoom(), "side room duplicate");
            });
            Test("twelve garden beds expand one at a time and persist without duplicate charges", () => {
                var s = Running(); var e = new Economy(s); s.coins = 1000000; s.reputation = 100; s.sold[0] = 3;
                while (s.plots.Length < 4) Check(e.BuyPlot(), "original plot gates changed");
                long blocked = s.coins; Check(!e.BuyPlot() && s.coins == blocked, "side-room gate bypassed");
                s.sideRoom = true;
                while (s.plots.Length < Catalog.MaxPlots)
                {
                    int next = s.plots.Length; long before = s.coins;
                    Check(e.BuyPlot() && s.plots.Length == next + 1, "expansion failed " + next);
                    Check(before - s.coins == Catalog.PlotCost[next], "wrong expansion cost");
                    StateValidation.Check(s);
                }
                long full = s.coins; Check(!e.BuyPlot() && s.coins == full, "thirteenth bed accepted/charged");
                StateValidation.Check(Clone(s));
                var invalid = Clone(s); Array.Resize(ref invalid.plots, Catalog.MaxPlots + 1); invalid.plots[Catalog.MaxPlots] = new Facility();
                bool rejected = false; try { StateValidation.Check(invalid); } catch (InvalidDataException) { rejected = true; }
                Check(rejected, "oversized garden accepted by save validation");
            });
            Test("twelve-bed fractional production matches offline without changing chapter requirements", () => {
                var a = Running(); a.reputation = 100; a.sideRoom = true; a.tutorial = 4;
                a.plots = new Facility[Catalog.MaxPlots];
                for (int i = 0; i < a.plots.Length; i++) a.plots[i] = new Facility { config = i % 4, level = 3 };
                a.machines = new[] { new Facility { config = 3, level = 3 }, new Facility { config = 5, level = 3 } };
                a.shelves = new[] { new Facility { config = 3 }, new Facility { config = 5 } };
                var b = Clone(a); var ea = new Economy(a); var eb = new Economy(b);
                ea.Advance(3600); for (int i = 0; i < 108000; i++) eb.Advance(1d / 30);
                Check(a.coins == b.coins && a.sales == b.sales && a.harvests == b.harvests && a.crafts == b.crafts, "twelve-bed fractional totals differ");
                for (int i = 0; i < 4; i++) Check(a.crops[i] == b.crops[i], "crop totals differ");
                for (int i = 0; i < 6; i++) Check(a.items[i] == b.items[i] && a.sold[i] == b.sold[i], "item totals differ");
                StateValidation.Check(a); StateValidation.Check(b);
                var original = Running(); original.stories = 5; original.sideRoom = true; original.reputation = 150; original.sales = 60000;
                for (int i = 0; i < 6; i++) original.sold[i] = 1;
                new Economy(original).Advance(0); Check(original.chapterComplete, "new beds became a chapter requirement");
            });
            Test("new-save full chapter reachable using normal actions and no gifts", FullChapter);
            ForestCoreTests.Run(Test);
        FishingCoreTests.Run(Test);
            MomentCoreTests.Run(Test);
            Console.WriteLine("TOTAL " + passed + " tests passed"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    static void FullChapter()
    {
        var s = Running(); var e = new Economy(s); bool allStories = false;
        for (int second = 0; second < 7 * 86400 && !s.chapterComplete; second += 5)
        {
            if (e.ActiveOrder == null)
            {
                Order select = null; foreach (var o in s.orders) if (o.story && (s.stories == 0 || s.plots.Length > 1)) select = o;
                if (select == null && s.plots.Length > 1 && (s.reputation < 150 || s.stories < 5)) foreach (var o in s.orders) if (!o.cancelled) { select = o; break; }
                if (select != null) e.AcceptOrder(select.id);
            }
            int item = e.ActiveOrder != null ? e.ActiveOrder.item : 0;
            // Ensure every product gets a normal shelf sale in addition to story deliveries.
            if (e.ActiveOrder == null && s.stories == 5)
                for (int i = 0; i < 6; i++) if (e.ItemUnlocked(i) && s.sold[i] == 0) { item = i; break; }
            int machine = Catalog.Machine[item];
            if (s.plots.Length > 1 || item == 0)
            {
                var resources = new System.Collections.Generic.List<int>();
                for (int i = 0; i < 4; i++) if (Catalog.Materials[item][i] > 0) for (int n = 0; n < Catalog.Materials[item][i]; n++) resources.Add(i);
                for (int p = 0; p < s.plots.Length; p++) e.ConfigurePlot(p, resources[p % resources.Count]);
            }
            if (machine < s.machines.Length) e.ConfigureMachine(machine, item);
            // Avoid an inactive cauldron consuming mint needed by the workbench.
            if (machine == 1 && e.ItemUnlocked(3))
            {
                bool drainMint = s.crops[0] >= e.Capacity - 3;
                foreach (var p in s.plots) if (p.remaining == 0 && p.workItem == 0 && p.pending >= 0) drainMint = true;
                e.ConfigureMachine(0, drainMint ? 0 : 3);
                if (drainMint) e.ConfigureShelf(0, 0);
            }
            for (int sh = 0; sh < s.shelves.Length; sh++) e.ConfigureShelf(sh, machine == 1 && sh == 0 && s.shelves.Length > 1 && s.machines[0].config == 0 ? 0 : item);
            e.DeliverOrder();
            if (s.plots.Length == 1) e.BuyPlot();
            if (s.machines.Length == 1 && s.plots.Length > 1 && s.reputation >= 10 && s.coins >= 260) e.RepairWorkbench();
            if (s.plots.Length == 2 && s.reputation >= 25) e.BuyPlot();
            if (s.plots.Length == 3 && s.reputation >= 45) e.BuyPlot();
            if (s.shelves.Length == 1 && s.coins >= 500) e.BuyShelf();
            e.RepairSideRoom();
            if (s.stories == 5 && !allStories) { allStories = true; Console.WriteLine("  five stories at simulated " + second + "s, sales=" + s.sales); }
            e.Advance(5); StateValidation.Check(s);
        }
        if (!s.chapterComplete) Console.WriteLine(codec.Encode(s));
        Check(s.chapterComplete && s.sideRoom && s.stories == 5 && s.reputation >= 150 && s.sales >= 60000, "chapter unreachable: stories=" + s.stories + " rep=" + s.reputation + " sales=" + s.sales + " coins=" + s.coins);
        Console.WriteLine("  chapter complete, simulated seconds=" + s.playedSeconds + ", sales=" + s.sales + ", reputation=" + s.reputation);
        long before = s.sales; e.ConfigureMachine(0, 0); for (int p = 0; p < s.plots.Length; p++) e.ConfigurePlot(p, 0); e.ConfigureShelf(0, 0); e.Advance(60);
        Check(s.sales > before && s.chapterComplete, "post-chapter business broken");
    }
}
