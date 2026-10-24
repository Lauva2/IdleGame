using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using ForestShop.Core;

public static class FishingCoreTests
{
    const string Native = "Docs/Verification/3d-d21-forest02/normal-restorable.save";
    static readonly TestCodec Codec=new TestCodec();
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
    static void Check(bool ok,string reason) { if(!ok) throw new Exception(reason); }
    static string Canon(object value) => Json.Serialize(value);
    static GameState Copy(GameState state) => Codec.Decode(Codec.Encode(state));
    static GameState Prepared()
    {
        var s=Codec.Decode(File.ReadAllText(Native).Split(new[]{'\n'},4)[3]); StateMigration.Upgrade(s); return s;
    }
    static Economy AtSpot(GameState state,int spot=0)
    { var e=new Economy(state); Check(e.Forest.Visit(spot),"actual visit prerequisite"); return e; }
    static void Wrapped(string path,GameState state,long revision)
    {
        string payload=Codec.Encode(state),digest;
        using(var sha=SHA256.Create()) digest=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
        File.WriteAllText(path,"FORESTSHOP1\n"+revision+"\n"+digest+"\n"+payload,new UTF8Encoding(false));
    }
    public static void Run(Action<string,Action> test)
    {
        test("actual Android inline empty catch keeps report and ordinary income",()=>{
            var path="Docs/Verification/3d-d21-forest04/ordinary-upgrade-report.save";
            var state=Codec.Decode(File.ReadAllText(path).Split(new[]{'\n'},4)[3]);StateValidation.Check(state);
            Check(state.report.pending && state.fishing.phase==0 && state.fishing.candidate.sequence==0 && state.coins==847178,"actual Android checkpoint changed");
            var e=AtSpot(state);Check(e.Fishing.Start(0,0),"sentinel blocks next cast");e.Fishing.Advance(20);StateValidation.Check(Copy(state));
            var bad=Prepared();bad.fishing.candidate=new FishCatch{species=1};bool rejected=false;try{StateValidation.Check(bad);}catch(InvalidDataException){rejected=true;}Check(rejected,"nonempty invalid inactive candidate accepted");
        });
        test("actual v2 ordinary forest completion migrates to v3 without resetting progress",()=>{
            byte[] original=File.ReadAllBytes(Native); var before=Codec.Decode(Encoding.UTF8.GetString(original).Split(new[]{'\n'},4)[3]);
            Check(before.version==2 && before.forest.pavilion,"real version2 ordinary baseline");
            var after=Codec.Decode(Encoding.UTF8.GetString(original).Split(new[]{'\n'},4)[3]); StateMigration.Upgrade(after); before.version=3; before.fishing=new FishingProgress();
            Check(after.version==3 && Codec.Encode(before)==Codec.Encode(after),"migration changed existing economy/forest/queues/report/settings");
            Check(after.fishing.totalCaught==0 && after.fishing.entries.Length==12 && !after.fishing.running,"migration awarded fish");
            Check(Convert.ToBase64String(File.ReadAllBytes(Native))==Convert.ToBase64String(original),"native evidence changed"); StateValidation.Check(after);
        });
        test("fishing gates require bridge, actual zone and unlocked bait without consuming RNG",()=>{
            var fresh=new GameState(); var e=new Economy(fresh); string initial=Codec.Encode(fresh);
            Check(!e.Fishing.Start(0,0) && Codec.Encode(fresh)==initial,"fishing started before forest bridge");
            var s=Prepared(); e=AtSpot(s,1); string before=Codec.Encode(s);
            Check(!e.Fishing.Start(0,0) && !e.Fishing.Start(1,1) && !e.Fishing.Start(1,3) && Codec.Encode(s)==before,"wrong zone or bait changed cast");
            Check(e.Fishing.Start(1,0),"valid visible spot rejected"); StateValidation.Check(s);
        });
        test("selected spot continuously lands fish automatically with no confirmation award",()=>{
            var s=Prepared(); var e=AtSpot(s); Check(e.Fishing.Start(0,0),"start"); e.Fishing.Advance(80);
            Check(s.fishing.totalCaught>=4 && s.fishing.casts>=s.fishing.totalCaught,"automatic loop stopped before repeated catches");
            string before=Codec.Encode(s); Check(!e.Fishing.Start(0,0) && Codec.Encode(s)==before,"reopening an already running spot rerolled or awarded");
            for(int i=0;i<s.fishing.recent.Length;i++) Check(s.fishing.recent[i].sequence==s.fishing.totalCaught-i,"catch IDs duplicate"); StateValidation.Check(s);
        });
        test("pause and save reload preserve bite and landed phases without duplicate fish",()=>{
            var s=Prepared(); var e=AtSpot(s); e.Fishing.Start(0,0); e.Fishing.Advance(s.fishing.remaining+.9); e.Fishing.Pause();
            Check(s.fishing.phase==2 && Math.Abs(s.fishing.remaining-.9)<1e-8,"mid-bite setup"); string paused=Codec.Encode(s);
            e.Fishing.Advance(1000); Check(Codec.Encode(s)==paused,"paused time caught fish");
            s=Codec.Decode(paused); e=new Economy(s); int casts=s.fishing.casts,random=s.fishing.random;
            Check(e.Fishing.Start(0,0) && s.fishing.casts==casts && s.fishing.random==random,"resume reset pending outcome");
            e.Fishing.Advance(.9); Check(s.fishing.totalCaught==1 && s.fishing.phase==3,"automatic landing missing");
            s=Copy(s); e=new Economy(s); e.Fishing.Advance(0); e.Fishing.Advance(1);
            Check(s.fishing.totalCaught==1,"saved receipt landed again"); e.Fishing.Advance(1.2);
            Check(s.fishing.totalCaught==1 && s.fishing.phase==1 && s.fishing.casts==casts+1,"next cast replayed old fish"); StateValidation.Check(s);
        });
        test("automatic catch outcomes match one large tick and fractional foreground ticks",()=>{
            var a=Prepared(); var b=Copy(a); var x=AtSpot(a); var y=AtSpot(b); x.Fishing.Start(0,0); y.Fishing.Start(0,0);
            x.Fishing.Advance(600); for(int i=0;i<6000;i++) y.Fishing.Advance(.1);
            Check(Math.Abs(a.fishing.remaining-b.fishing.remaining)<1e-7,"phase time drift");
            double remaining=a.fishing.remaining; a.fishing.remaining=b.fishing.remaining;
            Check(Canon(a.fishing)==Canon(b.fishing),"seeded fish/quality/length/weight/history differ"); a.fishing.remaining=remaining;
            StateValidation.Check(a); StateValidation.Check(b);
        });
        test("eight hour ordinary offline settlement never advances an active fishing clock",()=>{
            var s=Prepared(); var e=AtSpot(s); e.Fishing.Start(0,0); e.Fishing.Advance(2.25);
            string fishing=Canon(s.fishing); long stamp=s.utcTicks; OfflineSettlement.Resume(s,stamp+TimeSpan.FromHours(8).Ticks);
            Check(s.report.pending && s.report.effective==28800 && Canon(s.fishing)==fishing,"offline auto-fished or rerolled the rod");
            OfflineSettlement.Resume(s,stamp+TimeSpan.FromHours(16).Ticks); Check(Canon(s.fishing)==fishing,"pending report altered fishing"); StateValidation.Check(s);
        });
        test("fishing cannot alter factory inventory, money, quests or order RNG",()=>{
            var a=Prepared(); var b=Prepared(); var x=AtSpot(a); var y=AtSpot(b); x.Advance(600); y.Advance(600); y.Fishing.Start(0,0); y.Fishing.Advance(600);
            Check(b.fishing.totalCaught>0,"isolation scenario did not fish"); b.fishing=new FishingProgress();
            Check(Codec.Encode(a)==Codec.Encode(b),"fishing changed ordinary economy or its random stream");
        });
        test("all12 species and four qualities are reachable with legal automatic casts",()=>{
            var s=Prepared(); var e=AtSpot(s);
            for(int spot=0;spot<3;spot++)
            {
                e.Fishing.Pause(); Check(e.Forest.Visit(spot),"spot travel"); Check(e.Fishing.Start(spot,e.Fishing.BaitUnlocked(2)?2:0),"legal bait");
                e.Fishing.Advance(3600); StateValidation.Check(s);
            }
            Check(e.Fishing.Discovered==12,"species unreachable");
            for(int q=0;q<4;q++) Check(s.fishing.entries.Sum(x=>x.qualityCounts[q])>0,"quality unreachable");
            Check(s.fishing.recent.Length==FishingCatalog.RecentLimit && s.fishing.entries.Sum(x=>x.count)==s.fishing.totalCaught,"bounded history dropped aggregate counts");
            Check(e.Fishing.BaitUnlocked(1) && e.Fishing.BaitUnlocked(2),"discovery bait unlocks missing");
        });
        test("malformed fishing statistics are rejected and valid v2 backup wins",()=>{
            var good=Codec.Decode(File.ReadAllText(Native).Split(new[]{'\n'},4)[3]); var bad=Prepared(); var e=AtSpot(bad); e.Fishing.Start(0,0); e.Fishing.Advance(20);
            int species=bad.fishing.recent[0].species; bad.fishing.entries[species].qualityCounts[0]++;
            string dir=Path.Combine("Tools/TestOutput",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
            Wrapped(Path.Combine(dir,"forest-shop.save.bak"),good,1); Wrapped(Path.Combine(dir,"forest-shop.save"),bad,2);
            var store=new SaveStore(dir,Codec); var loaded=store.Load();
            Check(loaded.version==3 && loaded.fishing.totalCaught==0 && loaded.forest.pavilion && loaded.coins==good.coins && store.RecoveryMessage!=null,"bad v3 was silently repaired or valid forest backup lost"); StateValidation.Check(loaded);
        });
    }
}
