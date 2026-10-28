using ForestShop.Core;

namespace ForestShop
{
    // A read-only view of existing progress. Greeting never advances a story,
    // consumes inventory or marks a dialogue as read.
    public static class ForestResidentNotes
    {
        static readonly string[] Before = {
            "我把今天的课本带来了。路过这里闻一闻茶香，脑袋也清醒了一点。",
            "月见花的花苞又鼓起来了。我想慢慢等它自己张开。",
            "我刚刚还记得那棵树的样子呢。先在这里歇一会儿，说不定就想起来了。",
            "信都在包里放好了。趁下一趟送信前，来看看小店。",
            "我出门前又看了一眼烤炉。做面包这件事，还是慢一点才安心。" };
        static readonly string[] After = {
            "今天的早课，我把故事讲到了最后一页。孩子们还想听一遍呢。",
            "昨晚月见花开了。我坐在旁边，听见了很轻的虫鸣。",
            "这回我把坚果的位置画下来了。画得有一点像面包，不过我自己认得！",
            "那封信已经送到啦。收信的人笑了好久，我也跟着高兴。",
            "今天的面包颜色刚刚好。我给出炉的那一排留了最暖的位置。" };
        static readonly string[] Familiar = {
            "课本里又夹进了一片落叶。我先替它留着，明天拿给孩子们看。",
            "新长出来的小叶子很柔软。我浇水时总要再轻一点。",
            "我在本子上画了三颗坚果。这次连本子放在哪里，也一起记下来了。",
            "今天的信袋轻了些。我可以沿着林间小路，慢慢走完最后一程。",
            "揉面时闻到了麦香。明天还想试试，把面包边烤得再脆一点。" };

        public static bool Known(GameState state, int resident)
            => state != null && resident >= 0 && resident < Catalog.Residents.Length && resident <= state.stories;

        public static string Read(GameState state, int resident)
        {
            if (!Known(state, resident)) return null;
            if (state.stories <= resident) return Before[resident];
            return state.friendship[resident] >= 25 ? Familiar[resident] : After[resident];
        }
    }
}
