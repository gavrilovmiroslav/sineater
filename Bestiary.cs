using Microsoft.Xna.Framework;

namespace SINEATER;

public static class Bestiary
{
    public static Enemy Bat()
    {
        var gob = new Enemy
        {
            Name = "Bat",
            Icon = (7, 65),
            DeadIcon = (8, 65),
            Portrait = (5, 1),
            Sin = 1,
            HP = 2,
            Tint = Color.Red,
            Armor = new Armor("Hide", 1, EWeightClass.Tiny, 1),
            Stats = new Stats(10, 2, 2, 2),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout(), new BehaviorFlyAbout() ],
        };
        
        gob.LeftWeapon = new Weapon("Bite", 2, EWeightClass.Small, 1);
        if (Rnd.Instance.D6 <= 2) gob.Traits.Add(new TraitFrenzied(20));
        
        return gob;
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
            HP = Rnd.Instance.Next(5, 10),
            Tint = Color.LightGreen,
            Armor = new Armor("Rags", Rnd.Instance.Next(3, 4), EWeightClass.Tiny, 1),
            Stats = new Stats(2, 2, 2, Rnd.Instance.Next(3, 4)),
            Behaviors = [ new BehaviorAggro(), new BehaviorFlyAbout() ],
        };
        if (Rnd.Instance.D4 > gob.Sin)
            gob.LeftWeapon = new Weapon("Stick", Rnd.Instance.D4 + 1, EWeightClass.Small, 1);
        gob.RightWeapon = new Weapon("Bone dagger", Rnd.Instance.D4, EWeightClass.Tiny, 1);
        return gob;
    }
    
    public static Enemy Hobgoblin()
    {
        var gob = new Enemy
        {
            Name = "Hobgoblin",
            Icon = (6, 64),
            DeadIcon = (8, 65),
            Portrait = (1, 2),
            Sin = 3 + Rnd.Instance.D2,
            HP = 8,
            Tint = Color.Red,
            Armor = new Armor("Rags", 4, EWeightClass.Tiny, 1),
            Stats = new Stats(6, 3, 2, 4),
            Behaviors = [ 
                new BehaviorAggro(),  
                new BehaviorIfWounded(4, new BehaviorThrowHealing(), new BehaviorAggro())
            ],
        };
        
        gob.LeftWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        gob.RightWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        if (Rnd.Instance.D6 <= 2) gob.Traits.Add(new TraitWise());
        if (Rnd.Instance.D6 <= 2) gob.Traits.Add(new TraitSneaky());
        if (Rnd.Instance.D6 <= 2) gob.Traits.Add(new TraitProficient());
        
        return gob;
    }
}