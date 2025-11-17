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
