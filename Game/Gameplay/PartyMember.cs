using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using static SINEATER.Game.CoreUtils.Extensions;

namespace SINEATER.Game.Gameplay;

public enum ECharacterClass
{
    Wizard = 0,
    Witch,
    Brute,
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
            case ECharacterClass.Brute:
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
            case ECharacterClass.Brute:
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

public class PartyMember : Character
{
    public PartyMember(ECharacterClass? job = null)
    {
        if (job == null)
        {
            Job = Enum<ECharacterClass>.Random();
        }
        else
        {
            Job = job.Value;
        }
        
        Guard = 0;
    }

    public bool Details { get; set; }
}

public record struct Party
{
    public readonly PartyMember[] Characters = new PartyMember[4];
    public Inventory Inventory { get; } = new();
    
    public Party()
    {}

    public void MakeParty()
    {
        var jobs = new[]
        {
            ECharacterClass.Brute,
            ECharacterClass.Witch,
            ECharacterClass.Wizard,
            ECharacterClass.Monk,
            // ECharacterClass.Sage,
            // ECharacterClass.Priest,
            // ECharacterClass.Thief,
        };
        jobs.Shuffle();
        var queue = new Queue<ECharacterClass>(jobs);
        for (var i = 0; i < 4; i++)
        {
            Characters[i] = new PartyMember(queue.Dequeue());
            
            switch (Characters[i].Job)
            {
                case ECharacterClass.Wizard:
                    Characters[i].Equip(Items.Instance.Make("AshBranch"));
                    Characters[i].Equip(Items.Instance.Make("Dagger"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Witch:
                    Characters[i].Equip(Items.Instance.Make("Dagger"));
                    Characters[i].Stats.Will = 4;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 1;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Brute:
                    Characters[i].Equip(Items.Instance.Make("LongSword"));
                    Characters[i].Stats.Will = 3;
                    Characters[i].Stats.Clarity = 1;
                    Characters[i].Stats.Poise = 5;
                    Characters[i].Stats.Vigor = 5;
                    break;
                case ECharacterClass.Monk:
                    Characters[i].Equip(Items.Instance.Make("ThornWhip"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 6;
                    break;
                case ECharacterClass.Sage:
                    Characters[i].Equip(Items.Instance.Make("ThornWhip"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 3;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Priest:
                    Characters[i].Equip(Items.Instance.Make("ThornWhip"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 4;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Thief:
                    Characters[i].Equip(Items.Instance.Make("Dagger"));
                    Characters[i].Stats.Will = 6;
                    Characters[i].Stats.Clarity = 6;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 2;
                    break;
            }
        }
    }
}
