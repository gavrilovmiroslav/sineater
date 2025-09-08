using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Enemy : ICharacter
{
    public int X, Y;
    public Color Tint;
    public ActionPoints AP;
    public Stats Stats;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;
    public (int, int) Icon; 

    public Enemy()
    {
        
    }

    public static Enemy Goblin()
    {
        var gob = new Enemy
        {
            Icon = (5, 64),
            Tint = Color.LightGreen,
            Armor = new Armor("Rags", Rnd.Instance.D4, EWeightClass.Tiny, 1),
            RightWeapon = new Weapon("Bone dagger", Rnd.Instance.D4, EWeightClass.Tiny, 1)
        };
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

    public void ApplyOnCausedWounds(ICharacter defender, int wounds, bool crit)
    {
    }
}