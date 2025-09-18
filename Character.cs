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
    public static (int, int) GetPortrait(this ECharacterClass job)
    {
        switch (job)
        {
            case ECharacterClass.Wizard:
                return (3, 0);
            case ECharacterClass.Witch:
                return (2, 0);
            case ECharacterClass.Knight:
                return (4, 0);
            case ECharacterClass.Monk:
                return (2, 1);
            case ECharacterClass.Sage:
                return (3, 1);
            case ECharacterClass.Priest:
                return (1, 1);
            case ECharacterClass.Thief:
                return (0, 1);
            default:
                throw new ArgumentOutOfRangeException(nameof(job), job, null);
        }
    }
    
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

    public Stats(int wil, int cla, int poi, int vig)
    {
        Will = wil;
        Clarity = cla;
        Poise = poi;
        Vigor = vig;
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

public interface ICharacter : ICombatFlowParticipant
{
    public Stats GetStats();
    public Color GetTint();
    public ActionPoints GetAP();
    public Weapon? GetLeftWeapon();
    public Weapon? GetRightWeapon();
    public Armor? GetArmor();
    public List<Trait> GetTraits();
    public bool IsStunned();
    
    string GetName();
    (int, int) GetPortait();
    void Die();
    void RemoveArmor();
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
    public List<Trait> Traits = [];

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

    public Color GetTint()
    {
        return Tint;
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
    
    public string GetName()
    {
        return Job.ToString();
    }

    public void Die()
    {}

    public void RemoveArmor()
    {
        this.Armor = null;
    }

    public IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyDiceCountModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyDiceCountModifiers(flow);
    }

    public IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyCombatModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyCombatModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyCombatModifiers(flow);
    }

    public IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyStrikeModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyStrikeModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyStrikeModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyStrikeBlocked(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyStrikeBlocked(flow);
    }

    public IEnumerable AsDefender_ApplyArmorDented(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyArmorDented(flow);
    }

    public IEnumerable AsAttacker_ApplyLeftWeaponShattered(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsAttacker_ApplyLeftWeaponShattered(flow);
        this.LeftWeapon = null;
    }

    public IEnumerable AsAttacker_ApplyRightWeaponShattered(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsAttacker_ApplyRightWeaponShattered(flow);
        this.RightWeapon = null;
    }

    public IEnumerable AsDefender_ApplyArmorDestroyed(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsDefender_ApplyArmorDestroyed(flow);
    }

    public IEnumerable AsAttacker_ApplyHitModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyHitModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyHitModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyHitModifiers(flow);
    }

    public IEnumerable AsAttacker_DetermineHitDieDamage(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_DetermineHitDieDamage(flow);
    }

    public IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_DetermineHitDieDamage(flow);
    }

    public IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyTotalIncomingDamageModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyTotalIncomingDamageModifiers(flow);
    }

    public string GetRandomBark()
    {
        var barks = Barks.Instance[this.Job];
        return barks[Rnd.Instance.Next(0, barks.Length)];
    }

    public (int, int) GetPortait()
    {
        return Job.GetPortrait();
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
                    Characters[i].Stats.Vigor -= 2;
                    if (Characters[i].Stats.Vigor <= 0) Characters[i].Stats.Vigor = 1;
                    break;
                case ECharacterClass.Witch:
                    Characters[i].RightWeapon = new Weapon("Dagger", 2,EWeightClass.Small, 4);
                    Characters[i].Armor = new Armor("Veil", 3, EWeightClass.Medium, 2);
                    Characters[i].Traits.Add(new TraitSneaky());
                    SineaterGame.Instance.Inventory.Put(new PotionBloodReliquary());
                    break;
                case ECharacterClass.Knight:
                    Characters[i].RightWeapon = new Weapon("Sword", 4, EWeightClass.Large, 4);
                    Characters[i].Armor = new Armor("Plate", 5, EWeightClass.Heavy, 4);
                    break;
                case ECharacterClass.Monk:
                    Characters[i].RightWeapon = new Weapon("Staff", 3, EWeightClass.Heavy, 1);
                    Characters[i].Armor = new Armor("Robe", 1, EWeightClass.Tiny, 1);
                    Characters[i].Traits.Add(new TraitHeavy());
                    break;
                case ECharacterClass.Sage:
                    Characters[i].LeftWeapon = new Weapon("Dagger", 2, EWeightClass.Tiny, 3);
                    Characters[i].RightWeapon = new Weapon("Book", 2, EWeightClass.Heavy, 5);
                    Characters[i].Armor = new Armor("Robe", 2, EWeightClass.Medium, 1);
                    for (var n = 0; n < 3; n++)
                        SineaterGame.Instance.Inventory.Put(new PotionBloodReliquary());
                    break;
                case ECharacterClass.Priest:
                    Characters[i].LeftWeapon = new Weapon("Sceptre", 3, EWeightClass.Heavy, 8);
                    Characters[i].Armor = new Armor("Robe", 2, EWeightClass.Medium, 4);
                    break;
                case ECharacterClass.Thief:
                    Characters[i].LeftWeapon = new Weapon("Dagger", 2, EWeightClass.Tiny, 7);
                    Characters[i].RightWeapon = new Weapon("Sword", 3, EWeightClass.Medium, 7);
                    Characters[i].Traits.Add(new TraitSkilled());
                    Characters[i].Stats.Vigor -= 1;
                    if (Characters[i].Stats.Vigor <= 0) Characters[i].Stats.Vigor = 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public int WorldSight {
        get
        {
            int max = 0;
            foreach (var character in Characters)
            {
                var m = 0;
                var c = character.Stats.Clarity;
                if (c > 10) m = 3;
                if (c > 0) m = 2;
                if (m > max) max = m;
            }

            return max;
        }
    }

    public int Selected { get; set; } = -1;
}