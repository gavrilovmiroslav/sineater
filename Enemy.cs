using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Enemy : Character
{
    public int Wait = 3;
    public string Name;
    public ICharacter? LastHit = null;
    public (int, int) Icon;
    public (int, int) Portrait;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    public List<IBehavior> Behaviors = [];

    public override Color GetTint()
    {
        return Wait switch
        {
            1 => Color.Red,
            2 => Color.Orange,
            3 => Color.Yellow,
            4 => Color.Green,
            5 => Color.Blue,
            _ => Tint
        };
    }
    
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