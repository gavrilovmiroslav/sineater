using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class Enemy : ICharacter, ICombatFlowParticipant
{
    public int X, Y;
    public string Name;
    public Color Tint;
    public ActionPoints AP;
    public int HP;
    public Stats Stats;
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;
    public Armor? Armor = null;
    public readonly List<Trait> Traits = [];
    public (int, int) Icon;
    public (int, int) DeadIcon;
    public int Sin;
    public bool IsDead = false;
    
    public Enemy()
    {
        
    }

    public static Enemy Goblin()
    {
        var gob = new Enemy
        {
            Name = "Goblin",
            Icon = (5, 64),
            DeadIcon = (8, 65),
            Sin = Rnd.Instance.D4,
            HP = Rnd.Instance.Next(5, 10),
            Tint = Color.LightGreen,
            Armor = new Armor("Rags", Rnd.Instance.Next(3, 4), EWeightClass.Tiny, 1),
            Stats = new Stats(1, 2, 2, Rnd.Instance.Next(3, 4)),
        };
        if (Rnd.Instance.D4 > gob.Sin)
            gob.LeftWeapon = new Weapon("Stick", Rnd.Instance.D4 + 1, EWeightClass.Small, 1);
        gob.RightWeapon = new Weapon("Bone dagger", Rnd.Instance.D4, EWeightClass.Tiny, 1);
        return gob;
    }
    
    public static Enemy Hobgoblin()
    {
        var gob = new Enemy
        {
            Name = "Hobgoblin",
            Icon = (6, 64),
            DeadIcon = (8, 65),
            Sin = 3 + Rnd.Instance.D2,
            HP = 8,
            Tint = Color.Red,
            Armor = new Armor("Rags", 4, EWeightClass.Tiny, 1),
            Stats = new Stats(2, 3, 2, 4),
        };
        
        gob.LeftWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        gob.RightWeapon = new Weapon("Obsidian dagger", 3, EWeightClass.Small, 1);
        //gob.Traits.Add(new TraitSneaky());
        //gob.Traits.Add(new TraitProficient());
        gob.Traits.Add(new TraitWise());
        return gob;
    }

    public Stats GetStats()
    {
        return Stats;
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

    public void Die()
    {
        IsDead = true;
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
}