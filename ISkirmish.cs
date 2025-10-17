using System.Collections;

namespace SINEATER;

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

public interface ISkirmishFlowParticipant
{
    public IEnumerable AsAttacker_ApplyCombatModifiers(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyCombatModifiers(SkirmishFlow flow);
    
    public IEnumerable AsAttacker_ApplyStrikeModifiers(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyStrikeModifiers(SkirmishFlow flow);
    
    public IEnumerable AsDefender_ApplyStrikeBlocked(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyArmorDented(SkirmishFlow flow);
    public IEnumerable AsAttacker_ApplyLeftWeaponShattered(SkirmishFlow flow);
    public IEnumerable AsAttacker_ApplyRightWeaponShattered(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyArmorDestroyed(SkirmishFlow flow);
    
    public IEnumerable AsAttacker_ApplyHitModifiers(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyHitModifiers(SkirmishFlow flow);
    
    public IEnumerable AsAttacker_DetermineHitDieDamage(SkirmishFlow flow);
    public IEnumerable AsDefender_DetermineHitDieDamage(SkirmishFlow flow);
    
    public IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(SkirmishFlow flow);
    public IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(SkirmishFlow flow);
}