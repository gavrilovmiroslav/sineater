using ImGuiNET;

namespace ImGuiTools
{
    public static class Tools
    {

        public static void ShowTools()
        {
            ImGuiNET.ImGui.Begin("Tools");
            ImGuiNET.ImGui.SetWindowSize(new System.Numerics.Vector2(1000.0f, 500.0f));

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
