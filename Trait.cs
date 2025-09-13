using System;
using System.Collections.Generic;

namespace SINEATER;

public interface ITrait
{
    void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice);
    void ApplyOnRolledAttack(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice);
    void ApplyOnAttackBlocked(ICharacter attacker, ICharacter defender, ref (int attack, Weapon weapon) attackValue, ref (int defense, Armor armor) defenseValue);
    void ApplyOnSuccessfulBlock(ICharacter attacker, ICharacter defender, ref int attack, Weapon weapon);
    void ApplyOnWounded(ICharacter attacker, ICharacter defender, ref int wounds);
    void ApplyOnDamageIncoming(ICharacter attacker, ICharacter defender, ref int wounds);
    void ApplyOnWoundCounted(ICharacter self, int hitDie, int index, int count, ref int damage);
}

public class Trait(string name)
{
    public string Name => name;
    
    public static List<Type> All = [
        typeof(TraitBalanced),
        typeof(TraitHeavy),
        typeof(TraitPadded),
        typeof(TraitProficient),
        typeof(TraitSkilled),
        typeof(TraitSneaky),
        typeof(TraitWise)
    ];
    
    public virtual void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice) {}
    public virtual void ApplyOnRolledAttack(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice) {}
    public virtual void ApplyOnAttackBlocked(ICharacter attacker, ICharacter defender, ref (int attack, Weapon weapon) attackValue, ref (int defense, Armor armor) defenseValue) {}
    public virtual void ApplyOnSuccessfulBlock(ICharacter attacker, ICharacter defender, ref int attack, Weapon weapon) {}
    public virtual void ApplyOnWounded(ICharacter attacker, ICharacter defender, ref int wounds) {}
    public virtual void ApplyOnDamageIncoming(ICharacter attacker, ICharacter defender, ref int wounds) {}
    public virtual void ApplyOnWoundCounted(ICharacter self, int hitDie, int index, int count, ref int damage) {}
}

public class TraitSneaky() : Trait("Sneaky")
{
    public override void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        var left = attacker.GetLeftWeapon();
        var right = attacker.GetRightWeapon();
        if (left == null || right == null) return;

        if ((int)left.Weight <= 6 && (int)right.Weight <= 6)
        {
            attackDice.Add((Rnd.Instance.D6, Rnd.Instance.D2 == 0 ? left : right));
            Console.WriteLine($"SNEAKY: Added another attack dice to {attacker.GetName()}");
        }
    }
}

public class TraitProficient() : Trait("Proficient")
{
    public override void ApplyOnWoundCounted(ICharacter self, int hitDie, int index, int count, ref int damage)
    {
        var left = self.GetLeftWeapon();
        var right = self.GetRightWeapon();
        if (left == null || right == null) return;
        if ((int)left.Weight <= 6 && (int)right.Weight <= 6)
        {
            if (count == 1)
            {
                damage += 1;
                Console.WriteLine($"PROFICIENT: +1 damage to {self.GetName()}");
            }
        }
    }
}

public class TraitBalanced() : Trait("Balanced")
{
    public override void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        var left = attacker.GetLeftWeapon();
        var right = attacker.GetRightWeapon();
        if (left == null || right == null) return;

        if (left.Weight == right.Weight)
        {
            attackDice.Add((Rnd.Instance.D6, left));
            attackDice.Add((Rnd.Instance.D6, right));
            Console.WriteLine($"BALANCED: Two more dice to {attacker.GetName()}");
        }
    }
}

public class TraitSkilled() : Trait("Skilled")
{
    public override void ApplyOnWoundCounted(ICharacter self, int hitDie, int index, int count, ref int damage)
    {
        if (Rnd.Instance.D100 <= 50)
        {
            damage += 1;
            Console.WriteLine($"SKILLED: Added +1 damage to {self.GetName()}");
        }
    }
}

public class TraitPadded() : Trait("Padded")
{
    public override void ApplyOnDamageIncoming(ICharacter attacker, ICharacter defender, ref int wounds)
    {
        if (wounds > 0)
            wounds -= 1;
        Console.WriteLine($"PADDED: -1 damage to {attacker.GetName()}");
    }
}

public class TraitHeavy() : Trait("Heavy")
{
    public override void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        var left = attacker.GetLeftWeapon();
        var right = attacker.GetRightWeapon();

        if (left != null && (int)left.Weight > 6)
        {
            attackDice.Add((Rnd.Instance.D6, left));
        }
        
        if (right != null && (int)right.Weight > 6)
        {
            attackDice.Add((Rnd.Instance.D6, right));
        }
        
        Console.WriteLine($"HEAVY: Added some dice if heavy weapons, probably");
    }
}

public class TraitWise() : Trait("Wise")
{
    public override void ApplyOnAttackRoll(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        var min = 100;
        for (int i = 0; i < attackDice.Count; i++)
        {
            if (attackDice[i].Item1 < min) min = attackDice[i].Item1;
        }
        
        for (int i = 0; i < attackDice.Count; i++)
        {
            if (attackDice[i].Item1 == min)
            {
                var a = attackDice[i];
                a.Item1 = Rnd.Instance.D6;
                attackDice[i] = a;
            }
        }
    }

    public override void ApplyOnRolledAttack(ICharacter attacker, ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice)
    {
        var min = 100;
        for (int i = 0; i < attackDice.Count; i++)
        {
            if (attackDice[i].Item1 < min) min = attackDice[i].Item1;
        }
        
        for (int i = 0; i < attackDice.Count; i++)
        {
            if (attackDice[i].Item1 == min)
            {
                var a = attackDice[i];
                a.Item1 = Rnd.Instance.D6;
                Console.WriteLine($"WISE: Rerolling {attackDice[i].Item1} into {a.Item1}");
                attackDice[i] = a;
            }
        }
    }
}