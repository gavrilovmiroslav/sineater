using System;
using System.Collections;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class TraitSneaky() : Trait("Sneaky", "Sn", "SNEAKY: If both weapons are light-weight, +1 attack die.")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        var right = flow.Attacker.GetRightWeapon();
        if (left == null || right == null) yield break;

        if ((int)left.Weight < 6 && (int)right.Weight < 6)
        {
            flow.AttackDicePreRoll.Add(new Die(this));
            yield return new CombatFlow_Notify(Description);
        }
    }
}

public class TraitProficient() : Trait("Proficient", "Pr", "PROFICIENT: +1 damage on first hit!")
{
    public override IEnumerable AsAttacker_DetermineHitDieDamage(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        var right = flow.Attacker.GetRightWeapon();
        if (left == null || right == null) yield break;
        
        if ((int)left.Weight <= 6 && (int)right.Weight <= 6)
        {
            if (flow.CurrentHitDieIndex == 0)
            {
                flow.HitDieDamage += 1;
                for (int i = 0; i <= 10; i++)
                {
                    SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + flow.CurrentHitDieIndex + 1, 12,
                        new Glyph(flow.HitDieDamage - 1, 68, Color.Black, Color.Lerp(Color.Blue, Color.CornflowerBlue, (float)i / 10.0f)));
                    yield return new WaitForSeconds(0.01f);
                }
                yield return new CombatFlow_Notify(this.Description);
            }
        }
    }
}

public class TraitBalanced() : Trait("Balanced", "Ba", "BALANCED: If both weapons are the same weight, +1 attack die.")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        var right = flow.Attacker.GetRightWeapon();
        if (left == null || right == null) yield break;

        if (left.Weight == right.Weight)
        {
            flow.AttackDicePreRoll.Add(new Die(this));
            yield return new CombatFlow_Notify(Description);
        }
    }
}

public class TraitSkilled() : Trait("Skilled", "Sk", "SKILLED: Skilled shot deals 1 damage.")
{
    public override IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        if (flow.TotalIncomingDamage == 0)
        {
            flow.TotalIncomingDamage += 1;
            yield return new CombatFlow_Notify(Description);
        }
    }
}

public class TraitPadded() : Trait("Padded", "Pd", "PADDED: Reducing incoming damage by 1.")
{
    public override IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        if (flow.TotalIncomingDamage > 0)
        {
            flow.TotalIncomingDamage -= 1;
            yield return new CombatFlow_Notify(Description);
        }
    }
}

public class TraitHeavy() : Trait("Heavy", "Hv", "HEAVY: Add +1 attack die for each heavy weapon.")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        if (left != null && (int)left.Weight > 6)
        {
            flow.AttackDicePreRoll.Add(new Die(left));
        }
        
        var right = flow.Attacker.GetRightWeapon();
        if (right != null && (int)right.Weight > 6)
        {
            flow.AttackDicePreRoll.Add(new Die(right));
        }
        
        yield return new CombatFlow_Notify(Description);
    }
}

public class TraitWise() : Trait("Wise", "Ws", "WISE: Reroll the lowest attack dice.")
{
    public override IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    {
        yield return new CombatFlow_Notify(Description);
        var min = 100;
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            if (flow.AttackDiceRolled[i].Value < min) min = flow.AttackDiceRolled[i].Value;
        }
        // find minimal attack die value
        
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            if (flow.AttackDiceRolled[i].Value == min)
            {
                // reroll minimal values
                for (int j = 0; j < 10; j++)
                {
                    if (j % 2 == 0)
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
                            new Glyph(flow.HitDieDamage - 1, 68, Color.Black, Color.Gray));
                    }
                    else
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
                            new Glyph(0, 0, Color.Black, Color.Black));
                    }

                    yield return new WaitForSeconds(0.01f);
                }

                for (int j = 0; j <= 10; j++)
                {
                    SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
                    yield return new WaitForSeconds(0.01f);
                }
                var a = flow.AttackDiceRolled[i];
                a.Value = Math.Min(6, a.Value + 1);
                flow.AttackDiceRolled[i] = a;
                SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + i + 1, 9,
                    new Glyph(a.Value - 1, 68, Color.Black, Color.Green));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}

public class TraitFrenzied(int duration) : LimitedTrait("Frenzied", "Fr", duration, "FRENZIED: +1 attack die while insane!")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield return new CombatFlow_Notify(Description);
        flow.AttackDicePreRoll.Add(new Die { Source = this });
        yield break;
    }
}

