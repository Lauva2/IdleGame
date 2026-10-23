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
            Equal(after.report.pending && after.report.away >= 30, "actual pending offline report");
            long coins = before.coins;
            new Economy(before).Advance(after.report.effective);
            StateValidation.Check(before); StateValidation.Check(after);
            Equal(before.coins == after.coins && before.sales == after.sales, "coins / sales");
            Equal(before.harvests == after.harvests && before.crafts == after.crafts, "production totals");
            Equal(before.crops.SequenceEqual(after.crops) && before.items.SequenceEqual(after.items), "inventory");
            Equal(before.sold.SequenceEqual(after.sold), "sold counts");
            Equal(before.coins - coins == after.report.coins, "reported income");
            Equal(before.stories == after.stories && before.reputation == after.reputation, "story / reputation");
            var predicted = before.plots.Concat(before.machines).Concat(before.shelves).ToArray();
            var actual = after.plots.Concat(after.machines).Concat(after.shelves).ToArray();
            Equal(predicted.Length == actual.Length, "facilities");
            for (int i = 0; i < predicted.Length; i++)
                Equal(predicted[i].config == actual[i].config && predicted[i].workItem == actual[i].workItem && Math.Abs(predicted[i].remaining - actual[i].remaining) < .002, "facility " + i);
            Console.WriteLine("PASS actual Android offline replay: away=" + after.report.away + "s effective=" + after.report.effective + "s income=" + after.report.coins + " coins=" + after.coins + " sales=" + after.sales);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
