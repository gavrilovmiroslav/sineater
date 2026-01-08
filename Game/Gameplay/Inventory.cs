using System.Collections.Generic;

namespace SINEATER.Game.Gameplay;

public class Inventory
{
    public readonly List<Item> Items = [];

    public Item? GetItem(string name)
    {
        return Items.Find(i => i.Name == name);
    }
}