using System;
using System.Collections;
using System.Collections.Generic;

namespace SINEATER;

public interface IPresentation {}

public record struct Present_Notify(string Message, bool WaitKey = true) : IPresentation;
public record struct Present_AttackRolled : IPresentation;
public record struct Present_Crit(int index) : IPresentation;
public record struct Present_GuardBreak(int index) : IPresentation;
public record struct Present_ArmorDent(int index) : IPresentation;
public record struct Present_ArmorBreak(int index) : IPresentation;
public record struct Present_DealDamage(int index, int damage) : IPresentation;

public record struct Die(IAbilitySource Source)
{
    public RolledDie Roll => new RolledDie(this, Rnd.Instance.D6);
}

public class RolledDie(Die die, int value)
{
    public Die Die => die;
    public int Value { get; set; } = value;
}

public struct SkirmishFlow(CombatFlow parent, ICharacter attacker, Weapon? weapon, WeaponAttack? attack, ICharacter? defender, (int, int) position)
{
    public CombatFlow Parent => parent;
    public ICharacter Attacker => attacker;
    public Weapon? Weapon => weapon;
    public WeaponAttack? WeaponAttack => attack;
    public ICharacter? Defender { get; set; } = defender;
    public (int, int) Position => position;
    
    public List<Die> AttackDice = [];
    public List<RolledDie> AttackDiceRolled = [];
    
    public int DefenderArmor;
    public int DefenderPoise;
    public int TotalGuard;

    public int CritOn;
    public int OpeningsPerCrit;

    public bool IsCurrentDieCrit;
    public List<bool> Hits = [];
    public List<bool> Crits = [];

    public bool GuardBreak = false;
    public bool ArmorDented = false;
    public bool ArmorBreak = false;
    
    public IEnumerable Attack()
    {
        if (Defender == null)
        {
            yield break;
        }
        
        if (Weapon != null)
        {
            for (var i = 0; i < WeaponAttack?.Attack; i++)
            {
                AttackDice.Add(new Die(Weapon));
            }
        }
        
        yield return Attacker.GetTraits().AsAttacker_OnAttackDiceCount(this);
        yield return WeaponAttack?.Traits?.AsAttacker_OnAttackDiceCount(this);
        yield return Defender?.GetTraits().AsDefender_OnAttackDiceCount(this);
        for (var i = 0; i < AttackDice.Count; i++)
        {
            AttackDiceRolled.Add(new RolledDie(AttackDice[i], Rnd.Instance.D6));
        }
        
        yield return new Present_AttackRolled();
        yield return Defender?.GetTraits().AsDefender_OnAttackDiceRolled(this);
        yield return Attacker.GetTraits().AsAttacker_OnAttackDiceRolled(this);
        yield return WeaponAttack?.Traits?.AsAttacker_OnAttackDiceRolled(this);
        
        DefenderArmor = Defender?.GetArmor()?.Guard ?? 0;
        DefenderPoise = Defender?.Stats.Poise ?? 0;
        TotalGuard = DefenderArmor + DefenderPoise;

        yield return Defender?.GetTraits().AsDefender_OnGuardUp(this);
        yield return Attacker.GetTraits().AsAttacker_OnGuardUp(this);
        yield return WeaponAttack?.Traits?.AsAttacker_OnGuardUp(this);
        
        CritOn = WeaponAttack?.CritOn ?? 0;
        if (CritOn < TotalGuard) CritOn = TotalGuard;
        OpeningsPerCrit = WeaponAttack?.OpeningsPerCrit ?? 0;
        
        yield return Attacker.GetTraits().AsAttacker_OnCritChanceEstablished(this);
        yield return WeaponAttack?.Traits?.AsAttacker_OnCritChanceEstablished(this);
        yield return Defender?.GetTraits().AsDefender_OnCritChanceEstablished(this);

        yield return new Present_Notify($"Hit: {TotalGuard}+, Crit: {CritOn}+"); 

        Hits.Clear();
        Crits.Clear();

        for (var i = 0; i < AttackDiceRolled.Count; i++)
        {
            var die = AttackDiceRolled[i];

            if (die.Value >= CritOn)
            {
                IsCurrentDieCrit = true;
                yield return Defender?.GetTraits().AsDefender_OnCritHit(this);
                yield return Attacker.GetTraits().AsAttacker_OnCritHit(this);
                yield return WeaponAttack?.Traits?.AsAttacker_OnCritHit(this);

                if (IsCurrentDieCrit)
                {
                    Crits.Add(IsCurrentDieCrit);
                    yield return new Present_Crit(i);

                    if (!ArmorBreak)
                    {
                        DefenderArmor--;
                        if (DefenderArmor == 0)
                        {
                            ArmorBreak = true;
                            yield return Defender?.GetTraits().AsDefender_OnArmorBreak(this);
                            yield return Attacker.GetTraits().AsAttacker_OnArmorBreak(this);
                            yield return WeaponAttack?.Traits?.AsAttacker_OnArmorBreak(this);
                            if (ArmorBreak)
                            {
                                yield return new Present_ArmorBreak(i);
                            }
                        }
                        else
                        {
                            ArmorDented = true;
                            yield return Defender?.GetTraits().AsDefender_OnArmorDented(this);
                            yield return Attacker.GetTraits().AsAttacker_OnArmorDented(this);
                            yield return WeaponAttack?.Traits?.AsAttacker_OnArmorDented(this);
                            if (ArmorDented)
                            {
                                yield return new Present_ArmorDent(i);
                            }
                        }
                    }
                    else
                    {
                        yield return new Present_DealDamage(i, die.Value);
                    }
                }
            }
            else if (die.Value >= TotalGuard)
            {
                Hits.Add(true);
                yield return new Present_DealDamage(i, Math.Min(Math.Max(die.Value - TotalGuard, 1), 6));
            }
        }
    }

