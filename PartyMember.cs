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
    None = 0,
    Vigor = 1,
    Will = 2,
    Clarity = 3,
    Poise = 4
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

    public int this[int n]
    {
        get
        {
            switch (n)
            {
                case 1: return Will;
                case 2: return Clarity;
                case 3: return Poise;
                case 0: return Vigor;
                default:
                    return 0;
            }
        }
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

    public EStat Highest()
    {
        List<(EStat stat, int val)> stats = [(EStat.Will, Will), (EStat.Clarity, Clarity), (EStat.Poise, Poise), (EStat.Vigor, Vigor)];
        var max = stats.MaxBy((w) => w.val);
        return max.stat;
    }
}

public interface ICharacter
{
    public int X { get; set; }
    public int Y { get; set; }
    public Track Guard { get; set; }    
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; }
    public List<string> Tags { get; set; }
    public Color GetTint();
    public void Equip(EStat stat, Item? item);
    public void Equip(Item item);
    public Item? GetItem(EStat stat);
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
    public Track Hits { get; set; }
    public Track Guard { get; set; }
    public bool Render { get; set; }
    public Stats Stats { get; set; }
    public Stats Bonus { get; set; } = new(0, 0, 0, 0);
    public List<string> Tags { get; set; } = [];

    public Color GetTint()
    {
        return Color.White;
    }

    public void Equip(EStat stat, Item? item)
    {
    }

    public void Equip(Item item)
    {
    }

    public Item? GetItem(EStat stat)
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

public record struct Attack(List<Weapon> Weapons, EStatus[] Mods, StatsScaling StatScaling = default, 
    Func<Character, Attack, Character, CombatMapScreen, IEnumerable>? AttackProc = null);

public interface IStatus
{
    public IEnumerable OnActivated(Character c, CombatMapScreen w);
    public IEnumerable OnMove(Character c, CombatMapScreen w);
    public IEnumerable OnAttack(Character c, Character o, CombatMapScreen w);
    public IEnumerable OnDamage(Character c, CombatMapScreen w);
    public IEnumerable OnDeactivated(Character c, CombatMapScreen w);
}

public enum ELoudness
{
    Silent = 0,
    Quiet = 2,
    Moderate = 5,
    Loud = 8
}

public abstract class Character : ICharacter
{
    public static Dummy Dummy(int x, int y) => new Dummy() { X = x, Y = y };
    public List<string> Tags { get; set; } = [];
    public bool HasTurn { get; set; } = true;
    public string? SelectedMove { get; set; } = null;
    public bool Broken { get; set; } = false;

    public bool CheckBroken()
    {
        if (Guard == 0)
        {
            Broken = true;
            return true;
        }

        return false;
    }
    
    public void ForceRestart(Screen screen)
    {
        MovementLeft = Stats.Initiative;
        HasTurn = true;
        Attacks.Clear();
        SelectedMove = null;
        IsDone = false;
    }
    
    public List<Attack> Attacks { get; set; } = [];

    public int MovementLeft { get; set; } = 0;
    public bool IsDone { get; set; } = false;
    public bool IsRightHanded { get; set; } = true;
    
    public bool CanPay(EStatus[] costs)
    {
        var stamina = AP.Count(EStatus.Stamina);
        var fatigue = AP.Count(EStatus.Fatigue);
        var fire = AP.Count(EStatus.Fire);
        var ice = AP.Count(EStatus.Frozen);
        var wound = AP.Count(EStatus.Death);
        var death = AP.Count(EStatus.Death);
        var sin  = AP.Count(EStatus.Sin);
        var insanity  = AP.Count(EStatus.Insanity);
        
        foreach (var part in costs)
        {
            switch (part)
            {
                case EStatus.Stamina:
                    stamina--;
                    break;
                case EStatus.Fatigue:
                    fatigue--;
                    break;
                case EStatus.Fire:
                    fire--;
                    break;
                case EStatus.Frozen:
                    ice--;
                    break;
                case EStatus.Wound:
                    wound--;
                    break;
                case EStatus.Death:
                    death--;
                    break;
                case EStatus.Sin:
                    sin--;
                    break;
                case EStatus.Insanity:
                    insanity--;
                    break;
            }
        }

        return !(stamina < 0 || fatigue < 0 || fire < 0 || ice < 0 || wound < 0 || death < 0 || sin < 0 || insanity < 0);
    }
    
