using System;
using System.Collections;

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
                break;
            case Status.Void:
                break;
            case Status.Wound:
                break;
            case Status.Fire:
                break;
            case Status.Tired:
                break;
            case Status.Insanity:
                break;
            case Status.Poison:
                break;
            case Status.Sin:
                break;
            case Status.Stunned:
                break;
            case Status.Frozen:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        yield break;
    }
}