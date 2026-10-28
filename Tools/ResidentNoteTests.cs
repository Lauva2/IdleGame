using System;
using System.Linq;
using System.Web.Script.Serialization;
using ForestShop;
using ForestShop.Core;

static class ResidentNoteTests
{
    static void Main()
    {
        var serializer = new JavaScriptSerializer();
        int reads = 0;
        for (int stories = 0; stories <= 5; stories++)
        foreach (int friendship in new[] { 0, 24, 25, 50, 100 })
        {
            var state = new GameState { stories = stories, dialogue = stories % 5 };
            for (int i = 0; i < 5; i++) state.friendship[i] = friendship;
            string before = serializer.Serialize(state);
            for (int i = -1; i <= 5; i++)
            {
                string note = ForestResidentNotes.Read(state, i);
                bool known = i >= 0 && i < 5 && i <= stories;
                if (ForestResidentNotes.Known(state, i) != known || (note != null) != known)
                    throw new Exception("Recognition gate differs");
                if (note != null && (note.Length > 100 || note.Count(c => c == '。' || c == '！' || c == '？') > 2))
                    throw new Exception("Greeting exceeds short conversation budget");
                if (serializer.Serialize(state) != before) throw new Exception("Greeting mutated state");
                reads++;
            }
        }
        if (ForestResidentNotes.Read(null, 0) != null) throw new Exception("Missing state accepted");
        for (int i = 0; i < 5; i++)
        {
            var s = new GameState { stories = i };
            string waiting = ForestResidentNotes.Read(s, i);
            s.stories++;
            string completed = ForestResidentNotes.Read(s, i);
            s.friendship[i] = 25;
            string familiar = ForestResidentNotes.Read(s, i);
            if (waiting == completed || completed == familiar || waiting == familiar)
                throw new Exception("Progress variants do not differ");
        }
        Console.WriteLine("PASS " + reads + " read-only gate/progress reads, null state and five distinct story/friendship variants");
    }
}
