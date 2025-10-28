using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
public record struct SkirmishStep_StepForwards(int n) : ISkirmishStep;
public record struct SkirmishStep_StepBackwards(int n) : ISkirmishStep;
public record struct SkirmishStep_SidestepLeft(int n) : ISkirmishStep;
public record struct SkirmishStep_SidestepRight(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackFront(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackBack(int n) : ISkirmishStep;
public record struct SkirmishStep_AttackHand : ISkirmishStep;
public record struct SkirmishStep_AttackLeft : ISkirmishStep;
public record struct SkirmishStep_AttackRight : ISkirmishStep;
public record struct SkirmishStep_AttackRanged((int, int) position) : ISkirmishStep;
public record struct SkirmishStep_AddTrait(Trait trait, int n = 0) : ISkirmishStep;

public enum EScalingFactor
{
    F = 0,
    D = 1,
    C = 2,
    B = 3,
    A = 5,
    S = 10,
}

public record struct Unlockable<T>(T Thing, int MinLevel);

public interface IWeaponUpgrade;

public record struct WeaponUpgrade_None : IWeaponUpgrade;
public record struct WeaponUpgrade_ScaleBaseChanged(float n) : IWeaponUpgrade;
public record struct WeaponUpgrade_ScaleFactorChanged(float n) : IWeaponUpgrade;
public record struct WeaponUpgrade_WeightChanged(EWeightClass weight) : IWeaponUpgrade;
public record struct WeaponUpgrade_AttackUnlocked(string name) : IWeaponUpgrade;
public record struct WeaponUpgrade_TraitUnlocked(string name) : IWeaponUpgrade;
public record struct WeaponUpgrade_ScaleChanged(EStat stat) : IWeaponUpgrade;
public record struct WeaponUpgrade_OpeningsChanged(int n) : IWeaponUpgrade;
public record struct WeaponUpgrade_CritOnChanged(int n) : IWeaponUpgrade;
public record struct WeaponUpgrade_QualityChanged(int n) : IWeaponUpgrade;

[JsonObject(MemberSerialization.OptIn)]
public class Weapon(string name, List<Unlockable<WeaponAttack>> attacks, EWeightClass weight,
    int quality, (int, int) inventoryPicture,
    List<Unlockable<Trait>> traits = null,
    List<IWeaponUpgrade> upgrades = null,
    EScalingFactor wilScaling = EScalingFactor.F, EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, EScalingFactor vigScaling = EScalingFactor.F,
    float scalingBase = 14.0f, float scalingCurve = 1.5f) : ICloneable, IEquippable, IItem
{
    ~Weapon()
    {
        if (ItemLibrary.InstancedWeapons.ContainsKey(name))
        {
            ItemLibrary.InstancedWeapons.Remove(name, this);
        }
    }
    
    #region Serialization
    [JsonProperty]
    public string Name { get; set; } = name;
    [JsonProperty]
    public List<Unlockable<WeaponAttack>> Attacks { get => attacks; set => attacks = value; }
    [JsonProperty]
    public List<IWeaponUpgrade> Upgrades { get => upgrades; set => upgrades = value; }
    [JsonProperty]
    public List<Unlockable<Trait>> Traits { get => traits; set => traits = value; }
    [JsonProperty]
    public EWeightClass Weight { get; set; } = weight;
    [JsonProperty]
    public int Quality { get; set; } = quality;
    [JsonProperty]
    public (int, int) Picture { get; set; } = inventoryPicture;
    [JsonProperty]
    public EScalingFactor WilScaling { get; set; } = wilScaling;
    [JsonProperty]
    public EScalingFactor ClaScaling { get; set; } = claScaling;
    [JsonProperty]
    public EScalingFactor PoiScaling { get; set; } = poiScaling;
    [JsonProperty]
    public EScalingFactor VigScaling { get; set; } = vigScaling;
    [JsonProperty]
    public float ScalingBase { get => scalingBase; set => scalingBase = value; }
    [JsonProperty]
    public float ScalingCurve { get => scalingCurve; set => scalingCurve = value; }
    #endregion // Serialization

    public int Level { get; set; } = 1;

    //            base   level scaling   quality^2            level
    // =Floor((Pow($B$24 * A3, $B$25 - $B$26 * $B$26 * 0.01 / A3)))
    public int ExperienceNeeded => (int)Math.Floor(Math.Pow(scalingBase * Level, scalingCurve - Quality * Quality * 0.01f / Level));
    public int ExperienceNow { get; set; } = 0;
    
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

    public override string ToString()
    {
        return $"{Name}";
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }

    public virtual string ToLongString()
    {
        return $"{Name} (Quality: {Quality}, Weight: {Weight.ToString()})";
    }

    public string GetName()
    {
        return Name;
    }

    public List<Unlockable<WeaponAttack>> GetAvailableAttacks()
    {
        return attacks;
    }
    
    public virtual Glyph GetIcon()
    {
        return Glyph;
    }

    public void Copy(Weapon original)
    {
        this.WilScaling = original.WilScaling;
        this.ClaScaling = original.ClaScaling;
        this.PoiScaling = original.PoiScaling;
        this.VigScaling = original.VigScaling;

        this.ExperienceNow = original.ExperienceNow;
        this.Name = original.Name;
        this.Picture = original.Picture;
        this.Quality = original.Quality;
        this.Traits = original.Traits;
        this.Upgrades = original.Upgrades;
        this.Weight = original.Weight;
        this.ScalingBase = original.ScalingBase;
        this.ScalingCurve = original.ScalingCurve;
        
        this.Attacks = original.Attacks;
    }

    public static Weapon Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR WEAPON {name}");
        return new Weapon($"{name} (DUMMY)", [], EWeightClass.Tiny, 0, (0, 0), [], []);
    }
}

public record struct WeaponAttack(
     string Name,
     int Attack,
     int CritOn = 6,
     int OpeningsPerCrit = 1,
     List<Trait>? Traits = null,
     List<ISkirmishStep>? Steps = null
);

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

public class Shield(string name, List<Unlockable<WeaponAttack>> attacks, List<Unlockable<Trait>> traits, List<IWeaponUpgrade> upgrades, int defense, EWeightClass weight, int quality, (int, int) inventoryPicture, 
    EScalingFactor wilScaling = EScalingFactor.F, EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, EScalingFactor vigScaling = EScalingFactor.F,
    float scalingBase = 14.0f, float scalingCurve = 1.5f)
    : Weapon(name, attacks, weight, quality, inventoryPicture, traits, upgrades, wilScaling, claScaling, poiScaling, vigScaling, scalingBase, scalingCurve)
{
    ~Shield()
    {
        if (ItemLibrary.InstancedShields.ContainsKey(name))
        {
            ItemLibrary.InstancedShields.Remove(name, this);
        }
    }
    
    [JsonProperty]
    public int Defense { get; set; } = defense;

    public void Copy(Shield original)
    {
        base.Copy(original);
        this.Defense = original.Defense;
    }

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
    
    public new static Shield Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR SHIELD {name}");
        return new Shield($"{name} (DUMMY)", [], [], [], 0, EWeightClass.Tiny, 0, (0, 0));
    }
}