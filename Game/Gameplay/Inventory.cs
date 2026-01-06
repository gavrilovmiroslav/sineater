using System.Collections.Generic;

namespace SINEATER.Game.Gameplay;

public class Inventory
{
    public List<Item> Items = new();

    public Item? GetItem(int ID)
    {
        return Items.Find(i => i.ID == ID);
    }
}