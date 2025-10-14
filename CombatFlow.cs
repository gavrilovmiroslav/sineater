using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER;

public interface ICombatFlowStep {}

public record struct CombatFlow_PresentAttacker(ICharacter Attacker) : ICombatFlowStep;
public record struct CombatFlow_PresentDefender(ICharacter Defender) : ICombatFlowStep;
public record struct CombatFlow_Notify(string Message, bool WaitKey = true) : ICombatFlowStep;
public record struct CombatFlow_PresentRollingAttackDie(int Index) : ICombatFlowStep;
public record struct CombatFlow_PresentAttackDie(int Index, int Value) : ICombatFlowStep;
public record struct CombatFlow_PresentRollingDefenseDie(int Index) : ICombatFlowStep;
public record struct CombatFlow_PresentDefenseDie(int Index, int Value) : ICombatFlowStep;
public record struct CombatFlow_DefenderArmorDented : ICombatFlowStep;
public record struct CombatFlow_PresentStrike(int Index, RolledDie Attack, RolledDie? Defense) : ICombatFlowStep; 
public record struct CombatFlow_PresentHitDie(int Index, int Value) : ICombatFlowStep;
public record struct CombatFlow_PresentDamagingHitDie(int Index) : ICombatFlowStep;
public record struct CombatFlow_PresentDamageDie(int Index, int Value) : ICombatFlowStep;
public record struct CombatFlow_TotalIncomingDamage(int TotalDamage) : ICombatFlowStep;
public record struct CombatFlow_PresentArmorDestroyed : ICombatFlowStep;
public record struct CombatFlow_ShatteredLeftWeapon : ICombatFlowStep;
public record struct CombatFlow_ShatteredRightWeapon : ICombatFlowStep;
public record struct CombatFlow_DefenderApplyWounds(int Count) : ICombatFlowStep;
public record struct CombatFlow_DefenderStumble : ICombatFlowStep;
public record struct CombatFlow_DefenderApplyStatus(Trait trait) : ICombatFlowStep;

public record struct Die(IAbilitySource Source)
{
    public RolledDie Roll => new RolledDie(this, Rnd.Instance.D6);
}

public class RolledDie(Die die, int value)
{
    public Die Die => die;
    public int Value { get; set; } = value;
}

public record struct AttackDefensePair(RolledDie Attack, RolledDie? Defense);

public interface ICombatFlowParticipant
{
    public IEnumerable AsAttacker_ApplyDiceCountModifiers(CombatFlow flow);
    public IEnumerable AsDefender_ApplyDiceCountModifiers(CombatFlow flow);
    
    public IEnumerable AsAttacker_ModifyAttackRollDie(CombatFlow flow);
    public IEnumerable AsDefender_ModifyDefenseRollDie(CombatFlow flow);
    public IEnumerable AsAttacker_ApplyCombatModifiers(CombatFlow flow);
    public IEnumerable AsDefender_ApplyCombatModifiers(CombatFlow flow);
    
    public IEnumerable AsAttacker_ApplyStrikeModifiers(CombatFlow flow);
    public IEnumerable AsDefender_ApplyStrikeModifiers(CombatFlow flow);
    
    public IEnumerable AsDefender_ApplyStrikeBlocked(CombatFlow flow);
    public IEnumerable AsDefender_ApplyArmorDented(CombatFlow flow);
    public IEnumerable AsAttacker_ApplyLeftWeaponShattered(CombatFlow flow);
    public IEnumerable AsAttacker_ApplyRightWeaponShattered(CombatFlow flow);
    public IEnumerable AsDefender_ApplyArmorDestroyed(CombatFlow flow);
    
    public IEnumerable AsAttacker_ApplyHitModifiers(CombatFlow flow);
    public IEnumerable AsDefender_ApplyHitModifiers(CombatFlow flow);
    
    public IEnumerable AsAttacker_DetermineHitDieDamage(CombatFlow flow);
    public IEnumerable AsDefender_DetermineHitDieDamage(CombatFlow flow);
    
    public IEnumerable AsAttacker_ApplyTotalIncomingDamageModifiers(CombatFlow flow);
    public IEnumerable AsDefender_ApplyTotalIncomingDamageModifiers(CombatFlow flow);
}

public class CombatFlow(ICharacter attacker, ICharacter defender)
{
    public ICharacter Attacker => attacker;
    public ICharacter Defender => defender;

    public HashSet<Trait> AttackerTraits = [];
    public HashSet<Trait> DefenderTraits = [];

