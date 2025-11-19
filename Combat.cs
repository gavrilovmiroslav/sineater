using System;
using System.Collections;

namespace SINEATER;

public record struct CombatCalculus(
    float Physical,
    float Mental,
    float PhysicalAttack,
    float MentalAttack,
    float PhysicalDefense,
    float MentalDefense,
    float WeaponAttack,
    float WeaponDefense,
    float BaseAttack,
    float BaseDefense,

    float WilScaling,
    float ClaScaling,
    float PoiScaling,
    float VigScaling,
    float StatAlign,

    float FatigueScaling,
    float FrostScaling,
    float FireScaling,
    float PoisonScaling,
    float WoundScaling,
    float InsanityScaling,
    float DeathScaling,
    float VoidScaling,
    float StatusAlign,
    float Offense,
    float Defense);

public record struct Damage(
    Character Attacker,
    Character Defender,
    CombatCalculus OffenseCalc,
    CombatCalculus DefenseCalc,
    int Offense,
    int Defense,
    int Flat,
    int Wounds,
    int HP,
    int Poise,
    int StatusFatigue,
    int StatusFire,
    int StatusFrost,
    int StatusPoison,
    int StatusInsanity,
    int StatusDeath,
    int SelfFatigue,
    int SelfFire,
    int SelfFrost,
    int SelfPoison,
    int SelfWound,
    int SelfInsanity,
    int SelfDeath)
{
    public int SelfDamage => 
        SelfWound + SelfFatigue + SelfFire + SelfFrost + 
        SelfPoison + SelfInsanity + SelfDeath;
}

