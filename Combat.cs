using System;
using System.Collections;

namespace SINEATER;

public static class Combat
{
    private static int CalculateOffenseOrDefense(Character chr, bool isAttacking)
    {
        var physical = chr.CountPhysical;
        var mental = chr.CountMental;
        var physicalAttack = chr.Vig * Math.Max(1, 1 + physical * 0.2f);
        var mentalAttack = chr.Wil * Math.Max(1, 1 + mental * 0.2f);
        var baseAttack = Math.Ceiling((physicalAttack + mentalAttack) * chr.WeightFactor);
        
        var physicalDefense = chr.Poi * Math.Max(1, 1 + physical * 0.2f);
        var mentalDefense = chr.Cla * Math.Max(1, 1 + mental * 0.2f);
        var baseDefense = Math.Ceiling((physicalDefense + mentalDefense) * chr.WeightFactor);
        
        var wilScaling = chr.Wil * Math.Max(
            (int)(chr.GetLeftWeapon()?.WilScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.WilScaling ?? EScalingFactor.F));
        
        var claScaling = 0.5f * chr.Cla * Math.Max(
            (int)(chr.GetLeftWeapon()?.ClaScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.ClaScaling ?? EScalingFactor.F));
        
        var poiScaling = 0.5f * chr.Poi * Math.Max(
            (int)(chr.GetLeftWeapon()?.PoiScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.PoiScaling ?? EScalingFactor.F));
        
        var vigScaling = chr.Vig * Math.Max(
            (int)(chr.GetLeftWeapon()?.VigScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.VigScaling ?? EScalingFactor.F));

        var scalingAlign = wilScaling + claScaling + poiScaling + vigScaling;
        
        var fatigueScaling = chr.AP.Count<StatusFatigue>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFatigueScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFatigueScaling ?? EScalingFactor.F));
        
        var frostScaling = chr.AP.Count<StatusFrozen>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFrostScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFrostScaling ?? EScalingFactor.F));
        
        var fireScaling = chr.AP.Count<StatusFire>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFireScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFireScaling ?? EScalingFactor.F));
        
        var poisonScaling = chr.AP.Count<StatusPoison>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyPoisonScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirPoisonScaling ?? EScalingFactor.F));

        var woundScaling = chr.AP.Count<StatusWounds>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyWoundScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirWoundScaling ?? EScalingFactor.F));
        
        var insanityScaling = chr.AP.Count<StatusInsanity>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyInsanityScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirInsanityScaling ?? EScalingFactor.F));
        
        var deathScaling = chr.AP.Count<StatusDeath>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyDeathScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirDeathScaling ?? EScalingFactor.F));
        
        var voidScaling = chr.AP.Count<StatusVoid>() * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyVoidScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirVoidScaling ?? EScalingFactor.F));

        var statusAlign = fatigueScaling + frostScaling + fireScaling
                           + poisonScaling + woundScaling + insanityScaling 
                           + deathScaling + voidScaling;

        var offense = baseAttack + scalingAlign + statusAlign;
        var defense = baseDefense + scalingAlign + statusAlign;

        if (isAttacking)
        {
            return (int)Math.Ceiling(offense);
        }
        else
        {
            return (int)Math.Ceiling(defense);
        }
    }
    
    public static IEnumerable Attack(Character attacker, Character defender)
    {
        var offense = CalculateOffenseOrDefense(attacker, true);
        var defense = CalculateOffenseOrDefense(defender, false);
        if (defense >= offense)
        {
            yield return new DealFatigueDamage(1);
        }
        else
        {
            var flatDamage = offense - defense;
            var woundDamage = flatDamage % 10;
            var poiseDamage = (int)Math.Floor(flatDamage / 10.0) % 10;
            var hpDamage = (int)Math.Floor(flatDamage / 100.0) % 10;

            var delta1 = Math.Sign(woundDamage - poiseDamage);
            var delta2 = Math.Sign(poiseDamage - hpDamage);
            if (delta1 <= 0) woundDamage = 10;
            if (delta2 <= 0 && delta1 <= 0) hpDamage += 1;
            
            if (woundDamage > 0) yield return new DealWoundDamage(woundDamage);
            if (poiseDamage > 0) yield return new DealPoiseDamage(poiseDamage);
            if (hpDamage > 0) yield return new DealHPDamage(hpDamage);
        }
    }
}

public record struct DealFatigueDamage(int Amount);
public record struct DealWoundDamage(int Amount);
public record struct DealPoiseDamage(int Amount);
public record struct DealHPDamage(int Amount);