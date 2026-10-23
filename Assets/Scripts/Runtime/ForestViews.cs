using System;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        float sheetY;
        RectTransform sheetContent;
        float shopPosition = 1;
        bool shopBuilt;
        int collectionTab;

        ForestWorld3D world3D;
        ForestWorld3D World()
        {
            if (world3D != null) return world3D;
            world3D = new GameObject("Forest 3D").AddComponent<ForestWorld3D>(); world3D.Initialize();
            world3D.DoorEntered = () => audioController?.Cue("door");
            world3D.Selected = (kind, index) =>
            {
                if (kind == "plot" && State.tutorial == 0) Act(Engine.HarvestFirst, "薄荷已入库，庭院开始自动种植。", true);
                else if (kind == "futurePlot")
                {
                    if (index == State.plots.Length) OpenObject("expansion", 0);
                    else Toast("田地按顺序开放，请先查看第 " + (State.plots.Length + 1) + " 块地的扩建条件。", 3);
                }
                else OpenObject(kind, index);
            };
            return world3D;
        }
        void FocusOpeningStep()
        {
            if (State.tutorial >= 4) return;
            var world = World(); world.Bind(State);
            world.SetRegion(State.tutorial == 0 ? 1 : State.tutorial == 1 ? 2 : 3);
            shopPosition = 1;
        }
        void BuildOpeningGuide(Transform parent)
        {
            var card = Panel(parent, "Opening guide", Paper, 30, 110, 1020, 112);
            var stepIcon = Symbol(card, ForestSymbol.Harvest, 15, 18, 76);
            var title = Label(card, "", 31, Sage, 110, 3, 590, 50);
            var body = Label(card, "", 25, Muted, 110, 55, 590, 48);
            var action = IconButton(card, "收获", ForestSymbol.Harvest, 726, 12, 270, 88, () =>
            {
                if (State.tutorial == 0) Act(Engine.HarvestFirst, "薄荷已入库，接着去工坊制作醒神茶。", true);
                else if (State.tutorial == 1 || State.tutorial == 2)
                {
                    FocusOpeningStep(); ShowPage("小店");
                    OpenObject(State.tutorial == 1 ? "machine" : "shelf", 0);
                }
                else if (State.tutorial == 3) OpenObject("shelf", 0);
                else ShowPage("委托");
            }, true, true);
            action.name = "Opening action";
            var caption = action.GetComponentInChildren<Text>();
            string[] titles = { "1/3 · 收获薄荷", "2/3 · 制作醒神茶", "3/3 · 安排售卖", "等待首笔收入", "自动经营已开启" };
            string[] descriptions = { "收获后自动种植", "坩埚选择醒神茶", "1号货架选择醒神茶", "加工完成后自动出售", "去看看邻居的心愿" };
            string[] captions = { "收获", "配置", "上架", "查看", "委托" };
            ForestSymbol[] icons = { ForestSymbol.Harvest, ForestSymbol.Workshop, ForestSymbol.Shelf, ForestSymbol.Timer, ForestSymbol.Orders };
            bindings.Add(() =>
            {
                int step = Math.Min(4, State.tutorial);
                title.text = titles[step]; body.text = descriptions[step]; caption.text = captions[step];
                stepIcon.Symbol = icons[step]; action.GetComponentInChildren<ForestSymbolGraphic>().Symbol = icons[step];
            });
        }
        void BuildShop()
        {
            var world = World(); world.Bind(State); world.SetVisible(true);
            content = Scroll(page, 0, 0, 2600, "Treehouse 3D scroll");
            string[] regions = { "小院", "庭院", "工坊", "店面" };
            ForestSymbol[] regionIcons = { ForestSymbol.Home, ForestSymbol.Garden, ForestSymbol.Workshop, ForestSymbol.Shop };
            for (int i = 0; i < regions.Length; i++)
            {
                int region = i;
                IconButton(content, regions[i], regionIcons[i], 25 + i * 263, 14, 242, 78, () => { shopPosition = 1; shopBuilt = false; world.SetRegion(region); ShowPage("小店"); }, true, world.Region == i);
            }
            float viewTop = 112;
            if (State.tutorial < 4) { BuildOpeningGuide(content); viewTop = 236; }
            else if (world.Region == 1)
            {
                var legend = Panel(content, "Garden expansion legend", Paper, 30, 110, 1020, 88);
                Symbol(legend, ForestSymbol.Garden, 16, 14, 60);
                Label(legend, State.plots.Length + "/12", 30, Sage, 88, 7, 170, 74);
                if (State.plots.Length < Catalog.MaxPlots)
                {
                    Symbol(legend, ForestSymbol.Expand, 278, 14, 60);
                    Label(legend, "+" + (State.plots.Length + 1), 30, Sage, 348, 7, 145, 74);
                    Symbol(legend, ForestSymbol.Coin, 528, 14, 60);
                    Label(legend, Catalog.PlotCost[State.plots.Length].ToString("N0"), 30, Ink, 598, 7, 290, 74);
                }
                else Symbol(legend, ForestSymbol.Check, 300, 14, 60);
                IconButton(legend, "", ForestSymbol.Info, 904, 0, 100, 88, ShowGardenHelp).name = "Garden details";
                viewTop = 214;
            }
            float viewHeight = world.Region == 0 ? 1320 : 1280;
            Canvas.ForceUpdateCanvases();
            viewHeight = Mathf.Min(viewHeight, Mathf.Max(900, page.rect.height - viewTop - 104));
            world.SetViewportAspect(1080 / viewHeight);
            var view = Rect(content, "Live 3D treehouse", 0, viewTop, 1080, viewHeight);
            var image = view.gameObject.AddComponent<RawImage>(); image.texture = world.View;
            view.gameObject.AddComponent<RectMask2D>();
            var pointer = view.gameObject.AddComponent<ForestWorldPointer>(); pointer.world = world;
            IconButton(view, "", ForestSymbol.ZoomOut, 730, viewHeight - 108, 96, 88, () => world.Zoom(1.22f)).name = "Map zoom out";
            IconButton(view, "", ForestSymbol.ZoomIn, 840, viewHeight - 108, 96, 88, () => world.Zoom(1 / 1.22f)).name = "Map zoom in";
            IconButton(view, "", ForestSymbol.Recenter, 950, viewHeight - 108, 96, 88, world.ResetCamera).name = "Map recenter";
            Label(content, "拖动平移 · 双指缩放", 23, Sage, 350, viewTop + viewHeight + 12, 350, 72, TextAnchor.MiddleCenter).name = "Camera gestures outside world";
            if (world.Region == 3)
                IconButton(view, "", ForestSymbol.Info, 956, 16, 96, 88, ShowShelfHelp).name = "Shelf details";
            if (world.Region == 1) for (int i = 0; i < Catalog.MaxPlots; i++)
            {
                Vector2 at = world.PlotMarker(i);
                bool built = i < State.plots.Length, next = i == State.plots.Length;
                float width = next ? 80 : 54;
                var badge = Panel(view, "Garden marker " + (i + 1), next ? Sage : built ? Paper : new Color32(231, 235, 219, 255), Mathf.Clamp(at.x * 1080 - width / 2, 4, 1076 - width), Mathf.Clamp((1 - at.y) * viewHeight - 25, 4, viewHeight - 58), width, 54);
                badge.GetComponent<Image>().raycastTarget = false;
                Label(badge, (next ? "+ " : "") + (i + 1), next ? 27 : 29, next ? Paper : built ? Sage : Muted, 0, 0, width, 54, TextAnchor.MiddleCenter);
                var marker = badge.gameObject.AddComponent<ForestWorldMarker>(); marker.world = world; marker.index = i;
            }
            if (world.Region == 3) for (int i = 0; i < 4; i++)
            {
                int n = i; Vector2 at = world.ShelfMarker(i);
                var badge = Panel(view, "Shelf marker " + (i + 1), Paper, Mathf.Clamp(at.x * 1080 - 48, 4, 980), Mathf.Clamp((1 - at.y) * viewHeight - 32, 4, viewHeight - 66), 96, 60);
                badge.GetComponent<Image>().raycastTarget = false;
                Label(badge, (n + 1).ToString(), 28, Sage, 3, 0, 35, 60, TextAnchor.MiddleCenter);
                var status = Symbol(badge, ShelfSymbol(n), 39, 6, 48);
                bindings.Add(() => status.Symbol = ShelfSymbol(n));
                var marker = badge.gameObject.AddComponent<ForestWorldMarker>(); marker.world = world; marker.index = i; marker.shelf = true;
            }
            IconButton(content, "设施", ForestSymbol.Arrow, 60, viewTop + viewHeight + 12, 280, 72, () => { Canvas.ForceUpdateCanvases(); var scroll = content.parent.GetComponent<ScrollRect>(); float excess = content.rect.height - scroll.viewport.rect.height; scroll.verticalNormalizedPosition = excess > 0 ? Mathf.Clamp01(1 - (viewTop + viewHeight + 104) / excess) : 1; });
            var hint = IconButton(content, "经营说明", ForestSymbol.Info, 710, viewTop + viewHeight + 12, 310, 72, () => { if (world.Region == 3) ShowShelfHelp(); else ShowGoal(); });
            hint.GetComponentInChildren<Text>().fontSize = 25;
            float y = viewTop + viewHeight + 106;
            if (world.Region == 1)
            {
                for (int i = 0; i < Catalog.MaxPlots; i++)
                {
                    int n = i; bool built = i < State.plots.Length, next = i == State.plots.Length;
                    var b = Button(content, "", 35 + i % 3 * 341, y + i / 3 * 160, 322, 139, () => { if (n == State.plots.Length) OpenObject("expansion", 0); else if (State.tutorial == 0) Act(Engine.HarvestFirst, "薄荷已入库，庭院开始自动种植。", true); else OpenObject("plot", n); }, built || next, next);
                    b.name = "Plot card " + n;
                    var label = b.GetComponentInChildren<Text>(); label.fontSize = 25;
                    label.rectTransform.anchoredPosition = new Vector2(108, -10); label.rectTransform.sizeDelta = new Vector2(205, 119);
                    if (built)
                    {
                        var cropImage = Picture(b.transform, "inventory", Math.Max(0, State.plots[n].config), 4, 4, 3, 22, 100, 100);
                        int shownCrop = State.plots[n].config;
                        bindings.Add(() => { var f = State.plots[n]; int crop = Math.Max(0, f.workItem >= 0 ? f.workItem : f.config); if (shownCrop != crop) { cropImage.sprite = Art("inventory", crop, 4, 4); shownCrop = crop; } label.text = (n + 1) + " · " + Catalog.Crops[crop] + "\n" + (State.tutorial == 0 ? "收获" : f.remaining > 0 ? Math.Ceiling(f.remaining) + "秒" : f.remaining == 0 ? "待入库" : "满仓"); });
                    }
                    else
                    {
                        Symbol(b.transform, next ? ForestSymbol.Expand : ForestSymbol.Lock, 21, 37, 64, next);
                        label.text = next ? "+" + (n + 1) + " · 扩建" : (n + 1) + " · 预留";
                        if (next)
                        {
                            label.rectTransform.sizeDelta = new Vector2(205, 54);
                            Symbol(b.transform, ForestSymbol.Coin, 110, 78, 32, true);
                            Label(b.transform, Catalog.PlotCost[n].ToString("N0"), 25, Paper, 151, 67, 164, 54);
                        }
                    }
                }
                y += Mathf.CeilToInt(Catalog.MaxPlots / 3f) * 160 + 5;
            }
            else if (world.Region == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    int n = i;
                    var b = Button(content, "", 35 + i * 515, y, 495, 140, () => { if (n >= State.machines.Length) OpenObject("expansion", 0); else OpenObject("machine", n); });
                    var label = b.GetComponentInChildren<Text>(); label.fontSize = 28;
                    bindings.Add(() => label.text = (n == 0 ? "魔法坩埚" : "附魔工作台") + "\n" + (n < State.machines.Length ? Engine.MachineStatus(State.machines[n]) : "尚未修复 · 查看条件"));
                }
                y += 165;
            }
            else if (world.Region == 3)
            {
                for (int i = 0; i < 4; i++)
                {
                    int n = i;
                    var b = Button(content, "", 35 + i % 2 * 515, y + i / 2 * 155, 495, 139, () => { if (n >= State.shelves.Length) OpenObject("expansion", 0); else OpenObject("shelf", n); });
                    var label = b.GetComponentInChildren<Text>(); label.fontSize = 27;
                    bindings.Add(() => { if (n >= State.shelves.Length) label.text = "+ 新增货架 " + (n + 1); else { var f = State.shelves[n]; label.text = "货架 " + (n + 1) + (f.config < 0 ? "\n尚未配置" : " · " + Catalog.Items[f.config] + "\n" + (State.items[f.config] <= Engine.Reserved(f.config) ? Engine.Reserved(f.config) > 0 ? "委托预留" : "暂时缺货" : "营业中 · 库存 " + State.items[f.config])); } });
                }
                y += 320;
            }
            else
            {
                Label(content, "沿着石板路，照看森林小院", 34, Sage, 40, y, 1000, 68, TextAnchor.MiddleCenter);
                y += 90;
            }
            IconButton(content, "仓库", ForestSymbol.Warehouse, 35, y, 310, 92, () => OpenObject("warehouse", 0));
            IconButton(content, "扩建", ForestSymbol.Expand, 385, y, 310, 92, () => OpenObject("expansion", 0));
            IconButton(content, "装饰", ForestSymbol.Decoration, 735, y, 310, 92, () => OpenObject("decoration", 0)); y += 120;
            var footer = Panel(content, "Chapter progress", Paper, 30, y, 1020, 226);
            Label(footer, State.chapterComplete ? "开张茶会已完成" : "第一章 · 开张茶会", 32, Sage, 28, 8, 820, 60);
            IconButton(footer, "", ForestSymbol.Info, 900, 0, 100, 80, ShowGoal);
            ForestSymbol[] progressIcons = { ForestSymbol.Orders, ForestSymbol.Reputation, ForestSymbol.Coin, ForestSymbol.Collection };
            for (int i = 0; i < 4; i++)
            {
                int n = i; float x = 25 + i % 2 * 500, rowY = 76 + i / 2 * 68;
                Symbol(footer, progressIcons[i], x, rowY, 54);
                var value = Label(footer, "", 27, Muted, x + 72, rowY, 400, 58);
                bindings.Add(() => value.text = n == 0 ? "故事 " + State.stories + "/5" : n == 1 ? "声望 " + State.reputation + "/150" : n == 2 ? "销售 " + Math.Min(60000, State.sales).ToString("N0") + "/60,000" : "商品 " + SoldKinds() + "/6");
            }
            content.sizeDelta = new Vector2(0, y + 256);
            Canvas.ForceUpdateCanvases();
            content.parent.GetComponent<ScrollRect>().verticalNormalizedPosition = shopBuilt ? shopPosition : 1;
            shopBuilt = true;
            content.parent.GetComponent<ScrollRect>().onValueChanged.AddListener(v => shopPosition = v.y);
        }
        int SoldKinds() { int n = 0; foreach (var value in State.sold) if (value > 0) n++; return n; }
        string PlotUnlockDetails(int count)
        {
            string condition = count == 1 ? "累计售出 " + Engine.TotalSold + "/3 件商品"
                : (count >= 4 ? (State.sideRoom ? "侧屋已修复" : "侧屋待修复") + " · " : "") + "声望 " + State.reputation + "/" + Catalog.PlotRep[count];
            return condition + "\n已有 " + State.coins + " / 所需 " + Catalog.PlotCost[count] + " 金币";
        }
        void EnlargeTouch(Button button, float up, float height)
        {
            var target = button.transform.Find("Touch target") as RectTransform;
            if (target == null) return;
            target.anchoredPosition = new Vector2(0, up); target.sizeDelta = new Vector2(button.GetComponent<RectTransform>().rect.width, height);
        }
        void Plaque(Transform parent, string title, string body, float x, float y, float w)
        { var p = Panel(parent, title, Paper, x, y, w, 104); Label(p, title, 33, Sage, 20, 4, w - 36, 48); Label(p, body, 24, Muted, 20, 52, w - 36, 44); }

        void BuildOrders()
        {
            content = Scroll(page, 0, 0, 1600, "Orders scroll");
            Label(content, "邻居的心愿", 52, Sage, 44, 32, 980, 84);
            Label(content, "每次接受一份委托，不限时。预留商品后仍可调整经营。", 29, Muted, 44, 127, 980, 96);
            float y = 245;
            foreach (var o in State.orders)
            {
                if (o.active) y = OrderCard(o, y);
            }
            foreach (var o in State.orders)
            {
                if (!o.active) y = OrderCard(o, y);
            }
            if (State.orders.Length == 0) { Label(content, "完成第一笔销售后，猫头鹰老师会带来第一份委托。", 34, Ink, 60, y + 30, 950, 140); y += 200; }
            var refresh = Button(content, "", 50, y + 24, 980, 85, () => Act(Engine.RefreshOrders, "未接的普通委托已更新。"));
            var rt = refresh.GetComponentInChildren<Text>(); bindings.Add(() => { refresh.interactable = State.stories > 0 && State.refreshSeconds <= 0; rt.text = State.refreshSeconds > 0 ? "免费刷新 · " + Math.Ceiling(State.refreshSeconds / 60) + "分钟后可用" : "免费刷新未接的普通委托"; });
            content.sizeDelta = new Vector2(0, Math.Max(1450, y + 220));
        }
        float OrderCard(Order o, float y)
        {
            var card = Panel(content, "Order " + o.id, o.active ? new Color32(232, 238, 215, 255) : new Color32(248, 238, 208, 255), 32, y, 1016, o.active ? 410 : 330);
            Picture(card, "residents", o.resident, 5, 2, 18, 24, 160, 180);
            Label(card, (o.story ? "故事 · " + Catalog.Stories[o.resident] : "日常委托 · " + Catalog.Residents[o.resident]), 33, Sage, 195, 20, 790, 64);
            Label(card, Catalog.Items[o.item] + " ×" + o.count + "   ·   金币 +" + Engine.OrderCoins(o) + "  声望 +" + Engine.OrderReputation(o), 29, Ink, 195, 91, 790, 86);
            var stock = Label(card, "", 28, Muted, 195, 173, 790, 60);
            bindings.Add(() => stock.text = "仓库 " + State.items[o.item] + "/" + o.count + (State.items[o.item] < o.count ? " · 还缺 " + (o.count - State.items[o.item]) + " 件" : " · 已备齐"));
            if (o.active)
            {
                var reserve = Button(card, "", 25, 252, 480, 67, () => { o.reserve = !o.reserve; Save(); });
                bindings.Add(() => reserve.GetComponentInChildren<Text>().text = o.reserve ? "商品预留：已开启" : "商品预留：已关闭");
                Button(card, o.story ? "暂停故事" : "取消委托", 535, 252, 450, 67, () => Act(Engine.CancelOrder, o.story ? "故事已暂停，可随时继续。" : "委托已取消，下次刷新替换。"));
                var deliver = Button(card, "交付委托", 25, 332, 600, 65, () => Act(Engine.DeliverOrder, "心愿已完成，奖励已存档。"), true, true);
                bindings.Add(() => deliver.interactable = State.items[o.item] >= o.count);
                Button(card, "前往设备", 655, 332, 330, 65, () => { ShowPage("小店"); OpenObject("machine", Catalog.Machine[o.item]); });
            }
            else
            {
                var accept = Button(card, o.cancelled ? "已取消 · 等待刷新" : "接受委托", 25, 245, 600, 68, () => Act(() => Engine.AcceptOrder(o.id), "已接单，所需商品会默认预留。"), !o.cancelled && Engine.ActiveOrder == null, true);
                Button(card, "听听心愿", 655, 245, 330, 68, () => ShowWish(o));
            }
            return y + (o.active ? 436 : 356);
        }

        void BuildCollection()
        {
            content = Scroll(page, 0, 0, 2100, "Collection scroll");
            Label(content, "森林手记", 52, Sage, 44, 24, 950, 90);
            Button(content, "商品图鉴", 44, 128, 480, 80, () => { collectionTab = 0; ShowPage("图鉴"); }, true, collectionTab == 0);
            Button(content, "熟客故事", 556, 128, 480, 80, () => { collectionTab = 1; ShowPage("图鉴"); }, true, collectionTab == 1);
            float y = 235;
            if (collectionTab == 0)
                for (int i = 0; i < 6; i++)
                {
                    int item = i; bool unlocked = Engine.ItemUnlocked(i); var card = Panel(content, Catalog.Items[i], Sand, 32, y, 1016, 256);
                    var icon = Picture(card, "inventory", i + 4, 4, 4, 18, 30, 175, 175); if (!unlocked) icon.color = new Color(.25f, .29f, .25f, .5f);
                    Label(card, Catalog.Items[i] + (unlocked ? "" : " · 未解锁"), 36, Sage, 215, 12, 770, 60);
                    Label(card, unlocked ? RecipeText(i) + "\n" + Catalog.ItemSeconds[i] + "秒 / 件 · 基础售价 " + Catalog.Price[i] : "解锁：声望 " + Catalog.Unlock[i] + (Catalog.Machine[i] == 1 ? "，修复附魔台" : ""), 29, Ink, 215, 82, 770, 110);
                    Label(card, State.sold[i] > 0 ? "已首次售出 · 累计 " + State.sold[i] + " 件" : "尚未通过货架售出", 26, Muted, 215, 195, 770, 45); y += 280;
                }
            else
                for (int i = 0; i < 5; i++)
                {
                    int resident = i; bool known = i < State.stories || (i == State.stories && i < 5 && Engine.ItemUnlocked(Catalog.StoryItem[i]));
                    var card = Panel(content, Catalog.Residents[i], Sand, 32, y, 1016, 310);
                    var actor = Picture(card, "residents", i, 5, 2, 12, 40, 190, 225); if (!known) actor.color = new Color(.22f, .28f, .22f, .5f);
                    Label(card, known ? Catalog.Residents[i] : "等待相遇 · " + Catalog.Residents[i], 34, Sage, 225, 18, 755, 62);
                    Label(card, known ? Catalog.Descriptions[i] + "\n好感 " + State.friendship[i] + "/100" : "沿着第一章故事，帮助前一位邻居并解锁所需商品。", 28, Ink, 225, 88, 755, 100);
                    Button(card, State.stories > i ? "回看故事" : "尚未完成故事", 225, 213, 350, 66, () => ShowResident(resident), State.stories > i);
                    Button(card, State.friendship[i] >= 50 ? "纪念插画" : "好感 50 解锁纪念", 600, 213, 380, 66, () => ShowPortrait(resident), State.friendship[i] >= 50); y += 338;
                }
            content.sizeDelta = new Vector2(0, y + 30);
        }
        string RecipeText(int item)
        { string text = ""; for (int c = 0; c < 4; c++) if (Catalog.Materials[item][c] > 0) text += (text.Length > 0 ? "、" : "") + Catalog.Crops[c] + " ×" + Catalog.Materials[item][c]; return text; }

        void StartSheet(string title, string key, float height = 1130)
        {
            Canvas.ForceUpdateCanvases();
            CloseSheet(); currentSheet = key;
            modal = Panel(safe, "Modal", new Color(0.13f, .18f, .12f, .48f)); Fill(modal); modal.SetAsLastSibling();
            var dismiss = modal.gameObject.AddComponent<Button>(); dismiss.onClick.AddListener(() => { if (State == null || !State.report.pending) CloseSheet(); });
            float available = Math.Max(650, Screen.safeArea.height * 1080f / Math.Max(1, Screen.width) - 240); height = Math.Min(height, available);
            var sheet = Panel(modal, "Paper sheet", Paper); AnchorBar(sheet, false, height); sheet.gameObject.AddComponent<Button>();
            Label(sheet, title, 42, Sage, 35, 20, 850, 82);
            if (key != "report" && key != "recovery") Button(sheet, "关闭", 886, 28, 156, 66, CloseSheet);
            sheetContent = Scroll(sheet, 123, 28, 1700, "Sheet scroll"); sheetY = 6;
        }
        void EndSheet() { sheetContent.sizeDelta = new Vector2(0, Math.Max(sheetY + 30, 100)); }
        void Paragraph(string text, float height = 150, int size = 30)
        { Label(sheetContent, text, size, Ink, 38, sheetY, 1000, height); sheetY += height + 15; }
        void Row(string title, string detail, int icon, string actionText, Action action, bool enabled = true, Func<bool> ready = null, Func<string> liveDetail = null)
        {
            float y = sheetY;
            var row = Panel(sheetContent, title, new Color32(244, 235, 208, 255), 28, y, 1024, 170);
            if (icon >= 0) Picture(row, "inventory", icon, 4, 4, 12, 18, 125, 125);
            float left = icon >= 0 ? 153 : 22;
            Label(row, title, 32, Sage, left, 12, 640 - left, 54);
            var details = Label(row, detail, 25, Ink, left, 70, 640 - left, 88);
            if (liveDetail != null) modalBindings.Add(() => details.text = liveDetail());
            if (action != null)
            {
                var button = Button(row, actionText, 655, 34, 345, 99, action, enabled, true);
                if (ready != null) modalBindings.Add(() => button.interactable = ready());
            }
            else if (!string.IsNullOrEmpty(actionText)) Label(row, actionText, 37, Sage, 655, 30, 345, 100, TextAnchor.MiddleCenter);
            sheetY += 185;
        }
        void CloseSheet()
        {
            if (State != null && currentSheet == "dialogue") { State.dialogue = -1; Save(); }
            if (State != null && currentSheet == "celebration") { State.chapterCelebrated = true; Save(); }
            currentSheet = null; modalBindings.Clear();
            if (modal != null) { modal.gameObject.SetActive(false); Destroy(modal.gameObject); modal = null; }
        }
        void OpenObject(string type, int index)
        {
            selectedIndex = index;
            if (type == "settings") { ShowSettings(); return; }
            StartSheet(type == "plot" ? "庭院地块 " + (index + 1) : type == "machine" ? (index == 0 ? "魔法坩埚" : "附魔工作台") : type == "shelf" ? "货架 " + (index + 1) : type == "warehouse" ? "公共仓库" : type == "decoration" ? "小店装饰" : "修复与扩建", type);
            if (type == "plot")
            {
                var f = State.plots[index]; var status = Label(sheetContent, "", 29, Ink, 38, sheetY, 1000, 115); sheetY += 130;
                modalBindings.Add(() => status.text = "等级 " + f.level + "/5 · " + Engine.PlotStatus(f) + (f.pending >= 0 ? "\n本轮结束后切换：" + Catalog.Crops[f.pending] : "\n自动收获并重新种植"));
                if (f.remaining == 0 && f.pending >= 0) Paragraph("这一轮成熟作物正在等待入库。加工或出售旧原料对应商品，或升级仓库腾出空间后，作物切换会继续。", 160, 28);
                if (f.pending >= 0) Row("待切换作物", Catalog.Crops[f.pending], f.pending, "取消切换", () => { Engine.CancelSwitch(f); Save(); OpenObject(type, index); });
                for (int i = 0; i < 4; i++)
                {
                    int crop = i; bool unlocked = Engine.CropUnlocked(i);
                    Row(Catalog.Crops[i], unlocked ? "库存 " + State.crops[i] + "/" + Engine.Capacity + " · 基础 " + Catalog.CropSeconds[i] + "秒" : (i == 1 ? "声望 10 并修复附魔台" : "声望 " + (i == 2 ? 25 : 45) + " 解锁"), i, f.config == i && f.pending < 0 ? "已选择" : "选择作物", () => Act(() => Engine.ConfigurePlot(index, crop), "作物配置已保存，本轮结束后生效。"), unlocked);
                }
                UpgradeRow(f);
            }
            else if (type == "machine")
            {
                if (index >= State.machines.Length) { CloseSheet(); OpenObject("expansion", 0); return; }
                var f = State.machines[index]; var status = Label(sheetContent, "", 29, Ink, 38, sheetY, 1000, 115); sheetY += 130;
                modalBindings.Add(() => status.text = "等级 " + f.level + "/5 · " + Engine.MachineStatus(f) + (f.pending >= 0 ? "\n下轮配方：" + Catalog.Items[f.pending] : "\n材料充足时自动循环加工"));
                if (f.pending >= 0) Row("待切换配方", Catalog.Items[f.pending], 4 + f.pending, "取消切换", () => { Engine.CancelSwitch(f); Save(); OpenObject(type, index); });
                for (int i = 0; i < 6; i++)
                {
                    if (Catalog.Machine[i] != index) continue; int item = i;
                    Row(Catalog.Items[i], RecipeText(i) + "\n" + (Engine.ItemUnlocked(i) ? Catalog.ItemSeconds[i] + "秒 · 售价 " + Catalog.Price[i] : "声望 " + Catalog.Unlock[i] + " 解锁"), 4 + i, f.config == i && f.pending < 0 ? "已配置" : "循环制作", () => Act(() => Engine.ConfigureMachine(index, item), "循环配方已保存。"), Engine.ItemUnlocked(i));
                }
                UpgradeRow(f);
            }
            else if (type == "shelf")
            {
                var f = State.shelves[index]; Paragraph("货架直接出售公共仓库的商品；订单预留部分不会被卖出。基础销售间隔 8 秒。", 130);
                for (int i = 0; i < 6; i++)
                {
                    int item = i; bool unlocked = Engine.ItemUnlocked(i);
                    Row(Catalog.Items[i], unlocked ? "库存 " + State.items[i] + "/" + Engine.Capacity + " · 预留 " + Engine.Reserved(i) + "\n售价 " + Catalog.Price[i] + " 金币" : "解锁：声望 " + Catalog.Unlock[i] + (Catalog.Machine[i] == 1 ? " · 修复附魔台" : ""), 4 + i, f.config == i ? "已售卖" : "选择售卖", () => Act(() => Engine.ConfigureShelf(index, item), "货架售卖品已保存。"), unlocked);
                }
                UpgradeRow(f);
            }
            else if (type == "warehouse")
            {
                Paragraph("仓库等级 " + State.warehouse + "/4。每种物品独立容量，不会挤占其他物品的空间。", 120);
                for (int i = 0; i < 4; i++) Row(Catalog.Crops[i], State.crops[i] + "/" + Engine.Capacity, i, "", null);
                for (int i = 0; i < 6; i++) Row(Catalog.Items[i], State.items[i] + "/" + Engine.Capacity + " · 加工预留仓位 " + Engine.Incoming(i), 4 + i, "", null);
                if (State.warehouse < 4) Row("升级仓库", "", 12, "升级仓库", () => Act(Engine.UpgradeWarehouse, "仓库容量提升了。"), true, () => State.coins >= Catalog.WarehouseCost[State.warehouse], () => "每种容量 → " + Catalog.Capacity[State.warehouse] + "\n已有 " + State.coins + " / 所需 " + Catalog.WarehouseCost[State.warehouse]);
            }
            else if (type == "decoration")
            {
                string[] names = { "门牌", "窗帘", "地毯", "门前花盆" }; string[] reward = { "完成第一章", "完成园丁故事", "完成面包师故事", "完成第一章" };
                string[] basic = { "旧橡木牌", "素色亚麻帘", "黄麻编织垫", "薄荷陶盆" };
                string[] unlocked = { "开张茶会牌", "园丁叶纹帘", "面包纹地毯", "邻里花束盆" };
                for (int i = 0; i < 4; i++)
                {
                    int n = i; bool available = Engine.DecorationUnlocked(i);
                    var row = Panel(sheetContent, names[i], new Color32(244, 235, 208, 255), 28, sheetY, 1024, 224);
                    for (int variant = 0; variant < 2; variant++)
                    {
                        float x = 12 + variant * 140;
                        Picture(row, "decorations", i * 2 + variant, 2, 4, x, 8, 128, 145);
                        Label(row, State.decoration[i] == (variant == 1) ? "使用中" : variant == 1 && !available ? "待解锁" : "可选择", 23, Sage, x, 164, 128, 43, TextAnchor.MiddleCenter);
                    }
                    Label(row, names[i], 32, Sage, 304, 12, 325, 50);
                    Label(row, basic[i] + " /\n" + unlocked[i] + "\n" + (available ? "免费切换" : reward[i] + "后解锁"), 25, Ink, 304, 66, 330, 150);
                    Button(row, "切换外观", 655, 60, 345, 99, () =>
                    {
                        if (!Engine.ToggleDecoration(n)) return;
                        Save(); audioController.Cue("confirm");
                        // The selected preview is the confirmation, without a floating
                        // message obscuring the decoration choices below it.
                        ShowPage(currentPage); OpenObject("decoration", 0);
                    }, available, true);
                    sheetY += 239;
                }
            }
            else
            {
                int p = State.plots.Length, sh = State.shelves.Length;
                if (p < Catalog.MaxPlots) Row("第 " + (p + 1) + " 块地 · 共十二块", "", 0, "扩建地块", () => Act(Engine.BuyPlot, "新的地块已开放。"), true, () => State.coins >= Catalog.PlotCost[p] && (p != 1 || Engine.TotalSold >= 3) && State.reputation >= Catalog.PlotRep[p] && (p < 4 || State.sideRoom), () => PlotUnlockDetails(p));
                if (State.machines.Length == 1) Row("修复附魔工作台", "", 11, "修复", () => Act(Engine.RepairWorkbench, "工坊亮起了新的魔法微光。"), true, () => State.reputation >= 10 && State.coins >= 260, () => "声望 10 · 已有 " + State.coins + " / 所需 260\n解锁蘑菇与萤光瓶");
                if (sh < 4) Row("第 " + (sh + 1) + " 个货架", "", 12, "增加货架", () => Act(Engine.BuyShelf, "新的货架已开放。"), true, () => State.coins >= Catalog.ShelfCost[sh] && State.machines.Length > 1 && (sh != 2 || State.reputation >= 60) && (sh != 3 || State.sideRoom), () => (sh == 1 ? "先修复附魔台" : sh == 2 ? "声望 60" : "先修复侧屋") + "\n已有 " + State.coins + " / 所需 " + Catalog.ShelfCost[sh]);
                if (!State.sideRoom) Row("修复侧屋", "", 14, "修复侧屋", () => Act(Engine.RepairSideRoom, "侧屋恢复了，更多经营位置已开放。"), true, () => State.reputation >= 100 && State.sales >= 15000 && State.coins >= 5000, () => "声望 " + State.reputation + "/100 · 销售额 " + State.sales + "/15000\n已有 " + State.coins + " / 所需 5000");
                Paragraph("温室与阁楼是未来的扩展方向。第一章无需购买，也没有建造等待。", 110, 27);
            }
            EndSheet();
        }
        void UpgradeRow(Facility f)
        {
            if (f.level >= 5) { Paragraph("设施已达到最高等级。", 70); return; }
            Row("设施升级 · " + f.level + " → " + (f.level + 1), "", 13, "升级", () => Act(() => Engine.Upgrade(f), "升级完成，新速度从下一轮生效。"), true, () => State.coins >= Catalog.Upgrade[f.level], () => "速度 ×" + Catalog.Speed[f.level] + "，本轮进度保留\n已有 " + State.coins + " / 所需 " + Catalog.Upgrade[f.level]);
        }
        void ShowSettings()
        {
            StartSheet("小店设置", "settings");
            Row("背景音乐", "轻柔的森林旋律", -1, State.music ? "已开启" : "已关闭", () => { State.music = !State.music; Save(); audioController.Apply(State); ShowSettings(); });
            Row("音效", "操作、收获与营业反馈", -1, State.sound ? "已开启" : "已关闭", () => { State.sound = !State.sound; Save(); audioController.Apply(State); ShowSettings(); });
            Row("简化装饰动画", "不改变生产速度或收益", -1, State.simpleAnimation ? "已简化" : "完整动画", () => { State.simpleAnimation = !State.simpleAnimation; Save(); ShowSettings(); });
            Row("制作信息", "林间奇物铺 0.1.0\n免费 · 无广告 · 单机离线", -1, "素材与许可", ShowCredits);
            Row("进度管理", "清除操作不可撤销，请谨慎操作", -1, "查看进度管理", ShowReset); EndSheet();
        }
        void ShowWelcome()
        {
            StartSheet("一把树屋的旧钥匙", "welcome");
            Picture(sheetContent, "residents", 3, 5, 2, 365, 0, 320, 350); sheetY = 370;
            Paragraph("兔子邮差：欢迎来到森林村庄！这是树洞小店的钥匙，邻居们一直盼着这里重新开张呢。\n接过钥匙，我们先去庭院收下成熟的月光薄荷吧。", 280, 34);
            Row("从一杯温暖的茶开始", "种植、加工与销售在教学后会自动循环", 4, "接过钥匙", () => { State.welcomeRead = true; Save(); FocusOpeningStep(); ShowPage("小店"); }); EndSheet();
        }
        void ShowCredits()
        {
            StartSheet("制作信息与许可", "credits");
            Paragraph("林间奇物铺\n森林生活魔法小店 · 第一章\n版本 0.1.0", 180, 36);
            Paragraph("美术：本项目原创制作的立体场景、角色、商品模型与材质。\n音乐：Kistol《Hot Springs Town》（CC0）。\n环境与操作音效：项目原创合成制作。\n中文字体：Noto Sans CJK SC，SIL Open Font License 1.1。", 270);
            Paragraph("背景音乐来源与处理\n" + (Resources.Load<TextAsset>("Audio/ThirdPartyMusic")?.text ?? "许可记录缺失"), 620, 24);
            string fontLicense = "Noto 字体许可随工程与应用提供。\n" + (Resources.Load<TextAsset>("Fonts/OFL")?.text ?? "许可文件缺失");
            var legalText = Label(sheetContent, fontLicense, 24, Ink, 35, sheetY, 1000, 100);
            float legalHeight = Mathf.Ceil(legalText.preferredHeight) + 30;
            legalText.rectTransform.sizeDelta = new Vector2(1000, legalHeight);
            sheetY += legalHeight + 18;
            EndSheet();
        }
        void ShowReset()
        {
            StartSheet("进度管理", "reset"); Paragraph("重新开始会移除当前游戏进度，包括金币、故事和装饰。", 150);
            Row("重新开始", "下一步仍需明确确认", -1, "继续", () => { StartSheet("确认重新开始", "reset-confirm"); Paragraph("确认清除当前进度并从第一天重新开始？", 160); Row("最终确认", "当前进度将不再作为游戏存档使用", -1, "确认重新开始", ResetGame); EndSheet(); }); EndSheet();
        }
        void ResetGame()
        {
            try
            {
                store.ArchiveAndReset(); recovering = false; State = new GameState { utcTicks = DateTime.UtcNow.Ticks }; Engine = new Economy(State); previousSales = previousHarvests = previousCrafts = 0;
                if (audioController == null) audioController = gameObject.AddComponent<ForestAudio>(); audioController.Apply(State); shopBuilt = false; Save(); FocusOpeningStep(); ShowPage("小店"); ShowWelcome();
            }
            catch (Exception e) { Toast("无法重新开始：" + e.Message, 5); }
        }
        void ShowRecovery(string message)
        { StartSheet("存档需要恢复", "recovery"); Paragraph(message + "\n原始文件已保留，尚未覆盖进度。", 260); Row("重新开始", "将保留旧文件的本地归档", -1, "进度管理", ShowReset); EndSheet(); }
        void ShowReport()
        {
            StartSheet("离线营业报告", "report"); var r = State.report;
            Paragraph("欢迎回来，店主。\n离开 " + Duration(r.away) + " · 有效营业 " + Duration(r.effective), 150, 34);
            Row("营业收入", "收益已保存，关闭报告不会重复发放。", 13, "+" + r.coins, null);
            for (int i = 0; i < 4; i++) if (r.cropDelta[i] != 0) Row(Catalog.Crops[i], "库存变化 " + r.cropDelta[i].ToString("+0;-0;0"), i, "", null);
            for (int i = 0; i < 6; i++) if (r.itemDelta[i] != 0) Row(Catalog.Items[i], "库存变化 " + r.itemDelta[i].ToString("+0;-0;0"), i + 4, "", null);
            if (!string.IsNullOrEmpty(r.hint)) Paragraph(r.hint, 155);
            if (r.coins == 0) Paragraph("本次未产生销售收入。可以检查货架配置、材料供应与订单预留。", 125);
            Row("今日的小店也很温暖", "最多记录 8 小时，报告停留期间经营暂停。", -1, "收下", () => { OfflineSettlement.CloseReport(State, DateTime.UtcNow.Ticks); Save(); CloseSheet(); }); EndSheet();
        }
        string Duration(double seconds) => seconds >= 3600 ? (seconds / 3600).ToString("F1") + "小时" : Math.Floor(seconds / 60) + "分钟";
        void ShowStoryDialogue(int resident)
        { StartSheet(Catalog.Residents[resident], "dialogue"); Picture(sheetContent, "residents", resident, 5, 2, 340, sheetY, 380, 410); sheetY += 420; Paragraph(StoryLines(Catalog.After[resident]), 220, 34); Row("心愿完成", "故事已记录在森林手记中", -1, "继续营业", CloseSheet); EndSheet(); if (audioController != null) audioController.Cue("voice"); }
        // Each authored sentence fits within the story text column. Explicit
        // sentence breaks keep legacy Text from stranding Chinese punctuation.
        string StoryLines(string text)
        {
            var result = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i]; result.Append(c);
                if ((c == '。' || c == '！' || c == '？') && i + 1 < text.Length && text[i + 1] != '\n') result.Append('\n');
            }
            return result.ToString();
        }
        void ShowWish(Order o)
        { StartSheet(Catalog.Residents[o.resident] + "的心愿", "wish"); Paragraph(StoryLines(o.story ? Catalog.Before[o.resident] : "想请你帮忙准备 " + Catalog.Items[o.item] + " ×" + o.count + "。不着急，准备好了再告诉我吧。"), 250, 34); EndSheet(); }
        void ShowResident(int resident)
        { StartSheet(Catalog.Residents[resident] + "的故事", "resident"); Paragraph(StoryLines(Catalog.Before[resident]), 240); Paragraph(StoryLines(Catalog.After[resident]), 240); if (State.friendship[resident] >= 25) Paragraph("好感 25 · 邻里的问候\n" + Catalog.Residents[resident] + "：路过树屋的时候，看见窗里亮着灯，心里就觉得很踏实。谢谢你让小店重新热闹起来。", 260); else Paragraph("好感达到 25 后，还会有新的问候。", 100); EndSheet(); }
        void ShowPortrait(int resident)
        { StartSheet("森林手记 · 纪念", "portrait"); var frame = Panel(sheetContent, "Keepsake frame", new Color32(222, 228, 207, 255), 38, 10, 1000, 700); Picture(frame, State.sideRoom ? "treehouse-restored" : "treehouse", 0, 1, 1, 10, 10, 590, 680); Picture(frame, "residents", resident, 5, 2, 625, 180, 330, 420); Label(frame, Catalog.Residents[resident], 32, Sage, 600, 90, 380, 70, TextAnchor.MiddleCenter); sheetY = 735; Paragraph(Catalog.Residents[resident] + " · 相遇在温暖的树屋\n愿每个平凡日子，都有一点小小的魔法。", 180, 34); EndSheet(); }
        void ShowCelebration()
        {
            StartSheet("第一章 · 林间开张茶会", "celebration");
            Paragraph("窗边的灯亮了，茶也泡好了。\n五位熟客带着花和新鲜面包，来到重新开张的小店。", 190, 34);
            Picture(sheetContent, "celebration", 0, 1, 1, 20, 200, 1020, 600);
            sheetY = 825; Paragraph("“欢迎光临，林间奇物铺！”\n获得新门牌与茶会花盆。第一章完成后，你仍然可以继续经营、帮助邻居和布置小店。", 250, 32);
            Row("森林里的新日常", "没有重置，所有进度继续保留", 15, "继续经营", () => { State.decoration[0] = State.decoration[3] = true; State.chapterCelebrated = true; Save(); ShowPage("小店"); }); EndSheet();
            if (audioController != null) audioController.Cue("unlock");
        }
        void ShowExit()
        { StartSheet("今天先休息了吗？", "exit", 600); Paragraph("离开后小店会继续按当前配置经营，最多记录 8 小时。进度会保存在设备上。", 160); Row("明天也欢迎回来", "确认后退出游戏", -1, "保存并退出", () => { quitting = true; Save(); Application.Quit(); }); EndSheet(); }
    }
}