    public IEnumerable GainExp()
    {
        if (Weapon != null)
        {
            var hs = Hits.Count;
            var cs = Crits.Count;
            var h = Math.Ceiling(hs + 3 * cs - (this.AttackDice.Count - (hs + cs)) / 4.0f);
            var s = 1.0;
            s += ((int)Weapon.ClaScaling / 10.0f) * Attacker.Stats.Clarity;
            s += ((int)Weapon.WilScaling / 10.0f) * Attacker.Stats.Will;
            s += ((int)Weapon.PoiScaling / 10.0f) * Attacker.Stats.Poise;
            s += ((int)Weapon.VigScaling / 10.0f) * Attacker.Stats.Vigor;
            s = Math.Floor(s);
            var exp = (int)(h + s);
            Console.WriteLine($"+{exp} ({Weapon.ExperienceNow + exp}/{Weapon.ExperienceNeeded})");
            Weapon.ExperienceNow += exp;
            
            if (Weapon.ExperienceNow >= Weapon.ExperienceNeeded)
            {
                var message = $"{Weapon.GetName()} leveled up ({Weapon.Level} -> {Weapon.Level + 1})!";
                yield return new ShowPopupWindowWithPortraitAndWaitForKey(Attacker.GetPortait(),
                    (_, bnd) => { bnd.Add(message); },
                    true);
                Weapon.Level++;
                Weapon.ExperienceNow = 0;
            }
        }
    }
}

public class CombatFlow
{
    private CombatMapScreen _level;
    
    public CombatFlow(CombatMapScreen level, ICharacter attacker, Weapon? weapon, WeaponAttack attack, (int, int) position, (int, int) direction)
    {
        _level = level;
        Attacker = attacker;
        Weapon = weapon;
        WeaponAttack = attack;
        Position = position;
        Direction = direction;

        Update();
    }
    
