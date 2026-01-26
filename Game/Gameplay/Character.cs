using System.Collections.Generic;
using System.Linq;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Screens;

namespace SINEATER.Game.Gameplay;

public abstract class Character
{
    public static Character Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    public List<string> Tags { get; set; } = [];
    public bool Broken { get; set; } = false;
    
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
        if (item == null) return;
        
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i] == null)
            {
                Items[i] = item;
                SineaterGame.Instance.Party.Inventory.Items.Remove(item);
                break;
            }
        }
    }

    public void Unequip(Item item)
    {
        for (int i = 0; i < Items.Length; i++)
        {
            if (Items[i] == item)
            {
                Items[i] = null;
                SineaterGame.Instance.Party.Inventory.Items.Add(item);
                break;
            }
        }
    }
    
    public int Wil => Stats.Will;
    public int Cla => Stats.Clarity;
    public int Poi => Stats.Poise;
    public int Vig => Stats.Vigor;
    
    public ECharacterClass Job;
    public Item?[] Items = new Item?[2];
    
    public virtual int X { get; set; }
    public virtual int Y { get; set; }
    public Track Guard { get; set; }
    public float Speed { get; set; } = 0;
    public float Resist { get; set; } = 0;
    public int Shield { get; set; } = 0;
    public Stats Stats { get; set; } = new();
    public bool AnyItemReady => Items.Any(item => item?.TimeGauge >= 100);

    public virtual string GetName()
    {
        return Job.ToString();
    }
    
    public virtual (int, int) GetPortait()
    {
        return Job.GetPortrait();
    }
}
