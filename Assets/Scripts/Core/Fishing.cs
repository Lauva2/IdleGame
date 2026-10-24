using System;
using System.IO;

namespace ForestShop.Core
{
    public sealed class FishSpecies
    {
        public readonly string Name, Habit;
        public readonly int Rarity, Spots, Bait, MinLength, MaxLength, MinWeight, MaxWeight, Weight, Form;
        public FishSpecies(string name, string habit, int rarity, int spots, int bait, int minLength, int maxLength, int minWeight, int maxWeight, int weight, int form)
        { Name=name; Habit=habit; Rarity=rarity; Spots=spots; Bait=bait; MinLength=minLength; MaxLength=maxLength; MinWeight=minWeight; MaxWeight=maxWeight; Weight=weight; Form=form; }
    }
    public static class FishingCatalog
    {
        public const int RecentLimit = 30;
        public static readonly string[] Spots = { "溪边浅滩", "木桥深潭", "旧亭洄湾" };
        public static readonly string[] Baits = { "谷粒饵", "草虫饵", "莓果饵" };
        public static readonly string[] Qualities = { "普通", "优良", "上品", "珍品" };
        public static readonly string[] Rarities = { "常见", "少见", "稀有", "传说" };
        // Authored picture-book species and collection ranges, not zoological claims.
        public static readonly FishSpecies[] Species = {
            new FishSpecies("晨光青鳉", "喜欢有细草的浅水", 0,3,0,60,130,4,32,46,0),
            new FishSpecies("银尾鲦", "成群穿过亮亮的水纹", 0,3,1,100,240,18,180,34,0),
            new FishSpecies("苔纹鳅", "沿着苔石慢慢寻找食物", 0,5,1,110,260,20,190,28,2),
            new FishSpecies("圆叶溪鲫", "在平静水面下悠然转身", 0,7,0,120,320,60,650,26,1),
            new FishSpecies("花影鳑鲏", "喜欢水草投下的碎花影", 1,3,0,60,120,4,28,13,1),
            new FishSpecies("青背鲈", "藏在木桥下的深水处", 1,6,1,180,420,140,1500,10,0),
            new FishSpecies("红鳍林鲤", "常在岸边寻找熟透的果子", 1,5,2,200,520,200,2800,8,1),
            new FishSpecies("斑点溪鳟", "顺着清凉的溪流游动", 1,2,1,180,400,160,1900,8,0),
            new FishSpecies("圆月鲂", "把月牙般的背影留在水面", 2,6,0,180,380,150,1800,4,1),
            new FishSpecies("金线鲤", "金色细线藏在柔和的鳞片间", 2,4,2,220,560,320,3500,3,1),
            new FishSpecies("蓝鳍溪鲶", "用长长的须轻触石缝", 2,6,1,200,540,280,3100,3,3),
            new FishSpecies("星尾锦鱼", "尾鳍像落进溪里的小星光", 3,4,2,160,400,120,1700,1,4)
        };
    }
    [Serializable]
    public sealed class FishCatch
    {
        public int sequence, species, quality, lengthMm, weightGrams, spot, bait;
    }
    [Serializable]
    public sealed class FishEntry
    {
        public int count, longestMm, heaviestGrams, firstSequence;
        public int bestQuality = -1;
        public int[] qualityCounts = new int[4];
    }
    [Serializable]
    public sealed class FishingProgress
    {
        public bool running;
        // 0 idle, 1 watching float, 2 automatic bite/reel, 3 landed receipt.
        public int phase, spot, bait, casts, totalCaught;
        public int random = 731263;
        public double remaining, duration;
        // Unity serializes plain inline classes by value; use a zero sequence
        // sentinel for no pending fish so null never changes meaning on reload.
        public FishCatch candidate = new FishCatch();
        public FishCatch[] recent = new FishCatch[0];
        public FishEntry[] entries = NewEntries();
        static FishEntry[] NewEntries()
        { var result = new FishEntry[FishingCatalog.Species.Length]; for (int i=0;i<result.Length;i++) result[i]=new FishEntry(); return result; }
    }
    // Only the visible foreground fishing view advances this clock. Ordinary
    // Economy.Advance/OfflineSettlement never catches fish or consumes this RNG.
    public sealed class FishingRules
    {
        readonly Economy economy;
        GameState S => economy.State;
        FishingProgress F => S.fishing;
        public FishingRules(Economy owner) { economy=owner; }
        public int Discovered { get { int n=0; foreach(var entry in F.entries) if(entry.count>0) n++; return n; } }
        public bool SpotUnlocked(int spot) => spot>=0 && spot<3 && S.forest.started && S.forest.bridge && (spot<2 || S.forest.vines);
        public bool BaitUnlocked(int bait) => bait>=0 && bait<3 && (bait==0 || Discovered >= bait*3);
        int Next(int limit) { F.random=(int)((long)F.random*48271%2147483647); return F.random%limit; }
        public bool Start(int spot, int bait)
        {
            if (!SpotUnlocked(spot) || S.forest.location!=spot || !BaitUnlocked(bait)) return false;
            bool same=F.spot==spot && F.bait==bait && F.phase!=0;
            if(F.running && same) return false;
            F.spot=spot; F.bait=bait; F.running=true;
            if(!same) Queue(); return true;
        }
        public bool Pause() { if(!F.running) return false; F.running=false; return true; }
        void Queue()
        {
            F.phase=1; F.casts++; F.remaining=F.duration=6+Next(51)/10.0;
            int total=0;
            foreach(var fish in FishingCatalog.Species) if((fish.Spots & (1<<F.spot))!=0) total+=fish.Weight*(fish.Bait==F.bait?2:1);
            int roll=Next(total), species=0;
            for(int i=0;i<FishingCatalog.Species.Length;i++)
            {
                var fish=FishingCatalog.Species[i]; if((fish.Spots & (1<<F.spot))==0) continue;
                roll-=fish.Weight*(fish.Bait==F.bait?2:1); if(roll<0) { species=i; break; }
            }
            var kind=FishingCatalog.Species[species]; int qualityRoll=Math.Min(999,Next(1000)+F.bait*20+(kind.Rarity>=2?25:0));
            int quality=qualityRoll<600?0:qualityRoll<850?1:qualityRoll<970?2:3;
            int size=Math.Min(1000,Next(1001)+quality*70);
            int length=kind.MinLength+(kind.MaxLength-kind.MinLength)*size/1000;
            double fraction=(double)(length-kind.MinLength)/(kind.MaxLength-kind.MinLength);
            int weight=(int)Math.Round(kind.MinWeight+(kind.MaxWeight-kind.MinWeight)*Math.Pow(fraction,2.5)*(0.9+Next(21)/100.0));
            weight=Math.Max(kind.MinWeight,Math.Min(kind.MaxWeight,weight));
            F.candidate=new FishCatch { sequence=F.totalCaught+1,species=species,quality=quality,lengthMm=length,weightGrams=weight,spot=F.spot,bait=F.bait };
        }
        void Land()
        {
            var fish=F.candidate; var entry=F.entries[fish.species];
            entry.count++; entry.qualityCounts[fish.quality]++;
            entry.longestMm=Math.Max(entry.longestMm,fish.lengthMm); entry.heaviestGrams=Math.Max(entry.heaviestGrams,fish.weightGrams);
            entry.bestQuality=Math.Max(entry.bestQuality,fish.quality); if(entry.firstSequence==0) entry.firstSequence=fish.sequence;
            var recent=new FishCatch[Math.Min(FishingCatalog.RecentLimit,F.recent.Length+1)]; recent[0]=fish;
            Array.Copy(F.recent,0,recent,1,recent.Length-1); F.recent=recent; F.totalCaught++; F.candidate=new FishCatch();
            F.phase=3; F.remaining=F.duration=2.2;
        }
        public void Advance(double seconds)
        {
            if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!F.running) return;
            const double epsilon=0.00000001;
            while(seconds>epsilon)
            {
                if(seconds<F.remaining-epsilon) { F.remaining-=seconds; return; }
                seconds=Math.Max(0,seconds-F.remaining); F.remaining=0;
                if(F.phase==1) { F.phase=2; F.remaining=F.duration=1.8; }
                else if(F.phase==2) Land();
                else Queue();
            }
        }
        public static void Check(GameState state)
        {
            var f=state.fishing;
            if(f==null || f.phase<0 || f.phase>3 || f.spot<0 || f.spot>2 || f.bait<0 || f.bait>2 || f.casts<0 || f.totalCaught<0 || f.casts<f.totalCaught || f.random<1 || f.random>=2147483647 || double.IsNaN(f.remaining) || double.IsInfinity(f.remaining) || double.IsNaN(f.duration) || double.IsInfinity(f.duration) || f.remaining<0 || f.remaining>f.duration || f.duration<0 || f.duration>11 || f.entries==null || f.entries.Length!=FishingCatalog.Species.Length || f.recent==null || f.recent.Length!=Math.Min(FishingCatalog.RecentLimit,f.totalCaught)) throw new InvalidDataException("钓鱼记录结构无效");
            bool hasCandidate=f.candidate!=null && f.candidate.sequence>0;
            bool emptyCandidate=f.candidate==null || f.candidate.sequence==0 && f.candidate.species==0 && f.candidate.quality==0 && f.candidate.lengthMm==0 && f.candidate.weightGrams==0 && f.candidate.spot==0 && f.candidate.bait==0;
            if((f.phase==0 && (f.running || f.remaining!=0 || f.duration!=0 || hasCandidate)) || (f.phase!=0 && (f.remaining<=0 || !state.forest.bridge || f.spot==2 && !state.forest.vines)) || ((f.phase==1 || f.phase==2)!=hasCandidate) || ((f.phase==1 || f.phase==2) && f.casts<=f.totalCaught) || (f.phase==1 && f.duration<6) || (f.phase==3 && f.totalCaught==0) || f.phase==2 && f.duration!=1.8 || f.phase==3 && f.duration!=2.2 || f.totalCaught>0 && !state.forest.bridge || !hasCandidate && !emptyCandidate) throw new InvalidDataException("垂钓阶段无效");
            long total=0; int discovered=0;
            for(int i=0;i<f.entries.Length;i++)
            {
                var e=f.entries[i]; var kind=FishingCatalog.Species[i];
                if(e==null || e.count<0 || e.qualityCounts==null || e.qualityCounts.Length!=4) throw new InvalidDataException("鱼类品质统计无效");
                long count=0; int best=-1; for(int q=0;q<4;q++) { if(e.qualityCounts[q]<0) throw new InvalidDataException("鱼类品质数量无效"); count+=e.qualityCounts[q]; if(e.qualityCounts[q]>0) best=q; }
                if(count!=e.count || e.bestQuality!=best || e.count==0 && (e.firstSequence!=0 || e.longestMm!=0 || e.heaviestGrams!=0) || e.count>0 && (e.firstSequence<1 || e.firstSequence>f.totalCaught || e.longestMm<kind.MinLength || e.longestMm>kind.MaxLength || e.heaviestGrams<kind.MinWeight || e.heaviestGrams>kind.MaxWeight)) throw new InvalidDataException("鱼类体型纪录无效");
                total+=e.count; if(e.count>0) discovered++;
            }
            if(total!=f.totalCaught || f.bait>0 && discovered<f.bait*3) throw new InvalidDataException("鱼谱总量或饵料开放状态无效");
            for(int i=0;i<f.recent.Length;i++)
            {
                var recent=f.recent[i]; CheckCatch(recent); var entry=f.entries[recent.species];
                if(recent.sequence!=f.totalCaught-i || entry.count<1 || entry.qualityCounts[recent.quality]<1 || entry.longestMm<recent.lengthMm || entry.heaviestGrams<recent.weightGrams || entry.firstSequence>recent.sequence) throw new InvalidDataException("最近钓获次序或纪录无效");
            }
            if(hasCandidate) { CheckCatch(f.candidate); if(f.candidate.sequence!=f.totalCaught+1 || f.candidate.spot!=f.spot || f.candidate.bait!=f.bait) throw new InvalidDataException("待钓鱼记录无效"); }
        }
        static void CheckCatch(FishCatch fish)
        {
            if(fish==null || fish.sequence<1 || fish.species<0 || fish.species>=FishingCatalog.Species.Length || fish.quality<0 || fish.quality>3 || fish.spot<0 || fish.spot>2 || fish.bait<0 || fish.bait>2) throw new InvalidDataException("钓获参数无效");
            var kind=FishingCatalog.Species[fish.species];
            if((kind.Spots & (1<<fish.spot))==0 || fish.lengthMm<kind.MinLength || fish.lengthMm>kind.MaxLength || fish.weightGrams<kind.MinWeight || fish.weightGrams>kind.MaxWeight) throw new InvalidDataException("钓获体型或水域无效");
        }
    }
}
