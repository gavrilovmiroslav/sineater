using ImGuiNET;

namespace SINEATER.ImGuiTools
{
    public static class Tools
    {
        public static void ShowTools(ref bool isOpen)
        {
            ImGui.Begin("Tools", ref isOpen);

            if (ImGui.BeginTabBar("#ToolsTabBar", ImGuiTabBarFlags.Reorderable))
            {
                if (ImGui.BeginTabItem("Ambient"))
                {
                    Ambient.ImguiEditor();

                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Empty"))
                {
                    ImGui.Text("EMPTY!");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }
}