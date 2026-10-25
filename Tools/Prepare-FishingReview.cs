using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using ForestShop.Core;

// Explicitly prepared review saves, not ordinary catch/probability evidence.
public static class PrepareFishingReview
{
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
    public static int Main(string[] args)
    {
        try
        {
            var folder=Path.GetFullPath(args[0]);var root=Path.GetFullPath("Docs/Verification")+Path.DirectorySeparatorChar;
            if(!folder.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new Exception("Review folder outside verification");
            foreach(var name in new[]{"normal-held.save","normal-held.save.bak"})if(!File.Exists(Path.Combine(folder,name)))throw new Exception("Protect ordinary main and backup first");
            var source=File.ReadAllLines(Path.Combine(folder,"normal-held.save"));
            var bytes=Encoding.UTF8.GetBytes(source.Last());if(Hash(bytes)!=source[2])throw new Exception("Ordinary source checksum mismatch");
            var state=Json.Deserialize<GameState>(source.Last());StateMigration.Upgrade(state);var engine=new Economy(state);
            if(!state.forest.pavilion || !state.forest.vines)throw new Exception("Use protected completed ordinary forest");
            state.fishing=new FishingProgress();state.simpleAnimation=false;state.forest.location=0;state.report.pending=false;
            string variant=args[1];object expected=null;
            if(variant=="fishing-filled" || variant=="fishing-simple")
            {
                for(int spot=0;spot<3;spot++){state.forest.location=spot;engine.Fishing.Start(spot,0);engine.Fishing.Advance(60000);engine.Fishing.Pause();}
                if(engine.Fishing.Discovered!=12)throw new Exception("Prepared species coverage incomplete");
                state.forest.location=2;state.simpleAnimation=variant=="fishing-simple";
                expected=new{method="Prepared60000seconds-per-habitat using actual core; not natural catch/probability evidence",state.fishing.totalCaught,discovered=engine.Fishing.Discovered};
            }
            else if(variant.StartsWith("fishing-notice-"))
            {
                string notice=variant.Substring("fishing-notice-".Length);
                if(!new[]{"new","length","weight","both","ordinary"}.Contains(notice))throw new Exception("Unknown notice fixture");
                int seed=1;FishCatch candidate=null;
                for(;seed<10000;seed++)
                {
                    state.fishing=new FishingProgress{random=seed};engine.Fishing.Start(0,0);candidate=state.fishing.candidate;var k=FishingCatalog.Species[candidate.species];
                    if(candidate.lengthMm>k.MinLength+12 && candidate.lengthMm<k.MaxLength-12 && candidate.weightGrams>k.MinWeight+4 && candidate.weightGrams<k.MaxWeight-4)break;
                }
                if(seed==10000)throw new Exception("No interior record sample");
                state.fishing=new FishingProgress{random=seed};FishCatch prior=null;
                if(notice!="new")
                {
                    bool longer=notice=="length"||notice=="both",heavier=notice=="weight"||notice=="both";
                    prior=new FishCatch{sequence=1,species=candidate.species,quality=0,lengthMm=candidate.lengthMm+(longer?-10:10),weightGrams=candidate.weightGrams+(heavier?-3:3),spot=0,bait=0};
                    state.fishing.totalCaught=state.fishing.casts=1;state.fishing.recent=new[]{prior};var e=state.fishing.entries[prior.species];e.count=1;e.qualityCounts[0]=1;e.bestQuality=0;e.firstSequence=1;e.longestMm=prior.lengthMm;e.heaviestGrams=prior.weightGrams;
                }
                candidate.sequence=notice=="new"?1:2;
                expected=new{method="Prepared RNG seed and explicitly artificial prior record for notice coverage, not ordinary progress/probability; final APK must land normally",seed,notice,prior,expectedCatch=candidate};
            }
            else if(variant.StartsWith("fishing-seed-q"))
            {
                int quality=int.Parse(variant.Substring("fishing-seed-q".Length));if(quality<0 || quality>3)throw new Exception("Quality outside0..3");
                int seed=1;FishCatch candidate=null;
                for(;seed<100000;seed++){state.fishing=new FishingProgress{random=seed};engine.Fishing.Start(0,0);candidate=state.fishing.candidate;if(candidate.quality==quality)break;}
                if(seed==100000)throw new Exception("No sample seed found");
                expected=new{method="Prepared independent RNG seed only; actual automatic waiting/bite/landing must be observed in final APK; no natural probability claim",seed,expected=candidate};
                state.fishing=new FishingProgress{random=seed};
            }
            else if(variant!="fishing-empty")throw new Exception("Unknown variant");
            state.utcTicks=DateTime.UtcNow.Ticks;StateValidation.Check(state);
            var prepared=Path.Combine(folder,"Prepared");Directory.CreateDirectory(prepared);var payload=Encoding.UTF8.GetBytes(Json.Serialize(state));
            File.WriteAllText(Path.Combine(prepared,variant+".save"),"FORESTSHOP1\n1\n"+Hash(payload)+"\n"+Encoding.UTF8.GetString(payload),new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(prepared,variant+"-scope.json"),Json.Serialize(new{variant,prepared=true,utc=DateTime.UtcNow.ToString("o"),ordinarySourceSha256=Hash(File.ReadAllBytes(Path.Combine(folder,"normal-held.save"))),expected,restriction="Preserve latest legitimate pair; restore after review. All fixture catches/seeded outcomes excluded from ordinary progression."}),new UTF8Encoding(false));
            Console.WriteLine("PASS prepared review fixture "+variant);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","");}
}
