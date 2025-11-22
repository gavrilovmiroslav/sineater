using ImGuiNET;

namespace ImGuiTools
{
    public static class Tools
    {

        public static void ShowTools()
        {
            ImGui.Begin("Tools");

            if (ImGui.BeginTabBar("#ToolsTabBar"))
            {
                if (ImGui.BeginTabItem("Localization"))
                {
                    LocaTool.Show();

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Second"))
                {
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }
}
