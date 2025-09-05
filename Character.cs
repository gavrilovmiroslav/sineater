using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            < 5 => 1,
            < 8 => 2,
            < 11 => 3,
            _ => 4
        };
    }
}

public record struct Character
{
    public ECharacterClass Job;
    public Stats Stats = new();

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
}

public record struct Party
{
    public Character[] Characters = new Character[4];

    public Party()
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
        for (var i = 0; i < 4; i++) Characters[i] = new Character(queue.Dequeue());
    }
}