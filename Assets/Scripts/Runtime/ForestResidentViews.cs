using ForestShop.Core;
using UnityEngine;

namespace ForestShop
{
    public sealed partial class ForestApp
    {
        void ShowResidentNote(int resident)
        {
            string note = ForestResidentNotes.Read(State, resident);
            if (note == null) return;
            StartSheet(Catalog.Residents[resident] + " · 近况", "resident-note", 820);
            var portrait = Picture(sheetContent, "residents", resident, 5, 2, 435, 0, 200, 210);
            portrait.name = "Resident note portrait " + resident;
            sheetY = 228;
            Paragraph(note, 160, 34);
            Row("林间的一声问候", "", -1, "继续营业", CloseSheet);
            EndSheet();
            world3D?.GreetResident(resident);
        }

        void ShowResidentNotes()
        {
            StartSheet("邻里的近况", "resident-notes", 940);
            for (int i = 0; i < Catalog.Residents.Length; i++)
            {
                if (!ForestResidentNotes.Known(State, i)) continue;
                int resident = i;
                Row(Catalog.Residents[i], "聊一句林间的日常", -1, "问候", () => ShowResidentNote(resident));
            }
            EndSheet();
        }
    }
}
