using System;
using ImGuiNET;

namespace SINEATER.ImGuiTools
{
    public static class Tools
    {
        public static IScreen? DebugScreen;

        public static T? MakeEditor<T>(T instance)
        {
            foreach (var field in typeof(T).GetProperties())
            {
                if (field.PropertyType == typeof(string))
                {
                    string value = field.GetValue(instance)?.ToString() ?? "";
                    if (ImGui.InputText(field.Name, ref value, 100))
                    {
                        field.SetValue(instance, value);
                        return instance;
                    }
                }
                else if (field.PropertyType == typeof(int))
                {
                    if (field.GetValue(instance) is int i)
                    {
                        if (ImGui.InputInt(field.Name, ref i, 1))
                        {
                            field.SetValue(instance, i);
                            return instance;
                        }
                    }
                }
            }
            ImGui.Separator();

            return default(T);
        }
        
        public static void ShowTools(ref bool isOpen)
        {
            ImGui.Begin("Tools", ref isOpen);

            if (ImGui.BeginTabBar("#ToolsTabBar", ImGuiTabBarFlags.Reorderable))
            {
                if (ImGui.BeginTabItem("World"))
                {
                    if (DebugScreen is WorldMapScreen w)
                    {
                        var changed = false;
                        var (x, y) = w.CurrentPlayerPosition;
                        ImGui.Text($"Current Tile: {x}, {y}");
                        if (!w.World.Encounters.Has(x, y))
                        {
                            if (ImGui.Button("Add Introduction"))
                            {
                                w.World.Introduction.Add((x, y), new Introduction());
                            }
                        }
                        if (!w.World.Encounters.Has(x, y))
                        {
                            if (ImGui.Button("Add Encounter"))
                            {
                                w.World.Encounters.Add((x, y), new Encounter());
                            }
                        }
                        
                        
                        if (w.World.Encounters.Has(x, y))
                        {
                            if (MakeEditor<Encounter>(w.World.Encounters.Get(x, y)) is { } e)
                            {
                                w.World.Encounters.Set(x, y, e);
                            }
                        }
                        
                        if (w.World.Introduction.Has(x, y))
                        {
                            if (MakeEditor<Introduction>(w.World.Introduction.Get(x, y)) is { } i)
                            {
                                w.World.Introduction.Set(x, y, i);
                            }
                        }
                    }
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Ambient"))
                {
                    Ambient.ImguiEditor();
                    ImGui.EndTabItem();
                }
                
                ImGui.EndTabBar();
            }

            ImGui.End();
        }
    }
}