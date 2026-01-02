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

    public void Init()
    {
        //AP = new AP(15, SineaterGame.Instance.Layers["ascii"], 15 - Stamina);
    }

    public Enemy Copy()
    {
        var enemy = new Enemy();
        enemy.X = X;
        enemy.Y = Y;
        enemy.Stats.Clarity = this.Stats.Clarity;
        enemy.Stats.Will = this.Stats.Will;
        enemy.Stats.Poise = this.Stats.Poise;
        enemy.Stats.Vigor = this.Stats.Vigor;
        
        for (var i = 0; i < 4; i++)
            enemy.Items[i] = Items[i];

        return enemy;
    }
}