using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using ForestShop.Core;

// Read-only full-state replay. Optional cosmetic actions commute with production;
// only recorded visits/observations/choices are applied, never extra rewards.
public static class CompareMomentActions
{
    public sealed class Step { public string kind; public int id, arg; public bool expected; }
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static GameState Read(string path)
    {
        var parts=File.ReadAllText(path).Split(new[]{'\n'},4);Check(parts.Length==4 && parts[0]=="FORESTSHOP1","save envelope");
        using(var sha=SHA256.Create())Check(BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(parts[3]))).Replace("-","")==parts[2],"save checksum");
        var state=Json.Deserialize<GameState>(parts[3]);StateMigration.Upgrade(state);StateValidation.Check(state);return state;
    }
    public static int Main(string[] args)
    {
        try
        {
            var before=Read(args[0]);var after=Read(args[1]);var steps=Json.Deserialize<Step[]>(File.ReadAllText(args[2]));
            Check(!before.report.pending && !after.report.pending && !before.fishing.running && !after.fishing.running,"online cosmetic-only interval, fish paused");
            double elapsed=after.playedSeconds-before.playedSeconds;Check(elapsed>=0 && elapsed<=3600,"actual action interval");
            bool fixture=args.Length>3 && args[3]=="--from-fixture";
            if(fixture)
            {
                // A pushed input precedes actual app loading. Reproduce the real
                // short startup offline settlement, including its report fields,
                // rather than silently normalizing them to the final snapshot.
                Check(after.report.away>=0 && after.report.away<30,"fixture startup interval is short, no pending report");
                long loadedAt=before.utcTicks+(long)Math.Round(after.report.away*TimeSpan.TicksPerSecond);
                OfflineSettlement.Resume(before,loadedAt);
            }
            var replay=new Economy(before);double foreground=after.playedSeconds-before.playedSeconds;
            Check(foreground>=0,"startup cannot exceed complete interval");replay.Advance(foreground);
            foreach(var step in steps)
            {
                bool result=step.kind=="visit"?replay.Forest.Visit(step.id):step.kind=="observe"?replay.Moments.Observe(step.id,step.arg):step.kind=="choose"?replay.Moments.Choose(step.id,step.arg):throw new Exception("Unknown recorded action");
                Check(result==step.expected,"recorded action "+step.kind+" "+step.id+" "+step.arg);
            }
            var predicted=before.plots.Concat(before.machines).Concat(before.shelves).Concat(before.forest.beds).ToArray();var actual=after.plots.Concat(after.machines).Concat(after.shelves).Concat(after.forest.beds).ToArray();
            Check(predicted.Length==actual.Length,"facility count");
            for(int i=0;i<predicted.Length;i++){Check(Math.Abs(predicted[i].remaining-actual[i].remaining)<.002,"facility clock "+i);predicted[i].remaining=actual[i].remaining;}
            for(int i=0;i<3;i++){Check(Math.Abs(before.forest.regrow[i]-after.forest.regrow[i])<.002,"forage clock");before.forest.regrow[i]=after.forest.regrow[i];}
            Check(Math.Abs(before.fishing.remaining-after.fishing.remaining)<1e-10 && Math.Abs(before.fishing.duration-after.fishing.duration)<1e-10,"paused fish clocks");before.fishing.remaining=after.fishing.remaining;before.fishing.duration=after.fishing.duration;
            Check(Math.Abs(before.playedSeconds-after.playedSeconds)<.002 && after.utcTicks>=before.utcTicks,"ordinary monotonic time");before.playedSeconds=after.playedSeconds;before.utcTicks=after.utcTicks;
            Check(Json.Serialize(before)==Json.Serialize(after),"all other saved fields, economy, inventory, quests, RNG, fish and recorded optional progress match");
            Console.WriteLine("PASS actual Android moment action replay: elapsed="+elapsed+"s actions="+steps.Length+" stages="+string.Join(",",after.moments.stages)+" choices="+string.Join(",",after.moments.choices)+" coins="+after.coins+" fish="+after.fishing.totalCaught+"; full saved state matches within existing clock tolerances"+(fixture?"; startup OfflineSettlement.Resume replayed separately":""));return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
