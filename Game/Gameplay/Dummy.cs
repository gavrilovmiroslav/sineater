using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SINEATER.Game.CoreUtils;

namespace SINEATER.Game.Gameplay;

public class Dummy : Character
{
    public int X { get; set; }
    public int Y { get; set; }
    public Track Hits { get; set; }
    public Track Guard { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; } = new(0, 0, 0, 0);
    public List<string> Tags { get; set; } = [];

    public Color GetTint()
    {
        return Color.White;
    }

    public void Equip(EStat stat, Item? item)
    {
    }

    public void Equip(Item item)
    {
    }

    public Item? GetItem(EStat stat)
    {
        return null;
    }

    public void EquipLeftWeapon(Weapon? weapon)
    {
    }

    public Weapon? GetLeftWeapon()
    {
        return null;
    }

    public void EquipRightWeapon(Weapon? weapon)
    {
    }

    public Weapon? GetRightWeapon()
    {
        return null;
    }

    public void EquipItem(Item? item)
    {
    }

    public Item? GetItem()
    {
        return null;
    }
    
    public bool IsStunned()
    {
        return false;
    }

    public string GetName()
    {
        return "";
    }

    public (int, int) GetPortait()
    {
        return (0, 0);
    }

    public void Die()
    {
    }

    public void RemoveArmor()
    {
    }

    public bool IsDone { get; set; } = false;
}
