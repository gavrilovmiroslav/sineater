using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER;

public interface ISkirmish_CombatLifetime
{
    public IEnumerable OnCombatStarts(CombatFlow flow);
    public IEnumerable OnSkirmishStarts(SkirmishFlow flow);
    public IEnumerable OnSkirmishEnds(SkirmishFlow flow);
    public IEnumerable OnCombatEnds(CombatFlow flow);
}

public interface ISkirmish_AttackDiceCount
{
    public IEnumerable AsAttacker_OnAttackDiceCount(SkirmishFlow flow);
    public IEnumerable AsDefender_OnAttackDiceCount(SkirmishFlow flow);
}

public interface ISkirmish_AttackDiceRolled
{
    public IEnumerable AsAttacker_OnAttackDiceRolled(SkirmishFlow flow);
    public IEnumerable AsDefender_OnAttackDiceRolled(SkirmishFlow flow);
}

public interface ISkirmish_GuardUp
{
    public IEnumerable AsAttacker_OnGuardUp(SkirmishFlow flow);
    public IEnumerable AsDefender_OnGuardUp(SkirmishFlow flow);
}

public interface ISkirmish_CritChanceEstablished
{
    public IEnumerable AsAttacker_OnCritChanceEstablished(SkirmishFlow flow);
    public IEnumerable AsDefender_OnCritChanceEstablished(SkirmishFlow flow);
}

public interface ISkirmish_CritHit
{
    public IEnumerable AsAttacker_OnCritHit(SkirmishFlow flow);
    public IEnumerable AsDefender_OnCritHit(SkirmishFlow flow);
}

public interface ISkirmish_GuardBreak
{
    public IEnumerable AsAttacker_OnGuardBreak(SkirmishFlow flow);
    public IEnumerable AsDefender_OnGuardBreak(SkirmishFlow flow);
}

public interface ISkirmish_ArmorDented
{
    public IEnumerable AsAttacker_OnArmorDented(SkirmishFlow flow);
    public IEnumerable AsDefender_OnArmorDented(SkirmishFlow flow);
}

public interface ISkirmish_ArmorBreak
{
    public IEnumerable AsAttacker_OnArmorBreak(SkirmishFlow flow);
    public IEnumerable AsDefender_OnArmorBreak(SkirmishFlow flow);
}

public interface ISkirmish_DamageAnnounced
{
    public IEnumerable AsAttacker_OnDamageAnnounced(SkirmishFlow flow);
    public IEnumerable AsDefender_OnDamageAnnounced(SkirmishFlow flow);
}

public interface ISkirmish_PoiseBroken
{
    public IEnumerable AsAttacker_OnPoiseBroken(SkirmishFlow flow);
    public IEnumerable AsDefender_OnPoiseBroken(SkirmishFlow flow);
}

