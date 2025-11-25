using System;

namespace SINEATER;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class LargeTextAttribute : Attribute {}


public record struct Introduction(
    [property: LargeText] string Default = "", 
    [property: LargeText] string InMorning = "", 
    [property: LargeText] string InAfternoon = "", 
    [property: LargeText] string InEvening = "", 
    [property: LargeText] string InNight = "");
public record struct Encounter(int MinEnemyLevel, int MaxEnemyLevel, int ResourceCap, string Reward);