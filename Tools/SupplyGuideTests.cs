using System;
using System.Linq;
using System.Web.Script.Serialization;
using ForestShop;
using ForestShop.Core;

static class SupplyGuideTests
{
    static int checks;
    static readonly JavaScriptSerializer Json=new JavaScriptSerializer();
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static Economy Setup()
    {
        var s=new GameState{tutorial=4,stories=5,reputation=150,sideRoom=true,chapterComplete=true,warehouse=4};
        s.machines=new[]{new Facility(),new Facility()};
        s.plots=Enumerable.Range(0,4).Select(i=>new Facility{config=i}).ToArray();
        s.forest=new ForestProgress{started=true,greenhouse=true,bridge=true,vines=true,pavilion=true,discovered=7,visited=7,
            beds=new[]{new Facility{config=4},new Facility{config=5},new Facility(),new Facility()}};
        return new Economy(s);
    }
    static ForestSupplyHint Read(Economy e,int item,int count,bool delivery)
    {
        string before=Json.Serialize(e.State);var h=ForestSupplyGuide.Read(e,item,count,delivery);
        Check(Json.Serialize(e.State)==before,"Suggestion mutated saved state");
        Check(h.Title.Length<28 && h.Detail.Length<70 && h.Action.Length<=5,"Short hint budget");return h;
    }
    static void Main()
    {
        for(int item=0;item<Catalog.Items.Length;item++)
        {
            var e=Setup();var s=e.State;var m=s.machines[Catalog.Machine[item]];
            s.items[item]=3;Check(Read(e,item,3,true).Target=="orders","Ready order not sent to delivery");
            Check(Read(e,item,1,false).Target=="shelf","Stock must go to actual shelf");
            s.orders=new[]{new Order{id=1,item=item,count=3,active=true,reserve=true}};
            Check(Read(e,item,1,false).Target=="orders","Reserved stock presented as saleable");
            s.items[item]=0;m.config=item;m.workItem=item;m.remaining=12;
            Check(Read(e,item,3,true).Title.Contains("制作中"),"Committed materials mistaken for a shortage");
            m.workItem=(item+1)%9;m.pending=item;
            Check(Read(e,item,3,true).Title.Contains("等待切换"),"Pending recipe presented as current work");
            m.workItem=-1;m.remaining=-1;m.pending=-2;
            int missing=Array.FindIndex(Catalog.Materials[item],v=>v>0);
            var h=Read(e,item,3,true);Check(h.Target==(missing<4?"plot":missing<6?"nursery":"forage"),"Missing resource destination differs");
            for(int c=0;c<7;c++)s.crops[c]=Catalog.Materials[item][c];
            Check(Read(e,item,3,true).Target=="machine","Stocked ingredients must go to recipe");
        }
        var edge=Setup();edge.State.plots[0]=new Facility{config=0,workItem=0,remaining=8,pending=3};
        edge.State.machines[0].config=3;edge.State.crops[0]=1;
        Check(Read(edge,3,6,true).Title.Contains("等待改种"),"Queued crop lost");
        edge.State.plots[2]=new Facility{config=3,workItem=3,remaining=4};
        Check(Read(edge,3,6,true).Index==2,"Growing crop should be preferred to pending crop");
        edge.State.forest.regrow[2]=42;
        var resin=ForestSupplyGuide.Resource(edge,6);Check(resin.Target=="forage" && resin.Detail.Contains("42"),"Resource cooldown missing");
        edge.State.forest.vines=false;Check(ForestSupplyGuide.Resource(edge,6).Target=="forest-repair","Locked forest shortcut");
        edge.State.forest.started=false;Check(ForestSupplyGuide.Resource(edge,4).Target=="forest-entry","Unstarted forest shortcut");
        var early=new Economy(new GameState{tutorial=4});
        Check(Read(early,1,4,true).Target=="expansion","Missing workbench must not index machine1");
        Console.WriteLine("PASS "+checks+" supply route/read-only checks across all nine recipes, reservations, committed inputs, pending switches and locked routes");
    }
}
