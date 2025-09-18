using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace SINEATER;

public interface IAbilitySource {}

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

public class Weapon(string name, int attack, EWeightClass weight, int quality) : IAbilitySource
{
    public string Name { get; set; } = name;
    public int Attack{ get; set; } = attack;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;
}

public class Armor(string name, int guard, EWeightClass weight, int quality) : IAbilitySource
{
    public string Name { get; set; } = name;
    public int Guard{ get; set; } = guard;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;
}