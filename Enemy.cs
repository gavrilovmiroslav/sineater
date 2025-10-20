using System.Collections;
using System.Collections.Generic;

namespace SINEATER;

public class Enemy : Character
{
    public string Name;
    public ICharacter? LastHit = null;
    public (int, int) Icon;
    public (int, int) Portrait;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    public List<IBehavior> Behaviors = [];

    public override void Die()
    {
        IsDead = true;
    }

    public override string GetName()
    {
        return Name;
    }

    public override (int, int) GetPortait()
    {
        return Portrait;
    }
    
    public IEnumerable MoveTo(CombatMapScreen level, int x, int y, int? oldX = null, int? oldY = null)
    {
        var ox = X;
        var oy = Y;
        X = x;
        Y = y;
        if (level.Domains.Tiles.ContainsKey(((int)X, (int)Y)))
        {
            level.DrawCombat();
            yield return level.Domains.Tiles[((int)X, (int)Y)]
                .ApplyOnDomainStepped(level, this, X, Y, oldX ?? ox, oldY ?? oy);
        }
    }
}