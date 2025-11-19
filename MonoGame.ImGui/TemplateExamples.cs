using ImGuiNET;

namespace Monogame.ImGuiExamples
{
    public static class TemplateExamples
    {
        private static bool _secondWindowOpened = false;
        public static void Example1()
        {

            ImGui.Text("Hello!");

            string _input = "";
            ImGui.InputText("Input", ref _input, 128);

            if (ImGui.BeginTabBar("#TabBar"))
            {
                if (ImGui.BeginTabItem("Fist"))
                {
                    ImGui.Text("First tab content");
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Second"))
                {
                    if (ImGui.Button("Second tab content"))
                    {
                        _secondWindowOpened = !_secondWindowOpened;
                    }
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            if (_secondWindowOpened)
            {
                ImGui.Begin("Second window", ref _secondWindowOpened);

                ImGui.Text("This is second window");

                ImGui.End();
            }
        }
    }
}
