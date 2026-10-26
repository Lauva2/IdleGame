using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using ForestShop.Core;

// Labelled review states only. No fixture may become ordinary progression.
public static class PrepareJourneyReview
{
    static string Hash(byte[] b) { using (var h = SHA256.Create()) return BitConverter.ToString(h.ComputeHash(b)).Replace("-", ""); }
    public static int Main(string[] args)
    {
        try
        {
            string folder = Path.GetFullPath(args[0]), allowed = Path.GetFullPath("Docs/Verification") + Path.DirectorySeparatorChar;
            if (!folder.StartsWith(allowed, StringComparison.OrdinalIgnoreCase)) throw new Exception("Review folder outside verification");
            foreach (string n in new[] { "normal-held.save", "normal-held.save.bak" }) if (!File.Exists(Path.Combine(folder, n))) throw new Exception("Protect latest ordinary pair first");
            var original = File.ReadAllLines(Path.Combine(folder, "normal-held.save"));
            if (Hash(Encoding.UTF8.GetBytes(original.Last())) != original[2]) throw new Exception("Protected checksum mismatch");
            string variant = args[1];
            if (!new[] { "journey-entry", "journey-berry", "journey-dew", "journey-resin", "journey-partial", "journey-complete", "journey-deliver", "journey-window" }.Contains(variant)) throw new Exception("Unknown journey fixture");
            var json = new JavaScriptSerializer(); var s = json.Deserialize<GameState>(original.Last()); StateMigration.Upgrade(s);
            s.moments = new ForestMomentsProgress();
            s.coins = 25000; s.reputation = 180; s.sales = 60000; s.warehouse = 4; s.harvests = 140; s.crafts = 160;
            s.crops = Enumerable.Range(0, 7).Select(i => i < 4 ? 30 : 0).ToArray(); s.items = Enumerable.Range(0, 9).Select(i => i < 6 ? 24 : 0).ToArray(); s.sold = Enumerable.Range(0, 9).Select(i => i < 6 ? 20 : 0).ToArray();
            s.plots = Enumerable.Range(0, 12).Select(i => new Facility { config = i % 4, workItem = i % 4, remaining = 10 + i % 4 * 4 }).ToArray();
            s.machines = new[] { new Facility { config = 4, workItem = 4, remaining = 20 }, new Facility { config = 5, workItem = 5, remaining = 25 } };
            s.shelves = new[] { new Facility { config = 0, remaining = 8 }, new Facility { config = 1, remaining = 8 }, new Facility { config = 4, remaining = 8 }, new Facility { config = 5, remaining = 8 } };
            s.orders = new Order[0]; s.stories = 5; s.tutorial = 4; s.welcomeRead = true; s.sideRoom = true; s.chapterComplete = s.chapterCelebrated = true; s.dialogue = -1; s.nextOrderId = 301; s.refreshSeconds = s.playedSeconds = 0; s.decoration = new[] { true, true, true, true }; s.friendship = new[] { 60, 60, 60, 60, 60 }; s.report = new OfflineReport(); s.simpleAnimation = false;
            bool entry = variant == "journey-entry", berry = variant == "journey-berry" || variant == "journey-deliver", dew = variant == "journey-dew", partial = variant == "journey-partial", complete = variant == "journey-complete";
            var f = s.forest = new ForestProgress { started = !entry, greenhouse = !entry, bridge = !entry && !berry, vines = !entry && !berry && !dew && !partial, pavilion = complete, celebrationSeen = complete, discovered = entry ? 0 : berry || partial ? 1 : dew ? 3 : 7, visited = entry ? 0 : berry ? 1 : dew || partial ? 3 : 7, location = 0, requests = entry || berry ? 0 : dew || partial ? 1 : complete ? 3 : 2, dialogue = -1 };
            f.beds = entry ? new Facility[0] : Enumerable.Range(0, 4).Select(i => new Facility { config = dew && i >= 2 ? 5 : 4, workItem = dew && i >= 2 ? 5 : 4, remaining = dew && i >= 2 ? 55 : 40 }).ToArray();
            for (int i = 0; i < 3; i++) f.gathered[i] = (f.discovered & (1 << i)) != 0 ? 3 : 0;
            s.fishing = new FishingProgress();
            if (variant == "journey-window")
            {
                s.crops = new int[7]; s.crops[0] = s.crops[4] = engineCapacity;
                s.items = new int[9];
                foreach (var plot in s.plots) { plot.config = plot.workItem = 0; plot.remaining = 0; }
                foreach (var bed in f.beds) { bed.config = bed.workItem = 4; bed.remaining = 0; }
                s.machines = new[] { new Facility { config = 4, workItem = 4, remaining = 15 }, new Facility() };
                foreach (var shelf in s.shelves) { shelf.config = -1; shelf.remaining = 8; }
            }
            if (berry || dew || variant == "journey-resin")
            {
                int item = berry ? 6 : dew ? 7 : 8;
                s.orders = new[] { new Order { id = 301, forestStory = true, item = item, resident = ForestRules.RequestResidents[item - 6], count = 2, active = true, reserve = true } }; s.nextOrderId = 302;
            }
            var engine = new Economy(s);
            if (variant == "journey-deliver") s.items[6] = 2;
            if (partial) { engine.Fishing.Start(0, 0); engine.Fishing.Advance(45); engine.Fishing.Pause(); }
            if (complete) for (int spot = 0; spot < 3; spot++) { f.location = spot; engine.Fishing.Start(spot, 0); engine.Fishing.Advance(60000); engine.Fishing.Pause(); }
            s.utcTicks = DateTime.UtcNow.Ticks; StateValidation.Check(s);
            string prepared = Path.Combine(folder, "Prepared"); Directory.CreateDirectory(prepared); byte[] payload = Encoding.UTF8.GetBytes(json.Serialize(s));
            File.WriteAllText(Path.Combine(prepared, variant + ".save"), "FORESTSHOP1\n1\n" + Hash(payload) + "\n" + Encoding.UTF8.GetString(payload), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(prepared, variant + "-scope.json"), json.Serialize(new { variant, prepared = true, utc = DateTime.UtcNow.ToString("o"), ordinarySourceSha256 = Hash(File.ReadAllBytes(Path.Combine(folder, "normal-held.save"))), restriction = "Explicit resource, gate, facility and quest preparation. Partial/all-fish records generated with actual Core simulated foreground seconds. Not natural progression, probability or ordinary income. Preserve/restore latest normal pair." }), new UTF8Encoding(false));
            Console.WriteLine("PASS prepared journey fixture " + variant); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
    const int engineCapacity = 160;
}
