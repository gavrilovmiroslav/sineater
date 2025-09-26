using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using RogueSharp;

namespace SINEATER;

public class Enemy : ICharacter, ICombatFlowParticipant
{
    public int X, Y;
    public string Name;
    public Color Tint;
    public ActionPoints AP;
    public int HP;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;
    public readonly List<Trait> Traits = [];
    public (int, int) Icon;
    public (int, int) Portrait;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    public List<IBehavior> Behaviors = [];
    public Enemy() {}
    
    public Stats Stats { get; set; } = new();

    public Color GetTint()
    {
        return Tint;
    }
    
    public ActionPoints GetAP()
    {
        return AP;
    }

    public Weapon? GetLeftWeapon()
    {
        return LeftWeapon;
    }

    public Weapon? GetRightWeapon()
    {
        return RightWeapon;
    }

    public Armor? GetArmor()
    {
        return Armor;
    }

    public List<Trait> GetTraits()
    {
        return Traits;
    }

    public bool IsStunned()
    {
        return AP.Contains<StatusStunned>();
    }
    
    public string GetName()
    {
        return Name;
    }

    public (int, int) GetPortait()
    {
        return Portrait;
    }

    public void Die()
    {
        IsDead = true;
    }

    public void RemoveArmor()
    {
        this.Armor = null;
    }

    public IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyDiceCountModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyDiceCountModifiers(flow);
    }

    public IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyCombatModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyCombatModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyCombatModifiers(flow);
    }

    public IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyStrikeModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyStrikeModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyStrikeModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyStrikeBlocked(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyStrikeBlocked(flow);
    }

    public IEnumerable AsDefender_ApplyArmorDented(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyArmorDented(flow);
    }

    public IEnumerable AsAttacker_ApplyLeftWeaponShattered(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsAttacker_ApplyLeftWeaponShattered(flow);
        this.LeftWeapon = null;
    }

    public IEnumerable AsAttacker_ApplyRightWeaponShattered(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsAttacker_ApplyRightWeaponShattered(flow);
        this.RightWeapon = null;
    }

    public IEnumerable AsDefender_ApplyArmorDestroyed(CombatFlow flow)
    {
        foreach (var trait in Traits)
            yield return trait.AsDefender_ApplyArmorDestroyed(flow);
        this.Armor = null;
    }

    public IEnumerable AsAttacker_ApplyHitModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyHitModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyHitModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyHitModifiers(flow);
    }

    public IEnumerable AsAttacker_DetermineHitDieDamage(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_DetermineHitDieDamage(flow);
    }

    public IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_DetermineHitDieDamage(flow);
    }

    public IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsAttacker_ApplyTotalIncomingDamageModifiers(flow);
    }

    public IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        foreach (var trait in Traits) 
            yield return trait.AsDefender_ApplyTotalIncomingDamageModifiers(flow);
    }

    public IEnumerable MoveTo(CombatMapScreen level, int x, int y)
    {
        X = x;
        Y = y;
        if (level.Domains._tiles.ContainsKey(((int)X, (int)Y)))
        {
            level.DrawCombat();
            yield return level.Domains._tiles[((int)X, (int)Y)]
                .ApplyOnDomainStepped(level, this, X, Y);
        }
    }
}