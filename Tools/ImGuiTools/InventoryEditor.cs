using ImGuiNET;
using SINEATER.Game;
using SINEATER.Game.Loadable;
using System.Linq;

namespace SINEATER.Tools.ImGuiTools
{
    public static class InventoryEditor
    {
        static int selectedItemLibrary = 0;
        static int selectedItemInventory = 0;

        static string[] _itemsLibrary = [];
        static string[] _itemsInventory = [];
        public static void ImguiEditor()
        {
            if (_itemsLibrary.Length == 0)
            {
                _itemsLibrary = Items.Instance.EnumerateItems().ToArray();
                RefreshInvenotory();
            }

            if (ImGui.CollapsingHeader("Item Library"))
            {
                ImGui.ListBox("", ref selectedItemLibrary, _itemsLibrary, _itemsLibrary.Length, _itemsLibrary.Length);

                ImGui.SameLine();

                if (ImGui.Button("Add item"))
                {
                    var selected = _itemsLibrary[selectedItemLibrary];
                    SineaterGame.Instance.Party.Inventory.Items.Add(Items.Instance.Make(selected));
                    RefreshInvenotory();
                }

                ImGui.ListBox("##FruitList", ref selectedItemInventory, _itemsInventory, _itemsInventory.Length, _itemsInventory.Length);
            }
        }

        private static void RefreshInvenotory()
        {
            _itemsInventory = SineaterGame.Instance.Party.Inventory.Items.Where(i => i != null).Select(i => i.Name).ToArray();
        }
    }
}
