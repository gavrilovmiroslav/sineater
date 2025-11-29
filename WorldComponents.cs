using System;

namespace SINEATER;

public interface IWorldComponent
{
    public bool IsOkay();
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class LargeTextAttribute : Attribute {}

public readonly record struct GeneralDescription(
    [property: LargeText] string Text = "") : IWorldComponent
{
    public bool IsOkay()
    {
        return Text is { Length: > 0 };
    }
}

public readonly record struct SpecificDescription(
    ETimeOfDay TimeOfDay,
    [property: LargeText] string Text = "") : IWorldComponent
{
    public bool IsOkay()
    {
        return Text is { Length: > 0 };
    }
}

public readonly record struct Encounter(ETerrainKind Biome, int MinEnemyLevel, int MaxEnemyLevel, int ResourceCap, string Reward) : IWorldComponent
{
    public bool IsOkay()
    {
        return MaxEnemyLevel >= MinEnemyLevel && MinEnemyLevel > 0 && ResourceCap > 0;
    }
}

public readonly record struct SlowDown(int HoursSpent, int FatigueGained) : IWorldComponent
{
    public bool IsOkay()
    {
        return HoursSpent > 1 || FatigueGained > 0;
    }
}