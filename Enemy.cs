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

    public bool IsStunned()
    {
        return AP.Contains<StatusStunned>();
    }

    public void ApplyOnAttackRoll(ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
    }

    public void ApplyOnRolledAttack(ICharacter attacker, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
    }

    public void ApplyOnAttackBlocked(ICharacter attacker, (int attack, Weapon weapon) attackValue,
        (int defense, Armor armor) defenseValue)
    {
    }

    public void ApplyOnSuccessfulBlock(ICharacter attacker, int attack, Weapon weapon)
    {
    }

    public void ApplyOnWounded(ICharacter attacker, int wounds)
    {
    }

    public void ApplyOnCausedWounds(ICharacter defender, int wounds)
    {
    }

    public void ApplyOnWoundCounted(int hitDie, int index, ref int damage)
    {
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