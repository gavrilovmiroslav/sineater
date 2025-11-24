namespace SINEATER;

public record struct Introduction(string Default = "", string? InMorning = null, string? InAfternoon = null, string? InEvening = null, string? InNight = null);
public record struct Encounter(int MinEnemyLevel, int MaxEnemyLevel, int ResourceCap, string Reward);