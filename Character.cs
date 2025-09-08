using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using static SINEATER.Extensions;

namespace SINEATER;

public enum ECharacterClass
{
    Wizard,
    Witch,
    Knight,
    Monk,
    Sage,
    Priest,
    Thief
}

public static class ECharacterClassExtensions
{
    public static (int, int) GetImage(this ECharacterClass job)
    {
        switch (job)
        {
            case ECharacterClass.Wizard:
                return (0, 64);
            case ECharacterClass.Witch:
                return (4, 67);
            case ECharacterClass.Knight:
                return (4, 65);
            case ECharacterClass.Monk:
                return (1, 64);
            case ECharacterClass.Sage:
                return (2, 65);
            case ECharacterClass.Priest:
                return (6, 65);
            case ECharacterClass.Thief:
                return (3, 65);
            default:
                throw new ArgumentOutOfRangeException(nameof(job), job, null);
        }
    }
}

public enum EStat
{
    Will,
    Clarity,
    Poise,
    Vigor
}

public record struct Stats
{
    public int Will;
    public int Clarity;
    public int Poise;
    public int Vigor;
    
    public int Score => Will + Clarity + Poise + Vigor;

    public Stats()
    {
        var bag = Rnd.Instance.Bag((i => i >= 2), 6, 6, 6, 8);
        
        Will = bag[0];
        Clarity = bag[1];
        Poise = bag[2];
        Vigor = bag[3];
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
                case EStat.Vigor: return Vigor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat), stat, null);
            }
        }
    }
    
    public readonly int Mod(EStat stat)
    {
        return this[stat] switch
        {
            < 3 => -1,  
            < 5 => 0,
            < 8 => 1,
            < 10 => 2,
            _ => 3
        };
    }
}

public interface ICharacter
{
    public Stats GetStats();
    public ActionPoints GetAP();
    public Weapon? GetLeftWeapon();
    public Weapon? GetRightWeapon();
    public Armor? GetArmor();
    public bool IsStunned();
    void ApplyOnAttackRoll(ICharacter defender, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice);
    void ApplyOnRolledAttack(ICharacter attacker, ref List<(int, Weapon)> attackDice, ref List<(int, Armor)> defenseDice);
    void ApplyOnAttackBlocked(ICharacter attacker, (int attack, Weapon weapon) attackValue, (int defense, Armor armor) defenseValue);
    void ApplyOnSuccessfulBlock(ICharacter attacker, int attack, Weapon weapon);
    void ApplyOnWounded(ICharacter attacker, int wounds);
    void ApplyOnCausedWounds(ICharacter defender, int wounds);
    string GetName();
}

public class Character : ICharacter
{
    public int Index;
    public Color Tint;
    public ActionPoints AP;
    public ECharacterClass Job;
    public Stats Stats = new();
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;

    public Character(ECharacterClass? job = null)
    {
        if (job == null)
        {
            Job = Enum<ECharacterClass>.Random();
            Console.WriteLine($"Created character with {Stats} and random class: {Job}");
        }
        else
        {
            Job = job.Value;
            Console.WriteLine($"Created character with {Stats} and class: {Job}");
        }
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

    public string GetName()
    {
        return Job.ToString();
    }
}

public record struct Party
{
    private static readonly Color[] Colors = [Color.Yellow, Color.GreenYellow, Color.CornflowerBlue, Color.Crimson];
    public Character[] Characters = new Character[4];

    public Party(ActionPoints AP)
    {
        var jobs = new[]
        {
            ECharacterClass.Wizard,
            ECharacterClass.Witch,
            ECharacterClass.Knight,
            ECharacterClass.Monk,
            ECharacterClass.Sage,
            ECharacterClass.Priest,
            ECharacterClass.Thief,
        };
        jobs.Shuffle();
        var queue = new Queue<ECharacterClass>(jobs);
        for (var i = 0; i < 4; i++)
        {
            Characters[i] = new Character(queue.Dequeue())
            {
                Index = i,
                Tint = Colors[i],
                AP = AP
            };
            switch (Characters[i].Job)
            {
                case ECharacterClass.Wizard:
                    Characters[i].LeftWeapon = new Weapon("Staff", 2, EWeightClass.Heavy, 1);
                    Characters[i].Armor = new Armor("Robe", 2, EWeightClass.Heavy, 1);
                    break;
                case ECharacterClass.Witch:
                    Characters[i].RightWeapon = new Weapon("Dagger", 2,EWeightClass.Small, 4);
                    Characters[i].Armor = new Armor("Veil", 3, EWeightClass.Medium, 2);
                    break;
                case ECharacterClass.Knight:
                    Characters[i].RightWeapon = new Weapon("Sword", 6, EWeightClass.Large, 4);
                    Characters[i].Armor = new Armor("Plate", 9, EWeightClass.Heavy, 4);
                    break;
                case ECharacterClass.Monk:
                    Characters[i].RightWeapon = new Weapon("Staff", 5, EWeightClass.Heavy, 1);
                    Characters[i].Armor = new Armor("Robe", 1, EWeightClass.Tiny, 1);
                    break;
                case ECharacterClass.Sage:
                    Characters[i].LeftWeapon = new Weapon("Dagger", 3, EWeightClass.Tiny, 3);
                    Characters[i].RightWeapon = new Weapon("Book", 2, EWeightClass.Heavy, 5);
                    Characters[i].Armor = new Armor("Robe", 2, EWeightClass.Medium, 1);
                    break;
                case ECharacterClass.Priest:
                    Characters[i].LeftWeapon = new Weapon("Sceptre", 3, EWeightClass.Heavy, 8);
                    Characters[i].Armor = new Armor("Robe", 2, EWeightClass.Medium, 4);
                    break;
                case ECharacterClass.Thief:
                    Characters[i].LeftWeapon = new Weapon("Dagger", 2, EWeightClass.Tiny, 7);
                    Characters[i].RightWeapon = new Weapon("Sword", 4, EWeightClass.Medium, 7);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}