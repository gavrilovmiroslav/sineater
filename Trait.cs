using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public interface ITrait : IAbilitySource
{
}

public class Trait(string name, string shortName) : ICombatFlowParticipant, IAbilitySource
{
    public string Name => name;
    public virtual string ShortName => shortName;
    
    public static List<Type> All = [
        typeof(TraitBalanced),
        typeof(TraitHeavy),
        typeof(TraitPadded),
        typeof(TraitProficient),
        typeof(TraitSkilled),
        typeof(TraitSneaky),
        typeof(TraitWise),
    ];

    public virtual IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyOnEndTurn(ICharacter character)
    {
        yield break;
    }
    
    public virtual IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyCombatModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyStrikeModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyStrikeBlocked(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyArmorDented(CombatFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_ApplyLeftWeaponShattered(CombatFlow flow)
    {
        yield break;
    }

    public IEnumerable AsAttacker_ApplyRightWeaponShattered(CombatFlow flow)
    {
        yield break;
    }

    public IEnumerable AsDefender_ApplyArmorDestroyed(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsAttacker_ApplyHitModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyHitModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsAttacker_DetermineHitDieDamage(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        yield break;
    }

    public virtual IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        yield break;
    }
    
    public virtual string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph.Bw(0, 0);
    }
}

public class LimitedTrait(string name, string shortName, int duration) : Trait(name, shortName)
{
    private int _duration = duration;

    public int Duration => _duration;

    public virtual IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield break;
    }
    
    public override IEnumerable ApplyOnEndTurn(ICharacter character)
    {
        _duration--;
        if (_duration <= 0)
        {
            yield return ApplyOnExpires(character);
            character.GetTraits().Remove(this);
        }
    }
    
    public override string GetName()
    {
        return $"{Name} ({Duration})";
    }

    public override string ShortName => $"{shortName}{Duration}";
}