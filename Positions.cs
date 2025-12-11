using System.Net.Security;
using RogueSharp;

namespace SINEATER;

public static class Positions
{
    public static Enemy? IsEnemyAt(CombatMapScreen screen, int x, int y)
    {
        foreach (var enemy in screen.Structure.Enemies)
        {
            if (enemy.X == x && enemy.Y == y) return enemy;
        }

        return null;
    }

    public static PartyMember? IsCharacterAt(CombatMapScreen screen, int x, int y)
    {
        foreach (var chr in SineaterGame.Instance.Party.Characters)
        {
            if (chr.X == x && chr.Y == y) return chr;
        }

        return null;
    }
    
    public static Character? IsAnyCharacterAt(CombatMapScreen screen, int x, int y)
    {
        foreach (var chr in SineaterGame.Instance.Party.Characters)
        {
            if (chr.X == x && chr.Y == y) return chr as Character;
        }

        foreach (var enemy in screen.Structure.Enemies)
        {
            if (enemy.X == x && enemy.Y == y) return enemy as Character;
        }

        return null;
    }

    public static Enemy? IsEnemyAt(CombatMapScreen screen, (int, int) xy) => IsEnemyAt(screen, xy.Item1, xy.Item2);
    public static PartyMember? IsCharacterAt(CombatMapScreen screen, (int, int) xy) => IsCharacterAt(screen, xy.Item1, xy.Item2);

    public static ICharacter? GetCharAt(CombatMapScreen screen, int x, int y)
    {
        if (Positions.IsCharacterAt(screen, (x, y)) is { } c)
        {
            return c;
        }
        else if (Positions.IsEnemyAt(screen, (x, y)) is {} e)
        {
            return e;
        }

        return null;
    }

    public static ICharacter? GetCharAt(CombatMapScreen screen, (int, int) xy) => GetCharAt(screen, xy.Item1, xy.Item2);
    
    public static bool Swap(CombatMapScreen screen, (int, int) a, (int, int) b)
    {
        if (!screen.Map?.IsWalkable(a.Item1, a.Item2) ?? false) return false;
        if (!screen.Map?.IsWalkable(b.Item1, b.Item2) ?? false) return false;
        
        var ca = GetCharAt(screen, a);
        var cb = GetCharAt(screen, b);
        
        if (ca != null)
        {
            ca.X = b.Item1;
            ca.Y = b.Item2;
        }
        
        if (cb != null)
        {
            cb.X = a.Item1;
            cb.Y = a.Item2;
        }

        return ca != null || cb != null;
    }
}