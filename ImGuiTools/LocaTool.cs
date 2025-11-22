using ImGuiNET;
using SINEATER.Localization;
using SINEATER.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace ImGuiTools
{
    public class LocaTool
    {
        static List<LocaIDs> locaIDs = new();
        static bool IsInit = false;

        public static void Show()
        {
            if (!IsInit)
            {
                foreach (var item in Enum.GetValues(typeof(LocaIDs)))
                {
                    locaIDs.Add((LocaIDs)item);
                }
                IsInit = true;
            }


            if(ImGui.Button("Save Loca"))
            {
                var dir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
                Loca.Save($"{dir}/Content/loca/english.json");
                Loca.Save("Content/loca/english.json");
            }

            foreach (var x in locaIDs)
            {
                ImGui.PushID((int)x);
                ImGui.Text(x.ToString());
                string s = Loca.GetString(x);
                ImGui.SameLine();
                if (ImGui.InputText($"##{x}", ref s, 256))
                {
                    Loca.Setstring(x, s);
                }

                ImGui.PopID();
            }
        }
    }
}