public static class Combat
{
    private static CombatCalculus CalculateOffenseOrDefense(Character chr, bool isAttacking)
    {
        CombatCalculus calculus = new CombatCalculus();
        var physical = chr.CountPhysical;
        var mental = chr.CountMental;
        
        calculus.Physical = physical;
        calculus.Mental = mental;
        
        var lhWeapon = chr.GetLeftWeapon() ?? Weapon.Dummy("");
        var rhWeapon = chr.GetRightWeapon() ?? Weapon.Dummy("");
        
        var physicalAttack = chr.Vig * Math.Max(1, 1 + physical * 0.2f);
        var mentalAttack = chr.Wil * Math.Max(1, 1 + mental * 0.2f);
        calculus.PhysicalAttack = physicalAttack;
        calculus.MentalAttack = mentalAttack;

        var physicalDefense = chr.Poi * Math.Max(1, 1 + physical * 0.2f);
        var mentalDefense = chr.Cla * Math.Max(1, 1 + mental * 0.2f);
        calculus.PhysicalDefense = physicalDefense;
        calculus.MentalDefense = mentalDefense;
        
        var weaponAttack = 0.0f;
        var weaponDefense = 0.0f;
        
        if (chr.IsRightHanded)
        {
            weaponAttack = rhWeapon.Attack * rhWeapon.Base;
            weaponAttack += lhWeapon.Attack * 0.2f * lhWeapon.Base;
            
            weaponDefense = rhWeapon.Defense * rhWeapon.Base;
            weaponDefense += lhWeapon.Defense * 0.2f * lhWeapon.Base;
        }
        else
        {
            weaponAttack = lhWeapon.Attack * lhWeapon.Base;
            weaponAttack += rhWeapon.Attack * 0.2f * rhWeapon.Base;
            
            weaponDefense = lhWeapon.Defense * lhWeapon.Base;
            weaponDefense += rhWeapon.Defense * 0.2f * rhWeapon.Base;
        }

        calculus.WeaponAttack = weaponAttack;
        calculus.WeaponDefense = weaponDefense;

        var baseAttack = MathF.Ceiling((physicalAttack + mentalAttack) * weaponAttack);
        var baseDefense = MathF.Ceiling((physicalDefense + mentalDefense) * weaponDefense);

        if (chr is Enemy { Active: false })
        {
            baseDefense *= 0.33f;
        }

        calculus.BaseAttack = baseAttack;
        calculus.BaseDefense = baseDefense;
        
        var wilScaling = chr.Wil * Math.Max(
            (int)(chr.GetLeftWeapon()?.WilScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.WilScaling ?? EScalingFactor.F));
        calculus.WilScaling = wilScaling;
        
        var claScaling = 0.5f * chr.Cla * Math.Max(
            (int)(chr.GetLeftWeapon()?.ClaScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.ClaScaling ?? EScalingFactor.F));
        calculus.ClaScaling = claScaling;
        
        var poiScaling = 0.5f * chr.Poi * Math.Max(
            (int)(chr.GetLeftWeapon()?.PoiScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.PoiScaling ?? EScalingFactor.F));
        calculus.PoiScaling = poiScaling;
        
        var vigScaling = chr.Vig * Math.Max(
            (int)(chr.GetLeftWeapon()?.VigScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.VigScaling ?? EScalingFactor.F));
        calculus.VigScaling = vigScaling;
        
        var scalingAlign = wilScaling + claScaling + poiScaling + vigScaling;
        calculus.StatAlign = scalingAlign;
        
        var fatigueScaling = chr.AP.Count(EStatus.Fatigue) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFatigueScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFatigueScaling ?? EScalingFactor.F));
        calculus.FatigueScaling = fatigueScaling;
        
        var frostScaling = chr.AP.Count(EStatus.Frozen) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFrostScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFrostScaling ?? EScalingFactor.F));
        calculus.FrostScaling = frostScaling;
        
        var fireScaling = chr.AP.Count(EStatus.Fire) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFireScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFireScaling ?? EScalingFactor.F));
        calculus.FireScaling = fireScaling;
        
        var poisonScaling = chr.AP.Count(EStatus.Poison) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyPoisonScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirPoisonScaling ?? EScalingFactor.F));
        calculus.PoisonScaling = poisonScaling;

        var woundScaling = chr.AP.Count(EStatus.Wound) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyWoundScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirWoundScaling ?? EScalingFactor.F));
        calculus.WoundScaling = woundScaling;
        
        var insanityScaling = chr.AP.Count(EStatus.Insanity) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyInsanityScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirInsanityScaling ?? EScalingFactor.F));
        calculus.InsanityScaling = insanityScaling;
        
        var deathScaling = chr.AP.Count(EStatus.Death) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyDeathScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirDeathScaling ?? EScalingFactor.F));
        calculus.DeathScaling = deathScaling;
        
        var voidScaling = chr.AP.Count(EStatus.Void) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyVoidScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirVoidScaling ?? EScalingFactor.F));
        calculus.VoidScaling = voidScaling;

        var statusAlign = fatigueScaling + frostScaling + fireScaling
                           + poisonScaling + woundScaling + insanityScaling 
                           + deathScaling + voidScaling;
        calculus.StatusAlign = statusAlign;
        
        var offense = baseAttack + scalingAlign + statusAlign;
        calculus.Offense = (int)Math.Ceiling(offense * (1.5f - (chr.AP.Count(EStatus.Wound) / (float)chr.AP.Width)));
        var defense = baseDefense + scalingAlign + statusAlign;
        calculus.Defense = (int)Math.Ceiling(defense * (1.2f - (chr.AP.Count(EStatus.Wound) / (float)chr.AP.Width)));;

        return calculus;
    }
    
    // TODO
    public static Damage Attack(Character attacker, Character defender)
    {
        var atk = CalculateOffenseOrDefense(attacker, true);
        var dfn = CalculateOffenseOrDefense(defender, false);
        var damage = new Damage
        {
            Attacker = attacker,
            Defender = defender,
            OffenseCalc = atk,
            Offense = (int)Math.Ceiling(atk.Offense),
            DefenseCalc = dfn,
            Defense = (int)Math.Ceiling(dfn.Offense),
        };

        foreach (var gear in attacker.GetGear())
        {
            gear.AffectOffense(ref damage);
        }
        
        foreach (var gear in defender.GetGear())
        {
            gear.AffectDefense(ref damage);
        }

        var a = damage.Offense;
        var d = damage.Defense;
        
        if (d > a)
        {
            damage.Flat = 1;
            damage.Wounds = 1;
        }
        else
        {
            damage.Flat = a - d;
            damage.Wounds = damage.Flat % 10;
            damage.HP = (damage.Flat / 10) % 10;
            damage.Poise = (damage.Flat / 100) % 10;

            foreach (var gear in attacker.GetGear())
            {
                gear.AffectDamage(ref damage);
            }
        
            foreach (var gear in defender.GetGear())
            {
                gear.AffectDamage(ref damage);
            }
            
            // if (defender.AP.Count(EStatus.Wound) + damage.Wounds > defender.AP.Width)
            // {
            //     damage.HP += 1;
            //     damage.Flat += 10;
            //     
            //     var rem = defender.AP.Count(EStatus.Wound) + damage.Wounds - defender.AP.Width;
            //     damage.Wounds = -defender.AP.Width + rem;
            //     
            //     damage.Poise = (damage.Flat / 100) % 10;
            // }
            //
            // if (damage.Poise >= 0 && damage.HP < damage.Poise)
            // {
            //     damage.HP = Math.Min(5, damage.HP + 1);
            // }
            //
            // if (damage.Wounds >= 0 && damage.Wounds < damage.HP)
            // {
            //     damage.Wounds = Math.Min(9, damage.HP + 1);
            // }
        }

        foreach (var gear in attacker.GetGear())
        {
            gear.AffectStatuses(ref damage);
        }
        
        foreach (var gear in defender.GetGear())
        {
            gear.AffectStatuses(ref damage);
        }
        
        return damage;
    }
}
