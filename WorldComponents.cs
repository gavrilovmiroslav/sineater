using System;
using System.Collections.Generic;

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

public readonly record struct Encounter(List<Enemy> Enemies) : IWorldComponent
{
    public bool IsOkay()
    {
        return true;
    }
}

public readonly record struct SlowDown(int HoursSpent, int FatigueGained) : IWorldComponent
{
    public bool IsOkay()
    {
        return HoursSpent > 1 || FatigueGained > 0;
    }
}