public class TraitEagleEyed(int duration) : LimitedTrait("Eagle Eyed", "Ey", duration, "EAGLE-EYED: +3 CLARITY for a short duration.")
{
    public TraitEagleEyed() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new CombatFlow_Notify(Description);
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity += 3;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield return new CombatFlow_Notify($"{character.GetName()} loses EAGLE-EYED.");
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}

public class TraitCaptured(int duration) : LimitedTrait("Captured", "Cp", duration, "CAPTURED: Cannot move, have no defenses, receives +1 damage per hit!")
{
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new CombatFlow_Notify(Description);
    }

    public override IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
    {
        if (character is PartyMember c)
        {
            level.CombatStates[c].Move = 0;
        }
        else if (character is Enemy e)
        {
            e.IsDone = true;
        }

        yield return new WaitForSeconds(0.5f);
    }

    public override IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow)
    {
        flow.DefenseDicePreRoll.Clear();
        yield break;
    }

    public override IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow)
    {
        flow.HitDieDamage++;
        yield break;
    }
}

public class TraitBlind(int duration) : LimitedTrait("Blind", "Bl", duration, "BLIND: Character's CLARITY becomes 0 for a number of turns.")
{
    public TraitBlind() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield return new CombatFlow_Notify($"{character}'s CLARITY becomes 0 for {Duration} turns!");
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity = 0;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield return new CombatFlow_Notify($"{character} can see again!");
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}

public class TraitCritical(int duration) : LimitedTrait("Critical", "Cr", duration, "CRITICAL: Gives a chance to raise an attack roll to 6, or else...")
{
    public override IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        if (flow.TotalIncomingDamage == 0)
        {
            flow.Attacker.GetAP().Add<StatusDeath>(2);
        }

        yield break;
    }

    public override IEnumerable AsAttacker_ModifyAttackRollDie(CombatFlow flow)
    {
        if (flow.CurrentRoll is { } att)
        {
            if (att.Value == 6 || Rnd.Instance.D100 <= 5 * this.Duration)
            {
                flow.CurrentRoll.Value = 6;
                for (int i = 0; i < 10; i++)
                {
                    SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + flow.CurrentHitDieIndex + 1, 9,
                        new Glyph(5, 68, Color.Black, Color.Lerp(Color.Red, Color.Gray, 1 - ((float)i / 10.0f))));
                    yield return new WaitForSeconds(0.001f);
                }
            }
        }
    }

    public override IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow)
    {
        var (att, def) = flow.CurrentPair;
        if (att.Value == 6)
        {
            if (def != null)
            {
                def.Value = 0;
                flow.Defender.Stats.Poise = Math.Max(1, flow.Defender.Stats.Poise - 1);
                for (int i = 0; i < 10; i++)
                {
                    if (i > 3)
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(2 + 25 + flow.CurrentStrikeCount + 1, 10,
                            new Glyph(9, 68, Color.Black, Color.Lerp(Color.Gray, Color.Red, (float)i / 10.0f)));
                    }
                    yield return new WaitForSeconds(0.001f);
                }
            }
        }
    }
}

public class TraitCrippledLeftHand() : Trait("Crippled (Left)", "xL", "CRIPPLED (LEFT): Your left-hand weapon can no longer roll attacks.")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield return new CombatFlow_Notify(Description);
        flow.AttackDicePreRoll.RemoveAll(d => d.Source == flow.Attacker.GetLeftWeapon());
        yield break;
    }
}

public class TraitCrippledRightHand() : Trait("Crippled (Right)", "xR", "CRIPPLED (RIGHT): Your left-hand weapon can no longer roll attacks.")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        yield return new CombatFlow_Notify(Description);
        flow.AttackDicePreRoll.RemoveAll(d => d.Source == flow.Attacker.GetRightWeapon());
        yield break;
    }
}

public class TraitParalyzed() : Trait("Paralyzed", "Pa", "PARALYZED: Your body can hardly move an inch...")
{
    public override IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
    {
        yield return new CombatFlow_Notify(Description);
        if (character is PartyMember c)
        {
            level.CombatStates[c].Move = 1;
        }
        else if (character is Enemy e)
        {
            e.IsDone = true;
        }
        yield  break;
    }
}