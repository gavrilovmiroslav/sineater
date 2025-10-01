using System;
using System.Collections;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class TraitSneaky() : Trait("Sneaky", "Sn")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        var right = flow.Attacker.GetRightWeapon();
        if (left == null || right == null) yield break;

        if ((int)left.Weight <= 6 && (int)right.Weight <= 6)
        {
            flow.AttackDicePreRoll.Add(new Die(this));
            yield return new CombatFlow_Notify($"SNEAKY: Added another attack dice to {flow.Attacker.GetName()}");
        }
    }
}

public class TraitProficient() : Trait("Proficient", "Pr")
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
                yield return new CombatFlow_Notify($"PROFICIENT: +1 damage on proficient hit!");
            }
        }
    }
}

public class TraitBalanced() : Trait("Balanced", "Ba")
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        var left = flow.Attacker.GetLeftWeapon();
        var right = flow.Attacker.GetRightWeapon();
        if (left == null || right == null) yield break;

        if (left.Weight == right.Weight)
        {
            flow.AttackDicePreRoll.Add(new Die(this));
            yield return new CombatFlow_Notify($"BALANCED: Added dice for balanced strike.");
        }
    }
}

public class TraitSkilled() : Trait("Skilled", "Sk")
{
    public override IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        if (flow.TotalIncomingDamage == 0)
        {
            flow.TotalIncomingDamage += 1;
            yield return new CombatFlow_Notify($"SKILLED: Skilled shot deals 1 damage.");
        }
    }
}

public class TraitPadded() : Trait("Padded", "Pd")
{
    public override IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow)
    {
        if (flow.TotalIncomingDamage > 0)
        {
            flow.TotalIncomingDamage -= 1;
            yield return new CombatFlow_Notify($"PADDED: Reducing damage by 1.");
        }
    }
}

public class TraitHeavy() : Trait("Heavy", "Hv")
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
        
        yield return new CombatFlow_Notify($"HEAVY: Added some dice if heavy weapons, probably");

    }
}

public class TraitWise() : Trait("Wise", "Ws")
{
    public override IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow)
    {
        var min = 100;
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            if (flow.AttackDiceRolled[i].Value < min) min = flow.AttackDiceRolled[i].Value;
        }
        
        for (var i = 0; i < flow.AttackDiceRolled.Count; i++)
        {
            if (flow.AttackDiceRolled[i].Value == min)
            {
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

public class TraitFrenzied(int duration) : LimitedTrait("Frenzied", "Fr", duration)
{
    public override IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow)
    {
        flow.AttackDicePreRoll.Add(new Die { Source = this });
        yield break;
    }
}

public class TraitEagleEyed(int duration) : LimitedTrait("Eagle Eyed", "Ey", duration)
{
    public TraitEagleEyed() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity += 3;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}

public class TraitCaptured(int duration) : LimitedTrait("Captured", "Cp", duration)
{
    public override IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
    {
        if (character is Character c)
        {
            level.CombatStates[c].Move = 0;
        }
        else if (character is Enemy e)
        {
            e.IsDone = true;
        }

        yield return new WaitForSeconds(0.5f);
    }
}

public class TraitBlind(int duration) : LimitedTrait("Blind", "Bl", duration)
{
    public TraitBlind() : this(5)
    {
    }

    private int _oldClarity = 0;
    public override IEnumerable ApplyOnReceived(ICharacter character)
    {
        _oldClarity = character.Stats.Clarity;
        character.Stats.Clarity = 0;
        yield break;
    }

    public override IEnumerable ApplyOnExpires(ICharacter character)
    {
        character.Stats.Clarity = _oldClarity;
        yield break;
    }
}