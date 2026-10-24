using System;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using ForestShop.Core;

// Read-only replay of two actual Android saves. Never creates player resources.
// Use only an uninterrupted online interval without economy-changing UI actions.
public static class CompareAndroidOnline
{
    static GameState Read(string path) => new JavaScriptSerializer().Deserialize<GameState>(File.ReadAllLines(path).Last());
    static void Equal(bool ok, string name) { if (!ok) throw new Exception("Mismatch: " + name); }
    public static int Main(string[] args)
    {
        try
        {
            var before = Read(args[0]); var after = Read(args[1]);
            StateMigration.Upgrade(before); StateMigration.Upgrade(after);
            Equal(!before.report.pending && !after.report.pending, "online interval without pending report");
            double elapsed = after.playedSeconds - before.playedSeconds;
            Equal(elapsed >= 30, "at least 30 actual simulated online seconds");
            new Economy(before).Advance(elapsed);
            StateValidation.Check(before); StateValidation.Check(after);
            Equal(before.coins == after.coins && before.sales == after.sales, "coins / sales");
            Equal(before.harvests == after.harvests && before.crafts == after.crafts, "production totals");
            Equal(before.crops.SequenceEqual(after.crops) && before.items.SequenceEqual(after.items), "inventory");
            Equal(before.sold.SequenceEqual(after.sold), "sold counts");
            Equal(before.reputation == after.reputation && before.stories == after.stories, "progression");
            var predicted = before.plots.Concat(before.machines).Concat(before.shelves).Concat(before.forest.beds).ToArray();
            var actual = after.plots.Concat(after.machines).Concat(after.shelves).Concat(after.forest.beds).ToArray();
            Equal(predicted.Length == actual.Length, "facility count");
            for (int i = 0; i < predicted.Length; i++)
                Equal(predicted[i].config == actual[i].config && predicted[i].pending == actual[i].pending
                    && predicted[i].workItem == actual[i].workItem
                    && Math.Abs(predicted[i].remaining - actual[i].remaining) < .002,
                    "facility " + i + ": predicted " + predicted[i].config + "/" + predicted[i].workItem + "/" + predicted[i].remaining
                    + ", actual " + actual[i].config + "/" + actual[i].workItem + "/" + actual[i].remaining);
            Equal(before.forest.available.SequenceEqual(after.forest.available) && before.forest.gathered.SequenceEqual(after.forest.gathered), "forest node stock and no automatic gathering");
            for(int i=0;i<3;i++)Equal(Math.Abs(before.forest.regrow[i]-after.forest.regrow[i])<.002,"forest cooldown "+i);
            Equal(Math.Abs(before.fishing.remaining-after.fishing.remaining)<1e-10,"fishing clock does not advance outside fishing view");
            before.fishing.remaining=after.fishing.remaining;
            Equal(new JavaScriptSerializer().Serialize(before.fishing)==new JavaScriptSerializer().Serialize(after.fishing),"fish records unchanged outside fishing view");
            Console.WriteLine("PASS actual Android online replay: elapsed=" + elapsed + "s, simplified=" + after.simpleAnimation
                + ", coins=" + after.coins + ", sales=" + after.sales + "; all inventory, production and facility states match");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
