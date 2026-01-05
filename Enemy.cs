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
    public string Name;
    public (int, int) Icon;
    public (int, int) Portrait;
    public int NightSpeedUp = 0;
    public int DaySpeedUp = 0;
    public int NightGuardUp = 0;
    public int DayGuardUp = 0;
    
    public (int, int) GetIcon(bool selected = false)
    {
        var (x, y) = Icon;
        return (x, y + (selected ? -4 : 0));
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

    public static Enemy MakeFrom(EnemyDefinition def)
    {
        var enemy = new Enemy
        {
            X = 0,
            Y = 0,
            Stats = def.Stats,
            Icon = def.Icon,
            Portrait = def.Portrait,
            DayGuardUp = def.DayGuardUp,
            DaySpeedUp = def.DaySpeedUp,
            NightGuardUp = def.NightGuardUp,
            NightSpeedUp = def.NightSpeedUp, 
            Name = def.Display,
            Guard = def.Guard,
            Tags = def.Tags
        };
        return enemy;
    }
}