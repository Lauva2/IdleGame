using System;
using ForestShop.Core;
using UnityEngine.UI;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        ForestSymbol SupplyIcon(string target) => target=="plot"?ForestSymbol.Garden:target=="nursery" || target=="greenhouse"?ForestSymbol.Greenhouse:target=="forage"?ForestSymbol.Harvest:target=="shelf"?ForestSymbol.Shelf:target=="orders"?ForestSymbol.Orders:target=="expansion" || target=="forest-repair"?ForestSymbol.Expand:target=="forest-entry"?ForestSymbol.Forest:ForestSymbol.Workshop;
        JourneyStep SupplyStep(int item,int count,bool delivery)
        {
            var hint=ForestSupplyGuide.Read(Engine,item,count,delivery);
            return new JourneyStep(hint.Title,hint.Detail,Catalog.Items[item]+" "+State.items[item]+"/"+count+(delivery && Engine.Reserved(item)>0?" · 已预留":""),hint.Target,hint.Index,SupplyIcon(hint.Target));
        }
        void GoSupply(ForestSupplyHint hint) => GoJourney(new JourneyStep(hint.Title,hint.Detail,"",hint.Target,hint.Index,SupplyIcon(hint.Target)));
        void ShowSupplyGuide(int item)
        {
            var order=Engine.ActiveOrder;
            bool delivery=order!=null && order.item==item;
            int count=delivery?order.count:1;
            StartSheet(Catalog.Items[item]+" · 备货路线","supply");
            ForestSymbol[] icons={ForestSymbol.Harvest,ForestSymbol.Workshop,delivery?ForestSymbol.Orders:ForestSymbol.Shelf};
            string[] names={"原料", "制作",delivery?"交付":"售出"};
            for(int i=0;i<3;i++)
            {
                float x=38+i*350;Symbol(sheetContent,icons[i],x,sheetY+4,60);
                Label(sheetContent,names[i],29,Sage,x+80,sheetY,200,68);
                if(i<2)Symbol(sheetContent,ForestSymbol.Arrow,x+272,sheetY+18,38);
            }
            sheetY+=90;
            var hint=ForestSupplyGuide.Read(Engine,item,count,delivery);
            Row(hint.Title,hint.Detail,ItemIcon(item),hint.Action,()=>GoSupply(ForestSupplyGuide.Read(Engine,item,count,delivery)),true,null,
                ()=>ForestSupplyGuide.Read(Engine,item,count,delivery).Detail);
            var nextRow=sheetContent.GetChild(sheetContent.childCount-1);
            var nextTitle=nextRow.GetComponentsInChildren<Text>()[0];
            var nextAction=nextRow.GetComponentInChildren<Button>().GetComponentInChildren<Text>();
            modalBindings.Add(()=>{var current=ForestSupplyGuide.Read(Engine,item,count,delivery);nextTitle.text=current.Title;nextAction.text=current.Action;});
            for(int crop=0;crop<Catalog.Crops.Length;crop++)
            {
                int ingredient=crop,need=Catalog.Materials[item][crop];if(need==0)continue;
                var source=ForestSupplyGuide.Resource(Engine,crop);
                Row(Catalog.Crops[crop],"",CropIcon(crop),source.Action,()=>GoSupply(ForestSupplyGuide.Resource(Engine,ingredient)),true,null,
                    ()=>"库存 "+State.crops[ingredient]+" · 每份需要 "+need);
            }
            Row("制作与去向","",ItemIcon(item),"看配方",()=>GoSupply(new ForestSupplyHint("","","machine",Catalog.Machine[item],"")),true,null,
                ()=>"商品 "+State.items[item]+(delivery?"/"+count+" · 委托预留 "+Engine.Reserved(item):" · 已售 "+State.sold[item]));
            EndSheet();
        }
    }
}
