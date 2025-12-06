using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SINEATER.MoveLibrary;
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
    public static string GetShortName(this ECharacterClass job)
    {
        switch (job)
        {
            case ECharacterClass.Wizard:
                return "WZD";
            case ECharacterClass.Witch:
                return "WIT";
            case ECharacterClass.Knight:
                return "KNT";
            case ECharacterClass.Monk:
                return "MNK";
            case ECharacterClass.Sage:
                return "SAG";
            case ECharacterClass.Priest:
                return "PRI";
            case ECharacterClass.Thief:
                return "THF";
            default:
                throw new ArgumentOutOfRangeException(nameof(job), job, null);
        }
    }
    
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
    public int Initiative => Will + Vigor;
    public int Fortitude => Clarity + Poise;

    public Stats(ICharacter chr)
    {
        Will = chr.Stats.Will;
        Clarity = chr.Stats.Clarity;
        Poise = chr.Stats.Poise;
        Vigor = chr.Stats.Vigor;
    }

    public Stats(Stats other)
    {
        Will = other.Will;
        Clarity = other.Clarity;
        Poise = other.Poise;
        Vigor = other.Vigor;
    }
    
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

    public void Reset()
    {
        Will = 0;
        Clarity = 0;
        Poise = 0;
        Vigor = 0;
    }
}

public interface ICharacter
{
    public int X { get; set; }
    public int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; }
    public List<string> Tags { get; set; }
    public Color GetTint();
    public void EquipLeftWeapon(Weapon? weapon);
    public Weapon? GetLeftWeapon();
    public void EquipRightWeapon(Weapon? weapon);
    public Weapon? GetRightWeapon();
    public void EquipItem(Item? item);
    public Item? GetItem();
    public AP GetAP();
    
    public string GetName();
    (int, int) GetPortait();
    void Die();
    public bool IsDone { get; set; }
}

public class Dummy : ICharacter
{
    public int X { get; set; }
    public int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; } = new(0, 0, 0, 0);
    public List<string> Tags { get; set; } = [];

    public Color GetTint()
    {
        return Color.White;
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

    public void EquipItem(Item? item)
    {
    }

    public Item? GetItem()
    {
        return null;
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

    public bool IsDone { get; set; } = false;
    public AP GetAP()
    {
        return null;
    }
}

public record struct Attack(List<Weapon> Weapons, StatsScaling StatScaling, StatusScaling StatusScaling, Action<Character, Attack, CombatMapScreen>? AttackProc = null);

public interface IStatus
{
    public IEnumerable OnActivated(Character c, CombatMapScreen w);
    public IEnumerable OnMove(Character c, CombatMapScreen w);
    public IEnumerable OnAttack(Character c, Character o, CombatMapScreen w);
    public IEnumerable OnDamage(Character c, CombatMapScreen w);
    public IEnumerable OnDeactivated(Character c, CombatMapScreen w);
}

public abstract class Character : ICharacter
{
    public static Dummy Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    public List<string> Tags { get; set; } = [];
    public string? SelectedMove = null;
    public bool CanSwapEnemies { get; set; } = false;
    public List<Attack> Attacks { get; set; } = [];
    public List<IStatus> Statuses { get; set; } = [];
    public int MovesLeft { get; set; } = 0;
    public bool IsDone { get; set; } = false;
    public bool IsRightHanded { get; set; } = true;
    
    public List<Move> Moves = [];
    
    public bool CanPay(MoveCost[] costs)
    {
        var stamina = AP.Count(EStatus.Stamina);
        var fatigue = AP.Count(EStatus.Fatigue);
        var fire = AP.Count(EStatus.Fire);
        var ice = AP.Count(EStatus.Frozen);
        var wound = AP.Count(EStatus.Death);
        var death = AP.Count(EStatus.Death);
        var sin  = AP.Count(EStatus.Sin);

        foreach (var part in costs)
        {
            switch (part)
            {
                case MoveCost.Stamina:
                    stamina--;
                    break;
                case MoveCost.Fatigue:
                    fatigue--;
                    break;
                case MoveCost.Fire:
                    fire--;
                    break;
                case MoveCost.Ice:
                    ice--;
                    break;
                case MoveCost.Wound:
                    wound--;
                    break;
                case MoveCost.Death:
                    death--;
                    break;
                case MoveCost.Sin:
                    sin--;
                    break;
                default:
                    break;
            }
        }

        return !(stamina < 0 || fatigue < 0 || fire < 0 || ice < 0 || wound < 0 || death < 0 || sin < 0);
    }
    
    public IEnumerable<Item> GetGear()
    {
        if (GetItem() is { } item)
            yield return item;

        if (!IsRightHanded)
        {
            if (GetLeftWeapon() is { } lhs)
                yield return lhs;
            if (GetRightWeapon() is { } rhs)
                yield return rhs;
        }
        else
        {
            if (GetRightWeapon() is { } rhs)
                yield return rhs;
            if (GetLeftWeapon() is { } lhs)
                yield return lhs;
        }
    }
    
    public float Weight
    {
        // =MAX(3,IFERROR(I8/ATK_LH_LEVEL,0)+IFERROR(I9/ATK_RH_LEVEL,0)+I10)
        get
        {
            var weight = 0.0f;
            if (GetLeftWeapon() is { } lw)
            {
                weight += (int)lw.Weight / (float)lw.Level;
            }
            
            if (GetRightWeapon() is { } rw)
            {
                weight += (int)rw.Weight / (float)rw.Level;
            }
            
            if (GetItem() is { } it)
            {
                weight += (int)it.Weight;
            }

            return Math.Max(3.0f, weight);
        }
    }
    
    public float WeightFactor => (Cla + Poi) / Math.Max(0.1f, Weight);
    
    public Item? GetItem()
    {
        return Item;
    }

    public AP GetAP()
    {
        return AP;
    }

    public Stats Bonus { get; set; } = new(0, 0, 0, 0);
    public Stats Temp  { get; set; } = new(0, 0, 0, 0);
    
    public int Wil => Stats.Will + Bonus.Will + Temp.Will;
    public int Cla => Stats.Clarity + Bonus.Clarity + Temp.Clarity;
    public int Poi => Stats.Poise + Bonus.Poise + Temp.Poise;
    public int Vig => Stats.Vigor + Bonus.Vigor + Temp.Vigor;
    
    public int Index;
    public Color Tint;
    public ECharacterClass Job;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Item? Item = null;
    public Ability? Ability = null;
    public AP AP;
    
    public virtual Color GetTint()
    {
        return Tint;
    }

    public virtual int X { get; set; }
    public virtual int Y { get; set; }
    public int HP { get; set; }
    public bool Render { get; set; } = true;
    
    public Stats Stats { get; set; } = new();
    
    public Weapon? GetLeftWeapon()
    {
        return LeftWeapon;
    }

    public Weapon? GetRightWeapon()
    {
        return RightWeapon;
    }

    public void EquipItem(Item? item)
    {
        Item = item;
    }
    
    public bool IsStunned()
    {
        return false;
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
        LeftWeapon = weapon;
    }
    
    public void EquipRightWeapon(Weapon? weapon)
    {
        RightWeapon = weapon;
    }
    
    public virtual void Done()
    {
        IsDone = true;
    }

    public void RemoveItem()
    {
        this.EquipItem(null);
    }
}

public class PartyMember : Character
{
    public HashSet<(int X, int Y)> Zone = [];
    public HashSet<(int X, int Y)> Fov = [];
    public (int X, int Y) Origin = (0, 0);
    
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

        HP = Math.Min(Stats.Poise + Rnd.Instance.D2, 9);
    }
    
    public string GetRandomBark()
    {
        var barks = Barks.Instance[this.Job];
        return barks[Rnd.Instance.Next(0, barks.Length)];
    }
    
    public override void Done()
    {
        
    }

    public void SetOrigin()
    {
        Origin = (X, Y);
    }

    public override void Die()
    {
        
    }

}

