using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Enemy : ICharacter
{
    public int X, Y;
    public string Name;
    public Color Tint;
    public ActionPoints AP;
    public int HP;
    public Stats Stats;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;
    public readonly List<Trait> Traits = [];
    public (int, int) Icon;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    
    public Enemy()
    {
        
    }

    public static Enemy Goblin()
    {
        var gob = new Enemy
        {
            Name = "Goblin",
            Icon = (5, 64),
            DeadIcon = (8, 65),
            Sin = Rnd.Instance.D4,
            HP = Rnd.Instance.Next(5, 10),
            Tint = Color.LightGreen,
            Armor = new Armor("Rags", Rnd.Instance.Next(3, 4), EWeightClass.Tiny, 1),
            Stats = new Stats(1, 2, 2, Rnd.Instance.Next(3, 4)),
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
            Sin = 3 + Rnd.Instance.D2,
            HP = 8,
            Tint = Color.Red,
            Armor = new Armor("Rags", 4, EWeightClass.Tiny, 1),
            Stats = new Stats(2, 3, 2, 4),
        };
        
        gob.LeftWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        gob.RightWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        gob.Traits.Add(new TraitSneaky());
        gob.Traits.Add(new TraitProficient());
        gob.Traits.Add(new TraitBalanced());
        return gob;
    }

    public Stats GetStats()
    {
        return Stats;
    }

    public ActionPoints GetAP()
    {
        return AP;
    }

    public Weapon? GetLeftWeapon()
    {
        return LeftWeapon;
    }

    public Weapon? GetRightWeapon()
    {
        return RightWeapon;
    }

    public Armor? GetArmor()
    {
        return Armor;
    }

    public List<Trait> GetTraits()
    {
        return Traits;
    }

    public bool IsStunned()
    {
        return AP.Contains<StatusStunned>();
    }

    public void ApplyOnAttackRoll(ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnAttackRoll(this, defender, ref attackDice, ref defenseDice);
        }
    }

    public void ApplyOnRolledAttack(ICharacter attacker, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnRolledAttack(attacker, this, ref attackDice, ref defenseDice);
        }
    }

    public void ApplyOnAttackBlocked(ICharacter attacker, ref (int attack, Weapon weapon) attackValue,
        ref (int defense, Armor armor) defenseValue)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnAttackBlocked(attacker, this, ref attackValue, ref defenseValue);
        }
    }

    public void ApplyOnSuccessfulBlock(ICharacter attacker, ref int attack, Weapon weapon)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnSuccessfulBlock(attacker, this, ref attack, weapon);
        }
    }

    public void ApplyOnWounded(ICharacter attacker, ref int wounds)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnWounded(attacker, this, ref wounds);
        }
    }

    public void ApplyOnDamageIncoming(ICharacter defender, ref int wounds)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnDamageIncoming(this, defender, ref wounds);
        }
    }

    public void ApplyOnWoundCounted(int hitDie, int index, int count, ref int damage)
    {
        foreach (var trait in Traits)
        {
            trait.ApplyOnWoundCounted(this, hitDie, index, count, ref damage);
        }
    }
    
    public string GetName()
    {
        return Name;
    }

    public void Die()
    {
        IsDead = true;
    }
}