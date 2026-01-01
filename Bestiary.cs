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
            Icon = (7, 65),
            DeadIcon = (8, 65),
            Portrait = (5, 1),
            Sin = 1,
            Guard = 0,
            Tint = Color.Red,
            Stats = new Stats(2, 2, 1, 4),
        };

        bat.Equip(EStat.Poise, ItemLibrary.GetWeapon("Fangs"));
        bat.Equip(EStat.Clarity, ItemLibrary.GetWeapon("Fangs"));
        bat.Equip(EStat.Vigor, ItemLibrary.GetWeapon("Fangs"));
        bat.Equip(EStat.Will, ItemLibrary.GetWeapon("Fangs"));
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
            Guard = 0,
            Tint = Color.LightGreen,
            Stats = new Stats(4, 3, 2, 4),
        };
        
        gob.Equip(ItemLibrary.GetWeapon("Dagger"));
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
            Stats = new Stats(5, 2, 3, 5),
        };

        gob.Equip(ItemLibrary.GetWeapon("Claymore"));
        gob.Equip(ItemLibrary.GetWeapon("Red Sign"));
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
            Guard = 0,
            Tint = Color.Red,
            Stats = new Stats(5, 4, 1, 6),
        };
        
        skel.Equip(ItemLibrary.GetWeapon("Dagger"));
        skel.Equip(ItemLibrary.GetWeapon("Round Shield"));
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
            Guard = 0,
            Tint = Color.Red,
            Stats = new Stats(3, 5, 6, 6),
        };

        skul.Equip(ItemLibrary.GetWeapon("Claymore"));
        skul.Equip(ItemLibrary.GetWeapon("Round Shield"));
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
            Guard = 5,
            Tint = Color.Green,
            Stats = new Stats(6, 6, 6, 6),
        };
        
        snek.Equip(ItemLibrary.GetWeapon("Claymore"));
        snek.Equip(ItemLibrary.GetWeapon("Thorn Whip"));
        snek.Init();
        return snek;
    }
}