public static class SkirmishExtensions
{
    public static IEnumerable OnCombatStarts(this IEnumerable<Trait> traits, CombatFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CombatLifetime))
        {
            yield return (trait as ISkirmish_CombatLifetime).OnCombatStarts(flow);
        }
    }
    
    public static IEnumerable OnCombatEnds(this IEnumerable<Trait> traits, CombatFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CombatLifetime))
        {
            yield return (trait as ISkirmish_CombatLifetime).OnCombatEnds(flow);
        }
    }

    public static IEnumerable OnSkirmishStarts(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CombatLifetime))
        {
            yield return (trait as ISkirmish_CombatLifetime).OnSkirmishStarts(flow);
        }
    }
    
    public static IEnumerable OnSkirmishEnds(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CombatLifetime))
        {
            yield return (trait as ISkirmish_CombatLifetime).OnSkirmishEnds(flow);
        }
    }
    
    public static IEnumerable AsAttacker_OnAttackDiceCount(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_AttackDiceCount))
        {
            yield return (trait as ISkirmish_AttackDiceCount).AsAttacker_OnAttackDiceCount(flow);
        }
    }
    
    public static IEnumerable AsDefender_OnAttackDiceCount(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_AttackDiceCount))
        {
            yield return (trait as ISkirmish_AttackDiceCount).AsDefender_OnAttackDiceCount(flow);
        }
    }
    
    public static IEnumerable AsAttacker_OnAttackDiceRolled(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_AttackDiceRolled))
        {
            yield return (trait as ISkirmish_AttackDiceRolled).AsAttacker_OnAttackDiceRolled(flow);
        }
    }
    
    public static IEnumerable AsDefender_OnAttackDiceRolled(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_AttackDiceRolled))
        {
            yield return (trait as ISkirmish_AttackDiceRolled).AsDefender_OnAttackDiceRolled(flow);
        }
    }
    
    public static IEnumerable AsAttacker_OnDamageAnnounced(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_DamageAnnounced))
        {
            yield return (trait as ISkirmish_DamageAnnounced).AsAttacker_OnDamageAnnounced(flow);
        }
    }

    public static IEnumerable AsDefender_OnDamageAnnounced(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_DamageAnnounced))
        {
            yield return (trait as ISkirmish_DamageAnnounced).AsDefender_OnDamageAnnounced(flow);
        }
    }

    public static IEnumerable AsAttacker_OnPoiseBroken(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_PoiseBroken))
        {
            yield return (trait as ISkirmish_PoiseBroken).AsAttacker_OnPoiseBroken(flow);
        }
    }

    public static IEnumerable AsDefender_OnPoiseBroken(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_PoiseBroken))
        {
            yield return (trait as ISkirmish_PoiseBroken).AsDefender_OnPoiseBroken(flow);
        }
    }

    public static IEnumerable AsAttacker_OnGuardUp(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_GuardUp))
        {
            yield return (trait as ISkirmish_GuardUp).AsAttacker_OnGuardUp(flow);
        }
    }

    public static IEnumerable AsDefender_OnGuardUp(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_GuardUp))
        {
            yield return (trait as ISkirmish_GuardUp).AsDefender_OnGuardUp(flow);
        }
    }

    public static IEnumerable AsAttacker_OnCritChanceEstablished(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CritChanceEstablished))
        {
            yield return (trait as ISkirmish_CritChanceEstablished).AsAttacker_OnCritChanceEstablished(flow);
        }
    }

    public static IEnumerable AsDefender_OnCritChanceEstablished(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CritChanceEstablished))
        {
            yield return (trait as ISkirmish_CritChanceEstablished).AsDefender_OnCritChanceEstablished(flow);
        }
    }
    
    public static IEnumerable AsAttacker_OnCritHit(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CritHit))
        {
            yield return (trait as ISkirmish_CritHit).AsAttacker_OnCritHit(flow);
        }
    }

    public static IEnumerable AsDefender_OnCritHit(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_CritHit))
        {
            yield return (trait as ISkirmish_CritHit).AsDefender_OnCritHit(flow);
        }
    }

    public static IEnumerable AsAttacker_OnGuardBreak(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_GuardBreak))
        {
            yield return (trait as ISkirmish_GuardBreak).AsAttacker_OnGuardBreak(flow);
        }
    }

    public static IEnumerable AsDefender_OnGuardBreak(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_GuardBreak))
        {
            yield return (trait as ISkirmish_GuardBreak).AsDefender_OnGuardBreak(flow);
        }
    }

    public static IEnumerable AsAttacker_OnArmorBreak(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_ArmorBreak))
        {
            yield return (trait as ISkirmish_ArmorBreak).AsAttacker_OnArmorBreak(flow);
        }
    }

    public static IEnumerable AsDefender_OnArmorBreak(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_ArmorBreak))
        {
            yield return (trait as ISkirmish_ArmorBreak).AsDefender_OnArmorBreak(flow);
        }
    }

    public static IEnumerable AsAttacker_OnArmorDented(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_ArmorDented))
        {
            yield return (trait as ISkirmish_ArmorDented).AsAttacker_OnArmorDented(flow);
        }
    }

    public static IEnumerable AsDefender_OnArmorDented(this IEnumerable<Trait> traits, SkirmishFlow flow)
    {
        foreach (var trait in traits.Where(t => t is ISkirmish_ArmorDented))
        {
            yield return (trait as ISkirmish_ArmorDented).AsDefender_OnArmorDented(flow);
        }
    }
}