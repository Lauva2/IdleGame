using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using ForestShop.Core;

public static class PrepareMomentReview
{
    public static int Main(string[] args)
    {
        try
        {
            string folder=Path.GetFullPath(args[0]),variant=args[1];var json=new JavaScriptSerializer();
            if(!folder.StartsWith(Path.GetFullPath("Docs/Verification")+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new ArgumentException("Fixture outside verification");
            string original=File.ReadAllText(Path.Combine(folder,"normal-held.save"));var s=json.Deserialize<GameState>(original.Split(new[]{'\n'},4)[3]);StateMigration.Upgrade(s);
            if(!s.forest.pavilion)throw new InvalidOperationException("This prepared review requires a preserved ordinary forest-complete baseline");
            s.report.pending=false;s.dialogue=-1;s.forest.dialogue=-1;s.chapterCelebrated=true;s.forest.celebrationSeen=true;s.fishing.running=false;s.moments=new ForestMomentsProgress();
            if(variant=="simple")s.simpleAnimation=true;
            else if(variant=="partial")s.moments.stages=new[]{1,2,0};
            else if(variant=="finished"){s.moments.stages=new[]{3,3,3};s.moments.choices=new[]{0,1,0};}
            else if(variant=="await")s.moments.stages=new[]{2,2,2};
            else if(variant=="locked"){s.orders=new Order[0];s.forest=new ForestProgress();s.fishing=new FishingProgress();}
            else if(variant!="start")throw new ArgumentException("Unknown prepared variant");
            s.utcTicks=DateTime.UtcNow.Ticks;StateValidation.Check(s);string payload=json.Serialize(s),digest;
            using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(payload))).Replace("-","");
            string prepared=Path.Combine(folder,"Prepared");Directory.CreateDirectory(prepared);string file=Path.Combine(prepared,"moment-"+variant+".save");
            File.WriteAllText(file,"FORESTSHOP1\n1\n"+digest+"\n"+payload,new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(prepared,"moment-"+variant+"-scope.json"),json.Serialize(new{prepared=true,variant,baseOrdinary="normal-held.save",stateVersion=s.version,stages=s.moments.stages,choices=s.moments.choices,scope="Explicitly prepared optional progress/arrangements for actual APK review. Not natural progression. Resources from the protected baseline are not restored as ordinary. All original input files retained."}),new UTF8Encoding(false));
            Console.WriteLine("PASS valid prepared moment fixture "+file);return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
