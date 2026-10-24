using System;
using System.IO;
using System.Linq;
using System.Text;
using ForestShop.Core;
using UnityEditor;
using UnityEngine;

namespace ForestShop.Editor
{
    public static class FishingCodecCheck
    {
        public static void Run()
        {
            const string output="Docs/Verification/3d-d21-fishing-codec-r01";Directory.CreateDirectory(output);var log=new StringBuilder();
            var state=JsonUtility.FromJson<GameState>(File.ReadAllLines("Docs/Verification/3d-d21-forest04/ordinary-upgrade-report.save").Last());StateValidation.Check(state);
            var engine=new Economy(state);state.report.pending=false;engine.Forest.Visit(0);var codec=new UnityStateCodec();
            for(int phase=0;phase<4;phase++)
            {
                if(phase==1)engine.Fishing.Start(0,0);
                if(phase==2)engine.Fishing.Advance(state.fishing.remaining);
                if(phase==3)engine.Fishing.Advance(1.8);
                if(state.fishing.phase!=phase)throw new Exception("Phase setup");
                var copy=codec.Decode(codec.Encode(state));StateValidation.Check(copy);
                if(JsonUtility.ToJson(copy.fishing)!=JsonUtility.ToJson(state.fishing) || copy.coins!=state.coins || !copy.items.SequenceEqual(state.items))throw new Exception("Actual Unity codec roundtrip changed fishing/counters phase "+phase);
                var store=new SaveStore("Builds/Previews/fishing-codec-r01/phase"+phase,codec);store.Save(state);var loaded=store.Load();
                if(JsonUtility.ToJson(loaded.fishing)!=JsonUtility.ToJson(state.fishing) || loaded.coins!=state.coins || !loaded.items.SequenceEqual(state.items) || store.RecoveryMessage!=null)throw new Exception("Actual Unity save reload changed fishing/counters phase "+phase);
                log.AppendLine("PASS actual Unity JsonUtility and SaveStore roundtrip phase "+phase+"; catches="+state.fishing.totalCaught);
            }
            File.WriteAllText(output+"/checks.txt",log.ToString());Debug.Log("FISHING_UNITY_CODEC_OK\n"+log);
        }
    }
}
