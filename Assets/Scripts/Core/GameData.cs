using System;

namespace ForestShop.Core
{
    public static class Catalog
    {
        public static readonly string[] Crops = { "月光薄荷", "发光蘑菇", "铃铛花", "荷叶" };
        public static readonly string[] Items = { "醒神茶", "萤光瓶", "寻物粉", "避雨药水", "暖炉药剂", "安眠香" };
        public static readonly string[] Residents = { "猫头鹰老师", "刺猬园丁", "松鼠居民", "兔子邮差", "小熊面包师" };
        public static readonly string[] Stories = { "老师的第一堂早课", "夜里开花的朋友", "坚果到底藏在哪", "不能淋湿的信", "面包出炉的温度" };
        public static readonly string[] Descriptions = {
            "温和又有点迷糊的老师，白天上课总容易犯困。",
            "细心的园丁，想看看夜里悄悄开放的花。",
            "热心的邻居，却总忘记把坚果藏在哪里。",
            "活泼而守时的邮差，雨天也要保护好每一封信。",
            "慢性子的面包师，希望每个面包都有温暖的香气。" };
        public static readonly string[] Before = {
            "新店主，早安！明天有一堂早课，可以请你帮我准备三杯醒神茶吗？",
            "我的月见花只在夜里开放。四瓶萤光能照亮它们，又不会惊醒花朵。",
            "唔，我把坚果藏在一棵很特别的树下……可森林里的树都很特别呀！",
            "最近的阵雨总来得突然。想请你做些避雨药水，让远方的心意平安到家。",
            "烤炉忽冷忽热，面包都要闹脾气啦。六份暖炉药剂应该能帮上忙。" };
        public static readonly string[] After = {
            "茶香把清晨都唤醒了！孩子们今天一定能听到完整的故事。谢谢你，店主。",
            "花瓣在小小的光里舒展开了。送你一副叶纹窗帘，把森林的绿意留在窗边吧。",
            "找到了！原来就在我家门口。下次我一定……先把位置记在本子上！",
            "信纸一点都没湿。有人等了很久的问候，终于可以准时送到了。",
            "今天每个面包都烤得刚刚好。送你一块面包纹地毯，愿小店一直暖洋洋的。" };
        public static readonly double[] CropSeconds = { 20, 30, 40, 50 };
        public static readonly double[] ItemSeconds = { 15, 20, 35, 30, 25, 45 };
        public static readonly int[] Price = { 12, 18, 42, 48, 30, 54 };
        public static readonly int[] Machine = { 0, 1, 1, 0, 0, 1 };
        public static readonly int[] Unlock = { 0, 10, 25, 45, 60, 100 };
        public static readonly int[][] Materials = {
            new[] { 1, 0, 0, 0 }, new[] { 0, 1, 0, 0 }, new[] { 0, 1, 1, 0 },
            new[] { 1, 0, 0, 2 }, new[] { 1, 1, 0, 0 }, new[] { 1, 0, 2, 0 } };
        public static readonly double[] Speed = { 1, 1.25, 1.55, 1.90, 2.30 };
        public static readonly int[] Upgrade = { 0, 120, 360, 1000, 2800 };
        public static readonly int[] Capacity = { 20, 40, 80, 160 };
        public static readonly int[] WarehouseCost = { 0, 300, 1200, 4000 };
        public const int MaxPlots = 12;
        public static readonly int[] PlotCost = { 0, 80, 500, 1200, 2500, 4000, 6000, 8500, 11500, 15000, 19000, 24000 };
        public static readonly int[] PlotRep = { 0, 0, 25, 45, 100, 100, 100, 100, 100, 100, 100, 100 };
        public static readonly int[] ShelfCost = { 0, 350, 1600, 3200 };
        public static readonly int[] StoryItem = { 0, 1, 2, 3, 4 };
        public static readonly int[] StoryCount = { 3, 4, 5, 6, 6 };
        public static readonly int[] StoryCoins = { 50, 100, 290, 390, 250 };
        public static readonly int[] StoryRep = { 10, 15, 20, 25, 30 };
    }

    [Serializable]
    public class Facility
    {
        public int level = 1;
        public int config = -1;
        // -2 means no pending change; -1 is an intentionally unconfigured facility.
        public int pending = -2;
        public int workItem = -1;
        // -1 idle, 0 waiting to deposit, positive means working.
        public double remaining = -1;
    }

    [Serializable]
    public class Order
    {
        public int id;
        public int resident;
        public int item;
        public int count;
        public bool story;
        public bool active;
        public bool reserve = true;
        public bool cancelled;
    }

    [Serializable]
    public class OfflineReport
    {
        public bool pending;
        public double away;
        public double effective;
        public long coins;
        public int[] cropDelta = new int[4];
        public int[] itemDelta = new int[6];
        public string hint = "";
    }

    [Serializable]
    public class GameState
    {
        public int version = 1;
        public long coins = 50;
        public int reputation;
        public long sales;
        public long harvests;
        public long crafts;
        public int[] sold = new int[6];
        public int[] crops = new int[4];
        public int[] items = new int[6];
        public int[] friendship = new int[5];
        public int warehouse = 1;
        public Facility[] plots = { new Facility { config = 0, workItem = 0, remaining = 0 } };
        public Facility[] machines = { new Facility() };
        public Facility[] shelves = { new Facility { remaining = 8 } };
        public Order[] orders = new Order[0];
        public int stories;
        public int tutorial;
        public bool welcomeRead;
        public bool sideRoom;
        public bool chapterComplete;
        public bool chapterCelebrated;
        public int dialogue = -1;
        public bool[] decoration = new bool[4];
        public bool music = true;
        public bool sound = true;
        public bool simpleAnimation;
        public long utcTicks;
        public double refreshSeconds;
        public int nextOrderId = 1;
        public uint random = 9527;
        public double playedSeconds;
        public OfflineReport report = new OfflineReport();
    }
}
