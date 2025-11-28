using System;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using ImGuiNET;

namespace SINEATER.ImGuiTools
{
    public static class Tools
    {
        public static IScreen? DebugScreen;

        public static (T Editor, bool Changed, bool Deleted) MakeEditor<T>(T instance, string name) where T: struct
        {
            var changed = false;
            object boxed = instance;

            if (ImGui.CollapsingHeader($"{instance.GetType().Name}##HEADER{name}{instance.GetType().Name}"))
            {
                if (ImGui.Button($"Delete##DEL{name}{instance.GetType().Name}"))
                {
                    return (instance, true, true);
                }

                ImGui.Separator();
                foreach (var prop in typeof(T).GetProperties())
                {
                    var field = $"{prop.Name}##{name}{instance.GetType().Name}{prop.Name}";
                    if (prop.PropertyType == typeof(string) &&
                        prop.GetCustomAttributes(true).Any(t => t is LargeTextAttribute))
                    {
                        field = $"##{name}{instance.GetType().Name}{prop.Name}";
                        var value = prop.GetValue(instance)?.ToString() ?? "";
                        ImGui.Text($"{prop.Name}:");
                        if (ImGui.InputTextMultiline(field, ref value, 1024,
                                new Vector2(ImGui.GetWindowWidth() - 30, 8 * ImGui.GetTextLineHeight())))
                        {
                            prop.SetValue(boxed, value);
                            changed = true;
                        }
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        var value = prop.GetValue(instance)?.ToString() ?? "";
                        if (ImGui.InputText(field, ref value, 100))
                        {
                            prop.SetValue(boxed, value);
                            changed = true;
                        }
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        if (prop.GetValue(instance) is int i)
                        {
                            if (ImGui.InputInt(field, ref i, 1))
                            {
                                prop.SetValue(boxed, i);
                                changed = true;
                            }
                        }
                    }
                    else if (prop.PropertyType.IsEnum)
                    {
                        var values = prop.PropertyType.GetEnumNames();
                        var value = prop.GetValue(instance);
                        var index = (int)value;
                        if (index < 0) index = 0;
                        ImGui.Combo(field, ref index, values, values.Length);
                        prop.SetValue(boxed, (prop.PropertyType.GetEnumValues().GetValue(index)));
                        changed = true;
                    }
                }
            }

            instance = (T)boxed;
            return (instance, changed, false);
        }

        private static void MakeButtonFor<T>(ComponentStorage<T> ts, int x, int y) where T: struct, IWorldComponent
        {
            if (!ts.Has(x, y))
            {
                if (ImGui.Button($"Add {typeof(T).Name}"))
                {
                    ts.Add((x, y), new T());
                }
            }
            else
            {
                ImGui.BeginDisabled();
                ImGui.Button($"Add {typeof(T).Name}");
                ImGui.EndDisabled();
            }
        }
        
        private static bool MakeEditorFor<T>(ComponentStorage<T> ts, int x, int y) where T: struct, IWorldComponent
        {
            bool changed = false;
            if (ts.Has(x, y))
            {
                if (MakeEditor<T>(ts.Get(x, y), $"{x}{y}{typeof(T).Name}") is { } e)
                {
                    if (e.Deleted)
                    {
                        ts.Remove(x, y);
                    }
                    else
                    {
                        ts.Set(x, y, e.Editor);
                        changed |= e.Changed;
                    }
                }
            }
            return changed;
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
                        MakeButtonFor(w.World.GeneralDescriptions, x, y);
                        MakeButtonFor(w.World.SpecificDescriptions, x, y);
                        MakeButtonFor(w.World.Encounters, x, y);
                        MakeButtonFor(w.World.SlowDowns, x, y);
                        ImGui.Separator();
                        changed |= MakeEditorFor(w.World.GeneralDescriptions, x, y);
                        changed |= MakeEditorFor(w.World.SpecificDescriptions, x, y);
                        changed |= MakeEditorFor(w.World.Encounters, x, y);
                        changed |= MakeEditorFor(w.World.SlowDowns, x, y);
                        if (changed)
                        {
                            w.World.Save();
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