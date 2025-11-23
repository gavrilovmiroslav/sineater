namespace SINEATER;

public record struct Introduction(string Description);
public record struct Encounter((int Min, int Max) EnemyLevel, int ResourceCap, IItem Reward);