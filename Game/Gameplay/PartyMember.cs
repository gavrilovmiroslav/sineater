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

    public string GetRandomBark()
    {
        var barks = Barks.Instance[this.Job];
        return barks[Rnd.Instance.Next(0, barks.Length)];
    }

    public void SetOrigin()
    {
        Origin = (X, Y);
    }
}

public record struct Party
{
    private static readonly Color[] Colors = [Color.ForestGreen, Color.GreenYellow, Color.CornflowerBlue, Color.Lerp(Color.Pink, Color.Purple, 0.5f)];
    public static readonly Color[] Zones = [new Color(34, 100, 34), new Color(100, 150, 34), new Color(30, 30, 100), Color.Lerp(Color.Purple, Color.Black, 0.5f)];
    public readonly PartyMember[] Characters = new PartyMember[4];
    public Inventory Inventory { get; set; }
    public Party()
    {
        Inventory = ItemLibrary.CreateDefaultInventory();
    }

    public void MakeParty()
    {
        var jobs = new[]
        {
            ECharacterClass.Witch,
            ECharacterClass.Wizard,
            ECharacterClass.Knight,
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
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Ash Branch"));
                    Characters[i].EquipAndAdd( ItemLibrary.GetItem("Dagger"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Witch:
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Dagger"));
                    Characters[i].Stats.Will = 4;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 1;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Knight:
                    Characters[i].EquipAndAdd(ItemLibrary.GetWeapon("Long Sword"));
                    Characters[i].Stats.Will = 3;
                    Characters[i].Stats.Clarity = 1;
                    Characters[i].Stats.Poise = 5;
                    Characters[i].Stats.Vigor = 5;
                    break;
                case ECharacterClass.Monk:
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Thorn Whip"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 2;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 6;
                    break;
                case ECharacterClass.Sage:
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Thorn Whip"));
                    Characters[i].Stats.Will = 2;
                    Characters[i].Stats.Clarity = 5;
                    Characters[i].Stats.Poise = 3;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Priest:
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Thorn Whip"));
                    Characters[i].Stats.Will = 5;
                    Characters[i].Stats.Clarity = 4;
                    Characters[i].Stats.Poise = 2;
                    Characters[i].Stats.Vigor = 3;
                    break;
                case ECharacterClass.Thief:
                    Characters[i].EquipAndAdd(ItemLibrary.GetItem("Dagger"));
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
