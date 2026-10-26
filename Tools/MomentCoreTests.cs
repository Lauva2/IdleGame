using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ForestShop.Core;

public static class MomentCoreTests
{
    const string Native = "Docs/Verification/3d-d24-experience02/normal-restorable.save";
    static readonly TestCodec Codec = new TestCodec();
    static void Check(bool ok, string why) { if (!ok) throw new Exception(why); }
    static GameState Read() { var s = Codec.Decode(File.ReadAllText(Native).Split(new[] { '\n' }, 4)[3]); StateMigration.Upgrade(s); return s; }
    static GameState Copy(GameState s) => Codec.Decode(Codec.Encode(s));
    static void Wrapped(string path, GameState s, long revision)
    {
        string text = Codec.Encode(s), hash;
        using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        File.WriteAllText(path, "FORESTSHOP1\n" + revision + "\n" + hash + "\n" + text, new UTF8Encoding(false));
    }
    public static void Run(Action<string, Action> test)
    {
        test("actual v3 ordinary save migrates to v4 retaining every previous field", () => {
            var raw = File.ReadAllBytes(Native); var s = Codec.Decode(Encoding.UTF8.GetString(raw).Split(new[] { '\n' }, 4)[3]);
            Check(s.version == 3 && s.fishing.totalCaught == 55, "wrong actual legacy evidence");
            var expected = Copy(s); expected.version = GameState.CurrentVersion; expected.moments = new ForestMomentsProgress();
            StateMigration.Upgrade(s); Check(Codec.Encode(s) == Codec.Encode(expected), "v3 migration changed prior state");
            Check(Convert.ToBase64String(raw) == Convert.ToBase64String(File.ReadAllBytes(Native)), "historical evidence modified"); StateValidation.Check(s);
        });
        test("moment actions cannot bypass chapter forest bridge pavilion or actual location", () => {
            var s = new GameState(); var e = new Economy(s); string before = Codec.Encode(s);
            Check(!e.Moments.Observe(0, 0) && !e.Moments.Choose(0, 0) && Codec.Encode(s) == before, "opened before chapter");
            s = Read(); s.orders = new Order[0]; s.forest = new ForestProgress { started = true, visited = 1 }; s.fishing = new FishingProgress(); e = new Economy(s); StateValidation.Check(s);
            before = Codec.Encode(s); Check(!e.Moments.Observe(1, 0) && !e.Moments.Observe(2, 0) && Codec.Encode(s) == before, "locked regions advanced");
            s = Read(); e = new Economy(s); e.Forest.Visit(1); before = Codec.Encode(s);
            Check(!e.Moments.Observe(0, 0) && Codec.Encode(s) == before, "distant clue advanced");
            s.forest.pavilion = false; s.forest.celebrationSeen = false; e.Forest.Visit(2); before = Codec.Encode(s);
            Check(!e.Moments.Observe(2, 0) && Codec.Encode(s) == before, "old pavilion not repaired"); StateValidation.Check(s);
        });
        test("three deliberate actions complete each event and stale double taps do not skip", () => {
            var s = Read(); var e = new Economy(s);
            for (int i = 0; i < 3; i++)
            {
                Check(e.Forest.Visit(i), "visit"); string before = Codec.Encode(s);
                Check(!e.Moments.Observe(i, 1) && !e.Moments.Choose(i, 0) && Codec.Encode(s) == before, "out-of-order choice");
                Check(e.Moments.Observe(i, 0) && !e.Moments.Observe(i, 0) && s.moments.stages[i] == 1, "clue tap doubled");
                Check(e.Moments.Observe(i, 1) && !e.Moments.Observe(i, 1) && s.moments.stages[i] == 2, "object tap doubled");
                Check(e.Moments.Choose(i, i % 2) && !e.Moments.Choose(i, i % 2), "choice not idempotent"); StateValidation.Check(s);
            }
            Check(e.Moments.Completed == 3, "not three keepsakes");
        });
        test("both keepsake arrangements remain changeable without changing completion", () => {
            var s = Read(); var e = new Economy(s); e.Forest.Visit(1); e.Moments.Observe(1, 0); e.Moments.Observe(1, 1); e.Moments.Choose(1, 0);
            Check(e.Moments.Choose(1, 1) && e.Moments.Completed == 1 && s.moments.stages[1] == 3, "alternate arrangement lost progress");
            Check(e.Moments.Choose(1, 0) && !e.Moments.Choose(1, 0), "arrangement cannot return or saves again"); StateValidation.Check(s);
        });
        test("moment actions leave money inventory quests fish and both RNG streams exact", () => {
            var s = Read(); var e = new Economy(s);
            for (int id = 0; id < 3; id++)
            {
                e.Forest.Visit(id); var before = Copy(s);
                e.Moments.Observe(id, 0); e.Moments.Observe(id, 1); e.Moments.Choose(id, 0); e.Moments.Choose(id, 1);
                var after = Copy(s); after.moments = before.moments; Check(Codec.Encode(after) == Codec.Encode(before), "cosmetic event changed original economy");
            }
        });
        test("time and offline reports neither advance moments nor erase a pending choice", () => {
            var s = Read(); var e = new Economy(s); e.Forest.Visit(0); e.Moments.Observe(0, 0); e.Moments.Observe(0, 1);
            string moments = string.Join(",", s.moments.stages) + ":" + string.Join(",", s.moments.choices);
            e.Advance(28800); OfflineSettlement.Resume(s, s.utcTicks + TimeSpan.FromHours(12).Ticks);
            Check(moments == string.Join(",", s.moments.stages) + ":" + string.Join(",", s.moments.choices), "clock changed optional progress"); StateValidation.Check(s);
        });
        test("each persisted event phase resumes through SaveStore and cannot complete twice", () => {
            for (int phase = 0; phase <= 3; phase++)
            {
                var s = Read(); var e = new Economy(s); e.Forest.Visit(2);
                if (phase >= 1) e.Moments.Observe(2, 0); if (phase >= 2) e.Moments.Observe(2, 1); if (phase >= 3) e.Moments.Choose(2, 1);
                string dir = Path.Combine("Tools/TestOutput", Guid.NewGuid().ToString("N")); var store = new SaveStore(dir, Codec); store.Save(s);
                var loaded = new SaveStore(dir, Codec).Load(); Check(Codec.Encode(loaded) == Codec.Encode(s), "event reload lost fields");
                var rule = new Economy(loaded).Moments; if (phase == 3) Check(!rule.Choose(2, 1) && rule.Completed == 1, "completed event repeated"); StateValidation.Check(loaded);
            }
        });
        test("invalid current moment arrays choices and impossible gates select a valid backup", () => {
            for (int defect = 0; defect < 6; defect++)
            {
                var good = Read(); var bad = Copy(good);
                if (defect == 0) bad.moments = null;
                else if (defect == 1) bad.moments.stages = new int[2];
                else if (defect == 2) bad.moments.stages[0] = 4;
                else if (defect == 3) bad.moments.choices[0] = 0;
                else if (defect == 4) bad.moments.stages[0] = 3;
                else { bad.moments.stages[2] = 1; bad.forest.pavilion = false; bad.forest.celebrationSeen = false; }
                string dir = Path.Combine("Tools/TestOutput", Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
                Wrapped(Path.Combine(dir, "forest-shop.save.bak"), good, 1); Wrapped(Path.Combine(dir, "forest-shop.save"), bad, 2);
                var store = new SaveStore(dir, Codec); var loaded = store.Load();
                Check(store.RecoveryMessage != null && Codec.Encode(loaded) == Codec.Encode(good), "damaged current moment state silently repaired");
            }
        });
        test("reading status and rejecting invalid IDs never edits any progress", () => {
            var s = Read(); var e = new Economy(s); string before = Codec.Encode(s);
            for (int i = 0; i < 3; i++) Check(e.Moments.Status(i).Length > 0, "empty status");
            Check(!e.Moments.Observe(-1, 0) && !e.Moments.Observe(3, 0) && !e.Moments.Observe(0, -1) && !e.Moments.Choose(0, 2), "invalid input accepted");
            Check(Codec.Encode(s) == before, "read or rejected action mutated progress");
        });
        test("optional events commute with existing deterministic production and offline income", () => {
            var a = Read(); var b = Copy(a); var x = new Economy(a); var y = new Economy(b);
            x.Forest.Visit(0); y.Forest.Visit(0); x.Moments.Observe(0, 0); x.Moments.Observe(0, 1); x.Moments.Choose(0, 1);
            x.Advance(28800); y.Advance(28800); a.moments = b.moments;
            Check(Codec.Encode(a) == Codec.Encode(b), "event choices changed production timing random or earnings");
        });
    }
}
