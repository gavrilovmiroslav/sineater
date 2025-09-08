using System.Collections.Generic;

namespace SINEATER;

public enum EWeightClass
{
    Tiny = 2,
    Small = 4,
    Medium = 6,
    Heavy = 8,
    Large = 10
}

public record struct Weapon(string Name, int Attack, EWeightClass Weight, int Quality);

public record struct Armor(string Name, int Guard, EWeightClass Weight, int Quality);