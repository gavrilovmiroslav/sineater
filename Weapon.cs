using System.Collections.Generic;

namespace SINEATER;

public enum EWeaponClass
{
    Tiny = 2,
    Small = 4,
    Medium = 6,
    Heavy = 8,
    Large = 10
}

public record struct Weapon(string Name, EWeaponClass WeaponClass, int Quality);

public record struct Armor(string Name, int Guard);