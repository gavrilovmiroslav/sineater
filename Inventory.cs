using System.Collections.Generic;

namespace SINEATER;

public class Inventory
{
    public List<Item> Items = new();

    public Item? GetItem(string name)
    {
        return Items.Find(i => i.Name == name);
    }
}