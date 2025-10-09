using System;
using System.Collections;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Ability
{
    public virtual bool CanBeUsed(ICharacter character)
    {
        return false;
    }

    public virtual IEnumerable Use(CombatMapScreen level, ICharacter character, int x, int y)
    {
        yield break;
    }
}

public class DomainExpansion : Ability
{
    public override bool CanBeUsed(ICharacter character)
    {
        return true; // todo: make this harder
    }

    public override IEnumerable Use(CombatMapScreen level, ICharacter character, int x, int y)
    {
        var kind = character.GetAP().GetAt(x * 2 + 1);
        switch (kind)
        {
            case Status.Stamina:
                yield return level.Domains.Add(new DomainOfAction(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
                break;
            case Status.Void:
                yield return level.Domains.Add(new DomainOfDarkness(character, x, y, 2 + character.Stats.Clarity));
                break;
            case Status.Wound:
                yield return level.Domains.Add(new DomainOfHealing(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
                break;
            case Status.Fire:
                yield return level.Domains.Add(new DomainOfFire(character, x, y, 4));
                break;
            case Status.Tired:
                yield return level.Domains.Add(new DomainOfFatigue(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
                break;
            case Status.Insanity:
                break;
            case Status.Poison:
                break;
            case Status.Sin:
                break;
            case Status.Death:
                break;
            case Status.Frozen:
                yield return level.Domains.Add(new DomainOfControl(character, x, y, 2));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        character.GetAP().Spend(1);
        yield break;
    }
}