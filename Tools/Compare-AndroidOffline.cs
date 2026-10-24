using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using ForestShop.Core;

// Compare actual before/after Android saves against the same deterministic core.
// No save is changed or pushed back to the device.
public static class CompareAndroidOffline
{
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer();
    static GameState Read(string path) => Json.Deserialize<GameState>(File.ReadAllLines(path).Last());
    static void Equal(bool condition, string field) { if (!condition) throw new Exception("Mismatch: " + field); }
    public static int Main(string[] args)
    {
        try
        {
            var before = Read(args[0]); var after = Read(args[1]);
            StateMigration.Upgrade(before); StateMigration.Upgrade(after);
            Equal(after.report.pending && after.report.away >= 30, "actual pending offline report");
            long coins = before.coins;
            bool alreadyPending=before.report.pending;
            if(alreadyPending) Equal(before.report.away==after.report.away && before.report.effective==after.report.effective && before.report.coins==after.report.coins, "existing report preserved without second settlement");
            if(!alreadyPending)new Economy(before).Advance(after.report.effective);
            StateValidation.Check(before); StateValidation.Check(after);
            Equal(before.coins == after.coins && before.sales == after.sales, "coins / sales");
            Equal(before.harvests == after.harvests && before.crafts == after.crafts, "production totals");
            Equal(before.crops.SequenceEqual(after.crops) && before.items.SequenceEqual(after.items), "inventory");
            Equal(before.sold.SequenceEqual(after.sold), "sold counts");
            Equal(alreadyPending || before.coins - coins == after.report.coins, "reported income");
            Equal(before.stories == after.stories && before.reputation == after.reputation, "story / reputation");
            var predicted = before.plots.Concat(before.machines).Concat(before.shelves).Concat(before.forest.beds).ToArray();
            var actual = after.plots.Concat(after.machines).Concat(after.shelves).Concat(after.forest.beds).ToArray();
            Equal(predicted.Length == actual.Length, "facilities");
            for (int i = 0; i < predicted.Length; i++)
                Equal(predicted[i].config == actual[i].config && predicted[i].workItem == actual[i].workItem && predicted[i].pending == actual[i].pending && predicted[i].level == actual[i].level && Math.Abs(predicted[i].remaining - actual[i].remaining) < .002, "facility " + i);
            Equal(before.forest.available.SequenceEqual(after.forest.available), "forage point stock");
            Equal(before.forest.gathered.SequenceEqual(after.forest.gathered), "no automatic gathering");
            Equal(before.forest.discovered == after.forest.discovered && before.forest.visited == after.forest.visited && before.forest.requests == after.forest.requests && before.forest.location == after.forest.location, "forest progress");
            Equal(before.forest.greenhouse == after.forest.greenhouse && before.forest.bridge == after.forest.bridge && before.forest.vines == after.forest.vines && before.forest.pavilion == after.forest.pavilion, "repair progress");
            for (int i = 0; i < 3; i++) Equal(Math.Abs(before.forest.regrow[i] - after.forest.regrow[i]) < .002, "forage cooldown " + i);
            Equal(!after.fishing.running, "fishing paused after background/load");
            Equal(Math.Abs(before.fishing.remaining-after.fishing.remaining)<1e-10 && Math.Abs(before.fishing.duration-after.fishing.duration)<1e-10, "offline preserves fishing timer within serialization precision");
            before.fishing.running=false; after.fishing.remaining=before.fishing.remaining; after.fishing.duration=before.fishing.duration;
            Equal(Json.Serialize(before.fishing)==Json.Serialize(after.fishing), "offline does not catch fish, reset the current cast, or change fish records");
            Console.WriteLine("PASS actual Android offline replay: away=" + after.report.away + "s effective=" + after.report.effective + "s income=" + after.report.coins + " coins=" + after.coins + " sales=" + after.sales);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
