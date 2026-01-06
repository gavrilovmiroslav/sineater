using System.Collections.Generic;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Screens;

namespace SINEATER.Game.Gameplay;

public abstract class Character : ICharacter
{
    public static Character Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    public List<string> Tags { get; set; } = [];
    public bool HasTurn { get; set; } = true;
    public string? SelectedMove { get; set; } = null;
    public bool Broken { get; set; } = false;
    public bool Acted { get; set; } = false;

    public bool CheckBroken()
    {
        if (Guard == 0)
        {
            Broken = true;
            return true;
        }

        return false;
    }
    
    public void ForceRestart(Screen screen)
    {
        HasTurn = true;
        SelectedMove = null;
    }
    
    public float Weight
    {
        get
        {
            var weight = 0.0f;

            foreach (var item in Items)
            {
                if (item != null)
                {
                    weight += (int)item.Weight;
                }
            }

            return weight;
        }
    }
    
    public void Equip(Item? item)
    {
        if (item != null)
            Equip(item.Stat, item);
    }

    public Item? GetItem(EStat stat)
    {
        return Items[(int)stat - 1];
    }
    
    public Stats Bonus { get; set; } = new(0, 0, 0, 0);
    public Stats Temp  { get; set; } = new(0, 0, 0, 0);
    
    public int Wil => Stats.Will + Bonus.Will + Temp.Will;
    public int Cla => Stats.Clarity + Bonus.Clarity + Temp.Clarity;
    public int Poi => Stats.Poise + Bonus.Poise + Temp.Poise;
    public int Vig => Stats.Vigor + Bonus.Vigor + Temp.Vigor;
    
    public ECharacterClass Job;
    public Item?[] Items = new Item?[4];
    
    public void Equip(EStat stat, Item? item)
    {
        if (stat == EStat.None) return;

        Items[(int)stat - 1] = item;
    }

    public void EquipAndAdd(EStat stat, Item? item)
    {
        if (stat == EStat.None) return;

        if (this is PartyMember pm && item != null)
        {
            SineaterGame.Instance.Party.Inventory.Items.Add(item);
        }

        Items[(int)stat - 1] = item;
    }

    public void EquipAndAdd(Item? item)
    {
        Equip(item);
        if (this is PartyMember pm && item != null)
        {
            SineaterGame.Instance.Party.Inventory.Items.Add(item);
        }
    }

    public virtual int X { get; set; }
    public virtual int Y { get; set; }
    public Track Guard { get; set; }
    public bool Render { get; set; } = true;
    
    public Stats Stats { get; set; } = new();
    
    public virtual string GetName()
    {
        return Job.ToString();
    }
    
    public virtual (int, int) GetPortait()
    {
        return Job.GetPortrait();
    }
}
