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
            GoForest(spot); var world=World(); world.FocusFishing(spot);
            StartSheet(FishingCatalog.Spots[spot],"fishing",800);
            // Keep the actual float visible and allow dragging the exposed world.
            modal.GetComponent<Image>().color=Color.clear; modal.GetComponent<Image>().raycastTarget=false; modal.GetComponent<Button>().enabled=false;
            int bait=Engine.Fishing.BaitUnlocked(State.fishing.bait)?State.fishing.bait:0;
            Engine.Fishing.Start(spot,bait); Save();
            var status=Panel(sheetContent,"Automatic fishing phase",Sand,28,sheetY,1024,125);
            var heading=Label(status,"",31,Sage,24,4,855,54);
            var totals=Label(status,"",25,Muted,24,58,855,48);
            var phaseIcon=Symbol(status,ForestSymbol.Timer,923,16,67);
            var track=Panel(status,"Fishing progress track",new Color32(217,218,186,255),24,109,976,9);
            var fill=Panel(track,"Actual fishing progress",Sage,0,0,1,9);
            var resume=status.gameObject.AddComponent<Button>(); resume.transition=Selectable.Transition.None; resume.onClick.AddListener(()=>{if(!State.fishing.running){Engine.Fishing.Start(spot,State.fishing.bait);Save();}});
            modalBindings.Add(()=>
            {
                var f=State.fishing;
                heading.text=!f.running?"已暂停 · 点这里继续":f.phase==1?"自动等待鱼讯":f.phase==2?"浮漂下沉 · 自动收竿":"钓到了 · 准备下一竿";
                totals.text="发现 "+Engine.Fishing.Discovered+"/12 · 已钓 "+f.totalCaught+"尾 · 离开钓点时暂停";
                phaseIcon.Symbol=!f.running?ForestSymbol.Timer:f.phase==3?ForestSymbol.Check:f.phase==2?ForestSymbol.Fish:ForestSymbol.Timer;
                fill.sizeDelta=new Vector2(976*(f.duration>0?Mathf.Clamp01(1-(float)(f.remaining/f.duration)):0),9);
            }); sheetY+=141;
            for(int i=0;i<3;i++)
            {
                int lure=i; bool available=Engine.Fishing.BaitUnlocked(i); bool selected=State.fishing.bait==i;
                var button=Button(sheetContent,FishingCatalog.Baits[i],30+i*350,sheetY,320,94,()=>{Engine.Fishing.Pause();Engine.Fishing.Start(spot,lure);Save();ShowFishing(spot);},available,selected);
                button.name="Fishing bait "+i; var label=button.GetComponentInChildren<Text>(); label.fontSize=29;label.rectTransform.anchoredPosition=new Vector2(103,-3);label.rectTransform.sizeDelta=new Vector2(207,48);
                var icon=Picture(button.transform,"fishing-bait",i,1,1,7,8,88,78);
                var hint=Label(button.transform,"",22,selected?Paper:Muted,103,47,207,40);
                modalBindings.Add(()=>{bool unlocked=Engine.Fishing.BaitUnlocked(lure);button.interactable=unlocked;icon.color=unlocked?Color.white:new Color(.4f,.4f,.35f,.55f);hint.text=!unlocked?"发现"+(lure*3)+"种鱼开放":State.fishing.bait==lure?"使用中":"偏好饵料";});
            }
            sheetY+=110;
            var recent=Panel(sheetContent,"Latest automatic catch",Sand,28,sheetY,1024,224);
            var emblem=Symbol(recent,ForestSymbol.Fish,42,54,130);
            var image=Picture(recent,"fish",0,1,1,6,36,196,142); image.gameObject.SetActive(false); int lastSequence=-1;
            var name=Label(recent,"等待第一条鱼",33,Sage,219,8,510,58);
            var quality=Panel(recent,"Written catch quality",Paper,754,10,244,54); var grade=Label(quality,"",28,Ink,0,0,244,54,TextAnchor.MiddleCenter);
            var detail=Label(recent,"选好饵料，浮漂会自动带来消息。",28,Ink,219,76,780,91);
            var note=Label(recent,"",24,Muted,219,170,780,43);
            var inspect=recent.gameObject.AddComponent<Button>();inspect.transition=Selectable.Transition.None;inspect.onClick.AddListener(()=>{if(State.fishing.recent.Length>0) ShowFishCatch(State.fishing.recent[0]);});
            modalBindings.Add(()=>
            {
                var f=State.fishing;
                if(f.recent.Length==0){quality.gameObject.SetActive(false);return;}
                var caught=f.recent[0]; var kind=FishingCatalog.Species[caught.species]; quality.gameObject.SetActive(true);
                if(caught.sequence!=lastSequence){image.sprite=Art("fish",caught.species*4+caught.quality,1,1);lastSequence=caught.sequence;}
                image.gameObject.SetActive(true);emblem.gameObject.SetActive(false);name.text=kind.Name;
                quality.GetComponent<Image>().color=FishQualityColor(caught.quality);grade.text=FishingCatalog.Qualities[caught.quality];
                detail.text=FishLength(caught.lengthMm)+" · "+FishWeight(caught.weightGrams)+"\n"+FishingCatalog.Spots[caught.spot]+" · "+FishingCatalog.Baits[caught.bait];
                note.text=(fishNotice.Length>0?fishNotice+" · ":"")+"品种 "+FishingCatalog.Rarities[kind.Rarity]+" · 第"+caught.sequence+"尾";
            }); sheetY+=240;
            IconButton(sheetContent,"鱼类图鉴",ForestSymbol.Fish,30,sheetY,560,84,ShowFishCollection).name="Open fish collection";
            IconButton(sheetContent,"",ForestSymbol.ZoomOut,626,sheetY,120,84,()=>world.Zoom(1.16f));
            IconButton(sheetContent,"",ForestSymbol.ZoomIn,766,sheetY,120,84,()=>world.Zoom(1/1.16f));
            IconButton(sheetContent,"",ForestSymbol.Recenter,906,sheetY,120,84,()=>world.FocusFishing(spot)); sheetY+=102;
            EndSheet();
        }
        void ShowFishCollection() { collectionTab=2; ShowPage("图鉴"); }
        float BuildFishCollection(Transform parent,float y)
        {
            string[] filters={"全部鱼类","已发现","最近钓获"};
            for(int i=0;i<3;i++){int filter=i;IconButton(parent,filters[i],i==2?ForestSymbol.Timer:ForestSymbol.Fish,35+i*350,y,310,78,()=>{fishFilter=filter;ShowFishCollection();},true,fishFilter==i).name="Fish filter "+i;}
            y+=92; Label(parent,"发现 "+Engine.Fishing.Discovered+"/12 · 总钓获 "+State.fishing.totalCaught+"尾 · 四级品质分别记录",28,Muted,44,y,990,75); y+=92;
            if(fishFilter==2)
            {
                if(State.fishing.recent.Length==0) return FishCollectionEmpty(parent,y);
                foreach(var entry in State.fishing.recent)
                {
                    var caught=entry; var kind=FishingCatalog.Species[caught.species];
                    var card=Panel(parent,"Recorded fish "+caught.sequence,Sand,32,y,1016,235);
                    Picture(card,"fish",caught.species*4+caught.quality,1,1,8,40,196,145);
                    Label(card,"第"+caught.sequence+"尾 · "+kind.Name,33,Sage,219,8,770,62);
                    Label(card,FishingCatalog.Qualities[caught.quality]+" · "+FishLength(caught.lengthMm)+" · "+FishWeight(caught.weightGrams),28,Ink,219,85,770,53);
                    Label(card,FishingCatalog.Spots[caught.spot]+" · "+FishingCatalog.Baits[caught.bait],25,Muted,219,153,770,58);
                    var click=card.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>ShowFishCatch(caught));y+=255;
                }
                Label(parent,"保留最近"+FishingCatalog.RecentLimit+"次明细，全部钓获与各品质总数持续记录。",25,Muted,44,y,990,90);return y+110;
            }
            int shown=0;
            for(int i=0;i<FishingCatalog.Species.Length;i++)
            {
                int species=i; var record=State.fishing.entries[i]; bool known=record.count>0; if(fishFilter==1 && !known) continue;
                var kind=FishingCatalog.Species[i];var card=Panel(parent,"Fish species "+i,Sand,32,y,1016,248);
                var icon=Picture(card,"fish",i*4+Math.Max(0,record.bestQuality),1,1,8,48,196,145);if(!known)icon.color=new Color(.27f,.32f,.27f,.48f);
                Label(card,kind.Name+(known?"":" · 未发现"),34,Sage,219,9,770,61);
                Label(card,known?"钓获 "+record.count+"尾 · 最佳品质 "+FishingCatalog.Qualities[record.bestQuality]:"品种稀有度 · "+FishingCatalog.Rarities[kind.Rarity],28,Ink,219,78,770,50);
                Label(card,known?"最长 "+FishLength(record.longestMm)+" · 最重 "+FishWeight(record.heaviestGrams):"钓点 · "+FishHabitats(i),26,Ink,219,135,770,60);
                Label(card,known?"首次发现于总钓获第"+record.firstSequence+"尾 · 查看品质":"偏好 · "+FishingCatalog.Baits[kind.Bait],24,Muted,219,199,770,40);
                var click=card.gameObject.AddComponent<Button>();click.transition=Selectable.Transition.None;click.onClick.AddListener(()=>ShowFishSpecies(species)); y+=272;shown++;
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
            var photo=Picture(sheetContent,"fish",species*4+Math.Max(0,entry.bestQuality),1,1,240,0,600,355);if(!known)photo.color=new Color(.27f,.32f,.27f,.48f);sheetY=367;
            Paragraph("品种稀有度 · "+FishingCatalog.Rarities[kind.Rarity]+"\n"+kind.Habit,120,30);
            Paragraph("栖息 · "+FishHabitats(species)+"\n偏好 · "+FishingCatalog.Baits[kind.Bait],130,28);
            Row("累计钓获",known?"首次发现于总钓获第"+entry.firstSequence+"尾":"尚未发现",-1,entry.count+"尾",null);
            Row("最长纪录",known?"全部品质合计比较":"等待记录",-1,known?FishLength(entry.longestMm):"—",null);
            Row("最重纪录",known?"全部品质合计比较":"等待记录",-1,known?FishWeight(entry.heaviestGrams):"—",null);
            for(int q=0;q<4;q++) Row(FishingCatalog.Qualities[q],q==entry.bestQuality?"当前最佳品质":"品质独立累计",-1,entry.qualityCounts[q]+"尾",null);
            int target=0;for(int i=0;i<3;i++)if((kind.Spots&(1<<i))!=0){target=i;break;}
            Row("林间钓点","自动等待、自动收鱼，离开时暂停",-1,"前往钓点",()=>ShowFishing(target),glyph:ForestSymbol.Fish);EndSheet();
        }
        void ShowFishCatch(FishCatch caught)
        {
            var kind=FishingCatalog.Species[caught.species];StartSheet("第"+caught.sequence+"尾 · "+kind.Name,"fish-catch");
            Picture(sheetContent,"fish",caught.species*4+caught.quality,1,1,240,0,600,355);sheetY=367;
            Paragraph("本次品质 · "+FishingCatalog.Qualities[caught.quality]+"\n品种稀有度 · "+FishingCatalog.Rarities[kind.Rarity],130,32);
            Row("长度","本条钓获的实际记录",-1,FishLength(caught.lengthMm),null);
            Row("重量","本条钓获的实际记录",-1,FishWeight(caught.weightGrams),null);
            Row("钓点",FishingCatalog.Spots[caught.spot],-1,"",null);
            Row("使用饵料",FishingCatalog.Baits[caught.bait],-1,"",null);
            Row("品种档案","最长、最重与全部品质统计",-1,"查看档案",()=>ShowFishSpecies(caught.species));EndSheet();
        }
    }
}
