using System;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        // Suggestions derive from existing progress. Reading the handbook never
        // grants resources, discovers fish, selects recipes or changes a gate.
        sealed class JourneyStep
        {
            public string Title, Detail, Progress, Target;
            public int Index;
            public ForestSymbol Icon;
            public JourneyStep(string title, string detail, string progress, string target, int index, ForestSymbol icon)
            { Title = title; Detail = detail; Progress = progress; Target = target; Index = index; Icon = icon; }
        }
        ForestSymbolGraphic goalSymbol;
        JourneyStep[] journeySteps;
        GameState journeyState;
        float journeyRefreshAt;
        JourneyStep[] JourneySteps(bool refresh = false)
        {
            if (!refresh && journeyState == State && journeySteps != null && Time.unscaledTime < journeyRefreshAt) return journeySteps;
            journeyState = State; journeyRefreshAt = Time.unscaledTime + .5f;
            var main = NextJourneyStep();
            JourneyStep side;
            var moment=NextMomentStep();
            if(moment!=null)side=moment;
            else if (State.chapterComplete && State.forest.bridge && main.Target != "fish-species" && main.Target != "fish-collection") side = NextFishStep();
            else if (State.forest.greenhouse && main.Target != "nursery" && main.Target != "greenhouse")
                side = new JourneyStep("照看温室", "野莓与露铃草，继续供应林间新配方。", "培育位 " + State.forest.beds.Length + "/4", "greenhouse", 0, ForestSymbol.Greenhouse);
            else side = new JourneyStep(State.chapterComplete ? "小店继续营业" : "看看开张进度", "故事、商品与侧屋，一起记在开张路线里。", "声望 " + State.reputation + "/150 · " + (State.sideRoom ? "侧屋已修复" : "侧屋待修复"), "chapter", 0, ForestSymbol.Home);
            return journeySteps = new[] { main, side };
        }
        JourneyStep NextJourneyStep()
        {
            if (State.tutorial < 4)
            {
                string[] names = { "收下第一份薄荷", "制作第一杯醒神茶", "为货架选择商品", "等待第一笔收入" };
                return new JourneyStep(names[State.tutorial], OpeningDetail(), "开张教学 " + Math.Min(4, State.tutorial + 1) + "/4", "opening", 0, State.tutorial == 0 ? ForestSymbol.Harvest : State.tutorial == 1 ? ForestSymbol.Workshop : ForestSymbol.Shelf);
            }
            var order = Engine.ActiveOrder;
            if (order != null)
            {
                return SupplyStep(order.item,order.count,true);
            }
            if (!State.chapterComplete)
            {
                if (State.machines.Length == 1 && State.stories > 0) return new JourneyStep("修复附魔工作台", "新的加工设备，会带来第二条生产路线。", "查看金币、声望与商品条件", "expansion", 0, ForestSymbol.Workshop);
                if (State.stories < 5) return new JourneyStep("听听邻居的心愿", "接取故事委托，带着商品去见熟悉的邻居。", "熟客故事 " + State.stories + "/5", "orders", 0, ForestSymbol.Orders);
                if (!State.sideRoom) return new JourneyStep("修复小店侧屋", "备好修缮所需，给小店添一间温暖的屋子。", "查看当前修复条件", "expansion", 0, ForestSymbol.Expand);
                if (State.reputation < 150) return new JourneyStep("积攒小店声望", "完成邻居委托，筹备开张茶会。", "声望 " + State.reputation + "/150", "orders", 0, ForestSymbol.Reputation);
                if (SoldKinds() < 6)
                    for(int item=0;item<Catalog.OriginalItems;item++)if(State.sold[item]==0)return SupplyStep(item,1,false);
                return new JourneyStep("照看小店营业", "安排原料、加工与货架，慢慢筹备开张茶会。", "累计销售 " + Math.Min(60000, State.sales).ToString("N0") + "/60,000", "shop", 3, ForestSymbol.Shop);
            }
            var f = State.forest;
            if (!f.started) return new JourneyStep("走进庭院后的林径", "开张茶会之后，和园丁一起寻找森林新发现。", "森林探索 · 即将启程", "forest-entry", 0, ForestSymbol.Forest);
            if (!Engine.Forest.Discovered(0)) return DiscoveryStep(0);
            if (!f.greenhouse) return RepairStep(0);
            if (f.requests == 0) return ForestSupplyStep(6);
            if (!f.bridge) return RepairStep(1);
            if (!Engine.Forest.Discovered(1)) return DiscoveryStep(1);
            if (f.requests == 1) return ForestSupplyStep(7);
            if (!f.vines) return RepairStep(2);
            if (!Engine.Forest.Discovered(2)) return DiscoveryStep(2);
            if (f.requests == 2) return ForestSupplyStep(8);
            if (!f.pavilion) return RepairStep(3);
            return NextFishStep();
        }
        JourneyStep DiscoveryStep(int node) => new JourneyStep("寻找" + ForestRules.Discoveries[node], "观察林中的实物，采集后记入发现手记。", ForestRules.Zones[node], "forage", node, ForestSymbol.Harvest);
        JourneyStep RepairStep(int repair) => new JourneyStep("修复" + RepairNames[repair], "备好修缮所需，让森林路线继续向前。", "森林心愿 " + State.forest.requests + "/3 · 查看所需", "forest-repair", repair, ForestSymbol.Expand);
        JourneyStep ForestSupplyStep(int item)
        {
            int count = ForestRules.RequestCount[item - 6];
            string progress = Catalog.Items[item] + " " + State.items[item] + "/" + count;
            if (Engine.ActiveOrder == null)
                return new JourneyStep("接取" + ForestRules.RequestTitles[item - 6], "接取时可预留商品，再安排原料与加工。", progress, "orders", 0, ForestSymbol.Orders);
            if (State.items[item] >= count) return new JourneyStep("交付林间心愿", "商品已备好，前往委托板确认交付。", progress, "orders", 0, ForestSymbol.Orders);
            return SupplyStep(item,count,true);
        }
        int NextUnfoundFish()
        {
            int fallback = -1;
            for (int i = 0; i < FishingCatalog.Species.Length; i++)
            {
                if (State.fishing.entries[i].count > 0) continue;
                if (fallback < 0) fallback = i;
                var kind = FishingCatalog.Species[i];
                for (int spot = 0; spot < 3; spot++)
                    if ((kind.Spots & (1 << spot)) != 0 && Engine.Fishing.SpotUnlocked(spot) && Engine.Fishing.BaitUnlocked(kind.Bait)) return i;
            }
            return fallback;
        }
        JourneyStep NextFishStep()
        {
            int fish = NextUnfoundFish();
            return fish < 0 ? new JourneyStep("整理鱼谱纪录", "十二种鱼都已发现，继续寻找自己的长度与重量纪录。", "累计钓获 " + State.fishing.totalCaught.ToString("N0") + " 尾", "fish-collection", 0, ForestSymbol.Ruler)
                : new JourneyStep("寻找下一种鱼", FishingCatalog.Species[fish].Habit + "。", "已发现 " + Engine.Fishing.Discovered + "/12 · 查看生态线索", "fish-species", fish, ForestSymbol.Fish);
        }
        void GoJourney(JourneyStep step)
        {
            CloseSheet();
            if (step.Target == "opening") { FocusOpeningStep(); shopBuilt = false; ShowPage("小店"); return; }
            if (step.Target == "orders") { ShowPage("委托"); return; }
            if (step.Target == "chapter") { ShowChapterProgress(); return; }
            if (step.Target == "fish-collection") { ShowFishCollection(); return; }
            if (step.Target == "fish-species") { ShowFishSpecies(step.Index); return; }
            if (step.Target == "products") { collectionTab = 0; ShowPage("图鉴"); return; }
            if (step.Target == "forest-entry") { OpenObject("forest-entry", 0); return; }
            if (step.Target == "moment") { GoMoment(step.Index); return; }
            if (step.Target == "forest-repair") { OpenObject("forest-repair", step.Index); return; }
            if (step.Target == "forage") { GoForest(step.Index); OpenObject("forage", step.Index); return; }
            if (step.Target == "greenhouse" || step.Target == "nursery") { GoGreenhouse(); if (step.Target == "nursery") OpenObject("nursery", step.Index); return; }
            int region = step.Target == "plot" ? 1 : step.Target == "machine" ? 2 : step.Target == "shelf" ? 3 : step.Target == "shop" ? step.Index : 0;
            World().SetRegion(region); shopPosition = 1; shopBuilt = false; ShowPage("小店");
            if (step.Target != "shop") OpenObject(step.Target, step.Index);
        }
        void ShowJourneyHandbook()
        {
            StartSheet("林间手册", "journey");
            JourneySteps(true);
            Label(sheetContent, "当前建议", 28, Muted, 36, sheetY, 990, 46); sheetY += 54;
            for (int i = 0; i < 2; i++)
            {
                int slot = i;
                var card = Panel(sheetContent, "Journey suggestion " + i, Sand, 28, sheetY, 1024, 216);
                var icon = Symbol(card, ForestSymbol.Forest, 24, 20, 80);
                var title = Label(card, "", 34, Sage, 128, 6, 868, 54);
                var detail = Label(card, "", 27, Ink, 128, 64, 856, 72);
                var progress = Label(card, "", 25, Muted, 30, 149, 730, 54);
                IconButton(card, "前往", ForestSymbol.Arrow, 780, 139, 220, 72, () => GoJourney(JourneySteps(true)[slot]), true, i == 0).name = "Journey go " + i;
                modalBindings.Add(() => { var step = JourneySteps()[slot]; icon.Symbol = step.Icon; title.text = step.Title; detail.text = step.Detail; progress.text = step.Progress; });
                sheetY += 232;
            }
            Label(sheetContent, State.chapterComplete ? "林间收集" : "开张路线", 28, Muted, 36, sheetY + 2, 990, 46); sheetY += 54;
            if (State.chapterComplete)
            {
                int found = 0; for (int i = 0; i < 3; i++) if (Engine.Forest.Discovered(i)) found++;
                FishStat(sheetContent, 0, sheetY, "森林发现", found + "/3", ForestSymbol.Forest);
                FishStat(sheetContent, 1, sheetY, "林间心愿", State.forest.requests + "/3", ForestSymbol.Orders);
                FishStat(sheetContent, 2, sheetY, "鱼类发现", Engine.Fishing.Discovered + "/12", ForestSymbol.Fish);
            }
            else
            {
                FishStat(sheetContent, 0, sheetY, "熟客故事", State.stories + "/5", ForestSymbol.Orders);
                FishStat(sheetContent, 1, sheetY, "商品首售", SoldKinds() + "/6", ForestSymbol.Shop);
                FishStat(sheetContent, 2, sheetY, "侧屋修复", State.sideRoom ? "已完成" : "待修复", ForestSymbol.Home);
            }
            sheetY += 134;
            if(State.chapterComplete)
            {
                string[] names={"开张","发现","鱼谱","小事"};ForestSymbol[] icons={ForestSymbol.Home,ForestSymbol.Forest,ForestSymbol.Fish,ForestSymbol.Collection};
                Action[] open={ShowChapterProgress,ShowForestJournal,ShowFishCollection,ShowMomentJournal};
                for(int i=0;i<4;i++)IconButton(sheetContent,names[i],icons[i],28+i*260,sheetY,244,82,open[i]).name=i==3?"Open moment journal":"Handbook collection "+i;
            }
            else
            {
                IconButton(sheetContent,"开张进度",ForestSymbol.Home,28,sheetY,328,82,ShowChapterProgress);
                IconButton(sheetContent,"发现手记",ForestSymbol.Forest,376,sheetY,328,82,ShowForestJournal);
                IconButton(sheetContent,"鱼类图鉴",ForestSymbol.Fish,724,sheetY,328,82,ShowFishCollection);
            }
            sheetY+=98;
            EndSheet();
        }
    }
}