    void Update()
    {
        Skirmishes.Clear();
        
        Dictionary<(int, int), ICharacter> chars = [];

        foreach (var p in SineaterGame.Instance.Party.Characters)
        {
            chars[(p.X, p.Y)] = p;
        }

        foreach (var e in _level.Enemies)
        {
            if (!chars.ContainsKey((e.X, e.Y)))
            {
                chars[(e.X, e.Y)] = e;
            }
        }
        
        var pos = Position;
        foreach (var step in WeaponAttack?.Steps ?? [])
        {
            if (step is SkirmishStep_StepForwards forwards)
            {
                for (var i = 0; i < forwards.n; i++)
                {
                    var (px, py) = Directions.GoForwards(pos, Direction);
                    if (px < 0 || py < 0 || px >= _level.Map.Width || py >= _level.Map.Height)
                    {
                        break;
                    }
                    
                    if (!_level.Map.IsWalkable(px, py))
                    {
                        break;
                    }

                    pos = (px, py);
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, null, null, null, pos));
                }
            }
            else if (step is SkirmishStep_StepBackwards backwards)
            {
                for (int i = 0; i < backwards.n; i++)
                {
                    var (px, py) = Directions.GoBackwards(pos, Direction);
                    if (!_level.Map.IsWalkable(px, py))
                    {
                        break;
                    }

                    pos = (px, py);
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, null, null, null, pos));
                }
            }
            else if (step is SkirmishStep_SidestepLeft sidestepLeft)
            {
                for (int i = 0; i < sidestepLeft.n; i++)
                {
                    var (px, py) = Directions.GoLeft(pos, Direction);
                    if (!_level.Map.IsWalkable(px, py))
                    {
                        break;
                    }

                    pos = (px, py);
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, null, null, null, pos));
                }
            }
            else if (step is SkirmishStep_SidestepRight sidestepRight)
            {
                for (int i = 0; i < sidestepRight.n; i++)
                {
                    var (px, py) = Directions.GoRight(pos, Direction);
                    if (!_level.Map.IsWalkable(px, py))
                    {
                        break;
                    }

                    pos = (px, py);
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, null, null, null, pos));
                }
            }
            else if (step is SkirmishStep_AttackFront front)
            {
                var (px, py) = Directions.GoForwards(pos, Direction, front.n);
                if (chars.ContainsKey((px, py)))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[(px, py)], pos));
                }
                else if (_level.Map?.IsWalkable(px, py) ?? false)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(px, py), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AttackBack back)
            {
                var (px, py) = Directions.GoBackwards(pos, Direction, back.n);
                if (chars.ContainsKey((px, py)))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[(px, py)], pos));
                }
                else if (_level.Map?.IsWalkable(px, py) ?? false)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(px, py), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AttackHand)
            {
                var (px, py) = (0, 0);
                if (Attacker.GetLeftWeapon() == Weapon)
                {
                    (px, py) = Directions.GoLeft(pos, Direction);
                }
                else
                {
                    (px, py) = Directions.GoRight(pos, Direction);
                }
                
                if (chars.ContainsKey((px, py)))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[(px, py)], pos));
                }
                else if (_level.Map?.IsWalkable(px, py) ?? false)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(px, py), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AttackLeft)
            {
                var (px, py) = Directions.GoLeft(pos, Direction);
                if (chars.ContainsKey((px, py)))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[(px, py)], pos));
                }
                else if (_level.Map?.IsWalkable(px, py) ?? false)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(px, py), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AttackRight)
            {
                var (px, py) = Directions.GoRight(pos, Direction);
                if (chars.ContainsKey((px, py)))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[(px, py)], pos));
                }
                else if (_level.Map?.IsWalkable(px, py) ?? false)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(px, py), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AttackRanged ranged)
            {
                bool canFly = true;
                (int, int) end = pos;
                foreach (var (x, y) in Bresenham.Line(pos.Item1, pos.Item2, 
                             ranged.position.Item1,
                             ranged.position.Item2))
                {
                    if (!_level.Map.IsWalkable(x, y))
                    {
                        end = (x, y);
                        break;
                    }
                    else
                    {
                        if (chars.ContainsKey((x, y)))
                        {
                            end = (x, y);
                            break;
                        }
                    }
                }

                if (end == ranged.position)
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, chars[end], pos));
                }
                else if (_level.Map.IsWalkable(end.Item1, end.Item2))
                {
                    Skirmishes.Add(new SkirmishFlow(this, Attacker, Weapon, WeaponAttack, Character.Dummy(end.Item1, end.Item2), pos));
                }
                else break;
            }
            else if (step is SkirmishStep_AddTrait tr)
            {
                if (chars.ContainsKey(pos))
                {
                    chars[pos].AddTrait(tr.trait);
                }
            }
        }
    }
    
    public ICharacter Attacker { get; set; }
    public Weapon? Weapon { get; set; }
    public WeaponAttack? WeaponAttack { get; set; }
    public List<SkirmishFlow> Skirmishes { get; set; } = [];
    public (int, int) Position { get; set; }
    
    private (int, int) _direction;
    public (int, int) Direction
    {
        get => _direction;
        
        set
        {
            _direction = value;
            Update();
        }
    }

    public int Score()
    {
        int score = 0;
        foreach (var sk in Skirmishes)
        {
            if (sk.Defender is Enemy { } e)
            {
                score += 5;
            }
            else if (sk.Defender is { } o)
            {
                score += 3;
            }
        }
        
        return score;
    }
}