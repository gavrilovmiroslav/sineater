namespace MonoGame.ImGui
{
    public static class TemplateExamples
    {
        private static bool _secondWindowOpened = false;
        public static void AtmosphereEditor()
        {
            ImGuiNET.ImGui.Text("Hello!");

            string _input = "";
            ImGuiNET.ImGui.InputText("Input", ref _input, 128);

            if (ImGuiNET.ImGui.BeginTabBar("#TabBar"))
            {
                if (ImGuiNET.ImGui.BeginTabItem("Fist"))
                {
                    ImGuiNET.ImGui.Text("First tab content");
                    ImGuiNET.ImGui.EndTabItem();
                }

                if (ImGuiNET.ImGui.BeginTabItem("Second"))
                {
                    if (ImGuiNET.ImGui.Button("Second tab content"))
                    {
                        _secondWindowOpened = !_secondWindowOpened;
                    }
                    ImGuiNET.ImGui.EndTabItem();
                }
                ImGuiNET.ImGui.EndTabBar();
            }

            if (_secondWindowOpened)
            {
                ImGuiNET.ImGui.Begin("Second window", ref _secondWindowOpened);

                ImGuiNET.ImGui.Text("This is second window");

                ImGuiNET.ImGui.End();
            }
        }
    }
}
