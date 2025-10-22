using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public static class Bestiary
{

    public static Enemy Bat()
    {
        var bat = new Enemy
        {
            Name = "Bat",
            Icon = (7, 65),
            DeadIcon = (8, 65),
            Portrait = (5, 1),
            Sin = 1,
            HP = 1,
            Tint = Color.Red,
            Stats = new Stats(4, 2, 1, 2),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout(), new BehaviorFlyAbout() ],
        };

        bat.LeftWeapon = ItemLibrary.Fang;
        bat.RightWeapon = ItemLibrary.Fang;
        if (Rnd.Instance.D6 <= 2) bat.Traits.Add(new TraitFrenzied(20));
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
            HP = 3,
            Tint = Color.LightGreen,
            Armor = ItemLibrary.Robe,
            Stats = new Stats(2, 2, 2, Rnd.Instance.Next(3, 4)),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout() ],
        };
        if (Rnd.Instance.D4 > gob.Sin)
            gob.LeftWeapon = ItemLibrary.Dagger;
        gob.RightWeapon = ItemLibrary.Dagger;
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
            Sin = 3 + Rnd.Instance.D2,
            HP = 3,
            Tint = Color.Red,
            Armor = ItemLibrary.Robe,
            Stats = new Stats(6, 3, 3, 4),
            Behaviors = [ 
                new BehaviorAggro(),  
                new BehaviorIfWounded(4, new BehaviorThrowHealing(), new BehaviorAggro())
            ],
        };

        gob.LeftWeapon = ItemLibrary.ThornWhip;
        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitWise()));

        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitSneaky()));

        if (Rnd.Instance.D6 <= 2)
            Coroutine.Consume(gob.AddTrait(new TraitProficient()));

        return gob;
    }
}