using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ForestShop.Core
{
    public interface IStateCodec { string Encode(GameState state); GameState Decode(string text); }

    public static class StateValidation
    {
        public static void Check(GameState s)
        {
            if (s == null || s.version != 1 || s.coins < 0 || s.reputation < 0 || s.sales < 0 || s.warehouse < 1 || s.warehouse > 4 || s.stories < 0 || s.stories > 5 || s.tutorial < 0 || s.tutorial > 4)
                throw new InvalidDataException("存档版本或经营数据无效");
            CheckArray(s.crops, 4); CheckArray(s.items, 6); CheckArray(s.sold, 6); CheckArray(s.friendship, 5);
            if (s.decoration == null || s.decoration.Length != 4 || s.report == null || s.orders == null || s.orders.Length > 3) throw new InvalidDataException("存档结构无效");
            CheckArray(s.report.cropDelta, 4, true); CheckArray(s.report.itemDelta, 6, true);
            CheckFacilities(s.plots, 1, 6, 4); CheckFacilities(s.machines, 1, 2, 6); CheckFacilities(s.shelves, 1, 4, 6);
            int active = 0; foreach (var o in s.orders)
            {
                if (o == null || o.id < 1 || o.resident < 0 || o.resident > 4 || o.item < 0 || o.item > 5 || o.count < 1 || o.count > 8 || (o.active && o.cancelled)) throw new InvalidDataException("委托无效");
                if (o.active) active++;
            }
            if (active > 1 || double.IsNaN(s.playedSeconds) || double.IsInfinity(s.playedSeconds) || s.playedSeconds < 0 || s.utcTicks < 0 || s.utcTicks > DateTime.MaxValue.Ticks) throw new InvalidDataException("时间或接单状态无效");
            int cap = Catalog.Capacity[s.warehouse - 1]; var e = new Economy(s);
            for (int i = 0; i < 4; i++) if (s.crops[i] > cap) throw new InvalidDataException("原料容量无效");
            for (int i = 0; i < 6; i++) if (s.items[i] + e.Incoming(i) > cap) throw new InvalidDataException("商品容量无效");
            for (int i = 0; i < s.machines.Length; i++)
                if ((s.machines[i].config >= 0 && Catalog.Machine[s.machines[i].config] != i) || (s.machines[i].workItem >= 0 && Catalog.Machine[s.machines[i].workItem] != i)) throw new InvalidDataException("设备配方无效");
        }
        private static void CheckArray(int[] a, int length, bool allowNegative = false)
        { if (a == null || a.Length != length) throw new InvalidDataException("存档数组无效"); foreach (int v in a) if (!allowNegative && v < 0) throw new InvalidDataException("库存无效"); }
        private static void CheckFacilities(Facility[] fs, int min, int max, int configs)
        {
            if (fs == null || fs.Length < min || fs.Length > max) throw new InvalidDataException("设施数量无效");
            foreach (var f in fs)
                if (f == null || f.level < 1 || f.level > 5 || f.config < -1 || f.config >= configs || f.pending < -2 || f.pending >= configs || f.workItem < -1 || f.workItem >= configs || double.IsNaN(f.remaining) || double.IsInfinity(f.remaining) || f.remaining < -1 || f.remaining > 3600)
                    throw new InvalidDataException("设施数据无效");
        }
    }

    public sealed class SaveStore
    {
        private readonly string path;
        private readonly IStateCodec codec;
        private long revision;
        public string RecoveryMessage { get; private set; }
        public SaveStore(string directory, IStateCodec stateCodec) { Directory.CreateDirectory(directory); path = Path.Combine(directory, "forest-shop.save"); codec = stateCodec; }
        public GameState Load()
        {
            GameState best = null; long highest = -1; string source = null; bool any = false;
            foreach (string file in new[] { path, path + ".tmp", path + ".bak" })
            {
                if (!File.Exists(file)) continue; any = true;
                try
                {
                    long seq; var state = Read(file, out seq);
                    if (seq > highest) { highest = seq; best = state; source = file; }
                }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is FormatException || e is ArgumentException || e is InvalidOperationException || e is OverflowException) { }
            }
            if (best == null && any) throw new InvalidDataException("主存档和备份均无法读取。原文件已保留，请在设置中明确选择重新开始。");
            revision = Math.Max(0, highest);
            RecoveryMessage = best != null && source != path ? "已从有效备份恢复进度。" : null;
            return best;
        }
        public void Save(GameState state)
        {
            StateValidation.Check(state);
            string payload = codec.Encode(state); string content = "FORESTSHOP1\n" + (++revision) + "\n" + Hash(payload) + "\n" + payload;
            byte[] bytes = new UTF8Encoding(false).GetBytes(content);
            using (var stream = new FileStream(path + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None)) { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
            if (File.Exists(path))
            {
                // Do not replace a valid backup with an already corrupted primary.
                bool valid = false; try { long seq; Read(path, out seq); valid = true; } catch (Exception) { }
                if (valid)
                {
                    try { File.Replace(path + ".tmp", path, path + ".bak"); return; }
                    catch (PlatformNotSupportedException) { }
                    catch (IOException) { }
                    File.Copy(path, path + ".bak", true);
                }
                File.Delete(path);
            }
            // A crash before this rename is recovered from the checksummed .tmp or .bak.
            File.Move(path + ".tmp", path);
        }
        private GameState Read(string file, out long seq)
        {
            string text = File.ReadAllText(file, Encoding.UTF8);
            int a = text.IndexOf('\n'), b = text.IndexOf('\n', a + 1), c = text.IndexOf('\n', b + 1);
            if (a < 0 || b < 0 || c < 0 || text.Substring(0, a) != "FORESTSHOP1") throw new InvalidDataException("存档格式无效");
            seq = long.Parse(text.Substring(a + 1, b - a - 1), System.Globalization.CultureInfo.InvariantCulture);
            string payload = text.Substring(c + 1);
            if (seq < 1 || Hash(payload) != text.Substring(b + 1, c - b - 1)) throw new InvalidDataException("存档校验失败");
            GameState result = codec.Decode(payload); StateValidation.Check(result); return result;
        }
        public void ArchiveAndReset()
        {
            string suffix = ".reset-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
            foreach (string p in new[] { path, path + ".bak", path + ".tmp" }) if (File.Exists(p)) File.Move(p, p + suffix);
            revision = 0;
        }
        private static string Hash(string text)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", ""); }
    }

    public static class OfflineSettlement
    {
        public static void Resume(GameState s, long nowTicks)
        {
            if (s.report.pending) return;
            double away = s.utcTicks > 0 ? Math.Max(0, (nowTicks - s.utcTicks) / (double)TimeSpan.TicksPerSecond) : 0;
            double effective = Math.Min(28800, away);
            var cropBefore = (int[])s.crops.Clone(); var itemBefore = (int[])s.items.Clone(); long before = s.coins;
            if (effective > 0) new Economy(s).Advance(effective);
            s.utcTicks = Math.Max(s.utcTicks, nowTicks);
            s.report = new OfflineReport { pending = away >= 30, away = away, effective = effective, coins = s.coins - before };
            for (int i = 0; i < 4; i++) s.report.cropDelta[i] = s.crops[i] - cropBefore[i];
            for (int i = 0; i < 6; i++) s.report.itemDelta[i] = s.items[i] - itemBefore[i];
            var engine = new Economy(s); string hint = ""; int count = 0;
            foreach (var m in s.machines)
                if (m.remaining < 0 && count < 2) { hint += engine.MachineStatus(m) + "\n"; count++; }
            if (count < 2 && engine.ActiveOrder != null && engine.ActiveOrder.reserve) hint += "委托商品已预留，达到需求前暂停出售。";
            s.report.hint = hint;
        }
        public static void CloseReport(GameState s, long nowTicks)
        { s.report.pending = false; s.utcTicks = Math.Max(s.utcTicks, nowTicks); }
        public static void SaveOnlineTime(GameState s, long nowTicks)
        { s.utcTicks = Math.Max(s.utcTicks, nowTicks); }
    }
}
