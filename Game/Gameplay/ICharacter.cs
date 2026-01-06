using System.Collections.Generic;
using SINEATER.Game.CoreUtils;

namespace SINEATER.Game.Gameplay;

public interface ICharacter
{
    public int X { get; set; }
    public int Y { get; set; }
    public Track Guard { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; }
    public List<string> Tags { get; set; }
    public void Equip(EStat stat, Item? item);
    public void Equip(Item item);
    public Item? GetItem(EStat stat);
    
    public string GetName();
    (int, int) GetPortait();
}
