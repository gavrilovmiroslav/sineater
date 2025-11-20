using Newtonsoft.Json;
using SINEATER.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER;

public interface IAbilitySource
{
    [JsonProperty]
    public LocalizedString LocaName { get; set; }
    public Glyph GetIcon();
}

public interface IEquippable {}

public enum EWeightClass
{
    Tiny = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Large = 4
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

public record struct StatusScaling(
    EScalingFactor fatigueScaling = EScalingFactor.F,
    EScalingFactor frostScaling = EScalingFactor.F,
    EScalingFactor fireScaling = EScalingFactor.F,
    EScalingFactor poisonScaling = EScalingFactor.F,
    EScalingFactor woundScaling = EScalingFactor.F,
    EScalingFactor insanityScaling = EScalingFactor.F,
    EScalingFactor deathScaling = EScalingFactor.F,
    EScalingFactor voidScaling = EScalingFactor.F);

public interface IWeaponUpgrade;

public enum EElement
{
    None = 0,
    Physical = 1,
    Mental = 2,
    Both = 3,
}

[JsonObject(MemberSerialization.OptIn)]
public class Weapon(string name, EWeightClass weight,
    int attack, int defense,
    int quality, (int, int) inventoryPicture,
    EElement element = EElement.None,
    // STAT SCALING
    EScalingFactor wilScaling = EScalingFactor.F, 
    EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, 
    EScalingFactor vigScaling = EScalingFactor.F,
    // STATUS SCALING (MINE)
    EScalingFactor myFatigueScaling = EScalingFactor.F,
    EScalingFactor myFrostScaling = EScalingFactor.F,
    EScalingFactor myFireScaling = EScalingFactor.F,
    EScalingFactor myPoisonScaling = EScalingFactor.F,
    EScalingFactor myWoundScaling = EScalingFactor.F,
    EScalingFactor myInsanityScaling = EScalingFactor.F,
    EScalingFactor myDeathScaling = EScalingFactor.F,
    EScalingFactor myVoidScaling = EScalingFactor.F,
    // STATUS SCALING (ENEMY)
    EScalingFactor theirFatigueScaling = EScalingFactor.F,
    EScalingFactor theirFrostScaling = EScalingFactor.F,
    EScalingFactor theirFireScaling = EScalingFactor.F,
    EScalingFactor theirPoisonScaling = EScalingFactor.F,
    EScalingFactor theirWoundScaling = EScalingFactor.F,
    EScalingFactor theirInsanityScaling = EScalingFactor.F,
    EScalingFactor theirDeathScaling = EScalingFactor.F,
    EScalingFactor theirVoidScaling = EScalingFactor.F,
    // SCALING CURVE VALUES
    float scalingBase = 14.0f, float scalingCurve = 1.5f) : ICloneable, IEquippable, IItem
{
    ~Weapon()
    {
        if (ItemLibrary.InstancedWeapons.ContainsKey(Name))
        {
            ItemLibrary.InstancedWeapons.Remove(Name, this);
        }
    }
    
    #region Serialization
    [JsonProperty]
    public string Name { get; set; } = name;

    [JsonProperty]
    public LocalizedString LocaName { get; set; } = new LocalizedString(name);
    [JsonProperty]
    public EWeightClass Weight { get; set; } = weight;
    [JsonProperty]
    public int Quality { get; set; } = quality;
    [JsonProperty]
    public int Attack { get; set; } = attack;
    [JsonProperty]
    public int Defense { get; set; } = defense;
    [JsonProperty]
    public EElement Element { get; set; } = element;
    [JsonProperty]
    public List<ITrait> Traits { get; set; } = [];

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
    public EScalingFactor MyFatigueScaling { get; set; } = myFatigueScaling;
    [JsonProperty]
    public EScalingFactor MyFrostScaling { get; set; } = myFrostScaling;
    [JsonProperty]
    public EScalingFactor MyFireScaling { get; set; } = myFireScaling;
    [JsonProperty]
    public EScalingFactor MyPoisonScaling { get; set; } = myPoisonScaling;
    [JsonProperty]
    public EScalingFactor MyWoundScaling { get; set; } = myWoundScaling;
    [JsonProperty]
    public EScalingFactor MyInsanityScaling { get; set; } = myInsanityScaling;
    [JsonProperty]
    public EScalingFactor MyDeathScaling { get; set; } = myDeathScaling;
    [JsonProperty]
    public EScalingFactor MyVoidScaling { get; set; } = myVoidScaling;
    
    [JsonProperty]
    public EScalingFactor TheirFatigueScaling { get; set; } = theirFatigueScaling;
    [JsonProperty]
    public EScalingFactor TheirFrostScaling { get; set; } = theirFrostScaling;
    [JsonProperty]
    public EScalingFactor TheirFireScaling { get; set; } = theirFireScaling;
    [JsonProperty]
    public EScalingFactor TheirPoisonScaling { get; set; } = theirPoisonScaling;
    [JsonProperty]
    public EScalingFactor TheirWoundScaling { get; set; } = theirWoundScaling;
    [JsonProperty]
    public EScalingFactor TheirInsanityScaling { get; set; } = theirInsanityScaling;
    [JsonProperty]
    public EScalingFactor TheirDeathScaling { get; set; } = theirDeathScaling;
    [JsonProperty]
    public EScalingFactor TheirVoidScaling { get; set; } = theirVoidScaling;

    [JsonProperty] 
    public float ScalingBase { get; set; } = scalingBase;
    [JsonProperty] 
    public float ScalingCurve { get; set; } = scalingCurve;
    
    #endregion // Serialization

    public int Level { get; set; } = 1;

    //            base   level scaling   quality^2            level
    // =Floor((Pow($B$24 * A3, $B$25 - $B$26 * $B$26 * 0.01 / A3)))
    public int ExperienceNeeded => (int)Math.Floor(Math.Pow(ScalingBase * Level, ScalingCurve - Quality * Quality * 0.01f / Level));
    public int ExperienceNow { get; set; } = 0;
    
    public Glyph Glyph => Glyph.Bw(14, 67);

    public float Base => Level * (int)Weight;

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
        this.LocaName = original.LocaName;
        this.Picture = original.Picture;
        this.Quality = original.Quality;
        this.Weight = original.Weight;
        this.ScalingBase = original.ScalingBase;
        this.ScalingCurve = original.ScalingCurve;
    }

    public static Weapon Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR WEAPON {name}");
        return new Weapon($"!{name}", EWeightClass.Medium, 1, 1, 0, (0, 0));
    }
}

public record struct WeaponAttack(
     string Name,
     int Attack, //Power
     int Stamina,
     int Accuracy
);