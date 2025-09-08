using System.Collections.Generic;
using System.Runtime.Versioning;

namespace SINEATER;

public enum EWeightClass
{
    Tiny = 2,
    Small = 4,
    Medium = 6,
    Heavy = 8,
    Large = 10
}

public class Weapon(string Name, int attack, EWeightClass weight, int quality)
{
    public int Attack{ get; set; } = attack;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;
}

public class Armor(string Name, int guard, EWeightClass weight, int quality)
{
    public int Guard{ get; set; } = guard;
    public EWeightClass Weight{ get; set; } = weight;
    public int Quality{ get; set; } = quality;
}