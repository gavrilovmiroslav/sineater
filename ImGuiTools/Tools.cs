using ImGuiNET;

namespace SINEATER.ImGuiTools
{
    public static class Tools
    {
        public static IScreen? DebugScreen;
        
        public static void ShowTools(ref bool isOpen)
        {
            ImGui.Begin("Tools", ref isOpen);

            if (ImGui.BeginTabBar("#ToolsTabBar", ImGuiTabBarFlags.Reorderable))
            {
                if (ImGui.BeginTabItem("World"))
                {
                    // if (DebugScreen is WorldMapScreen w)
                    // {
                    //     var changed = false;
                    //     var (x, y) = w.CurrentPlayerPosition;
                    //     ImGui.Text($"Current Tile: {x}, {y}");
                    //     if (!w.Entities[x, y].Has<Dungeon>())
                    //     {
                    //         if (ImGui.Button("Add Dungeon"))
                    //         {
                    //             w.Entities[x, y].Add<Dungeon>();
                    //             changed = true;
                    //         }
                    //     }
                    //
                    //     ImGui.Separator();
                    //     foreach (var comp in w.Entities[x, y].GetAllComponents())
                    //     {
                    //         if (comp is Dungeon dung)
                    //         {
                    //             if (ImGui.TreeNode("Dungeon"))
                    //             {
                    //                 ImGui.TreePop();
                    //             }
                    //         }
                    //     }
                    //     
                    //     if (changed)
                    //     {
                    //         var arch = new ArchJsonSerializer();
                    //         var worldPath =
                    //             System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                    //                 $"Content\\world.dat");
                    //         var json = arch.ToJson(w.World);
                    //         File.WriteAllText(worldPath, json);
                    //     }
                    // }
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