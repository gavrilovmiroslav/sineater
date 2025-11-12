using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public enum ECrewChoice
{
    None,
    Minions,
    Companion,
}

public class Enemy : Character
{
    public bool NoMove = false;
    public int Level = 0;
    public int Crew = 1;
    public ECrewChoice CrewChoice = ECrewChoice.None;
    public int Wait = 3;
    public string Name;
    public ICharacter? LastHit = null;
    public (int, int) Icon;
    public (int, int) Portrait;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    
    public (int, int) GetIcon(bool selected = false)
    {
        var (x, y) = Icon;
        return (x, y + (selected ? -4 : 0));
    }

    public override void Done()
    {
        NoMove = true;
    }

    public override Color GetTint()
    {
        return Wait switch
        {
            0 => Color.Red,
            1 => Color.Orange,
            2 => Color.Yellow,
            3 => Color.Green,
            _ => Color.LightGreen,
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

    public Enemy()
    {
        AP = new AP(40, SineaterGame.Instance.Layers["ascii"]);
    }
}