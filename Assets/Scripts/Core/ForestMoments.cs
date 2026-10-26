using System;
using System.IO;

namespace ForestShop.Core
{
    [Serializable]
    public sealed class ForestMomentsProgress
    {
        // 0 untouched, 1 clue observed, 2 object found / choice pending, 3 recorded.
        public int[] stages = new int[3];
        public int[] choices = { -1, -1, -1 };
    }

    // Optional, player-driven keepsakes. No inventory, rewards, timers or RNG.
    public sealed class ForestMomentRules
    {
        public static readonly string[] Titles = { "松鼠的纽扣", "顺流的问候", "给后来的人" };
        public static readonly string[] FirstClues = { "坚果痕迹的树洞", "岸边的小足迹", "旧亭旁的叶片留言" };
        public static readonly string[] Objects = { "落叶中的纽扣", "漂来的小信", "旧亭留言簿" };
        public static readonly string[] ClueHints = { "叶子下面，有一点铜色的光。", "脚印停在岸边，瓶子轻轻靠了过来。", "叶片上画着一本书，就在台阶旁。" };
        public static readonly string[] Questions = { "让回来找纽扣的松鼠，一眼就能看见。", "把瓶中的问候，留在下一位旅人的路上。", "在留言簿里，给后来的人留一个记号。" };
        public static readonly string[][] Choices = {
            new[] { "树洞小木盒", "路边布袋" },
            new[] { "岸边瓶铃", "芦苇小架" },
            new[] { "叶形书签", "星形书签" } };
        readonly GameState state;
        public ForestMomentRules(GameState owner) { state = owner; }
        public static bool Unlocked(GameState s, int id)
        {
            if (s == null || s.forest == null || id < 0 || id > 2 || !s.chapterComplete || !s.forest.started) return false;
            return id == 0 || id == 1 && s.forest.bridge || id == 2 && s.forest.vines && s.forest.pavilion;
        }
        public bool Available(int id) => Unlocked(state, id);
        public bool Here(int id) => Available(id) && state.forest.location == id;
        public int Completed { get { int n = 0; foreach (var stage in state.moments.stages) if (stage == 3) n++; return n; } }
        public bool Observe(int id, int clue)
        {
            if (!Here(id) || clue < 0 || clue > 1 || state.moments.stages[id] != clue) return false;
            state.moments.stages[id]++; return true;
        }
        public bool Choose(int id, int choice)
        {
            if (!Here(id) || choice < 0 || choice > 1 || state.moments.stages[id] < 2 || state.moments.choices[id] == choice) return false;
            state.moments.stages[id] = 3; state.moments.choices[id] = choice; return true;
        }
        public string Status(int id)
        {
            if (!Available(id)) return id == 0 ? "进入林径后可发现" : id == 1 ? "修好溪边木桥后可发现" : "修好林间旧亭后可发现";
            int stage = state.moments.stages[id];
            return stage == 0 ? "观察 · " + FirstClues[id] : stage == 1 ? (id == 2 ? "翻阅 · " : "寻找 · ") + Objects[id] : stage == 2 ? "为小物选择摆放" : "已留下 · " + Choices[id][state.moments.choices[id]];
        }
        public static void Check(GameState s)
        {
            var m = s.moments;
            if (m == null || m.stages == null || m.choices == null || m.stages.Length != 3 || m.choices.Length != 3) throw new InvalidDataException("林间小事结构无效");
            for (int i = 0; i < 3; i++)
            {
                int stage = m.stages[i], choice = m.choices[i];
                if (stage < 0 || stage > 3 || (stage < 3 ? choice != -1 : choice < 0 || choice > 1)) throw new InvalidDataException("林间小事步骤或摆放无效");
                if (stage > 0 && (!Unlocked(s, i) || (s.forest.visited & (1 << i)) == 0)) throw new InvalidDataException("林间小事尚未开放");
            }
        }
    }
}
