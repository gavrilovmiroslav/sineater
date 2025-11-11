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

[JsonObject(MemberSerialization.OptIn)]
public class Weapon(string name, EWeightClass weight,
    int quality, (int, int) inventoryPicture,
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

    public string GetName()
    {
        return Name;
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
        this.Weight = original.Weight;
        this.ScalingBase = original.ScalingBase;
        this.ScalingCurve = original.ScalingCurve;
    }

    public static Weapon Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR WEAPON {name}");
        return new Weapon($"!{name}", EWeightClass.Tiny, 0, (0, 0));
    }
}

public record struct WeaponAttack(
     string Name,
     int Attack, //Power
     int Stamina,
     int Accuracy
);