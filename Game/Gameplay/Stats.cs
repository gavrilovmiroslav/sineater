using System.Collections.Generic;
using System.Linq;
using SINEATER.Game.CoreUtils;

namespace SINEATER.Game.Gameplay;

public enum EStat
{
    None = 0,
    Vigor = 1,
    Will = 2,
    Clarity = 3,
    Poise = 4
}

public class Stats
{
    public int Will;
    public int Clarity;
    public int Poise;
    public int Vigor;
    
    public int Score => Will + Clarity + Poise + Vigor;
    public int Initiative => Will + Vigor;
    public int Fortitude => Clarity + Poise;

    public Stats(ICharacter chr)
    {
        Will = chr.Stats.Will;
        Clarity = chr.Stats.Clarity;
        Poise = chr.Stats.Poise;
        Vigor = chr.Stats.Vigor;
    }

    public Stats(Stats other)
    {
        Will = other.Will;
        Clarity = other.Clarity;
        Poise = other.Poise;
        Vigor = other.Vigor;
    }
    
    public Stats()
    {
        var bag = Rnd.Instance.Bag((i => i > 1), 6, 6, 6, 6);
        
        Will = bag[0];
        Clarity = bag[1];
        Poise = bag[2];
        Vigor = bag[3];
    }

    public Stats(int wil, int cla, int poi, int vig)
    {
        Will = wil;
        Clarity = cla;
        Poise = poi;
        Vigor = vig;
    }

    public int this[int n]
    {
        get
        {
            switch (n)
            {
                case 1: return Will;
                case 2: return Clarity;
                case 3: return Poise;
                case 0: return Vigor;
                default:
                    return 0;
            }
        }
    }
    
    public int this[EStat stat]
    {
        get
        {
            switch (stat)
            {
                case EStat.Will: return Will;
                case EStat.Clarity: return Clarity;
                case EStat.Poise: return Poise;
                case EStat.Vigor: 
                default: return Vigor;
            }
        }
    }
    
    public int Mod(EStat stat)
    {
        return this[stat] switch
        {
            < 3 => 1,  
            <= 5 => 2,
            <= 8 => 3,
            <= 10 => 4,
            _ => 5
        };
    }

    public void Reset()
    {
        Will = 0;
        Clarity = 0;
        Poise = 0;
        Vigor = 0;
    }

    public EStat Highest()
    {
        List<(EStat stat, int val)> stats = [(EStat.Will, Will), (EStat.Clarity, Clarity), (EStat.Poise, Poise), (EStat.Vigor, Vigor)];
        var max = stats.MaxBy((w) => w.val);
        return max.stat;
    }
}

