using System;
using ForestShop.Core;

namespace ForestShop
{
    // Presentation-only suggestions. Never configures, advances, saves or awards.
    public sealed class ForestSupplyHint
    {
        public readonly string Title, Detail, Target, Action;
        public readonly int Index;
        public ForestSupplyHint(string title, string detail, string target, int index, string action)
        { Title=title; Detail=detail; Target=target; Index=index; Action=action; }
    }
    public static class ForestSupplyGuide
    {
        static ForestSupplyHint Hint(string title,string detail,string target,int index,string action)
            => new ForestSupplyHint(title,detail,target,index,action);

        public static ForestSupplyHint Read(Economy engine,int item,int count,bool delivery)
        {
            if(engine==null || item<0 || item>=Catalog.Items.Length || count<1) throw new ArgumentOutOfRangeException();
            var s=engine.State;string name=Catalog.Items[item];int machineId=Catalog.Machine[item];
            if(delivery && s.items[item]>=count)
                return Hint("交付"+name,"商品已备齐，前往委托板确认交付。","orders",0,"去交付");
            if(!delivery && s.items[item]>engine.Reserved(item))
            {
                int shelf=Array.FindIndex(s.shelves,f=>f.config==item);
                return Hint(shelf<0?"上架"+name:"等待"+name+"售出",shelf<0?"商品已入库，选择一个货架售卖。":"货架已安排，首售以实际成交为准。","shelf",Math.Max(0,shelf),"去货架");
            }
            if(!delivery && engine.Reserved(item)>0 && s.items[item]>0)
                return Hint(name+"已预留","这部分商品留给已接委托，额外库存才能自动售出。","orders",0,"看委托");
            if(machineId>=s.machines.Length)
                return Hint("修复附魔工作台","这份商品需要附魔工作台。","expansion",0,"看条件");
            if(!engine.ItemUnlocked(item))
                return Hint("查看"+name+"条件","先查看配方的现有解锁条件。","machine",machineId,"看配方");
            var m=s.machines[machineId];
            if(m.workItem==item && m.remaining>=0)
                return Hint(name+"制作中","剩余 "+Math.Ceiling(m.remaining)+" 秒 · 本轮原料已投入。","machine",machineId,"看加工");
            if(m.pending==item && m.workItem>=0 && m.remaining>=0)
                return Hint("等待切换"+name,"本轮结束后，按新配方继续加工。","machine",machineId,"看加工");
            for(int crop=0;crop<Catalog.Crops.Length;crop++)
                if(s.crops[crop]<Catalog.Materials[item][crop]) return Resource(engine,crop);
            return Hint("制作"+name,m.config==item?"原料已备好，设备会自动继续加工。":"原料已备好，在设备中选择这份配方。","machine",machineId,"去工坊");
        }

        public static ForestSupplyHint Resource(Economy engine,int crop)
        {
            if(engine==null || crop<0 || crop>=Catalog.Crops.Length)throw new ArgumentOutOfRangeException();
            var s=engine.State;string name=Catalog.Crops[crop];
            if(crop>=4)
            {
                int node=crop-4;
                if(!s.forest.started)return Hint("走进林径","森林探索在开张茶会后开放。","forest-entry",0,"看林径");
                if(!engine.Forest.CanVisit(node))return Hint("打开"+ForestRules.Zones[node],"先查看通往这片林区的修复条件。","forest-repair",node==1?1:2,"看条件");
                if(crop==6 || !engine.Forest.Discovered(node))
                {
                    string detail=s.forest.regrow[node]>0?"林中资源恢复中 · 剩余 "+Math.Ceiling(s.forest.regrow[node])+" 秒。":ForestRules.Zones[node]+" · 点选实物采集。";
                    return Hint("采集"+name,detail,"forage",node,"去采集");
                }
                if(!s.forest.greenhouse || s.forest.beds.Length==0)return Hint("修复林间温室","发现种苗后，可在温室持续培育。","forest-repair",0,"看温室");
                return Growing(s.forest.beds,crop,"nursery","温室",name);
            }
            if(!engine.CropUnlocked(crop))return Hint("解锁"+name,crop==1?"修复附魔工作台后可种植。":"完成邻居委托，积攒所需声望。",crop==1?"expansion":"orders",0,"看条件");
            return Growing(s.plots,crop,"plot","庭院",name);
        }
        static ForestSupplyHint Growing(Facility[] beds,int crop,string target,string area,string name)
        {
            int index=Array.FindIndex(beds,f=>f.workItem==crop && f.remaining>=0);
            if(index>=0)return Hint(name+"生长中","剩余 "+Math.Ceiling(beds[index].remaining)+" 秒 · 成熟后自动入库。",target,index,"去"+area);
            index=Array.FindIndex(beds,f=>f.pending==crop);
            if(index>=0)return Hint("等待改种"+name,"本轮结束后改种，当前作物继续生长。",target,index,"去"+area);
            index=Array.FindIndex(beds,f=>f.config==crop);
            if(index>=0)return Hint("查看"+name,"已安排种植，查看当前状态与库存。",target,index,"去"+area);
            index=Array.FindIndex(beds,f=>f.config<0);
            return Hint("种植"+name,"在"+area+"选择种苗，成熟后自动补充原料。",target,Math.Max(0,index),"去"+area);
        }
    }
}
