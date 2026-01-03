using System;
using System.Collections;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Ability
{
    public virtual bool CanBeUsed(ICharacter character, int x, int y)
    {
        return false;
    }

    public virtual IEnumerable Use(IScreen level, ICharacter character, int x, int y)
    {
        yield break;
    }
}

public class DomainExpansion : Ability
{
    public override bool CanBeUsed(ICharacter character, int x, int y)
    {
        // var sin = character.GetAP().Count(EStatus.Sin);
        //
        // var kind = character.GetAP().GetAt(x);
        // switch (kind)
        // {
        //     case EStatus.Stamina:
        //         return sin >= 1;
        //     case EStatus.Void:
        //         return sin >= 3;
        //     case EStatus.Wound:
        //         return true;
        //     case EStatus.Fire:
        //         return sin >= 5;
        //     case EStatus.Fatigue:
        //         return sin >= 2;
        //     case EStatus.Frozen:
        //         return true;
        //     default:
        //         return false;
        // }
        return false;
    }

    public override IEnumerable Use(IScreen level, ICharacter character, int x, int y)
    {
        // var kind = character.GetAP().GetAt(x);
        // var domains = (level as CombatMapScreen).Domains;
        // switch (kind)
        // {
        //     case EStatus.Stamina:
        //         character.GetAP().Reduce(EStatus.Sin, 1);
        //         yield return domains.Add(new DomainOfAction(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
        //         break;
        //     case EStatus.Void:
        //         character.GetAP().Reduce(EStatus.Sin, 3);
        //         yield return domains.Add(new DomainOfDarkness(character, x, y, 2 + character.Stats.Clarity));
        //         break;
        //     case EStatus.Wound:
        //         yield return domains.Add(new DomainOfHealing(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
        //         break;
        //     case EStatus.Fire:
        //         character.GetAP().Reduce(EStatus.Sin, 5);
        //         yield return domains.Add(new DomainOfFire(character, x, y, 4));
        //         break;
        //     case EStatus.Fatigue:
        //         character.GetAP().Reduce(EStatus.Sin, 2);
        //         yield return domains.Add(new DomainOfFatigue(character, x, y, Math.Clamp(1 + character.Stats.Mod(EStat.Clarity), 3, 6)));
        //         break;
        //     case EStatus.Insanity:
        //         break;
        //     case EStatus.Poison:
        //         break;
        //     case EStatus.Sin:
        //         break;
        //     case EStatus.Death:
        //         break;
        //     case EStatus.Frozen:
        //         yield return domains.Add(new DomainOfControl(character, x, y, 2));
        //         break;
        //     default:
        //         throw new ArgumentOutOfRangeException();
        // }
        // character.GetAP().Spend(1);
        yield break;
    }
}