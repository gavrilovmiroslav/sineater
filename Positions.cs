using System.Net.Security;

namespace SINEATER;

public static class Positions
{
    public static Enemy? IsEnemyAt(int x, int y)
    {
        if (CombatMapScreen.Level == null) return null;
        
        foreach (var enemy in CombatMapScreen.Level.Enemies)
        {
            if (enemy.X == x && enemy.Y == y) return enemy;
        }

        return null;
    }

    public static PartyMember? IsCharacterAt(int x, int y)
    {
        foreach (var chr in SineaterGame.Instance.Party.Characters)
        {
            if (chr.X == x && chr.Y == y) return chr;
        }

        return null;
    }

    public static Enemy? IsEnemyAt((int, int) xy) => IsEnemyAt(xy.Item1, xy.Item2);
    public static PartyMember? IsCharacterAt((int, int) xy) => IsCharacterAt(xy.Item1, xy.Item2);

    public static ICharacter? GetCharAt(int x, int y)
    {
        if (Positions.IsCharacterAt((x, y)) is { } c)
        {
            return c;
        }
        else if (Positions.IsEnemyAt((x, y)) is {} e)
        {
            return e;
        }

        return null;
    }

    public static ICharacter? GetCharAt((int, int) xy) => GetCharAt(xy.Item1, xy.Item2);
    
    public static bool Swap((int, int) a, (int, int) b)
    {
        if (CombatMapScreen.Level == null) return false;
        if (!CombatMapScreen.Level.Map.IsWalkable(a.Item1, a.Item2)) return false;
        if (!CombatMapScreen.Level.Map.IsWalkable(b.Item1, b.Item2)) return false;
        
        var ca = GetCharAt(a);
        var cb = GetCharAt(b);
        
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