public record struct Party
{
    private static readonly Color[] Colors = [Color.ForestGreen, Color.GreenYellow, Color.CornflowerBlue, Color.Lerp(Color.Pink, Color.Purple, 0.5f)];
    public static readonly Color[] Zones = [new Color(34, 100, 34), new Color(100, 150, 34), new Color(30, 30, 100), Color.Lerp(Color.Purple, Color.Black, 0.5f)];
    public readonly PartyMember[] Characters = new PartyMember[4];
    
    public Party(AP actionPoints)
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
                AP = actionPoints,
            };
            
            Characters[i].Moves.Add(new Walk());
            Characters[i].Moves.Add(new Strike());
            
            switch (Characters[i].Job)
            {
                case ECharacterClass.Wizard:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Ash Branch"));
                    Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Fire Scroll"));
                    Characters[i].EquipItem(ItemLibrary.GetItem("Flame Band"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Witch:
                    Characters[i].IsRightHanded = true;
                    Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Kris"));
                    Characters[i].EquipItem(ItemLibrary.GetItem("Old Bell"));
                    Characters[i].Stats.Will = 4;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 1;
                    Characters[i].Stats.Vigor = 3;
                    Characters[i].Moves.Add(new OpenDomain());
                    break;
                case ECharacterClass.Knight:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Red Sign"));
                    Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Claymore"));
                    Characters[i].EquipItem(ItemLibrary.GetItem("Ruby Plate"));
                    Characters[i].Stats.Will = 3;
                    Characters[i].Stats.Clarity = 1;
                    Characters[i].Stats.Poise = 5;
                    Characters[i].Stats.Vigor = 5;
                    Characters[i].Moves.RemoveAt(1);
                    Characters[i].Moves.Add(new Chop());
                    Characters[i].Moves.Add(new Bash());
                    break;
                case ECharacterClass.Monk:
                    Characters[i].EquipRightWeapon(ItemLibrary.GetWeapon("Skolm Staff"));
                    Characters[i].EquipItem(ItemLibrary.GetItem("Tunic"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 6;
                    break;
                case ECharacterClass.Sage:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Misericorde"));
                    Characters[i].EquipItem(ItemLibrary.GetItem("Sash"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 3;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Priest:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Thorn Whip"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 4;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Thief:
                    Characters[i].EquipLeftWeapon(ItemLibrary.GetWeapon("Dagger"));
                    Characters[i].Stats.Will = 6;
                    Characters[i].Stats.Clarity = 6;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 2;
                    Characters[i].Moves.Add(new Steal());
                    break;
            }
        }

        Characters[0].Stats.Will = 7;
        Characters[1].Stats.Clarity = 7;
        Characters[2].Stats.Poise = 7;
        Characters[3].Stats.Vigor = 7;

        for (var i = 0; i < 4; i++)
        {
            Characters[i].HP = Math.Min(9, Characters[i].Poi + Characters[i].Cla);
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
