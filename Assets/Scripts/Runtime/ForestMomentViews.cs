using ForestShop.Core;
using UnityEngine;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        void BuildMomentMarker(Transform view,int id,ForestWorld3D world)
        {
            if(!Engine.Moments.Available(id))return;
            var button=IconButton(view,"线索",ForestSymbol.Collection,0,0,182,72,()=>TapMoment(id,State.moments.stages[id]==0?0:1));
            button.name="Moment marker "+id;
            var marker=button.gameObject.AddComponent<ForestExploreMarker>();marker.world=world;marker.index=id+10;
            var label=button.GetComponentInChildren<UnityEngine.UI.Text>();var icon=button.GetComponentInChildren<ForestSymbolGraphic>();
            bindings.Add(()=>{int stage=State.moments.stages[id];label.text=stage==0?"线索":stage==1?(id==2?"翻阅":"拾起"):stage==2?"留下":"留念";icon.Symbol=stage==3?ForestSymbol.Check:ForestSymbol.Collection;});
        }
        void TapMoment(int id,int clue)
        {
            if(!Engine.Moments.Here(id)){Toast("沿原来的林径，前往这处小事。");return;}
            int stage=State.moments.stages[id];
            if(stage<2)
            {
                if(!Engine.Moments.Observe(id,clue))return;
                Save();World().Bind(State);JourneySteps(true);Debug.Log("FOREST_MOMENT observed "+id+" "+clue);
                if(stage==0)Toast(ForestMomentRules.ClueHints[id],3.5f);
            }
            if(State.moments.stages[id]>=2)ShowMomentChoice(id);
        }
        void GoMoment(int id)
        {
            if(!Engine.Moments.Available(id)){Toast(Engine.Moments.Status(id));return;}
            GoForest(id);World().Bind(State);World().FocusMoment(id);
        }
        void ShowMomentChoice(int id)
        {
            StartSheet(ForestMomentRules.Titles[id],"moment-choice",660);
            Label(sheetContent,ForestMomentRules.Questions[id],29,Ink,36,0,1008,78);sheetY=92;
            for(int c=0;c<2;c++)
            {
                int choice=c;bool selected=State.moments.choices[id]==c;
                var card=Panel(sheetContent,"Moment arrangement "+c,Sand,28+c*520,sheetY,504,342);
                Picture(card,"moment",id*2+c,1,1,38,4,428,198);
                Label(card,ForestMomentRules.Choices[id][c],31,Sage,20,206,464,50,UnityEngine.TextAnchor.MiddleCenter);
                IconButton(card,selected?"已摆放":"放在这里",selected?ForestSymbol.Check:ForestSymbol.Decoration,28,264,448,74,()=>ChooseMoment(id,choice),!selected,selected).name="Moment choose "+id+" "+c;
            }
            sheetY+=356;
            Label(sheetContent,State.moments.stages[id]==3?"小事已记录 · 摆放可随时更换":"选喜欢的摆放 · 不消耗物品",25,Muted,35,sheetY,1010,44,UnityEngine.TextAnchor.MiddleCenter);sheetY+=52;
            EndSheet();
        }
        void ChooseMoment(int id,int choice)
        {
            bool first=State.moments.stages[id]<3;
            if(!Engine.Moments.Choose(id,choice))return;
            Save();CloseSheet();World().Bind(State);if(first)World().PresentMoment(id);JourneySteps(true);
            Toast(first?"一件林间小事，留在了这里。":"摆放已更换，回忆还在。",3);audioController?.Cue("confirm");
            Debug.Log("FOREST_MOMENT chosen "+id+" "+choice+" "+first);
        }
        void ShowMomentJournal()
        {
            StartSheet("林间小事","moment-journal");
            Label(sheetContent,"留下的回忆  "+Engine.Moments.Completed+" / 3",34,Sage,35,0,1010,64);sheetY=80;
            for(int id=0;id<3;id++)
            {
                int item=id,stage=State.moments.stages[id];bool available=Engine.Moments.Available(id);
                var card=Panel(sheetContent,"Moment journal "+id,Sand,28,sheetY,1024,196);
                if(stage==3)Picture(card,"moment",id*2+State.moments.choices[id],1,1,12,6,198,176);
                else Symbol(card,available?ForestSymbol.Forest:ForestSymbol.Lock,48,46,116);
                Label(card,ForestMomentRules.Titles[id],34,Sage,228,8,744,54);
                Label(card,ForestRules.Zones[id],24,Muted,228,60,744,42);
                Label(card,Engine.Moments.Status(id),25,Ink,228,106,494,68);
                IconButton(card,stage==3?"看留念":"前往",ForestSymbol.Arrow,764,108,230,74,()=>GoMoment(item),available).name="Moment journal go "+id;
                for(int step=0;step<3;step++)Symbol(card,stage>step?ForestSymbol.Check:ForestSymbol.Empty,228+step*48,164,28);
                sheetY+=212;
            }
            Label(sheetContent,"回忆只需观察与选择，沿已开放的林径慢慢发现。",25,Muted,36,sheetY,1000,84);sheetY+=92;EndSheet();
        }
        JourneyStep NextMomentStep()
        {
            for(int i=0;i<3;i++)if(Engine.Moments.Available(i) && State.moments.stages[i]<3)
                return new JourneyStep(ForestMomentRules.Titles[i],Engine.Moments.Status(i),"林间小事 "+Engine.Moments.Completed+"/3","moment",i,ForestSymbol.Collection);
            return null;
        }
    }
}
