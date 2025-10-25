using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

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

public interface ISkirmishStep;
public record struct SkirmishStep_Appear((int, int) position) : ISkirmishStep;
public record struct SkirmishStep_Forwards(int n) : ISkirmishStep;
public record struct SkirmishStep_Backwards(int n) : ISkirmishStep;
public record struct SkirmishStep_SidestepLeft(int n) : ISkirmishStep;
public record struct SkirmishStep_SidestepRight(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackFront(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackBack(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackHand : ISkirmishStep;
public record struct SkirmishStep_AttackLeft : ISkirmishStep;
public record struct SkirmishStep_AttackRight : ISkirmishStep;
public record struct SkirmishStep_AttackRanged((int, int) position) : ISkirmishStep;
public record struct SkirmishStep_AddTrait(Trait trait) : ISkirmishStep;

public enum EScalingFactor
{
    F = 0,
    D = 1,
    C = 2,
    B = 3,
    A = 5,
    S = 10,
}

public class Weapon(string name, List<WeaponAttack> attacks, EWeightClass weight, 
    int quality, (int, int) inventoryPicture,
    EScalingFactor wilScaling = EScalingFactor.F, EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, EScalingFactor vigScaling = EScalingFactor.F,
    float scalingBase = 14.0f, float scalingCurve = 1.5f) : IEquippable, IItem
{
#region Serialization
    [DataMember]
    public string Name { get; set; } = name;
    [DataMember]
    public virtual int Attack { get; set; } = attack;
    [DataMember]
    public EWeightClass Weight { get; set; } = weight;
    [DataMember]
    public int Quality { get; set; } = quality;
    [DataMember]
    public (int, int) Picture { get; set; } = inventoryPicture;
    [DataMember]
    public List<Trait> Traits { get; set; } = traits ?? [];
    [DataMember]
    public List<ISkirmishStep> Steps { get; set; } = steps ?? [];
    [DataMember]
    public EScalingFactor WilScaling { get; set; } = wilScaling;
    [DataMember]
    public EScalingFactor ClaScaling { get; set; } = claScaling;
    [DataMember]
    public EScalingFactor PoiScaling { get; set; } = poiScaling;
    [DataMember]
    public EScalingFactor VigScaling { get; set; } = vigScaling;
    [DataMember]
    public float ScalingBase { get; set; } = scalingBase;
    [DataMember]
    public float ScalingCurve { get; set; } = scalingCurve;
    [DataMember]
    public int CritOn { get; set; } = critOn;
    [DataMember]
    public int OpeningsPerCrit { get; set; } = openingsPerCrit;
#endregion // Serialization

    public int Level { get; set; } = 1;

    //            base   level scaling   quality^2            level
    // =Floor((Pow($B$24 * A3, $B$25 - $B$26 * $B$26 * 0.01 / A3)))
    public int ExperienceNeeded => (int)Math.Floor(Math.Pow(scalingBase * Level, scalingCurve - Quality * Quality * 0.01f / Level));
    public int ExperienceNow { get; set; } = 0;
    
    public EScalingFactor WilScaling => wilScaling;
    public EScalingFactor ClaScaling => claScaling;
    public EScalingFactor PoiScaling => poiScaling;
    public EScalingFactor VigScaling => vigScaling;
    
    public (int, int) Picture => inventoryPicture;
    public string Name { get; set; } = name;
    public Glyph Glyph => Glyph.Bw(14, 67);
    
    public bool CanBeUsed()
    {
        return false;
    }

    public virtual bool CanBeShattered()
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
                if (chr.X == x && chr.Y == y)
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

    public virtual IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        yield break;
    }
    
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;

    public override string ToString()
    {
        return $"{Name}";
    }
    
    public virtual string ToLongString()
    {
        return $"{Name} (Quality: {Quality}, Weight: {Weight.ToString()})";
    }

    public string GetName()
    {
        return Name;
    }

    public List<WeaponAttack> GetAvailableAttacks()
    {
        return attacks;
    }
    
    public virtual Glyph GetIcon()
    {
        return Glyph;
    }
}

public record struct WeaponAttack(
    string Name,
    int Attack,
    int CritOn = 6,
    int OpeningsPerCrit = 1,
    List<Trait>? Traits = null,
    List<ISkirmishStep>? Steps = null,
    int minLevel = 1);

public class TraitShielded(Shield shield) : ItemTrait("Shielded", "Sh", shield, "SHIELD: Adds defense dice as if the shield is an armor."), ISkirmish_GuardUp, ISkirmish_ArmorBreak
{
    public Shield Owner { get; private set; } = shield;
    
    public IEnumerable AsDefender_OnGuardUp(SkirmishFlow flow)
    {
        yield return new Present_Notify($"{Owner.GetName()} adds +{Owner.Defense} guard!");
        flow.DefenderArmor += Owner.Defense;
    }

    public IEnumerable AsDefender_OnArmorBreak(SkirmishFlow flow)
    {
        yield return new Present_Notify($"{Owner.GetName()} cracks under the heavy attack.");
        flow.ArmorBreak = false;
        Owner.Defense--;
        if (Owner.Defense < 0)
        {
            Owner.Defense = 0;
        }
    }

    public IEnumerable AsAttacker_OnGuardUp(SkirmishFlow flow) { yield break; }
    public IEnumerable AsAttacker_OnArmorBreak(SkirmishFlow flow) { yield break; }
}

public class Shield(string name, List<WeaponAttack> attacks, int defense, EWeightClass weight, int quality, (int, int) inventoryPicture, 
    EScalingFactor wilScaling = EScalingFactor.F, EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, EScalingFactor vigScaling = EScalingFactor.F,
    float scalingBase = 14.0f, float scalingCurve = 1.5f)
    : Weapon(name, attacks, weight, quality, inventoryPicture, wilScaling, claScaling, poiScaling, vigScaling, scalingBase, scalingCurve)
{
    [DataMember]
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