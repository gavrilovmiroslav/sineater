using ImGuiNET;
using SINEATER.Game;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using SINEATER.Game.Save;

namespace SINEATER.Tools.ImGuiTools
{
    public static class SaveEditor
    {
        private static Party Party => SineaterGame.Instance.Party;
        public static void ImguiEditor()
        {
            if (ImGui.Button("Save"))
            {
                SaveSystem.Save();
            }

            if (ImGui.Button("New save"))
            {
                SaveSystem.Save();
            }

            if (ImGui.Button("Reload"))
            {
                Enemies.Instance.Load();
                Items.Instance.Load();
                SaveSystem.Load();
            }

            //if (ImGui.Button("Save"))
            //{
            //    SaveSystem.Save();
            //}
            //else if (ImGui.Button("Load"))
            //{
            //    SaveSystem.Load();
            //}

            //if (ImGui.CollapsingHeader("Edit"))
            //{
            //    int x = Party.CurrentPlayerPosition.X;
            //    ImGui.InputInt("X", ref x);


            //    int y = Party.CurrentPlayerPosition.Y;
            //    ImGui.InputInt("Y", ref y);

            //    Party.CurrentPlayerPosition = (x, y);
            //}
        }
    }
}
