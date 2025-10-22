using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace SINEATER;

[DataContract]
public class TraitKnockback() : Trait("Knockback", "Kn", "KNOCKBACK: Critical hits push the target back."), ISkirmish_CritHit
{
    public IEnumerable AsAttacker_OnCritHit(SkirmishFlow flow)
    {
        CombatMapScreen.Level?.DrawCombat();
        if (flow.Defender != null)
        {
            var a = flow.Attacker;
            var d = flow.Defender;
            var dp = (d.X, d.Y);
            var dir = (d.X - a.X, d.Y - a.Y);
            var np = Directions.GoForwards(dp, dir);
            if (Positions.Swap(dp, np))
            {
                var mp = Directions.GoForwards(np, dir);
                Positions.Swap(np, mp);
            }
        }

        yield break;
    }

    public IEnumerable AsDefender_OnCritHit(SkirmishFlow flow) { yield break; }
}
[DataContract]
public class TraitForceful() : Trait("Forceful", "Fr", "FORCEFUL: +1 attack die every skirmish in combat.")
    , ISkirmish_CombatLifetime
    , ISkirmish_AttackDiceCount
{
    private int _bonus = 0;

    public IEnumerable OnCombatStarts(CombatFlow flow)
    {
        _bonus = 0;
        yield break;
    }
    
    public IEnumerable OnSkirmishEnds(SkirmishFlow flow)
    {
        _bonus++;
        yield break;
    }

    public IEnumerable AsAttacker_OnAttackDiceCount(SkirmishFlow flow)
    {
        for (var i = 0; i < _bonus; i++)
        {
            flow.AttackDice.Add(new Die(this));
        }

        yield break;
    }

    public IEnumerable OnSkirmishStarts(SkirmishFlow flow) { yield break; }
    public IEnumerable OnCombatEnds(CombatFlow flow) { yield break; }
    public IEnumerable AsDefender_OnAttackDiceCount(SkirmishFlow flow) { yield break; }
}
[DataContract]
public class TraitSneaky() : Trait("Sneaky", "Sn", "SNEAKY: If both weapons are light-weight, +1 attack die per weapon.")
    , ISkirmish_AttackDiceCount
{
    public IEnumerable AsAttacker_OnAttackDiceCount(SkirmishFlow flow)
    {
        if (flow.Attacker.GetLeftWeapon() is { Weight: > EWeightClass.Light })
            yield break;
        
        if (flow.Attacker.GetRightWeapon() is { Weight: > EWeightClass.Light })
            yield break;

        var dice = 0;
        if (flow.Attacker.GetLeftWeapon() is not null) dice++;
        if (flow.Attacker.GetRightWeapon() is not null) dice++;
        for (var i = 0; i < dice; i++)
            flow.AttackDice.Add(new Die(this));
        yield return YellName();
    }

    public IEnumerable AsDefender_OnAttackDiceCount(SkirmishFlow flow) { yield break; }
}
[DataContract]
public class TraitProficient() : Trait("Proficient", "Pr", "PROFICIENT: All 1s are 2 instead."), ISkirmish_AttackDiceRolled
{
    private static IEnumerable IfRollOneRiseToTwo(SkirmishFlow flow)
    {
        var colors = new Color[flow.AttackDiceRolled.Count];
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            colors[i] = SineaterGame.Instance.Layers["mrmo"].GetFg(3 + i, 0);
        }

        var active = flow.AttackDiceRolled.Any(d => d.Value == 1);
        if (active)
        {
            yield return new Present_Notify("Proficient: All 1s becomes 2s.");
        }
        
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            if (flow.AttackDiceRolled[i].Value == 1)
            {
                for (int j = 0; j < 5; j++)
                {
                    SineaterGame.Instance.Layers["mrmo"].Set(3 + i, 0,
                        new Glyph(0, 68, Color.Black, Color.Lerp(colors[i], Color.Purple, j / 5.0f)));
                    yield return new WaitForSeconds(0.01f);
                }

                SineaterGame.Instance.Layers["mrmo"].Set(3 + i, 0,
                    new Glyph(1, 68, Color.Black, Color.Purple));
                yield return new WaitForSeconds(0.1f);

                flow.AttackDiceRolled[i].Value = 2;
            }
        }
    }

    public IEnumerable AsAttacker_OnAttackDiceRolled(SkirmishFlow flow)
    {
        yield return IfRollOneRiseToTwo(flow);
    }

    public IEnumerable AsDefender_OnAttackDiceRolled(SkirmishFlow flow)
    {
        yield return IfRollOneRiseToTwo(flow);
    }
}

