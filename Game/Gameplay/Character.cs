using System.Collections.Generic;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Screens;

namespace SINEATER.Game.Gameplay;

public abstract class Character
{
    public static Character Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    public List<string> Tags { get; set; } = [];
    public bool Broken { get; set; } = false;

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
    
    public void Equip(HItem? item)
    {
        if (item != null)
            Equip(item.Stat, item);
    }

    public HItem? GetItem(EStat stat)
    {
        return Items[(int)stat - 1];
    }

    public int Wil => Stats.Will;
    public int Cla => Stats.Clarity;
    public int Poi => Stats.Poise;
    public int Vig => Stats.Vigor;
    
    public ECharacterClass Job;
    public HItem?[] Items = new HItem?[4];
    
    public void Equip(EStat stat, HItem? item)
    {
        if (stat == EStat.None) return;

        Items[(int)stat - 1] = item;
    }

    public void EquipAndAdd(EStat stat, HItem? item)
    {
        if (stat == EStat.None) return;

        if (this is PartyMember pm && item != null)
        {
            SineaterGame.Instance.Party.Inventory.Items.Add(item);
        }

        Items[(int)stat - 1] = item;
    }

    public void EquipAndAdd(HItem? item)
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