    public List<Die> AttackDicePreRoll = [];
    public List<Die> DefenseDicePreRoll = [];
    public RolledDie CurrentRoll;
    public List<RolledDie> AttackDiceRolled = [];
    public List<RolledDie> DefenseDiceRolled = [];
    public List<RolledDie?> HitDice = [];

    public int TotalStrikeCount = 0;
    public int CurrentStrikeCount = 0;
    public AttackDefensePair CurrentPair;
    public int CurrentHitDieIndex = 0;
    public int HitDieDamage = 0;
    public int TotalIncomingDamage = 0;
    public bool ArmorDented = false;
    public int TotalArmorDentage = 0;
    public bool ShatteredLeftWeapon = false;
    public bool ShatteredRightWeapon = false;

    public IEnumerable Attack()
    {
        AttackDicePreRoll.Clear();
        DefenseDicePreRoll.Clear();
        CurrentRoll = null;
        AttackDiceRolled.Clear();
        DefenseDiceRolled.Clear();
        HitDice.Clear();

        TotalStrikeCount = 0;
        CurrentStrikeCount = 0;
        CurrentHitDieIndex = 0;
        HitDieDamage = 0;
        TotalIncomingDamage = 0;
        ArmorDented = false;
        TotalArmorDentage = 0;
        ShatteredLeftWeapon = false;
        ShatteredRightWeapon = false;

        // 0

        yield return new CombatFlow_PresentAttacker(attacker);
        yield return new CombatFlow_PresentDefender(defender);

        // 1

        foreach (var weapon in new[] { attacker.GetLeftWeapon(), attacker.GetRightWeapon() })
        {
            if (weapon != null)
            {
                for (int i = 0; i < weapon.Attack; i++)
                {
                    this.AttackDicePreRoll.Add(new Die(weapon));
                }
            }
        }

        yield return attacker.AsAttacker_ApplyDiceCountModifiers(this);

        // 2

        var guard = 0;
        if (defender.GetArmor() is { } armor)
        {
            for (var i = 0; i < armor.Guard; i++)
            {
                this.DefenseDicePreRoll.Add(new Die(armor));
            }
        }

        yield return defender.AsDefender_ApplyDiceCountModifiers(this);

        // 3

        yield return new CombatFlow_Notify(
            $"{attacker.GetName()} ({AttackDicePreRoll.Count}) attacks {defender.GetName()} ({DefenseDicePreRoll.Count})...",
            false);

        int n = 0;
        foreach (var die in AttackDicePreRoll)
        {
            CurrentHitDieIndex = n;
            yield return new CombatFlow_PresentRollingAttackDie(n);
            CurrentRoll = die.Roll;
            yield return attacker.AsAttacker_ModifyAttackRollDie(this);
            AttackDiceRolled.Add(CurrentRoll);
            yield return new CombatFlow_PresentAttackDie(n, CurrentRoll.Value);
            n++;
        }

        yield return attacker.AsAttacker_ApplyCombatModifiers(this);

        n = 0;
        foreach (var die in DefenseDicePreRoll)
        {
            CurrentHitDieIndex = n;
            yield return new CombatFlow_PresentRollingDefenseDie(n);
            CurrentRoll = die.Roll;
            yield return defender.AsDefender_ModifyDefenseRollDie(this);
            DefenseDiceRolled.Add(CurrentRoll);
            yield return new CombatFlow_PresentDefenseDie(n, CurrentRoll.Value);
            n++;
        }

        yield return defender.AsDefender_ApplyCombatModifiers(this);

        // 4

        TotalStrikeCount = AttackDiceRolled.Count;
        CurrentStrikeCount = 0;
        var defenseDiceQueue = new Queue<RolledDie>(DefenseDiceRolled);
        TotalArmorDentage = 0;
        foreach (var nextAttackDie in AttackDiceRolled)
        {
            if (defenseDiceQueue.TryDequeue(out var nextDefenseDie))
            {
                CurrentPair = new AttackDefensePair(nextAttackDie, nextDefenseDie);

                yield return new CombatFlow_PresentStrike(CurrentStrikeCount, CurrentPair.Attack, CurrentPair.Defense);
                yield return defender.AsDefender_ApplyStrikeModifiers(this);
                yield return attacker.AsAttacker_ApplyStrikeModifiers(this);

                var atk = CurrentPair.Attack.Value;
                var dfn = CurrentPair.Defense.Value;
                if (atk < dfn)
                {
                    HitDice.Add(null);
                    yield return new CombatFlow_PresentHitDie(CurrentStrikeCount, 0);
                    yield return defender.AsDefender_ApplyStrikeBlocked(this);
                }
                else if (atk == dfn)
                {
                    HitDice.Add(null);
                    yield return new CombatFlow_PresentHitDie(CurrentStrikeCount, -1);
                    ArmorDented = true;
                    TotalArmorDentage++;
                    yield return new CombatFlow_DefenderArmorDented();
                    yield return defender.AsDefender_ApplyArmorDented(this);
                }
                else if (atk > dfn)
                {
                    var hit = atk - dfn;
                    HitDice.Add(new RolledDie(nextAttackDie.Die, hit));
                    yield return new CombatFlow_PresentHitDie(CurrentStrikeCount, hit);
                }
            }
            else
            {
                CurrentPair = new AttackDefensePair(nextAttackDie, null);
                yield return new CombatFlow_PresentStrike(CurrentStrikeCount, nextAttackDie, null);
                var hitDie = nextAttackDie.Value;
                HitDice.Add(new RolledDie(nextAttackDie.Die, hitDie));
                yield return new CombatFlow_PresentHitDie(CurrentStrikeCount, hitDie);
            }

            CurrentStrikeCount += 1;
        }

        if (ArmorDented)
        {
            yield return new CombatFlow_Notify(
                $"The {defender.GetName()}'s armor got dented! Its guard will be reduced by {TotalArmorDentage}.");

            if ((defender.GetArmor()?.Guard ?? 0) == 0)
            {
                yield return new CombatFlow_Notify(
                    $"The {defender.GetName()}'s armor got wrecked!");
                yield return defender.AsDefender_ApplyArmorDestroyed(this);
                yield return new CombatFlow_PresentArmorDestroyed();
            }
        }

        // 5

        yield return attacker.AsAttacker_ApplyHitModifiers(this);
        yield return defender.AsDefender_ApplyHitModifiers(this);

        TotalIncomingDamage = 0;
        yield return new CombatFlow_Notify(
            $"The {defender.GetName()}'s POISE is {defender.Stats.Poise}, nothing under that does damage.", false);

        for (var index = 0; index < HitDice.Count; index++)
        {
            CurrentHitDieIndex = index;
            if (HitDice[index] != null && HitDice[index].Value >= defender.Stats.Poise)
            {
                yield return new CombatFlow_PresentDamagingHitDie(index);
                HitDieDamage = 1;
                if (HitDice[index].Die.Source is Weapon wpn)
                {
                    if ((int)wpn.Weight >= 5 && attacker.Stats.Vigor >= 5)
                    {
                        HitDieDamage += 1;
                    }
                }

                yield return attacker.AsAttacker_DetermineHitDieDamage(this);
                yield return defender.AsDefender_DetermineHitDieDamage(this);
                yield return new CombatFlow_PresentDamageDie(index, HitDieDamage);
                TotalIncomingDamage += HitDieDamage;
            }
        }

        yield return attacker.AsAttacker_ApplyTotalIncomingDamageModifiers(this);
        yield return defender.AsDefender_ApplyTotalIncomingDamageModifiers(this);
        yield return new CombatFlow_TotalIncomingDamage(TotalIncomingDamage);

        if (TotalIncomingDamage > 0)
        {
            yield return new CombatFlow_Notify(
                $"{Attacker.GetName()} does {TotalIncomingDamage} damage to {Defender.GetName()}.");
        }
        else
        {
            yield return new CombatFlow_Notify(
                $"{Attacker.GetName()} does no damage to {Defender.GetName()}.");
        }

        var ap = Defender.GetAP();
        if (TotalIncomingDamage > 0)
        {
            var wnd = ap.Count<StatusWounds>();
            var min = Math.Max(TotalIncomingDamage, wnd);
            var effect = Rnd.Instance.Next(min, TotalIncomingDamage + wnd);
            if (effect < 3)
            {
                yield return new CombatFlow_DefenderApplyWounds(TotalIncomingDamage);
                yield return new CombatFlow_Notify(
                    $"{Defender.GetName()} accrues {TotalIncomingDamage} wounds.");
            }
            else if (effect < 5)
            {
                var ws = (int)Math.Ceiling(TotalIncomingDamage * 1.33f);
                yield return new CombatFlow_DefenderApplyWounds(ws);
                yield return new CombatFlow_Notify(
                    $"A solid blow! {Defender.GetName()} receives {ws} wounds.");
            }
            else
            {
                yield return new CombatFlow_DefenderApplyWounds((int)Math.Ceiling(TotalIncomingDamage * 1.5f));
                yield return new CombatFlow_DefenderStumble();
                yield return new CombatFlow_Notify(
                    $"{Defender.GetName()} receives {TotalIncomingDamage} wounds, stumbling from the hit.");
            }
        }
    }
}