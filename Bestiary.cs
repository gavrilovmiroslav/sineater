using System;
using System.Collections;
using System.Collections.Generic;
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
            Level = 1,
            Crew = 0,
            CrewChoice = ECrewChoice.None,
            Icon = (7, 65),
            DeadIcon = (8, 65),
            Portrait = (5, 1),
            Sin = 1,
            HP = 1,
            Tint = Color.Red,
            Stats = new Stats(4, 2, 1, 2),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout(), new BehaviorFlyAbout() ],
        };

        bat.LeftWeapon = ItemLibrary.GetWeapon("Fang");
        bat.RightWeapon = ItemLibrary.GetWeapon("Fang");
        if (Rnd.Instance.D6 <= 2) bat.Traits.Add(new TraitFrenzied(20));
        return bat;
    }

    public static Enemy Goblin()
    {
        var gob = new Enemy
        {
            Name = "Goblin",
            Level = 2,
            Crew = 2 + Rnd.Instance.D4,
            CrewChoice = ECrewChoice.Companion,
            Icon = (5, 64),
            DeadIcon = (8, 65),
            Portrait = (0, 2),
            Sin = Rnd.Instance.D4,
            HP = 3,
            Tint = Color.LightGreen,
            Armor = ItemLibrary.GetArmor("Robe"),
            Stats = new Stats(2, 2, 2, Rnd.Instance.Next(3, 4)),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout() ],
        };
        if (Rnd.Instance.D4 > gob.Sin)
            gob.LeftWeapon = ItemLibrary.GetWeapon("Dagger");
        gob.RightWeapon = ItemLibrary.GetWeapon("Dagger");
        return gob;
    }
    
    public static Enemy Hobgoblin()
    {
        var gob = new Enemy
        {
            Name = "Hobgob",
            Level = 3,
            Crew = 3 + Rnd.Instance.D4,
            CrewChoice = ECrewChoice.Minions,
            Icon = (6, 64),
            DeadIcon = (8, 65),
            Portrait = (1, 2),
            Sin = 3 + Rnd.Instance.D2,
            HP = 3,
            Tint = Color.Red,
            Armor = ItemLibrary.GetArmor("Robe"),
            Stats = new Stats(6, 3, 3, 4),
            Behaviors = [ 
                new BehaviorAggro(),  
                new BehaviorIfWounded(4, new BehaviorThrowHealing(), new BehaviorAggro())
            ],
        };

        gob.LeftWeapon = ItemLibrary.GetWeapon("Thorn Whip");
        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitWise()));

        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitSneaky()));

        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitProficient()));

        return gob;
    }
    
    public static Enemy Skel()
    {
        var skel = new Enemy
        {
            Name = "Skel",
            Level = 2,
            Crew = 2 + Rnd.Instance.D4,
            CrewChoice = ECrewChoice.Minions,
            Icon = (3, 64),
            DeadIcon = (9, 65),
            Portrait = (2, 2),
            Sin = 3 + Rnd.Instance.D2,
            HP = 5,
            Tint = Color.Red,
            Armor = null,
            Stats = new Stats(5, 4, 4, 4),
            Behaviors = [ 
                new BehaviorAggro(),
            ],
        };

        skel.LeftWeapon = ItemLibrary.GetWeapon("Dagger");
        skel.RightWeapon = ItemLibrary.GetShield("Round Shield");
        return skel;
    }
    
    public static Enemy Skul()
    {
        var skul = new Enemy
        {
            Name = "Skul",
            Level = 3,
            Crew = 3 + Rnd.Instance.D4,
            CrewChoice = ECrewChoice.Minions,
            Icon = (4, 64),
            DeadIcon = (9, 65),
            Portrait = (4, 2),
            Sin = 3 + Rnd.Instance.D2,
            HP = 7,
            Tint = Color.Red,
            Armor = null,
            Stats = new Stats(6, 5, 5, 5),
            Behaviors = [ 
                new BehaviorAggro(),
            ],
        };

        skul.LeftWeapon = ItemLibrary.GetWeapon("Claymore");
        skul.RightWeapon = ItemLibrary.GetShield("Round Shield");
        return skul;
    }
    
    public static Enemy Snek()
    {
        var snek = new Enemy
        {
            Name = "Snek",
            Level = 3,
            Crew = Rnd.Instance.D2 - 1,
            CrewChoice = ECrewChoice.Companion,
            Icon = (2, 64),
            DeadIcon = (9, 65),
            Portrait = (4, 3),
            Sin = 5 + Rnd.Instance.D2,
            HP = 10,
            Tint = Color.Green,
            Armor = null,
            Stats = new Stats(6, 3, 3, 6),
            Behaviors = [ 
                new BehaviorAggro(),
            ],
        };

        snek.LeftWeapon = ItemLibrary.GetWeapon("Fang");
        snek.RightWeapon = ItemLibrary.GetShield("Fang");
        return snek;
    }
}