using System;
using Microsoft.Xna.Framework;
using Wintellect.PowerCollections;

namespace SINEATER;

public static class Bestiary
{
    public static MultiDictionary<int, Func<Enemy>> Levels = new(false)
    {
        { 1, new Func<Enemy>(Bat) },
        { 2, new Func<Enemy>(Goblin) },
        { 2, new Func<Enemy>(Skel) },
        { 3, new Func<Enemy>(Hobgoblin) },
        { 3, new Func<Enemy>(Skul) },
        { 3, new Func<Enemy>(Snek) },
    };

    public static Enemy Bat()
    {
        var bat = new Enemy
        {
            Name = "Bat",
            Icon = (7, 65),
            DeadIcon = (8, 65),
            Portrait = (5, 1),
            Sin = 1,
            Guard = 1,
            Tint = Color.Red,
            Stats = new Stats(1, 1, 1, 2),
        };

        bat.Init();
        return bat;
    }

    public static Enemy Goblin()
    {
        var gob = new Enemy
        {
            Name = "Goblin",
            Icon = (5, 64),
            DeadIcon = (8, 65),
            Portrait = (0, 2),
            Sin = Rnd.Instance.D4,
            Guard = 2,
            Tint = Color.LightGreen,
            Stats = new Stats(2, 1, 1, 2),
            NightSpeedup = 1,
        };
        
        gob.Init();
        return gob;
    }
    
    public static Enemy Hobgoblin()
    {
        var gob = new Enemy
        {
            Name = "Hobgob",
            Icon = (6, 64),
            DeadIcon = (8, 65),
            Portrait = (1, 2),
            Sin = 3,
            Guard = 3,
            Tint = Color.Red,
            Stats = new Stats(3, 2, 2, 3),
            NightSpeedup = 2,
        };

        gob.Init();
        return gob;
    }
    
    public static Enemy Skel()
    {
        var skel = new Enemy
        {
            Name = "Skel",
            Icon = (3, 64),
            DeadIcon = (9, 65),
            Portrait = (2, 2),
            Sin = 3 + Rnd.Instance.D2,
            Guard = 3,
            Tint = Color.Red,
            Stats = new Stats(1, 4, 1, 1),
            NightSpeedup = 4,
        };
        
        skel.Init();
        return skel;
    }
    
    public static Enemy Skul()
    {
        var skul = new Enemy
        {
            Name = "Skul",
            Icon = (4, 64),
            DeadIcon = (9, 65),
            Portrait = (4, 2),
            Sin = 3 + Rnd.Instance.D2,
            Guard = 4,
            Tint = Color.Red,
            Stats = new Stats(1, 5, 2, 2),
            NightSpeedup = 5,
        };

        skul.Init();
        return skul;
    }
    
    public static Enemy Snek()
    {
        var snek = new Enemy
        {
            Name = "Snek",
            Icon = (2, 64),
            DeadIcon = (9, 65),
            Portrait = (4, 3),
            Sin = 5 + Rnd.Instance.D2,
            Guard = 4,
            Tint = Color.Green,
            Stats = new Stats(2, 2, 2, 2),
            NightSpeedup = 1,
            DaySpeedup = -3
        };
        
        snek.Init();
        return snek;
    }

    public static Enemy Make(string name)
    {
        switch(name)
        {
            case "Bat":
                return Bat();
            case "Goblin":
                return Goblin();
            case "Skel":
                return Skel();
            case "Hobgoblin":
                return Hobgoblin();
            case "Skul":
                return Skul();
            case "Snek":
                return Snek();
            default:
                throw new NotImplementedException($"Unknow enemy name: {name}");
        }
    }
}