    public float Weight
    {
        get
        {
            var weight = 0.0f;

            foreach (var item in Items)
            {
                if (item != null)
                {
                    weight += (int)item.Weight;
                }
            }

            return weight;
        }
    }
    
    public float WeightFactor => (Cla + Poi) / Math.Max(0.1f, Weight);

    public void Equip(Item? item)
    {
        if (item != null)
            Equip(item.Stat, item);
    }

    public Item? GetItem(EStat stat)
    {
        return Items[(int)stat - 1];
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
    public Item?[] Items = new Item?[4];
    public AP AP;
    
    public virtual Color GetTint()
    {
        return Tint;
    }

    public void Equip(EStat stat, Item? item)
    {
        if (stat == EStat.None) return;
        Items[(int)stat - 1] = item;
    }

    public virtual int X { get; set; }
    public virtual int Y { get; set; }
    public Track Guard { get; set; }
    public bool Render { get; set; } = true;
    
    public Stats Stats { get; set; } = new();
    
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
    
    public virtual void Done()
    {
        IsDone = true;
    }
    
    public IEnumerable Pay(EStatus[] costs)
    {
        foreach (var cost in costs)
        {
            var place = AP.View.FindIndex(c => c == cost);
            AP.View[place] = EStatus.Void;
            yield return new WaitForSeconds(0.01f);
        }

        yield break;
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
        
        Guard = 0;
    }

    public ELoudness Loudness { get; set; } = ELoudness.Moderate;

    public string GetRandomBark()
    {
        var barks = Barks.Instance[this.Job];
        return barks[Rnd.Instance.Next(0, barks.Length)];
    }
    
    public override void Done()
    {
        IsDone = true;
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
            ECharacterClass.Monk,
            ECharacterClass.Knight,
            // ECharacterClass.Sage,
            // ECharacterClass.Priest,
            // ECharacterClass.Thief,
        };
        //jobs.Shuffle();
        var queue = new Queue<ECharacterClass>(jobs);
        for (var i = 0; i < 4; i++)
        {
            Characters[i] = new PartyMember(queue.Dequeue())
            {
                Index = i,
                Tint = Colors[i],
                AP = actionPoints,
            };
            
            switch (Characters[i].Job)
            {
                case ECharacterClass.Wizard:
                    Characters[i].Equip(ItemLibrary.GetWeapon("Ash Branch"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Witch:
                    Characters[i].IsRightHanded = true;
                    Characters[i].Equip(EStat.Will, ItemLibrary.GetWeapon("Kris"));
                    Characters[i].Equip(ItemLibrary.GetItem("Old Bell"));
                    Characters[i].Stats.Will = 4;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 1;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Knight:
                    Characters[i].Equip(ItemLibrary.GetWeapon("Red Sign"));
                    Characters[i].Equip(EStat.Clarity, ItemLibrary.GetWeapon("Thorn Whip"));
                    //Characters[i].Equip(EStat.Will, ItemLibrary.GetWeapon("Claymore"));
                    Characters[i].Equip(ItemLibrary.GetItem("Ruby Plate"));
                    Characters[i].Stats.Will = 3;
                    Characters[i].Stats.Clarity = 1;
                    Characters[i].Stats.Poise = 5;
                    Characters[i].Stats.Vigor = 5;
                    break;
                case ECharacterClass.Monk:
                    Characters[i].Equip(ItemLibrary.GetWeapon("Skolm Staff"));
                    Characters[i].Equip(ItemLibrary.GetItem("Soft Tunic"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 6;
                    break;
                case ECharacterClass.Sage:
                    Characters[i].Equip(EStat.Vigor, ItemLibrary.GetWeapon("Thorn Whip"));
                    //Characters[i].Equip(EStat.Will, ItemLibrary.GetWeapon("Misericorde"));
                    Characters[i].Equip(ItemLibrary.GetItem("Sash"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 3;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Priest:
                    Characters[i].Equip(ItemLibrary.GetWeapon("Thorn Whip"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 4;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Thief:
                    Characters[i].Equip(ItemLibrary.GetWeapon("Dagger"));
                    Characters[i].Stats.Will = 6;
                    Characters[i].Stats.Clarity = 6;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 2;
                    break;
            }
        }

        Characters[0].Stats.Will = 6;
        Characters[1].Stats.Clarity = 6;
        Characters[2].Stats.Poise = 6;
        Characters[3].Stats.Vigor = 6;
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
