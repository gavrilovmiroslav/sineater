using System;

namespace SINEATER;

public interface ITrait
{
    public void AffectOffense(ref Damage dmg);
    public void AffectDefense(ref Damage dmg);
    public void AffectDamage(ref Damage dmg);
    public void AffectStatuses(ref Damage dmg);
}

public class Trait : ITrait
{
    public virtual void AffectOffense(ref Damage dmg) {}
    public virtual void AffectDefense(ref Damage dmg) {}
    public virtual void AffectDamage(ref Damage dmg) {}
    public virtual void AffectStatuses(ref Damage dmg) {}
}

public class TraitFrenzying : Trait
{
    public override void AffectStatuses(ref Damage dmg)
    {
        dmg.StatusInsanity = 2;
        dmg.SelfInsanity = 1;
    }
} 

public class TraitDisturbed : Trait
{
    public override void AffectDefense(ref Damage dmg)
    {
        var factor = dmg.Defender.AP.Count(EStatus.Insanity) / (float)dmg.Defender.AP.Width;
        factor *= (factor * 0.95f);
        dmg.Offense = (int)Math.Floor(dmg.Offense * (1.0f - factor));
        dmg.SelfInsanity = -1;
    }
}

public class TraitOneHandProficiency : Trait
{
    public override void AffectOffense(ref Damage dmg)
    {
        if (dmg.Attacker.GetLeftWeapon() == null || dmg.Attacker.GetRightWeapon() == null)
        {
            dmg.Offense = (int)(dmg.Offense * 1.1f);
        }
    }
}