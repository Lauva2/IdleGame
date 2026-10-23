using System;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        RectTransform productionFlow;
        readonly System.Collections.Generic.List<Button> navigationButtons = new System.Collections.Generic.List<Button>();
        ForestSymbolGraphic Symbol(Transform parent, ForestSymbol symbol, float x, float y, float size, bool light = false)
        {
            var graphic = Rect(parent, "Symbol " + symbol, x, y, size, size).gameObject.AddComponent<ForestSymbolGraphic>();
            graphic.Symbol = symbol; graphic.Light = light; graphic.raycastTarget = false; return graphic;
        }
        Button IconButton(Transform parent, string caption, ForestSymbol symbol, float x, float y, float width, float height, Action action, bool enabled = true, bool primary = false)
        {
            var button = Button(parent, caption, x, y, width, height, action, enabled, primary);
            float size = Mathf.Min(58, height - 20);
            var icon = Symbol(button.transform, symbol, caption.Length == 0 ? (width - size) / 2 : 20, (height - size) / 2, size, primary);
            var text = button.GetComponentInChildren<Text>();
            text.rectTransform.anchoredPosition = new Vector2(size + 32, -5);
            text.rectTransform.sizeDelta = new Vector2(width - size - 44, height - 10);
            button.GetComponent<ForestButtonVisual>().icon = icon;
            // Keep compact neighbouring controls from overlapping through the
            // generic button's invisible minimum-height touch extension.
            EnlargeTouch(button, 0, height);
            return button;
        }
        void RefreshHeader()
        {
            coinsLabel.text = State.coins.ToString("N0"); reputationLabel.text = State.reputation.ToString();
            string[] opening = { "收下第一份薄荷", "制作第一杯醒神茶", "为货架选择商品", "等待第一笔收入" };
            goalLabel.text = State.tutorial < 4 ? opening[Math.Max(0, State.tutorial)]
                : State.chapterComplete ? "开张茶会已完成" : State.stories == 0 ? "老师的第一份委托"
                : State.machines.Length == 1 ? "修复附魔工作台"
                : State.stories < 5 ? "邻居心愿  " + State.stories + "/5" : !State.sideRoom ? "修复侧屋"
                : State.reputation < 150 ? "小店声望  " + State.reputation + "/150"
                : State.sales < 60000 ? "累计销售  " + State.sales.ToString("N0") + "/60,000" : "商品首售  " + SoldKinds() + "/6";
        }
        void RefreshNavigation(string name)
        {
            foreach (var button in navigationButtons)
            {
                bool selected = button.GetComponentInChildren<Text>().text == name;
                var visual = button.GetComponent<ForestButtonVisual>();
                visual.normalBackground = selected ? Sage : Sand; visual.normalText = selected ? Paper : Ink;
                ((ForestSymbolGraphic)visual.icon).Light = selected;
            }
        }
        void ShowGoal()
        {
            StartSheet("第一章 · 开张茶会", "goal");
            Paragraph(Engine.Goal, 160, 32);
            Row("熟客故事", "五位邻居的故事", -1, State.stories + "/5", null);
            Row("小店声望", "完成委托提升声望", 14, State.reputation + "/150", null);
            Row("累计销售", "货架销售收入累计", 13, State.sales.ToString("N0") + "/60,000", null);
            Row("六种商品", "每种商品都通过货架售出一次", 4, SoldKinds() + "/6", null);
            Paragraph("庭院种植 → 工坊加工 → 店面售卖。完成开张教学后自动循环，随时点按场景或设施卡调整经营。", 150, 28);
            EndSheet();
        }
        void ShowShelfHelp()
        {
            StartSheet("货架 · 状态图标", "shelf-help");
            ForestSymbol[] symbols = { ForestSymbol.Coin, ForestSymbol.Reserved, ForestSymbol.Empty, ForestSymbol.Settings, ForestSymbol.Lock };
            string[] labels = { "营业 · 自动售卖仓库商品", "预留 · 优先留给已接委托", "缺货 · 等待工坊补货", "待配置 · 点按货架选择商品", "待开放 · 查看扩建条件" };
            for (int i = 0; i < symbols.Length; i++)
            {
                Symbol(sheetContent, symbols[i], 42, sheetY + 12, 64);
                Label(sheetContent, labels[i], 30, Ink, 130, sheetY, 870, 88); sheetY += 100;
            }
            Paragraph("场景牌上的数字对应货架编号。经营卡保留商品、库存和文字状态，点按货架可调整。", 130, 28);
            EndSheet();
        }
        void ShowGardenHelp()
        {
            StartSheet("庭院 · 田位说明", "garden-help");
            Paragraph("数字浅牌：已开放，可点按选择作物。\n深绿加号：下一块可扩建田位。\n浅色木桩与锁：远期预留，按顺序开放。", 240, 30);
            if (State.plots.Length < Catalog.MaxPlots)
            {
                Paragraph("下一块：第 " + (State.plots.Length + 1) + " 块\n" + PlotUnlockDetails(State.plots.Length), 180, 30);
                Row("修复与扩建", "查看所有设施的完整开放条件", 0, "查看条件", () => OpenObject("expansion", 0));
            }
            else Paragraph("十二块田均已开放，可自由安排种植。", 100);
            EndSheet();
        }
        ForestSymbol ShelfSymbol(int index)
        {
            if (index >= State.shelves.Length) return ForestSymbol.Lock;
            int item = State.shelves[index].config;
            if (item < 0) return ForestSymbol.Settings;
            if (Engine.Reserved(item) > 0 && State.items[item] <= Engine.Reserved(item)) return ForestSymbol.Reserved;
            return State.items[item] == 0 ? ForestSymbol.Empty : ForestSymbol.Coin;
        }
    }
}
