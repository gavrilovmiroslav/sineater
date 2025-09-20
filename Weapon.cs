using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace SINEATER;

public interface IAbilitySource
{
    public string GetName();
    public Glyph GetIcon();
}

public interface IEquippable {}

public enum EWeightClass
{
    Tiny = 2,
    Small = 4,
    Medium = 6,
    Heavy = 8,
    Large = 10
}

public static class WeightClassExtensions
{
    public static string Short(this EWeightClass weightClass)
    {
        switch (weightClass)
        {
            case EWeightClass.Tiny:
                return "T";
            case EWeightClass.Small:
                return "S";
            case EWeightClass.Medium:
                return "M";
            case EWeightClass.Heavy:
                return "H";
            case EWeightClass.Large:
                return "L";
            default:
                return "-";
        }
    }
}

public class Weapon(string name, int attack, EWeightClass weight, int quality) : IEquippable, IItem
{
    public string Name { get; set; } = name;
    public Glyph Glyph => Glyph.Bw(14, 67);
    
    public bool CanBeShattered()
    {
        return false;
    }

    public IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        if (character is Character chr)
        {
            if (chr.LeftWeapon == null)
            {
                chr.LeftWeapon = this;
            }
            else if (chr.RightWeapon == null)
            {
                chr.RightWeapon = this;
            }
            else
            {
                SineaterGame.Instance.Inventory.Put(this);
            }
        }
        
        yield break;
    }

    public IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
    {
        if (Rnd.Instance.D10 < this.Quality)
        {
            foreach (var chr in SineaterGame.Instance.Party.Characters)
            {
                var cs = level.CombatStates[chr];
                if (cs.X == x && cs.Y == y)
                {
                    chr.AP.Add<StatusWounds>(1);
                }
            }

            foreach (var enm in level.Enemies)
            {
                if (enm.X == x && enm.Y == y)
                {
                    enm.AP.Add<StatusWounds>(1);
                }
            }
        }
        
        if (level.Floor.ContainsKey((x, y)))
        {
            var onFloor = level.Floor[(x, y)];
            if (onFloor is Pile pile)
            {
                pile.Things.Add(this);
            }
            else
            {
                var heap = new Pile();
                heap.Things.Add(onFloor);
                heap.Things.Add(this);
                level.Floor[(x, y)] = heap;
            }
        }
        else
        {
            level.Floor[(x, y)] = this;
        }

        yield break;
    }

    public IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        yield break;
    }

    public int Attack{ get; set; } = attack;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;

    public override string ToString()
    {
        return $"{Name} ({Attack}{Weight.Short()})";
    }

    public string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph;
    }
}

public class Armor(string name, int guard, EWeightClass weight, int quality) : IEquippable, IItem
{
    public string Name { get; set; } = name;
    public Glyph Glyph => Glyph.Bw(8, 68);
    public bool CanBeShattered()
    {
        return false;
    }

    public IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        if (character is Character chr)
        {
            if (chr.Armor == null || chr.Armor.Guard < this.Guard)
            {
                if (chr.Armor != null)
                {
                    SineaterGame.Instance.Inventory.Put(chr.Armor);
                }
                chr.Armor = this;
            }
            else
            {
                SineaterGame.Instance.Inventory.Put(this);
            }
        }
        
        yield break;
    }

    public IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
    {
        if (level.Floor.ContainsKey((x, y)))
        {
            var onFloor = level.Floor[(x, y)];
            if (onFloor is Pile pile)
            {
                pile.Things.Add(this);
            }
            else
            {
                var heap = new Pile();
                heap.Things.Add(onFloor);
                heap.Things.Add(this);
                level.Floor[(x, y)] = heap;
            }
        }
        else
        {
            level.Floor[(x, y)] = this;
        }

        yield break;
    }

    public IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        yield break;
    }

    public int Guard{ get; set; } = guard;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;

    public override string ToString()
    {
        return $"{Name} ({Guard}{Weight.Short()})";
    }

    public string GetName()
    {
        return Name;
    }

    public Glyph GetIcon()
    {
        return Glyph;
    }
}