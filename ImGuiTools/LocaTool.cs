using ImGuiNET;
using SINEATER.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core.Tokens;

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
