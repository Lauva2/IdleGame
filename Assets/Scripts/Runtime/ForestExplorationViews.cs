using System;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    // These badges follow their real 3D source, rather than staying tappable
    // after the player has panned that source outside the viewport.
    public sealed class ForestExploreMarker : MonoBehaviour
    {
        public ForestWorld3D world; public int index;
        public void Refresh()
        {
            var rect = (RectTransform)transform; var parent = (RectTransform)rect.parent;
            var at = world.ForestMarker(index);
            bool visible = at.x > .10f && at.x < .90f && at.y > .17f && at.y < .94f;
            foreach (var g in GetComponentsInChildren<Graphic>()) g.enabled = visible;
            GetComponent<Button>().interactable = visible; GetComponent<Image>().raycastTarget = visible;
            rect.anchoredPosition = new Vector2(at.x * parent.rect.width - rect.rect.width / 2, -(1 - at.y) * parent.rect.height + rect.rect.height / 2);
        }
        void LateUpdate() { Refresh(); }
    }
    public sealed partial class ForestApp
    {
        static int CropIcon(int crop) => crop < 4 ? crop : 16 + crop - 4;
        static int ItemIcon(int item) => item < 6 ? item + 4 : 19 + item - 6;
        string ItemUnlockText(int item) => item < 6 ? "声望 " + Catalog.Unlock[item] + (Catalog.Machine[item] == 1 ? " · 修复附魔台" : "") : "修复温室，发现" + ForestRules.Discoveries[item - 6];
        string ForestGoalShort()
        {
            var f = State.forest;
            if (!f.started) return "新旅程 · 庭院后的林径";
            if (f.pavilion) return "林间旧亭已修复";
            if (f.requests >= 3) return "修复林间旧亭";
            if (f.vines) return Engine.Forest.Discovered(2) ? "旧亭心愿 · " + f.requests + "/3" : "苔石发现 · 松脂";
            if (f.bridge) return f.requests >= 2 ? "唤醒藤蔓 · 苔石小径" : Engine.Forest.Discovered(1) ? "邮差心愿 · 清露喷雾" : "溪边发现 · 露铃草";
            if (f.requests >= 1) return "修好溪边木桥";
            if (f.greenhouse) return "园丁心愿 · 莓香茶";
            return Engine.Forest.Discovered(0) ? "野莓新家 · 修复温室" : "林缘发现 · 野莓苗";
        }
        void GoForest(int zone)
        {
            if (!Engine.Forest.CanVisit(zone))
            { if (!State.forest.started) OpenObject("forest-entry", 0); else OpenObject("forest-repair", zone); return; }
            Engine.Forest.Visit(zone); Save(); World().SetRegion(zone + 4); shopBuilt = false; ShowPage("小店");
        }
        void GoGreenhouse()
        {
            if (!State.forest.started) { OpenObject("forest-entry", 0); return; }
            World().SetRegion(7); shopBuilt = false; ShowPage("小店");
        }
        void GoForestHome() { World().SetRegion(0); shopBuilt = false; ShowPage("小店"); }
        void BuildForestContent()
        {
            var world = World(); bool nursery = world.Region == 7;
            content = Scroll(page, 0, 0, 2300, "Forest exploration scroll");
            for (int i = 0; i < 3; i++)
            {
                int zone = i; bool unlocked = Engine.Forest.CanVisit(i);
                IconButton(content, ForestRules.Zones[i], unlocked ? ForestSymbol.Forest : ForestSymbol.Lock, 26 + i * 350, 14, 328, 78, () => GoForest(zone), true, world.Region == i + 4).name = "Forest zone " + i;
            }
            IconButton(content, "回小院", ForestSymbol.Home, 30, 108, 310, 78, GoForestHome).name = "Forest home";
            IconButton(content, "温室", ForestSymbol.Greenhouse, 385, 108, 310, 78, GoGreenhouse, true, nursery).name = "Forest greenhouse";
            IconButton(content, "发现手记", ForestSymbol.Collection, 740, 108, 310, 78, ShowForestJournal).name = "Forest journal";
            Canvas.ForceUpdateCanvases();
            float height = Mathf.Min(1280, Mathf.Max(850, page.rect.height - 296));
            world.SetViewportAspect(1080 / height);
            var view = Rect(content, "Live 3D forest exploration", 0, 204, 1080, height);
            view.gameObject.AddComponent<RawImage>().texture = world.View; view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<ForestWorldPointer>().world = world;
            IconButton(view, "", ForestSymbol.ZoomOut, 730, height - 108, 96, 88, () => world.Zoom(1.22f)).name = "Map zoom out";
            IconButton(view, "", ForestSymbol.ZoomIn, 840, height - 108, 96, 88, () => world.Zoom(1 / 1.22f)).name = "Map zoom in";
            IconButton(view, "", ForestSymbol.Recenter, 950, height - 108, 96, 88, world.ResetCamera).name = "Map recenter";
            if (nursery && State.forest.greenhouse)
            {
                for (int i = 0; i < 4; i++)
                {
                    int bed = i; var button = IconButton(view, (i + 1).ToString(), ForestSymbol.Garden, 0, 0, 138, 66, () => OpenObject("nursery", bed));
                    button.name = "Nursery marker " + i;
                    var marker = button.gameObject.AddComponent<ForestExploreMarker>(); marker.world = world; marker.index = i + 3;
                }
            }
            else if (!nursery)
            {
                int node = world.Region - 4;
                var button = IconButton(view, "观察", ForestSymbol.Harvest, 0, 0, 182, 72, () => OpenObject("forage", node)); button.name = "Forage marker " + node;
                var marker = button.gameObject.AddComponent<ForestExploreMarker>(); marker.world = world; marker.index = node;
                var status = button.GetComponentInChildren<ForestSymbolGraphic>();
                bindings.Add(() => status.Symbol = State.crops[node + 4] >= Engine.Capacity ? ForestSymbol.Warehouse : State.forest.regrow[node] > 0 ? ForestSymbol.Timer : ForestSymbol.Harvest);
                var fishButton=IconButton(view,"钓鱼",Engine.Fishing.SpotUnlocked(node)?ForestSymbol.Fish:ForestSymbol.Lock,0,0,182,72,()=>OpenObject("fishing",node)); fishButton.name="Fishing marker "+node;
                var fishMarker=fishButton.gameObject.AddComponent<ForestExploreMarker>(); fishMarker.world=world; fishMarker.index=node+7;
            }
            Label(content, "点按实物观察 · 拖动平移 · 双指缩放", 24, Sage, 30, 214 + height, 1020, 70, TextAnchor.MiddleCenter);
            float y = height + 296;
            if (nursery)
            {
                if (!State.forest.greenhouse)
                {
                    IconButton(content, "修复温室", ForestSymbol.Expand, 35, y, 1010, 94, () => OpenObject("forest-repair", 0)).name = "Repair greenhouse"; y += 118;
                }
                else for (int i = 0; i < 4; i++)
                {
                    int bed = i; var button = Button(content, "", 35 + i % 2 * 515, y + i / 2 * 158, 495, 140, () => OpenObject("nursery", bed)); button.name = "Nursery card " + bed;
                    var cropIcon = Picture(button.transform, "inventory", CropIcon(State.forest.beds[i].config), 4, 4, 6, 19, 100, 100); int shownCrop = State.forest.beds[i].config;
                    var label = button.GetComponentInChildren<Text>(); label.fontSize = 26; label.rectTransform.anchoredPosition = new Vector2(116, -10); label.rectTransform.sizeDelta = new Vector2(365, 120);
                    bindings.Add(() => { var f = State.forest.beds[bed]; int crop = f.workItem >= 0 ? f.workItem : f.config; if (crop != shownCrop) { cropIcon.sprite = Art("inventory", CropIcon(crop),4,4); shownCrop = crop; } label.text = (bed + 1) + " · " + Catalog.Crops[crop] + "\n" + Engine.PlotStatus(f); });
                    if (i == 3) y += 316;
                }
            }
            else
            {
                int node = world.Region - 4;
                var observe = IconButton(content, ForestRules.Discoveries[node], ForestSymbol.Harvest, 35, y, 495, 88, () => OpenObject("forage", node)); observe.name = "Forage details " + node;
                int next = node == 2 ? 3 : node + 1;
                var nextButton = IconButton(content, node == 2 ? "林间旧亭" : node == 0 ? "溪边木桥" : "苔石小径", ForestSymbol.Arrow, 550, y, 495, 88, () => { if (node < 2 && Engine.Forest.CanVisit(next)) GoForest(next); else OpenObject("forest-repair", next); }); nextButton.name = "Forest next " + node; y += 114;
            }
            IconButton(content, "仓库", ForestSymbol.Warehouse, 35, y, 310, 88, () => OpenObject("warehouse", 0));
            IconButton(content, "工坊", ForestSymbol.Workshop, 385, y, 310, 88, () => { world.SetRegion(2); shopBuilt = false; ShowPage("小店"); });
            IconButton(content, "邻居心愿", ForestSymbol.Orders, 735, y, 310, 88, () => ShowPage("委托")); y += 116;
            var goal = Panel(content, "Forest next goal", Paper, 30, y, 1020, 170);
            Symbol(goal, State.forest.pavilion ? ForestSymbol.Check : ForestSymbol.Collection, 22, 35, 76);
            var goalText = Label(goal, Engine.Forest.Goal, 29, Ink, 122, 16, 870, 140);
            bindings.Add(() => goalText.text = Engine.Forest.Goal); y += 192;
            content.sizeDelta = new Vector2(0, y); Canvas.ForceUpdateCanvases(); content.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = 1;
        }
        bool OpenForestObject(string type, int index)
        {
            if (type == "forest-home") { GoForestHome(); return true; }
            if (type == "forest-path") { GoForest(index); return true; }
            if (type == "greenhouse") { GoGreenhouse(); return true; }
            if (type == "fishing") { ShowFishing(index); return true; }
            if (type == "forest-entry")
            {
                if (State.forest.started) { GoForest(State.forest.location); return true; }
                StartSheet("庭院后的林径", type);
                Picture(sheetContent, "residents", 1, 5, 2, 360, 0, 340, 370); sheetY = 385;
                Paragraph("园丁：林子里有野莓，也有适合温室的新种苗。等开张茶会结束，我们沿小径一起去看看吧。", 200, 32);
                Row("森林探索", State.chapterComplete ? "观察林区，点选路径与采集物" : "完成第一章 · 开张茶会后开放", 15, "走进林径", () => { if (Act(Engine.Forest.Start, "林径已开放，看看脚边的野莓苗。", false)) GoForest(0); }, State.chapterComplete);
                EndSheet(); return true;
            }
            if (type == "forage")
            {
                if (!Engine.Forest.CanVisit(index)) { if (!State.forest.started) OpenObject("forest-entry",0); else ShowForestRepair(index); return true; }
                if (State.forest.location != index) GoForest(index);
                ShowForage(index); return true;
            }
            if (type == "forest-repair") { ShowForestRepair(index); return true; }
            if (type == "nursery") { ShowNursery(index); return true; }
            if (type == "forest-journal") { ShowForestJournal(); return true; }
            return false;
        }
        string ForageDetail(int node)
        {
            var f = State.forest; int raw = node + 4;
            return "库存 " + State.crops[raw] + "/" + Engine.Capacity + " · 此处剩余 " + f.available[node] + "\n" + (State.crops[raw] >= Engine.Capacity ? "仓库已满，余量留在原处" : f.regrow[node] > 0 ? "恢复中 · " + Math.Ceiling(f.regrow[node]) + "秒" : "可采集 · 采完后逐渐恢复");
        }
        void ShowForage(int node)
        {
            if (node < 0 || node > 2) return;
            StartSheet(ForestRules.Discoveries[node], "forage");
            Picture(sheetContent, "inventory", CropIcon(node + 4), 4, 4, 350, 0, 360, 350); sheetY = 365;
            Paragraph(node == 0 ? "红果藏在林缘的矮枝间。第一次采集会记住野莓种苗，修复温室后便能长期培育。" : node == 1 ? "细长的蓝绿叶子上，挂着小小的露铃。发现种苗后，可在温室里安排培育。" : "温润的松脂慢慢凝在老松树的伤口上。只采已经凝好的部分，等树木自己恢复。", 150, 29);
            Row(Catalog.Crops[node + 4], "", CropIcon(node + 4), "采集", () => { if (Act(() => Engine.Forest.Gather(node), Catalog.Crops[node + 4] + "已入库，发现记录已保存。", false)) CloseSheet(); }, true,
                () => Engine.Forest.CanVisit(node) && State.forest.location == node && State.forest.regrow[node] <= 0 && State.forest.available[node] > 0 && State.crops[node + 4] < Engine.Capacity, () => ForageDetail(node));
            if (Engine.Forest.Discovered(node)) Row("发现已记录", node < 2 ? "温室可培育这株种苗" : "松脂保留野外采集", CropIcon(node + 4), node < 2 ? "前往温室" : "查看配方", () => { if (node < 2) GoGreenhouse(); else { collectionTab = 0; ShowPage("图鉴"); } });
            EndSheet();
        }
        static readonly string[] RepairNames = { "林间温室", "溪边木桥", "苔石小径", "林间旧亭" };
        string RepairDetail(int id)
        {
            string result = "金币 " + State.coins.ToString("N0") + "/" + ForestRules.RepairCoins[id].ToString("N0");
            for (int i = 0; i < Catalog.Items.Length; i++) if (ForestRules.RepairItems[id][i] > 0)
                result += "\n" + Catalog.Items[i] + " " + Math.Max(0, State.items[i] - Engine.Reserved(i)) + "/" + ForestRules.RepairItems[id][i];
            return result;
        }
        void ShowForestRepair(int id)
        {
            if (id < 0 || id > 3) return;
            StartSheet(RepairNames[id], "forest-repair");
            bool repaired = Engine.Forest.IsRepaired(id);
            Paragraph(repaired ? id == 0 ? "暖色木框重新托住了玻璃，四个培育位已经准备好。" : id == 1 ? "木桥稳稳地跨过溪流，通往有露铃草的湿地。" : id == 2 ? "藤蔓舒展叶子，让出了通往苔石林地的小径。" : "旧亭的木头重新温润起来。邻居们又有了读书、喝茶和歇脚的地方。" : id == 0 ? "用两份暖炉药剂驱散潮气，修好旧木框和玻璃。四个培育位会一起开放。" : id == 1 ? "避雨药水保护桥板，暖炉药剂使木头干燥。修好木桥，就能抵达溪边湿地。" : id == 2 ? "清露喷雾能让沉睡的藤蔓恢复舒展，露出通往苔石林地的路。" : "用护木香膏照顾旧亭的木柱，修好长椅与茶桌，让邻居重新相聚。", 180, 31);
            if (repaired) Row("修复已完成", "进度已保存", 15, id == 0 ? "进入温室" : id == 3 ? "看发现手记" : "沿路前进", () => { if (id == 0) GoGreenhouse(); else if (id == 3) ShowForestJournal(); else GoForest(id); });
            else
            {
                string[] gates = { "先在林缘发现野莓苗", "先完成园丁的林间茶歇", "先完成邮差的露水路标", "先完成老师的旧亭心愿" };
                if (!Engine.Forest.RepairPrerequisite(id)) Paragraph(gates[id] + "。\n当前：" + Engine.Forest.Goal, 130, 28);
                Row("修复所需", RepairDetail(id), id == 0 ? 15 : id == 1 ? ItemIcon(3) : id == 2 ? ItemIcon(7) : ItemIcon(8), "修复", () =>
                {
                    if (Act(() => Engine.Forest.Repair(id), RepairNames[id] + "已修复。", false))
                    { audioController?.Cue("unlock"); CloseSheet(); ShowPage("小店"); }
                }, true, () => Engine.Forest.CanRepair(id), () => RepairDetail(id));
                Paragraph("已接委托预留的商品不计入修复可用库存。", 90, 25);
                float y = sheetY;
                IconButton(sheetContent, "去工坊", ForestSymbol.Workshop, 30, y, 495, 84, () => { World().SetRegion(2); shopBuilt = false; ShowPage("小店"); OpenObject("machine", id < 2 ? 0 : 1); });
                IconButton(sheetContent, "看货架", ForestSymbol.Shelf, 555, y, 495, 84, () => { World().SetRegion(3); shopBuilt = false; ShowPage("小店"); OpenObject("shelf",0); });
                sheetY += 102;
            }
            EndSheet();
        }
        void ShowNursery(int index)
        {
            if (!State.forest.greenhouse || index < 0 || index >= 4) { ShowForestRepair(0); return; }
            var bed = State.forest.beds[index]; StartSheet("温室培育位 " + (index + 1), "nursery");
            Row("培育状态", "", CropIcon(bed.config), "", null, true, null, () => "等级 " + bed.level + "/5 · " + Engine.PlotStatus(bed) + (bed.pending >= 0 ? "\n下轮：" + Catalog.Crops[bed.pending] : "\n成熟后自动入库并重新培育"));
            if (bed.remaining == 0) Paragraph("成熟作物会留在培育位，等仓库腾出空间后入库，不会丢失。", 120, 28);
            if (bed.pending >= 0) Row("待切换种苗", Catalog.Crops[bed.pending], CropIcon(bed.pending), "取消切换", () => { Engine.CancelSwitch(bed); Save(); OpenObject("nursery", index); });
            for (int crop = 4; crop <= 5; crop++)
            {
                int raw = crop; bool unlocked = Engine.Forest.CanGrow(crop);
                Row(Catalog.Crops[crop], unlocked ? "库存 " + State.crops[crop] + "/" + Engine.Capacity + " · 基础 " + Catalog.CropSeconds[crop] + "秒" : "先在溪边湿地发现露铃草种苗", CropIcon(crop), bed.config == crop && bed.pending < 0 ? "已选择" : "培育种苗", () => Act(() => Engine.Forest.ConfigureBed(index, raw), "培育配置已保存，本轮结束后切换。"), unlocked);
            }
            UpgradeRow(bed); EndSheet();
        }
        void ShowForestJournal()
        {
            StartSheet("森林探索 · 发现手记", "forest-journal"); Paragraph(Engine.Forest.Goal, 150, 32);
            for (int i = 0; i < 3; i++)
            {
                int node = i; bool found = Engine.Forest.Discovered(i);
                Row(ForestRules.Discoveries[i], found ? "发现已记录 · 累计采集 " + State.forest.gathered[i] : "等待发现 · " + ForestRules.Zones[i], CropIcon(i + 4), found ? "前往林区" : "寻找", () => GoForest(node));
            }
            Row("邻居的新心愿", "园丁 → 邮差 → 老师 · " + State.forest.requests + "/3", ItemIcon(6), "看委托", () => ShowPage("委托"));
            for (int i = 0; i < State.forest.requests; i++)
            {
                int story = i;
                Row(ForestRules.RequestTitles[i], "已完成 · " + Catalog.Residents[ForestRules.RequestResidents[i]], ItemIcon(6 + i), "回看心愿", () => ShowForestMemory(story));
            }
            Row("林间温室", State.forest.greenhouse ? "四位培育已开放" : "野莓种苗的新家", 15, "前往温室", GoGreenhouse);
            Row("溪边垂钓", "鱼类发现 "+Engine.Fishing.Discovered+"/12 · 自动收鱼",-1,"鱼类图鉴",ShowFishCollection,glyph:ForestSymbol.Fish);
            Row("林间旧亭", State.forest.pavilion ? "已修复 · 邻居的林间歇脚处" : "三份新心愿后的修缮目标", 14, "查看", () => OpenObject("forest-repair", 3)); EndSheet();
        }
        void ShowForestMemory(int story)
        {
            StartSheet(ForestRules.RequestTitles[story], "forest-memory");
            Picture(sheetContent, "residents", ForestRules.RequestResidents[story], 5, 2, 355, 0, 350, 390); sheetY = 405;
            Paragraph(StoryLines(ForestRules.RequestBefore[story]), 200, 31); Paragraph(StoryLines(ForestRules.RequestAfter[story]), 200, 31); EndSheet();
        }
        void ShowForestDialogue()
        {
            int story = State.forest.dialogue; StartSheet(ForestRules.RequestTitles[story], "forest-dialogue");
            Picture(sheetContent, "residents", ForestRules.RequestResidents[story], 5, 2, 355, 0, 350, 390); sheetY = 405;
            Paragraph(StoryLines(ForestRules.RequestAfter[story]), 200, 32); Row("心愿已完成", "新的记忆保存在发现手记中", ItemIcon(6 + story), "继续", CloseSheet); EndSheet(); audioController?.Cue("voice");
        }
        void ShowForestCelebration()
        {
            StartSheet("林间旧亭 · 重新相聚", "forest-celebration");
            Paragraph("园丁带来野莓茶，邮差在新路标旁停下脚步。老师把旧书放在温润的木桌上。\n林间旧亭，重新有了故事。", 250, 34);
            Row("森林里的新日常", "三处发现、四位温室与三道新配方继续保留", ItemIcon(8), "看看旧亭", () => { State.forest.celebrationSeen = true; Save(); GoForest(2); }); EndSheet(); audioController?.Cue("unlock");
        }
    }
}
