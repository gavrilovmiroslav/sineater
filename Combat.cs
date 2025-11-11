namespace SINEATER;

public record struct FriendlyTeam;
public record struct EnemyTeam;
public record struct LiveStats(Stats Stats);
public record struct Combatant(ICharacter Bas);
public record struct Position(int X, int Y);