public class TraitBalanced() : Trait("Balanced", "Ba", "BALANCED: Increase attack on repeated rolls by 1."), ISkirmish_AttackDiceRolled
{
    public IEnumerable AsAttacker_OnAttackDiceRolled(SkirmishFlow flow)
    {
        HashSet<int> values = [];
        var colors = new Color[flow.AttackDiceRolled.Count];
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            colors[i] = SineaterGame.Instance.Layers["mrmo"].GetFg(3 + i, 0);
        }
        
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            var r = flow.AttackDiceRolled[i];
            if (r.Value < 6 && values.Contains(r.Value))
            {
                yield return YellName();
                for (int j = 0; j < 5; j++)
                {
                    SineaterGame.Instance.Layers["mrmo"].Set(3 + i, 0,
                        new Glyph(r.Value - 1, 68, Color.Black, Color.Lerp(colors[i], Color.LightPink, j / 5.0f)));
                    yield return new WaitForSeconds(0.01f);
                }

                r.Value++;
                yield return new WaitForSeconds(0.01f);

                SineaterGame.Instance.Layers["mrmo"].Set(3 + i, 0,
                    new Glyph(r.Value - 1, 68, Color.Black, Color.LightPink));
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                values.Add(r.Value);
            }
        }

        
    }

    public IEnumerable AsDefender_OnAttackDiceRolled(SkirmishFlow flow) { yield break; }
}
[DataContract]
public class TraitSkilled() : Trait("Skilled", "Sk", "SKILLED: Skilled shot deals 1 damage.")
{
    // public override IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    // {
    //     if (flow.TotalIncomingDamage == 0)
    //     {
    //         flow.AttackerTraits.Add(this);
    //         flow.TotalIncomingDamage += 1;
    //         yield return YellName();
    //     }
    // }
}
[DataContract]
public class TraitPadded() : Trait("Padded", "Pd", "PADDED: Reducing incoming damage by 1.")
{
    // public override IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    // {
    //     if (flow.TotalIncomingDamage > 0)
    //     {
    //         flow.DefenderTraits.Add(this);
    //         flow.TotalIncomingDamage -= 1;
    //         yield return YellName();
    //     }
    // }
}
[DataContract]
public class TraitHeavy() : Trait("Heavy", "Hv", "HEAVY: Add +1 attack die for each heavy weapon.")
{
    // public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    // {
    //     var ok = false;
    //     var left = flow.Attacker.GetLeftWeapon();
    //     if (left != null && (int)left.Weight > 6)
    //     {
    //         ok = true;
    //         flow.AttackDicePreRoll.Add(new Die(left));
    //     }
    //     
    //     var right = flow.Attacker.GetRightWeapon();
    //     if (right != null && (int)right.Weight > 6)
    //     {
    //         ok = true;
    //         flow.AttackDicePreRoll.Add(new Die(right));
    //     }
    //
    //     if (ok)
    //     {
    //         flow.AttackerTraits.Add(this);
    //         yield return YellName();
    //     }
    // }
}
[DataContract]
public class TraitWise() : Trait("Wise", "Ws", "WISE: Reroll the lowest attack dice.")
{
    // public override IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    // {
    //     flow.AttackerTraits.Add(this);
    //     yield return YellName();
    //     var min = 100;
    //     for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
    //     {
    //         if (flow.AttackDiceRolled[i].Value < min) min = flow.AttackDiceRolled[i].Value;
    //     }
    //     // find minimal attack die value
    //     
    //     for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
    //     {
    //         if (flow.AttackDiceRolled[i].Value == min)
    //         {
    //             // reroll minimal values
    //             for (int j = 0; j < 10; j++)
    //             {
    //                 if (j % 2 == 0)
    //                 {
    //                     SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
    //                         new Glyph(flow.HitDieDamage - 1, 68, Color.Black, Color.Gray));
    //                 }
    //                 else
    //                 {
    //                     SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
    //                         new Glyph(0, 0, Color.Black, Color.Black));
    //                 }
    //
    //                 yield return new WaitForSeconds(0.01f);
    //             }
    //
    //             for (int j = 0; j <= 10; j++)
    //             {
    //                 SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
    //                     new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
    //                 yield return new WaitForSeconds(0.01f);
    //             }
    //             var a = flow.AttackDiceRolled[i];
    //             a.Value = Math.Min(6, a.Value + 1);
    //             flow.AttackDiceRolled[i] = a;
    //             SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
    //                 new Glyph(a.Value - 1, 68, Color.Black, Color.Green));
    //             yield return new WaitForSeconds(0.1f);
    //         }
    //     }
    // }
}
[DataContract]
public class TraitFrenzied(int duration) : LimitedTrait("Frenzied", "Fr", duration, "FRENZIED: +1 attack die while insane!")
{
    // public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    // {
    //     flow.AttackerTraits.Add(this);
    //     yield return YellName();
    //     flow.AttackDicePreRoll.Add(new Die { Source = this });
    //     yield break;
    // }
}
[DataContract]
public class TraitEagleEyed(int duration) : LimitedTrait("Eagle Eyed", "Ey", duration, "EAGLE-EYED: +3 CLARITY for a short duration.")
{
    public TraitEagleEyed() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new Present_Notify(Description);
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity += 3;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield return new Present_Notify($"{character.GetName()} loses EAGLE-EYED.");
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}
[DataContract]
public class TraitProne(int duration) : LimitedTrait("Prone", "Pn", duration, "PRONE: Cannot move, no defenses, receives +1 damage per hit!")
{
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new Present_Notify(Description);
    }

    public override IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
    {
        if (character is PartyMember c)
        {
            c.IsDone = true;
        }
        else if (character is Enemy e)
        {
            e.IsDone = true;
        }

        yield return new WaitForSeconds(0.25f);
    }

    // public override IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    // {
    //     flow.DefenderTraits.Add(this);
    //     flow.DefenseDicePreRoll.Clear();
    //     yield break;
    // }
    //
    // public override IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow)
    // {
    //     flow.DefenderTraits.Add(this);
    //     flow.HitDieDamage++;
    //     yield break;
    // }
}
[DataContract]
public class TraitBlind(int duration) : LimitedTrait("Blind", "Bl", duration, "BLIND: Character's CLARITY becomes 0 for a number of turns.")
{
    public TraitBlind() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new Present_Notify($"{character}'s CLARITY becomes 0 for {Duration} turns!", true);
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity = 0;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield return new Present_Notify($"{character} can see again!");
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}
[DataContract]
public class TraitCritical(int duration) : LimitedTrait("Critical", "Cr", duration, "CRITICAL: Gives a chance to raise an attack roll to 6, or else...")
{
    // public override IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    // {
    //     if (flow.TotalIncomingDamage == 0)
    //     {
    //         flow.AttackerTraits.Add(this);
    //         flow.Attacker.GetAP().Add<StatusDeath>(2);
    //     }
    //
    //     yield break;
    // }
    //
    // public override IEnumerable AsAttacker_ModifyAttackRollDie(CombatFlow flow)
    // {
    //     if (flow.CurrentRoll is { } att)
    //     {
    //         if (att.Value == 6 || Rnd.Instance.D100 <= 5 * this.Duration)
    //         {
    //             flow.AttackerTraits.Add(this);
    //             flow.CurrentRoll.Value = 6;
    //             for (int i = 0; i < 10; i++)
    //             {
    //                 SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + flow.CurrentHitDieIndex + 1, 9,
    //                     new Glyph(5, 68, Color.Black, Color.Lerp(Color.Red, Color.Gray, 1 - ((float)i / 10.0f))));
    //                 yield return new WaitForSeconds(0.001f);
    //             }
    //         }
    //     }
    // }
    //
    // public override IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow)
    // {
    //     var (att, def) = flow.CurrentPair;
    //     if (att.Value == 6)
    //     {
    //         if (def != null)
    //         {
    //             flow.AttackerTraits.Add(this);
    //             def.Value = 0;
    //             flow.Defender.Stats.Poise = Math.Max(1, flow.Defender.Stats.Poise - 1);
    //             for (int i = 0; i < 10; i++)
    //             {
    //                 if (i > 3)
    //                 {
    //                     SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + flow.CurrentStrikeCount + 1, 10,
    //                         new Glyph(9, 68, Color.Black, Color.Lerp(Color.Gray, Color.Red, (float)i / 10.0f)));
    //                 }
    //                 yield return new WaitForSeconds(0.001f);
    //             }
    //         }
    //     }
    // }
}
[DataContract]
public class TraitCrippledLeftHand() : Trait("Crippled (Left)", "xL", "CRIPPLED (LEFT): Your left-hand weapon can no longer roll attacks.")
{
    // public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    // {
    //     if (flow.Attacker.GetLeftWeapon() != null)
    //     {
    //         flow.AttackerTraits.Add(this);
    //         yield return YellName();
    //         flow.AttackDicePreRoll.RemoveAll(d => d.Source == flow.Attacker.GetLeftWeapon());
    //     }
    // }
}
[DataContract]
public class TraitCrippledRightHand() : Trait("Crippled (Right)", "xR", "CRIPPLED (RIGHT): Your left-hand weapon can no longer roll attacks.")
{
    // public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    // {
    //     if (flow.Attacker.GetRightWeapon() != null)
    //     {
    //         flow.AttackerTraits.Add(this);
    //         yield return YellName();
    //         flow.AttackDicePreRoll.RemoveAll(d => d.Source == flow.Attacker.GetRightWeapon());
    //     }
    // }
}
[DataContract]
public class TraitParalyzed() : Trait("Paralyzed", "Pa", "PARALYZED: Your body can hardly move an inch...")
{
    public override IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
    {
        yield return YellName();
        if (character is PartyMember c)
        {
            c.IsDone = true;
        }
        else if (character is Enemy e)
        {
            e.IsDone = true;
        }
        yield  break;
    }
}