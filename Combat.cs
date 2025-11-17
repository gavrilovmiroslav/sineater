using System;
using System.Collections;

namespace SINEATER;

public record struct Damage(
    Character Attacker,
    Character Defender,
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
    private static int CalculateOffenseOrDefense(Character chr, bool isAttacking)
    {
        var physical = chr.CountPhysical;
        var mental = chr.CountMental;
        var lhWeapon = chr.GetLeftWeapon() ?? Weapon.Dummy("");
        var rhWeapon = chr.GetRightWeapon() ?? Weapon.Dummy("");
        
        var physicalAttack = chr.Vig * Math.Max(1, 1 + physical * 0.2f);
        var mentalAttack = chr.Wil * Math.Max(1, 1 + mental * 0.2f);

        var physicalDefense = chr.Poi * Math.Max(1, 1 + physical * 0.2f);
        var mentalDefense = chr.Cla * Math.Max(1, 1 + mental * 0.2f);
        
        var weaponAttack = 0.0f;
        var weaponDefense = 0.0f;
        
        if (chr.IsRightHanded)
        {
            weaponAttack = rhWeapon.Attack * 0.2f * rhWeapon.Base;
            weaponAttack += lhWeapon.Attack * 0.1f * lhWeapon.Base;
            
            weaponDefense = rhWeapon.Defense * 0.2f * rhWeapon.Base;
            weaponDefense += lhWeapon.Defense * 0.1f * lhWeapon.Base;
        }
        else
        {
            weaponAttack = lhWeapon.Attack * 0.2f * lhWeapon.Base;
            weaponAttack += rhWeapon.Attack * 0.1f * rhWeapon.Base;
            
            weaponDefense = lhWeapon.Defense * 0.2f * lhWeapon.Base;
            weaponDefense += rhWeapon.Defense * 0.1f * rhWeapon.Base;
        }

        var baseAttack = MathF.Ceiling((physicalAttack + mentalAttack) * weaponAttack);
        var baseDefense = MathF.Ceiling((physicalDefense + mentalDefense) * weaponDefense);
        
        if (chr is Enemy { Active: false })
        {
            baseDefense *= 0.33f;
        }
        
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
        
        var fatigueScaling = chr.AP.Count(EStatus.Fatigue) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFatigueScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFatigueScaling ?? EScalingFactor.F));
        
        var frostScaling = chr.AP.Count(EStatus.Frozen) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFrostScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFrostScaling ?? EScalingFactor.F));
        
        var fireScaling = chr.AP.Count(EStatus.Fire) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyFireScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirFireScaling ?? EScalingFactor.F));
        
        var poisonScaling = chr.AP.Count(EStatus.Poison) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyPoisonScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirPoisonScaling ?? EScalingFactor.F));

        var woundScaling = chr.AP.Count(EStatus.Wound) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyWoundScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirWoundScaling ?? EScalingFactor.F));
        
        var insanityScaling = chr.AP.Count(EStatus.Insanity) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyInsanityScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirInsanityScaling ?? EScalingFactor.F));
        
        var deathScaling = chr.AP.Count(EStatus.Death) * Math.Max(
            (int)(chr.GetLeftWeapon()?.MyDeathScaling ?? EScalingFactor.F), 
            (int)(chr.GetRightWeapon()?.TheirDeathScaling ?? EScalingFactor.F));
        
        var voidScaling = chr.AP.Count(EStatus.Void) * Math.Max(
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
    
    // TODO
    public static Damage Attack(Character attacker, Character defender)
    {
        var offense = (int)Math.Ceiling(CalculateOffenseOrDefense(attacker, true) * (1.5f - (attacker.AP.Count(EStatus.Wound) / (float)attacker.AP.Width)));
        var defense = (int)Math.Ceiling(CalculateOffenseOrDefense(defender, false) * (1.25f - (defender.AP.Count(EStatus.Wound) / (float)defender.AP.Width)));
        var damage = new Damage
        {
            Attacker = attacker,
            Defender = defender,
            Offense = offense,
            Defense = defense
        };

        foreach (var gear in attacker.GetGear())
        {
            gear.AffectOffense(ref damage);
        }
        
        foreach (var gear in defender.GetGear())
        {
            gear.AffectDefense(ref damage);
        }
        
        if (damage.Defense >= damage.Offense)
        {
            damage.Flat = 1;
            damage.Wounds = 1;
        }
        else
        {
            damage.Flat = damage.Offense - damage.Defense;
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
            
            if (defender.AP.Count(EStatus.Wound) + damage.Wounds > defender.AP.Width)
            {
                damage.HP += 2;
                damage.Flat += 20;
                
                var rem = defender.AP.Count(EStatus.Wound) + damage.Wounds - defender.AP.Width;
                damage.Wounds = -defender.AP.Width + rem;
                
                damage.Poise = (damage.Flat / 100) % 10;
            }

            if (damage.Poise >= 0 && damage.HP < damage.Poise)
            {
                damage.HP = Math.Min(5, damage.HP + 2);
            }
            
            if (damage.Wounds >= 0 && damage.Wounds < damage.HP)
            {
                damage.Wounds = Math.Min(9, damage.HP + 1);
            }
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
