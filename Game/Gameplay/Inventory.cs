using System.Collections.Generic;

namespace SINEATER.Game.Gameplay;

public class Inventory
{
    public List<HItem> Items = new();

    public HItem? GetItem(int ID)
    {
        return Items.Find(i => i.ID == ID);
    }
}