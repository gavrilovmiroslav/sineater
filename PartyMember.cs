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
    
    public static (int, int) GetImage(this ECharacterClass job, bool selected = false)
    {
        var dy = selected ? -4 : 0;
        switch (job)
        {
            case ECharacterClass.Wizard:
                return (0, 64 + dy);
            case ECharacterClass.Witch:
                return (4, 67 + dy);
            case ECharacterClass.Knight:
                return (4, 65 + dy);
            case ECharacterClass.Monk:
                return (1, 64 + dy);
            case ECharacterClass.Sage:
                return (2, 65 + dy);
            case ECharacterClass.Priest:
                return (6, 65 + dy);
            case ECharacterClass.Thief:
                return (3, 65 + dy);
            default:
                throw new ArgumentOutOfRangeException(nameof(job), job, null);
        }
    }
}

public enum EStat
{
    Will = 1,
    Clarity = 2,
    Poise = 3,
    Vigor = 4
}

public class Stats
{
    public int Will;
    public int Clarity;
    public int Poise;
    public int Vigor;
    
    public int Score => Will + Clarity + Poise + Vigor;

    public Stats()
    {
        var bag = Rnd.Instance.Bag((i => i > 1), 6, 6, 6, 6);
        
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
    
    public int Mod(EStat stat)
    {
        return this[stat] switch
        {
            < 3 => 1,  
            <= 5 => 2,
            <= 8 => 3,
            <= 10 => 4,
            _ => 5
        };
    }
}

public interface ICharacter
{
    public Inventory Inventory { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Color GetTint();
    public ActionPoints GetAP();
    public void EquipLeftWeapon(Weapon? weapon);
    public Weapon? GetLeftWeapon();
    public void EquipRightWeapon(Weapon? weapon);
    public Weapon? GetRightWeapon();
    public Armor? GetArmor();
    public List<Trait> GetTraits();
    public IEnumerable AddTrait(Trait trait);
    public bool IsStunned();
    
    public string GetName();
    (int, int) GetPortait();
    void Die();
    void RemoveArmor();
}

public class Dummy : ICharacter
{
    public IEnumerable AsAttacker_OnAttackDiceCount(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnAttackDiceCount(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnAttackDiceRolled(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnAttackDiceRolled(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnGuardUp(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnGuardUp(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnCritChanceEstablished(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnCritChanceEstablished(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnCritHit(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnCritHit(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnGuardBreak(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnGuardBreak(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnArmorDented(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnArmorDented(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnArmorBreak(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnArmorBreak(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnDamageAnnounced(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnDamageAnnounced(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_OnPoiseBroken(SkirmishFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_OnPoiseBroken(SkirmishFlow flow)
    {
        yield break;
    }

    public Inventory Inventory { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Color GetTint()
    {
        return Color.White;
    }

    public ActionPoints GetAP()
    {
        return null;
    }

    public void EquipLeftWeapon(Weapon? weapon)
    {
    }

    public Weapon? GetLeftWeapon()
    {
        return null;
    }

    public void EquipRightWeapon(Weapon? weapon)
    {
    }

    public Weapon? GetRightWeapon()
    {
        return null;
    }

    public Armor? GetArmor()
    {
        return null;
    }

    public List<Trait> GetTraits()
    {
        return [];
    }

    public IEnumerable AddTrait(Trait trait)
    {
        yield break;
    }

    public bool IsStunned()
    {
        return false;
    }

    public string GetName()
    {
        return "";
    }

    public (int, int) GetPortait()
    {
        return (0, 0);
    }

    public void Die()
    {
    }

    public void RemoveArmor()
    {
    }
}

public abstract class Character : ICharacter
{
    public static Dummy Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    
    public bool IsDone { get; set; } = false;

    public int Index;
    public Color Tint;
    public ActionPoints AP;
    public ECharacterClass Job;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;
    public IItem? Item = null;
    public Ability? Ability = null;
    public readonly List<Trait> Traits = [];
    public Inventory Inventory { get; set; } = new();

    public IEnumerable AddTrait(Trait trait)
    {
        var alreadyHasIt = Traits.Any(t => t.GetName() == trait.GetName());
        if (trait is LimitedTrait lt && alreadyHasIt)
        {
            foreach (var lim in Traits.Where(t => t.GetName() == trait.GetName()))
            {
                if (lim is LimitedTrait limt)
                {
                    limt.Duration += lt.Duration;
                }
            }
        }
        else if (!alreadyHasIt)
        {
            Traits.Add(trait);
            yield return trait.ApplyOnReceived(this);
        }
    }
    
    public virtual Color GetTint()
    {
        return Tint;
    }

    public int X { get; set; }
    public int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; } = true;
    
    public Stats Stats { get; set; } = new();

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
        return AP.Contains<StatusDeath>();
    }
    
    public virtual string GetName()
    {
        return Job.ToString();
    }
    
    public virtual (int, int) GetPortait()
    {
        return Job.GetPortrait();
    }

    public virtual void Die()
    {}

    public void EquipLeftWeapon(Weapon? weapon)
    {
        if (LeftWeapon != null)
        {
            foreach (var e in LeftWeapon.ApplyItemUnequipped(this))
            { }
        }
        LeftWeapon = weapon;
        if (weapon != null)
        {
            foreach (var e in weapon.ApplyItemEquipped(this))
            { }
        }
    }
    
    public void EquipRightWeapon(Weapon? weapon)
    {
        if (RightWeapon != null)
        {
            foreach (var e in RightWeapon.ApplyItemUnequipped(this))
            { }
        }
        
        RightWeapon = weapon;
        if (weapon != null)
        {
            foreach (var e in weapon.ApplyItemEquipped(this))
            { }
        }
    }
    
    public void EquipArmor(Armor? armor)
    {
        Armor = armor;
    }
    
    public void EquipItem(IItem? item)
    {
        Item = item;
    }

    public void RemoveArmor()
    {
        this.EquipArmor(null);
    }
    
    public void RemoveItem()
    {
        this.EquipItem(null);
    }
}

public class PartyMember : Character
{
    public PartyMember(ECharacterClass? job = null)
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

        HP = Stats.Poise + Rnd.Instance.D2;
    }
    
    public string GetRandomBark()
    {
        var barks = Barks.Instance[this.Job];
        return barks[Rnd.Instance.Next(0, barks.Length)];
    }
}

public record struct Party
{
    private static readonly Color[] Colors = [Color.Yellow, Color.GreenYellow, Color.CornflowerBlue, Color.Coral];
    public PartyMember[] Characters = new PartyMember[4];

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
            Characters[i] = new PartyMember(queue.Dequeue())
            {
                Index = i,
                Tint = Colors[i],
                AP = AP
            };
            switch (Characters[i].Job)
            {
                default:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Wizard Staff"));
                    // Characters[i].EquipArmor(ItemLibrary.GetArmor("Chainmail"));
                    // Characters[i].EquipItem(ItemLibrary.GetItem("Ancient Scroll"));
                    Characters[i].Stats.Vigor -= 2;
                    if (Characters[i].Stats.Vigor <= 0) Characters[i].Stats.Vigor = 1;
                    break;
                // case ECharacterClass.Witch:
                //     Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Dagger"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Tunic"));
                //     Characters[i].Stats.Clarity++;
                //     Characters[i].Ability = new DomainExpansion();
                //     break;
                // case ECharacterClass.Knight:
                //     Characters[i].EquipLeftWeapon(ItemLibrary.GetShield("Round Shield"));
                //     Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Claymore"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Plate Armor"));
                //     Characters[i].EquipItem(ItemLibrary.GetItem("Family Ring"));
                //     break;
                // case ECharacterClass.Monk:
                //     Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Skolem Staff"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Leather Armor"));
                //     Characters[i].Traits.Add(new TraitHeavy());
                //     break;
                // case ECharacterClass.Sage:
                //     Characters[i].EquipItem(ItemLibrary.GetWeapon("Misericorde"));
                //     Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Scroll Tome"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Breast Plate"));
                //     Characters[i].EquipItem(new ItemStack(new PotionBloodReliquary(), 3));
                //     break;
                // case ECharacterClass.Priest:
                //     Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Thorn Whip"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Robe"));
                //     Characters[i].Stats.Vigor++;
                //     break;
                // case ECharacterClass.Thief:
                //     Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Dagger"));
                //     Characters[i].EquipArmor(ItemLibrary.GetArmor("Cloak"));
                //     Characters[i].EquipItem(new PotionGhylagsTear());
                //     Characters[i].EquipItem(ItemLibrary.GetItem("Sword of Old"));
                //     Characters[i].Stats.Vigor -= 1;
                //     if (Characters[i].Stats.Vigor <= 0) Characters[i].Stats.Vigor = 1;
                //     break;
                // default:
                //     throw new ArgumentOutOfRangeException();
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
