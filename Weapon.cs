using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    Light = 4,
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
            case EWeightClass.Light:
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

public class Weapon(string name, int attack, EWeightClass weight, int quality, (int, int) uv) : IEquippable, IItem
{
    public (int, int) Picture => (uv.Item1, 5 + uv.Item2);
    public string Name { get; set; } = name;
    public Glyph Glyph => Glyph.Bw(14, 67);

    public List<Trait> Traits { get; set; } = [];
    
    public bool CanBeUsed()
    {
        return false;
    }

    public bool CanBeShattered()
    {
        return false;
    }

    public IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyItemEquipped(ICharacter character)
    {
        yield break;
    }
    
    public virtual IEnumerable ApplyItemUnequipped(ICharacter character)
    {
        yield break;
    }

    public IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        if (character is PartyMember chr)
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
                character.Inventory.Put(this);
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

    public virtual int Attack{ get; set; } = attack;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;

    public override string ToString()
    {
        return $"{Name} ({Attack}{Weight.Short()})";
    }
    
    public virtual string ToLongString()
    {
        return $"{Name} (Attack: {Attack}, Weight: {Weight.ToString()})";
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

public class TraitShielded(Shield shield) : ItemTrait("Shielded", "Sh", shield, "SHIELD: Adds defense dice as if the shield is an armor.")
{
    public Shield Owner { get; set; } = shield;
    public override IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield return new CombatFlow_Notify($"SHIELD: {Owner.Defense} defense dice are added because of the {Owner.GetName()}!");
        for (var i = 0; i < Owner.Defense; i++)
        {
            flow.DefenseDicePreRoll.Add(new Die());    
        }

        yield break;
    }

    public override IEnumerable AsDefender_ApplyArmorDented(CombatFlow flow)
    {
        if (Rnd.Instance.D100 < 20)
        {
            flow.ArmorDented = false;
            this.Owner.Defense--;
            yield break;
        }
    }
}

public class Shield(string name, int defense, EWeightClass weight, int quality, (int, int) uv)
    : Weapon(name, 0, weight, quality, uv)
{
    public int Defense { get; set; } = defense;
    
    public override string ToString()
    {
        return $"{Name} ({Defense}G)";
    }
    
    public override string ToLongString()
    {
        return $"{Name} (Guard: {Defense}, Weight: {Weight.ToString()})";
    }

    public override IEnumerable ApplyItemEquipped(ICharacter character)
    {
        if (this.Defense > 0)
        {
            character.GetTraits().Add(new TraitShielded(this));
        }

        yield break;
    }

    public override IEnumerable ApplyItemUnequipped(ICharacter character)
    {
        foreach (var trait in character.GetTraits().Where(t => t is TraitShielded s && s.Owner == this).ToArray())
        {
            character.GetTraits().Remove(trait);
        }

        yield break;
    }
}