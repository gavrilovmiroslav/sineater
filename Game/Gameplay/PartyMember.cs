using SINEATER.Game.CoreUtils;
using SINEATER.Game.Loadable;
using SINEATER.Game.Save;
using System;
using System.Collections.Generic;
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

public class Party
{
    public readonly PartyMember[] Characters = new PartyMember[4];
    public Inventory Inventory { get; } = new();

    public (int X, int Y) CurrentPlayerPosition = (2, 2);
    public Party()
    {
        SaveSystem.OnSaveLoaded += LoadParty;
    }

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
            InitCharacter(Characters[i], defaultEquip: true);
        }
    }

    private void InitCharacter(PartyMember character, bool defaultEquip)
    {
        switch (character.Job)
        {
            case ECharacterClass.Wizard:
                if (defaultEquip)
                {
                    character.Equip(Items.Instance.Make("AshBranch"));
                    character.Equip(Items.Instance.Make("Dagger"));
                }

                character.Stats.Will = 5;
                character.Stats.Clarity = 2;
                character.Stats.Poise = 2;
                character.Stats.Vigor = 3;
                break;
            case ECharacterClass.Witch:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("Dagger"));

                character.Stats.Will = 4;
                character.Stats.Clarity = 5;
                character.Stats.Poise = 1;
                character.Stats.Vigor = 3;
                break;
            case ECharacterClass.Brute:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("LongSword"));

                character.Stats.Will = 3;
                character.Stats.Clarity = 1;
                character.Stats.Poise = 5;
                character.Stats.Vigor = 5;
                break;
            case ECharacterClass.Monk:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("ThornWhip"));

                character.Stats.Will = 2;
                character.Stats.Clarity = 2;
                character.Stats.Poise = 2;
                character.Stats.Vigor = 6;
                break;
            case ECharacterClass.Sage:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("ThornWhip"));

                character.Stats.Will = 2;
                character.Stats.Clarity = 5;
                character.Stats.Poise = 3;
                character.Stats.Vigor = 3;
                break;
            case ECharacterClass.Priest:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("ThornWhip"));

                character.Stats.Will = 5;
                character.Stats.Clarity = 4;
                character.Stats.Poise = 2;
                character.Stats.Vigor = 3;
                break;
            case ECharacterClass.Thief:
                if (defaultEquip)
                    character.Equip(Items.Instance.Make("Dagger"));

                character.Stats.Will = 6;
                character.Stats.Clarity = 6;
                character.Stats.Poise = 2;
                character.Stats.Vigor = 2;
                break;
        }
    }

    public void LoadParty(object? _, SaveData data)
    {
        // Load party
        Inventory.Items.Clear();
        int i = 0;
        foreach (var character in data.characterDatas)
        {
            var member = new PartyMember(character.Class);
            InitCharacter(member, defaultEquip: false);

            foreach (var item in character.Inventory)
            {
                if (item == null)
                    continue;

                member.Equip(Items.Instance.Make(item));
            }

            Characters[i] = member;
            i++;
        }

        // Load Inventory
        foreach (var item in data.Inventory)
        {
            if (item == null)
                continue;

            Inventory.Items.Add(Items.Instance.Make(item));
        }

        CurrentPlayerPosition = (data.PlayerPositionX, data.PlayerPositionY);
    }
}
