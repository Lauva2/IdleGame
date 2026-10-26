using System;
using ForestShop.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        int fishFilter;
        string fishNotice="";
        int fishNoticeSequence=-1;
        readonly int[] fishLengthsBefore=new int[FishingCatalog.Species.Length], fishWeightsBefore=new int[FishingCatalog.Species.Length];
        static string FishLength(int millimeters) => (millimeters/10.0).ToString("0.0")+"厘米";
        static string FishWeight(int grams) => grams.ToString("N0")+"克";
        static Color FishQualityColor(int quality) => quality==3?new Color32(242,220,165,255):quality==2?new Color32(209,226,233,255):quality==1?new Color32(215,233,215,255):Sand;
        string FishHabitats(int species)
        {
            string result=""; int mask=FishingCatalog.Species[species].Spots;
            for(int i=0;i<3;i++) if((mask & (1<<i))!=0) result+=(result.Length>0?"、":"")+FishingCatalog.Spots[i]; return result;
        }
        void AdvanceFishing(float seconds)
        {
            if(State==null || Engine==null || backgrounded || State.report.pending || currentSheet!="fishing" || !State.fishing.running) return;
            int before=State.fishing.totalCaught;
            for(int i=0;i<fishLengthsBefore.Length;i++) { fishLengthsBefore[i]=State.fishing.entries[i].longestMm; fishWeightsBefore[i]=State.fishing.entries[i].heaviestGrams; }
            Engine.Fishing.Advance(seconds);
            if(State.fishing.totalCaught==before) return;
            var caught=State.fishing.recent[0]; var entry=State.fishing.entries[caught.species];
            bool longest=caught.lengthMm>fishLengthsBefore[caught.species], heaviest=caught.weightGrams>fishWeightsBefore[caught.species];
            fishNotice=entry.count==1?"新发现":longest&&heaviest?"长度与重量新纪录":longest?"长度新纪录":heaviest?"重量新纪录":"自动钓获";
            fishNoticeSequence=caught.sequence;
            Save(); audioController?.Cue("harvest"); World().PresentFishCatch(caught);
        }
        void ShowFishing(int spot)
        {
            if(spot<0 || spot>2) return;
            if(!Engine.Fishing.SpotUnlocked(spot))
            {
                StartSheet(FishingCatalog.Spots[spot],"fishing-locked",650);
                Paragraph(spot==2?"沿藤蔓小径走进苔石林地，就能找到旧亭边安静的洄湾。":"修好溪边木桥，三处森林钓点会陆续开放。",150,32);
                Row("森林路径",Engine.Forest.Goal,15,"查看修复",()=>OpenObject("forest-repair",spot==2?2:1)); EndSheet(); return;
            }
            GoForest(spot); var world=World();
            StartSheet(FishingCatalog.Spots[spot],"fishing",660);
            BeginFishingViewport(world);
            // Keep the actual float visible and allow dragging the exposed world.
            modal.GetComponent<Image>().color=Color.clear; modal.GetComponent<Image>().raycastTarget=false; modal.GetComponent<Button>().enabled=false;
            int bait=Engine.Fishing.BaitUnlocked(State.fishing.bait)?State.fishing.bait:0;
            Engine.Fishing.Start(spot,bait); Save();
            var status=Panel(sheetContent,"Automatic fishing phase",Sand,28,sheetY,1024,82);
            var heading=Label(status,"",30,Sage,24,0,855,44);
            var totals=Label(status,"",24,Muted,24,39,855,37);
            var phaseIcon=Symbol(status,ForestSymbol.Timer,938,8,54);
            var track=Panel(status,"Fishing progress track",new Color32(217,218,186,255),24,73,976,7);
            var fill=Panel(track,"Actual fishing progress",Sage,0,0,1,9);
            var resume=status.gameObject.AddComponent<Button>(); resume.transition=Selectable.Transition.None; resume.onClick.AddListener(()=>{if(!State.fishing.running){Engine.Fishing.Start(spot,State.fishing.bait);Save();}});
            modalBindings.Add(()=>
            {
                var f=State.fishing;
                heading.text=!f.running?"已暂停 · 点这里继续":f.phase==1?(f.duration-f.remaining<.42 && !State.simpleAnimation?"抛竿入水":"自动等待鱼讯"):f.phase==2?"浮漂下沉 · 自动收竿":"钓到了 · 准备下一竿";
                totals.text="发现 "+Engine.Fishing.Discovered+"/12 · 已钓 "+f.totalCaught+"尾 · 离开暂停";
                phaseIcon.Symbol=!f.running?ForestSymbol.Timer:f.phase==3?ForestSymbol.Check:f.phase==2?ForestSymbol.Fish:ForestSymbol.Timer;
                fill.sizeDelta=new Vector2(976*(f.duration>0?Mathf.Clamp01(1-(float)(f.remaining/f.duration)):0),9);
            }); sheetY+=94;
            var baitStrip=Rect(sheetContent,"Fishing bait choices",0,sheetY,1080,94);
            for(int i=0;i<3;i++)
            {
                int lure=i; bool available=Engine.Fishing.BaitUnlocked(i); bool selected=State.fishing.bait==i;
                var button=Button(baitStrip,FishingCatalog.Baits[i],30+i*350,0,320,84,()=>{Engine.Fishing.Pause();Engine.Fishing.Start(spot,lure);Save();ShowFishing(spot);},available,selected);
                button.name="Fishing bait "+i;EnlargeTouch(button,0,84);var label=button.GetComponentInChildren<Text>(); label.fontSize=29;label.rectTransform.anchoredPosition=new Vector2(94,-1);label.rectTransform.sizeDelta=new Vector2(216,44);
                var icon=Picture(button.transform,"fishing-bait",i,1,1,6,6,82,69);
                var hint=Label(button.transform,"",22,selected?Paper:Muted,94,43,216,35);
                modalBindings.Add(()=>{bool unlocked=Engine.Fishing.BaitUnlocked(lure);button.interactable=unlocked;icon.color=unlocked?Color.white:new Color(.4f,.4f,.35f,.55f);hint.text=!unlocked?"发现"+(lure*3)+"种鱼开放":State.fishing.bait==lure?"使用中":"偏好饵料";});
            }
            sheetY+=94;
            var recent=Panel(sheetContent,"Latest automatic catch",Sand,28,sheetY,1024,176);
            var emblem=Symbol(recent,ForestSymbol.Fish,30,36,130);
            var image=Picture(recent,"fish",0,1,1,5,30,180,120); image.gameObject.SetActive(false); int lastSequence=-1;
            var name=Label(recent,"等待第一条鱼",32,Sage,195,0,535,48);
            var quality=Panel(recent,"Written catch quality",Paper,784,4,215,42); var grade=Label(quality,"",27,Ink,0,0,215,42,TextAnchor.MiddleCenter);
            var detail=Label(recent,"选好饵料，等候溪里的消息。",27,Ink,195,48,804,43);
            var context=Label(recent,"鱼获会自动记入图鉴",23,Muted,195,84,804,38);
            var notice=Panel(recent,"Catch discovery or record",Paper,195,124,804,43);
            var noticeIcon=Symbol(notice,ForestSymbol.Fish,10,5,32);
            var note=Label(notice,"",27,Sage,55,0,738,43);
            var inspect=recent.gameObject.AddComponent<Button>();inspect.transition=Selectable.Transition.None;inspect.onClick.AddListener(()=>{if(State.fishing.recent.Length>0) ShowFishCatch(State.fishing.recent[0]);});
            modalBindings.Add(()=>
            {
                var f=State.fishing;
                if(f.recent.Length==0){quality.gameObject.SetActive(false);notice.gameObject.SetActive(false);return;}
                var caught=f.recent[0]; var kind=FishingCatalog.Species[caught.species]; quality.gameObject.SetActive(true);notice.gameObject.SetActive(true);
                if(caught.sequence!=lastSequence){image.sprite=Art("fish",caught.species*4+caught.quality,1,1);lastSequence=caught.sequence;}
                image.gameObject.SetActive(true);emblem.gameObject.SetActive(false);name.text=kind.Name;
                quality.GetComponent<Image>().color=FishQualityColor(caught.quality);grade.text=FishingCatalog.Qualities[caught.quality];
                detail.text=FishLength(caught.lengthMm)+" · "+FishWeight(caught.weightGrams);
                context.text=FishingCatalog.Spots[caught.spot]+" · "+FishingCatalog.Baits[caught.bait]+" · "+FishingCatalog.Rarities[kind.Rarity];
                bool fresh=fishNoticeSequence==caught.sequence && fishNotice!="自动钓获";
                notice.GetComponent<Image>().color=fresh?Sage:Paper;note.color=fresh?Paper:Muted;noticeIcon.Light=fresh;
                noticeIcon.Symbol=fresh && fishNotice!="新发现"?ForestSymbol.Check:ForestSymbol.Fish;
                note.text=(fresh?fishNotice:"最近钓获")+" · 第"+caught.sequence+"尾";
            }); sheetY+=186;
            var tools=Rect(sheetContent,"Fishing controls",0,sheetY,1080,82);
            IconButton(tools,"鱼谱",ForestSymbol.Fish,30,0,320,82,ShowFishCollection).name="Open fish collection";
            bool details=false; Button toggle=null;
            Action reflow=()=>
            {
                baitStrip.gameObject.SetActive(details);context.gameObject.SetActive(details);
                recent.anchoredPosition=new Vector2(28,-(details?188:94));recent.sizeDelta=new Vector2(1024,details?176:140);
                notice.anchoredPosition=new Vector2(195,-(details?124:88));
                float toolsY=details?374:244;tools.anchoredPosition=new Vector2(0,-toolsY);sheetY=toolsY+92;
                toggle.GetComponentInChildren<Text>().text=details?"收起":"详情";
                FinishFishingSheet();FrameFishing(world,spot,false);
            };
            toggle=IconButton(tools,"详情",ForestSymbol.Info,370,0,260,82,()=>{details=!details;reflow();});toggle.name="Fishing details toggle";
            IconButton(tools,"",ForestSymbol.ZoomOut,662,0,110,82,()=>world.Zoom(1.16f)).name="Fishing zoom out";
            IconButton(tools,"",ForestSymbol.ZoomIn,792,0,110,82,()=>world.Zoom(1/1.16f)).name="Fishing zoom in";
            IconButton(tools,"",ForestSymbol.Recenter,922,0,110,82,()=>FrameFishing(world,spot)).name="Fishing recenter";
            reflow();FrameFishing(world,spot);
        }
        void FrameFishing(ForestWorld3D world,int spot,bool reset=true)
        {
            Canvas.ForceUpdateCanvases();float focus=.5f;
            if(page!=null && sheetPanel!=null)foreach(var pointer in page.GetComponentsInChildren<ForestWorldPointer>())
            {
                var view=(RectTransform)pointer.transform;var corners=new Vector3[4];var paper=new Vector3[4];view.GetWorldCorners(corners);sheetPanel.GetWorldCorners(paper);
                float bottom=corners[0].y,top=corners[1].y,visibleBottom=Mathf.Max(bottom,paper[1].y);
                focus=top>bottom?((top+Mathf.Min(top,visibleBottom))*.5f-bottom)/(top-bottom):.5f;break;
            }
            if(reset)world.FocusFishing(spot,focus);else world.ReframeFishing(focus);
        }
        void ShowFishCollection() { collectionTab=2; ShowPage("图鉴"); }
        float BuildFishCollection(Transform parent,float y)
        {
            y = BuildFishCollectionGoal(parent, y);
            string[] filters={"全部鱼类","已发现","最近钓获"};
            for(int i=0;i<3;i++){int filter=i;IconButton(parent,filters[i],i==2?ForestSymbol.Timer:ForestSymbol.Fish,35+i*350,y,310,78,()=>{fishFilter=filter;ShowFishCollection();},true,fishFilter==i).name="Fish filter "+i;}
            y+=94;
            if(fishFilter==2)
            {
                if(State.fishing.recent.Length==0) return FishCollectionEmpty(parent,y);
                foreach(var entry in State.fishing.recent)
                {
                    var caught=entry; var kind=FishingCatalog.Species[caught.species];
                    var card=Panel(parent,"Recorded fish "+caught.sequence,Sand,32,y,1016,208);
                    Picture(card,"fish",caught.species*4+caught.quality,1,1,8,32,196,132);
                    Label(card,kind.Name+" · 第"+caught.sequence+"尾",33,Sage,219,5,770,55);
                    Label(card,FishingCatalog.Qualities[caught.quality]+" · "+FishLength(caught.lengthMm)+" · "+FishWeight(caught.weightGrams),28,Ink,219,73,770,50);
                    Label(card,FishingCatalog.Spots[caught.spot]+" · "+FishingCatalog.Baits[caught.bait],25,Muted,219,137,770,54);
                    var click=card.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>ShowFishCatch(caught));y+=226;
                }
                Label(parent,"保留最近"+FishingCatalog.RecentLimit+"次明细，全部钓获与各品质总数持续记录。",25,Muted,44,y,990,90);return y+110;
            }
            int shown=0;
            for(int i=0;i<FishingCatalog.Species.Length;i++)
            {
                int species=i; var record=State.fishing.entries[i]; bool known=record.count>0; if(fishFilter==1 && !known) continue;
                var kind=FishingCatalog.Species[i];var card=Panel(parent,"Fish species "+i,Sand,32,y,1016,208);
                var icon=Picture(card,"fish",i*4+Math.Max(0,record.bestQuality),1,1,8,32,196,132);if(!known)icon.color=new Color(.27f,.32f,.27f,.48f);
                Label(card,kind.Name+(known?"":" · 未发现"),34,Sage,219,6,770,54);
                Label(card,known?record.count.ToString("N0")+"尾 · 最佳 "+FishingCatalog.Qualities[record.bestQuality]:FishingCatalog.Rarities[kind.Rarity]+" · 偏好"+FishingCatalog.Baits[kind.Bait],28,Ink,219,73,770,50);
                Label(card,known?"最长 "+FishLength(record.longestMm)+" · 最重 "+FishWeight(record.heaviestGrams):"栖息 · "+FishHabitats(i),26,Muted,219,136,770,60);
                var click=card.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>ShowFishSpecies(species)); y+=226;shown++;
            }
            return shown==0?FishCollectionEmpty(parent,y):y;
        }
        float FishCollectionEmpty(Transform parent,float y)
        {
            Symbol(parent,ForestSymbol.Fish,440,y+20,160);
            Label(parent,"从溪边的第一竿开始，慢慢收集森林里的鱼。",31,Sage,70,y+220,940,110,TextAnchor.MiddleCenter);
            IconButton(parent,"前往浅滩",ForestSymbol.Forest,260,y+365,560,90,()=>ShowFishing(0));return y+500;
        }
        void ShowFishSpecies(int species)
        {
            selectedIndex=species;var kind=FishingCatalog.Species[species];var entry=State.fishing.entries[species];bool known=entry.count>0;
            StartSheet(kind.Name,"fish-species");
            var photo=Picture(sheetContent,"fish",species*4+Math.Max(0,entry.bestQuality),1,1,240,0,600,306);if(!known)photo.color=new Color(.27f,.32f,.27f,.48f);sheetY=318;
            Paragraph(FishingCatalog.Rarities[kind.Rarity]+" · "+kind.Habit,60,28);
            Paragraph("栖息 · "+FishHabitats(species)+"\n偏好 · "+FishingCatalog.Baits[kind.Bait],100,27);
            FishStat(sheetContent,0,sheetY,"累计钓获",entry.count.ToString("N0")+"尾",ForestSymbol.Fish);
            FishStat(sheetContent,1,sheetY,"最长纪录",known?FishLength(entry.longestMm):"—",ForestSymbol.Ruler);
            FishStat(sheetContent,2,sheetY,"最重纪录",known?FishWeight(entry.heaviestGrams):"—",ForestSymbol.Weight);sheetY+=126;
            Label(sheetContent,known?"首次发现于总钓获第"+entry.firstSequence+"尾":"尚未发现 · 从栖息地开始寻找",26,Muted,40,sheetY,996,48);sheetY+=60;
            for(int q=0;q<4;q++)
            {
                var tile=Panel(sheetContent,"Fish quality total "+q,FishQualityColor(q),28+(q%2)*520,sheetY+(q/2)*100,504,88);
                Label(tile,FishingCatalog.Qualities[q]+(q==entry.bestQuality?" · 最佳":""),28,Ink,16,18,235,52);
                var number=Label(tile,entry.qualityCounts[q].ToString("N0")+"尾",32,Sage,250,14,236,60,TextAnchor.MiddleRight);number.resizeTextForBestFit=true;number.resizeTextMinSize=22;number.resizeTextMaxSize=32;
            }
            sheetY+=200;
            if (!known)
            {
                Paragraph(Engine.Fishing.BaitUnlocked(kind.Bait) ? "偏好饵料已开放，可在钓点切换。" : "发现" + (kind.Bait * 3) + "种鱼后开放偏好饵料。", 70, 27);
            }
            int target=0;for(int i=2;i>=0;i--)if((kind.Spots&(1<<i))!=0)target=i;
            for(int i=0;i<3;i++)if((kind.Spots&(1<<i))!=0 && Engine.Fishing.SpotUnlocked(i)){target=i;break;}
            IconButton(sheetContent,Engine.Fishing.SpotUnlocked(target)?"前往钓点":"查看开放条件",ForestSymbol.Fish,28,sheetY,1024,84,()=>ShowFishing(target),true,true).name="Go to species fishing spot";sheetY+=102;EndSheet();
        }
        float BuildFishCollectionGoal(Transform parent, float y)
        {
            int found = Engine.Fishing.Discovered, next = NextUnfoundFish();
            var card = Panel(parent, "Fish collection journey", Paper, 32, y, 1016, 218);
            Symbol(card, ForestSymbol.Fish, 20, 16, 62);
            Label(card, "发现 " + found + " / 12", 37, Sage, 102, 8, 470, 64);
            Label(card, State.fishing.totalCaught.ToString("N0") + " 尾 · 自动记录", 27, Muted, 572, 14, 420, 54, TextAnchor.MiddleRight);
            for (int i = 0; i < 12; i++) Panel(card, "Species progress " + i, i < found ? Sage : Sand, 24 + i * 82, 82, 68, 10);
            Label(card, next >= 0 ? "下一条线索" : "十二种鱼已发现", 28, Sage, 24, 105, 670, 45);
            Label(card, next >= 0 ? FishingCatalog.Species[next].Habit : "继续寻找更长、更重的个人纪录", 26, Ink, 24, 155, 680, 46);
            IconButton(card, next >= 0 ? "看线索" : "看纪录", next >= 0 ? ForestSymbol.Forest : ForestSymbol.Ruler, 734, 125, 258, 78,
                () => { int target = NextUnfoundFish(); ShowFishSpecies(target >= 0 ? target : State.fishing.recent.Length > 0 ? State.fishing.recent[0].species : 0); }, true, true).name = "Fish next clue";
            return y + 236;
        }
        void FishStat(Transform parent,int column,float y,string caption,string value,ForestSymbol symbol,int columns=3)
        {
            float width=(1024-(columns-1)*16)/(float)columns;
            var tile=Panel(parent,caption,Sand,28+column*(width+16),y,width,112);
            Symbol(tile,symbol,13,9,38);Label(tile,caption,25,Muted,63,8,width-76,39);
            var number=Label(tile,value,35,Sage,15,48,width-30,58,TextAnchor.MiddleCenter);number.resizeTextForBestFit=true;number.resizeTextMinSize=24;number.resizeTextMaxSize=35;
        }
        void ShowFishCatch(FishCatch caught)
        {
            var kind=FishingCatalog.Species[caught.species];StartSheet("第"+caught.sequence+"尾 · "+kind.Name,"fish-catch");
            Picture(sheetContent,"fish",caught.species*4+caught.quality,1,1,240,0,600,306);sheetY=318;
            Paragraph(FishingCatalog.Qualities[caught.quality]+"品质 · "+FishingCatalog.Rarities[kind.Rarity]+"品种",70,31);
            FishStat(sheetContent,0,sheetY,"本条长度",FishLength(caught.lengthMm),ForestSymbol.Ruler,2);
            FishStat(sheetContent,1,sheetY,"本条重量",FishWeight(caught.weightGrams),ForestSymbol.Weight,2);sheetY+=126;
            Paragraph("钓点 · "+FishingCatalog.Spots[caught.spot]+"\n饵料 · "+FishingCatalog.Baits[caught.bait],100,28);
            IconButton(sheetContent,"查看品种档案",ForestSymbol.Collection,28,sheetY,1024,84,()=>ShowFishSpecies(caught.species),true,true);sheetY+=102;EndSheet();
        }
    }
}
