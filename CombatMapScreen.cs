using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Content;
using Wintellect.PowerCollections;
using YamlDotNet.Core.Tokens;

namespace SINEATER;

public enum ETerrainKind
{
    Tomb,
    Temple,
    Cave,
    Clearing,
    Ruin,
}

public class CombatConfig
{
    public int Phase;
    public Trait? Reward;
    public int Sin;
    public ETerrainKind Terrain;
}

public class CombatMapScreen : IScreen
{
    public static CombatMapScreen? Level = null;

    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private readonly int _fullWidth = 20, _fullHeight = 20;
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    public float[,] Distance = null;
    public LevelStructure Structure;
    private bool _rendered = false;
    private bool _debugView = false;
    private int _time = 0;
    public int PlayerSelectedIndex = 0;
    private Glyph[,] _groundGlyphs;
    internal CoroutineHandler CoroutineHandler = new();

    public Domains Domains;
    public IMap? Map => Structure.Map;
    
    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = _fullWidth - 2;
            this._height = _fullHeight - 2;
        }

        Regenerate();
    }
    
    private void Regenerate() => Regenerate(_kind);
    private int _extraFill = 0;
    private readonly CombatConfig? _config;

    public CombatMapScreen(SineaterGame game, CombatConfig? config = null, int width = -1, int height = -1, string title = "???")
    {
        Level = this;
        Structure = new LevelStructure();
        _config = config;
        _width = width;
        _height = height;

        Domains = new(this);
        
        _kind = _config?.Terrain ?? ETerrainKind.Cave;
        _game = game;
        _groundGlyphs = new Glyph[_fullWidth, _fullHeight];
        Initialize(game);
        Regenerate(_width == -1 || _height == -1);
        UpdateAttackSelections();
    }

    public void Initialize(SineaterGame game)
    {
        _game = game;
    }
    
    private void Regenerate(ETerrainKind kind)
    {
        CoroutineHandler.Clear();
        _kind = kind;
        var (a, b, c, d, e) = (0, 0, 0, _width, _height);
        switch (_kind)
        {
            case ETerrainKind.Tomb:
                (a, b, c) = (36, 2, 2); //36
                break;
            case ETerrainKind.Temple:
                (a, b, c) = (16, 6, 2); //45
                break;
            case ETerrainKind.Cave:
                (a, b, c) = (47, 4, 4); //47
                break;
            case ETerrainKind.Clearing:
                (a, b, c) = (54, 3, 1); //49
                break;
            case ETerrainKind.Ruin:
                (a, b, c) = (20, 4, 2); //89
                break;
            default:
                (a, b, c) = (Rnd.Instance.Next(1, 99), Rnd.Instance.D6, Rnd.Instance.D6);
                break;
        }

        Console.WriteLine($"Fill probability: {a}, iterations: {b}, cutoff: {c}, size: {_width} x {_height}");

        IMapCreationStrategy<Map>? mapCreationStrategy = null;

        if (_width > _fullWidth - 1 || _height > _fullHeight - 1)
        {
            throw new Exception($"MAP CAN'T BE LARGER THAN {_fullWidth - 1}x{_fullHeight - 1} (is {_width}x{_height})");
        }

        if (_kind is ETerrainKind.Ruin or ETerrainKind.Temple or ETerrainKind.Tomb)
        {
            mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(_width, _height, a, b, c, Rnd.Instance);
        }
        else
        {
            mapCreationStrategy = new CaveMapCreationStrategy<Map>(_width, _height, a, b, c, Rnd.Instance);
        }
        
        var inner = RogueSharp.Map.Create(mapCreationStrategy);
        var map = RogueSharp.Map.Create(new FilledMapCreationStrategy<Map>(_fullWidth, _fullHeight));
        map.Copy(inner, 0, 0);

        Structure = new LevelStructure(map);
        
        for (var i = 0; i < _fullWidth; i++)
        {
            for (var j = 0; j < _fullHeight; j++)
            {
                var g = Glyph.Bw(0, 0);
                if (Structure.Map.IsWalkable(i, j))
                {
                    (g.U, g.V) = _game.Layers["mrmo"].Char('.');
                }
                else
                {
                    g.U = Rnd.Instance.Next(6, 12);
                    g.V = Rnd.Instance.Next(5, 6);
                }

                _groundGlyphs[i, j] = g;
            }
        }

        _rendered = false;

        var vas = Structure.Map.GetAllCells().Where(t => t.IsWalkable).ToArray();
        if (vas.Length <= 50)
        {
            _extraFill++;
            Regenerate();
            return;
        }

        vas.Shuffle();
    }
    
    public IEnumerable EnemyMove(Enemy enemy)
    {
        _currentEnemy = enemy;
        var (x, y) = enemy.GetIcon(true);
        SineaterGame.Instance.Layers["mrmo"].Set(enemy.X, enemy.Y + 2, new Glyph(x, y, Color.Black, enemy.GetTint()));
        yield return new WaitForSeconds(0.1f);
        if (enemy.Stats.Clarity == 0)
        {
            yield return new BehaviorBlind().Do(enemy, this, enemy.X, enemy.Y);
            yield return new WaitForSeconds(0.1f);
            (x, y) = enemy.GetIcon(false);
            SineaterGame.Instance.Layers["mrmo"].Set(enemy.X, enemy.Y + 2, new Glyph(x, y, Color.Black, enemy.GetTint()));
            yield break;
        }
            
        var beh = enemy.Behaviors[0];
        enemy.Behaviors.RemoveAt(0);
        if (!beh.ShouldFizzleOut())
        {
            enemy.Behaviors.Add(beh);
        }

        yield return beh.Do(enemy, this, enemy.X, enemy.Y);
        yield return new WaitForSeconds(0.1f);
        
        (x, y) = enemy.GetIcon(false);
        
        enemy.Wait = enemy.Stats.Vigor;
        SineaterGame.Instance.Layers["mrmo"].Set(enemy.X, enemy.Y + 2, new Glyph(x, y, Color.Black, enemy.GetTint()));
        
        _currentEnemy = null;
    }

    public void Update(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }

        if (Keyboard.GetState().IsKeyDown(Keys.Space))
        {
            Regenerate();
        }

        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }

        CheckInputs();
    }

    internal void DrawCombat(bool onlyNow = false)
    {
        var index = 0;
        
        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(_fullWidth - 1, _fullHeight + 2), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(_fullWidth * 2 - 2, _fullHeight * 2 + 2), ' ');
        
        _game.ActionPoints.Draw(1, 25);

        foreach (var w in _game.Party.Characters)
        {
            if (w.Job == ECharacterClass.Witch)
            {
                _game.ActionPoints.DrawCursor(w.X * 2 + 1, 25);
            }
        }

        index = 0;
        for (var i = 0; i < _fullWidth; i++)
        {
            for (var j = 0; j < _fullHeight; j++)
            {
                if (Structure.Map.IsWalkable(i, j))
                {
                    var g = Glyph.Bw(_groundGlyphs[i, j].U, _groundGlyphs[i, j].V);
                    g.Fg = Color.White;
                    g.Bg = (i % 2 == j % 2) ? new Color(10, 0, 0, 1) : new Color(20, 10, 0, 1);
            
                    _game.Layers["mrmo"].Set(i, j, g);
                }
                else
                {
                    if (onlyNow) continue;
                    var g = _groundGlyphs[i, j];
                    if (g == null) continue;
                    _game.Layers["mrmo"].Set(i, j, new Glyph(g.U, g.V, Color.Black, Color.Lerp(Color.Black, Color.Gray, 1)));

                    if (1 >= 1.0f)
                    {
                        _game.Layers["mrmo"].Set(i, j, new Glyph(g.U, g.V, Color.Black, Color.White));
                    }
                }
            }
        }
        
        foreach (var domain in Domains._domains)
        {
            domain.Draw(this);
        }
        
        // foreach (var ((x, y), item) in Floor)
        // {
        //     _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, item.GetIcon());
        // }
        //
        // foreach (var enemy in _enemies)
        // {
        //     var (ix, iy) = enemy.Icon;
        //     var c = enemy.GetTint();
        //     if (enemy.Traits.Count > 0) c = Color.Lerp(c, Color.Gold, 0.6f);
        //     _game.Layers["mrmo"].Set(enemy.X + _offsetX, enemy.Y + _offsetY, new Glyph(ix, iy, Color.Black, c));
        // }
        
        foreach (var chr in _game.Party.Characters)
        {
            var (ix, iy) = chr.Job.GetImage();
            var hasStamina = _game.ActionPoints.Count<StatusStamina>() > 0;
            if (chr.IsDone || !hasStamina)
            {
                _game.Layers["mrmo"].Set(chr.X, chr.Y,
                    new Glyph(ix, iy, Color.Black, Color.DarkGray));
            }
            else
            {
                _game.Layers["mrmo"].Set(chr.X, chr.Y,
                    new Glyph(ix, iy, Color.Black, chr.Tint));
            }

            index++;
        }
        
        // foreach (var d in Structure.Map.GetAllCells().Where(c => !c.IsWalkable) ?? [])
        // {
        //     var dt = Structure.Obstacles.GetDistance(d.X, d.Y);
        //     if (dt != -1)
        //     {
        //         _game.Layers["mrmo"].Set(d.X, d.Y, $"{distances[dt]}",
        //             Color.Lerp(Color.Purple, Color.Blue, (float)dt / (float)30.0f));
        //     }
        // }

        var max = Structure.Walkables.MaxDistance();
        var dm = Structure.Walkables.Distances[0];
        var fov = new FieldOfView<Cell>(Map);
        var pred = (IMap<Cell> mp, int mx, int my) => dm.Get(mx, my) >= 2 && fov.IsInFov(mx, my);

        foreach (var (hx, hy) in Structure.Heat.GetAll())
        {
            _game.Layers["mrmo"].Set(hx, hy, $".", Structure.Heat.Get(hx, hy));
        }
        
        var distances = "0123456789abcdefghijklmno0123456789abcdefghijklmno".ToCharArray();
        foreach (var d in Structure.Map.GetAllCells().Where(c => c.IsWalkable) ?? [])
        {
            var dt = Structure.Walkables.GetDistance(d.X, d.Y);
            if (dt != -1)
            {
                _game.Layers["mrmo"].Set(d.X, d.Y, $"{distances[dt]}",
                    Color.Lerp(Color.Green, Color.Red, (float)dt / (float)15.0f));
            }
        }

        var (ex, ey) = Structure.Entry;
        _game.Layers["mrmo"].Set(ex, ey, $"E", Color.Yellow);
        
        var (gx, gy) = Structure.Goals[0];
        _game.Layers["mrmo"].Set(gx, gy, new Glyph(13, 60, Color.Black, Color.Lerp(Color.Red, Color.Yellow, Rnd.Instance.Next01())));
        
        foreach (var chr in Structure.Enemies)
        {
            var (cu, cv) = chr.Icon;
            _game.Layers["mrmo"].Set(chr.X, chr.Y, new Glyph(cu, cv, Color.Black, chr.Tint));
        }
        
        foreach (var chr in Structure.Treasure)
        {
            _game.Layers["mrmo"].Set(chr.Item1, chr.Item2, "?", Color.White);
        }
    }

    public bool SkipGUI { get; set; } = false;

    private int _offset = 96;
    
    private void DrawCharacterCard(ICharacter? chr, int h = 12, int dp = 0, bool header = true)
    {
        if (KB.HasBeenPressed(Keys.V)) _offset--;
        if (KB.HasBeenPressed(Keys.B)) _offset++;
        
        if (chr == null) return;
        if (chr is Dummy) return;
        
        var (ix, iy) = (0, 0);
        if (chr is Enemy e)
        {
            (ix, iy) = e.Icon;
        }
        else if (chr is PartyMember p)
        {
            (ix, iy) = p.Job.GetImage();
        }
        var tint = chr.GetTint();
        if (chr is PartyMember pm)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, h + 1,
                new Glyph(ix, iy, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, h + 1, chr.GetName(),
                Color.Lerp(Color.White, tint, 0.5f));

            var ph = (h + 1) / 2 + dp;
            var (u, v) = chr.GetPortait(); 
            _game.Layers["porsmol"].Set(10, ph, new Glyph(u, v, Color.Black, tint));

            (u, v) = ItemLibrary.EmptyUv;
            var dh = 1;
            var opt = 0;
            _game.Layers["porsmol"].Set(11, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["mini"].Set(2 * _fullWidth + _offset - 20, h + 10, $"  ");
            _game.Layers["mini"].Set(2 * _fullWidth + 69, h + 11, $"                                                 ");
            if (chr.GetLeftWeapon() is { } lw)
            {
                (u, v) = lw.Picture;
                _game.Layers["ascii"].Set(2 * _fullWidth + 1, h + 7 + dh, $"[ LH ] {lw.GetName()}", tint);
                foreach (var att in lw.GetAvailableAttacks())
                {
                    dh++;
                    opt++;
                    _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 7 + dh, $" ({opt}) {att.Thing.Name}");
                    if (_confirmedCombatFlow != null && _confirmedCombatFlow.WeaponAttack != null &&
                        _confirmedCombatFlow.WeaponAttack == att.Thing)
                    {
                        _game.Layers["ascii"].Set(2 * _fullWidth + 3 - 1, h + 7 + dh, $">");
                    }
                }
                var exp = $"{(lw.ExperienceNow * 100 / lw.ExperienceNeeded)}%";
                _game.Layers["mini"].Set(2 * _fullWidth + 70, h + 11, $"L{lw.Level}");
                _game.Layers["mini"].Set(2 * _fullWidth + 77 - exp.Length, h + 11, exp);
                dh++;
                dh++;
            }
            _game.Layers["porsmol"].Set(11, ph, new Glyph(u, v, Color.Black, tint));

            (u, v) = ItemLibrary.EmptyUv;
            _game.Layers["porsmol"].Set(12, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["mini"].Set(2 * _fullWidth + _offset - 10, h + 7 + dh, $"  ");
            if (chr.GetRightWeapon() is { } rw)
            {
                (u, v) = rw.Picture;
                _game.Layers["ascii"].Set(2 * _fullWidth + 1, h + 7 + dh, $"[ RH ] {rw.GetName()}", tint);
                foreach (var att in rw.GetAvailableAttacks())
                {
                    dh++;
                    opt++;
                    _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 7 + dh, $" ({opt}) {att.Thing.Name}");
                    if (_confirmedCombatFlow != null && _confirmedCombatFlow.WeaponAttack != null &&
                        _confirmedCombatFlow.WeaponAttack == att.Thing)
                    {
                        _game.Layers["ascii"].Set(2 * _fullWidth + 3 - 1, h + 7 + dh, $">");
                    }
                }
                var exp = $"{(rw.ExperienceNow * 100 / rw.ExperienceNeeded)}%";
                _game.Layers["mini"].Set(2 * _fullWidth + 80, h + 11, $"L{rw.Level}");
                _game.Layers["mini"].Set(2 * _fullWidth + 87 - exp.Length, h + 11, exp);
            }

            _game.Layers["porsmol"].Set(12, ph, new Glyph(u, v, Color.Black, tint));
        
            (u, v) = ItemLibrary.EmptyUv;
            _game.Layers["porsmol"].Set(13, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["mini"].Set(2 * _fullWidth + _offset, h + 10, $"  ");
            if (chr.GetArmor() is { } ar)
            {
                (u, v) = ar.Picture;
                _game.Layers["mini"].Set(2 * _fullWidth + 89, h + 11, $" GUARD {ar.Guard}");
            }
            _game.Layers["porsmol"].Set(13, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 1, h + 6, $"STEPS {pm.Steps}/{chr.Stats.Vigor}", tint);
        }
        else if (chr is Enemy en)
        {
            if (header)
            {
                _game.Layers["ascii"].Set(2 * _fullWidth - 1, h, "NAME       GRD  POI  LIF");
            }
            else
            {
                h--;
            }

            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, h + 1,
                new Glyph(ix, iy, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, h + 1, chr.GetName(),
                Color.Lerp(Color.White, tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, h + 1, (chr.GetArmor()?.Guard.ToString() ?? "--"),
                Color.Lerp(Color.White, tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 12, h + 1, (chr.Stats.Poise.ToString() ?? "--"),
                Color.Lerp(Color.White, tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 17, h + 1, chr.HP.ToString(),
                Color.Lerp(Color.White, tint, 0.5f));
        }
    }
    
    public void Draw(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();
        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth + 40, 40), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth * 2 + 40, 40), ' ');

        DrawCombat();
    }

    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }
    }
    
    private IEnumerable CombatAlgebra(SkirmishFlow flow, IPresentation step)
    {
        if (flow.Defender is Enemy enm)
        {
            enm.LastHit = flow.Attacker;
        }
        
        if (step is Present_Notify notif)
        {
            SineaterGame.Instance.Layers["ascii"].SetRect(new Vector2(20, 0), new Vector2(55, 1), ' ');
            SineaterGame.Instance.Layers["ascii"].Set(21, 0, notif.Message);
        }
        else if (step is Present_AttackRolled atk)
        {
            SineaterGame.Instance.Layers["ascii"].Set(1, 0, "ATK");
            SineaterGame.Instance.Layers["ascii"].Set(1, 1, "DMG");
            yield return new WaitForSeconds(0.1f);

            _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(45, 2), ' ');
            for (int i = 0; i < 6; i++)
            {
                for (int d = 0; d < flow.AttackDiceRolled.Count; d++)
                {
                    _game.Layers["mrmo"].Set(3 + d, 0,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Lerp(Color.Gray, Color.White, i / 5.0f)));
                }

                yield return new WaitForSeconds(0.1f);
            }

            for (int i = 0; i < flow.AttackDiceRolled.Count; i++)
            {
                _game.Layers["mrmo"].Set(3 + i, 0,
                    new Glyph(flow.AttackDiceRolled[i].Value - 1, 68, Color.Black, Color.Green));
                for (int d = i + 1; d < flow.AttackDiceRolled.Count; d++)
                {
                    _game.Layers["mrmo"].Set(3 + d, 0,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.White));
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
        else if (step is Present_Crit crit)
        {
            _game.Layers["mrmo"].Set(3 + crit.index, 1,
                new Glyph(8, 68, Color.Black, Color.Gold));
            flow.Attacker.GetAP().Gain(flow.WeaponAttack?.OpeningsPerCrit ?? 1);
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_ArmorDent dent)
        {
            _game.Layers["mrmo"].Set(3 + dent.index, 1,
                new Glyph(6, 68, Color.Black, Color.Yellow));
            if (flow.Defender is { } d)
            {
                if (d.GetArmor() is { } a)
                {
                    a.Guard--;
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        else if (step is Present_ArmorBreak brk)
        {
            _game.Layers["mrmo"].Set(3 + brk.index, 1,
                new Glyph(6, 68, Color.Black, Color.Red));
            if (flow.Defender.GetArmor().Guard < 0)
            {
                flow.Defender.RemoveArmor();
            }
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_GuardBreak grd)
        {
            _game.Layers["mrmo"].Set(3 + grd.index, 1,
                new Glyph(10, 68, Color.Black, Color.Red));
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_DealDamage dmg)
        {
            _game.Layers["mrmo"].Set(3 + dmg.index, 1,
                new Glyph(dmg.damage - 1, 68, Color.Black, Color.Red));
            if (flow.Defender is PartyMember p)
            {
                p.GetAP().AddN<StatusWounds>(dmg.damage);
            }
            else if (flow.Defender is Enemy e)
            {
                e.HP -= dmg.damage;
                if (e.HP <= 0)
                {
                    e.IsDead = true;
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerable PreviewAttack(CombatFlow flow, IEnumerable log)
    {
        foreach (var part in log)
        {
            if (part is IEnumerable enm)
            {
                // PROCESS TRAITS COMPLETELY
                foreach (var p in enm)
                {
                    if (p is IEnumerable e)
                    {
                        Coroutine.Consume(e);
                    }
                }
            }
            else if (part is IPresentation step) 
            {
                // SKIP COMBAT ON PURPOSE!
            }
            else
            {
                yield return part;
            }
        }
    }
    
    private IEnumerable ResolveAttack(SkirmishFlow flow, IEnumerable log)
    {
        foreach (var part in log)
        {
            if (part is IEnumerable enm)
            {
                yield return ResolveAttack(flow, enm);
            }
            else if (part is IPresentation step) 
            {
                yield return CombatAlgebra(flow, step);
            }
            else
            {
                yield return part;
            }
        }
    }
    
    public IEnumerable Attack(CombatFlow flow)
    {
        if (flow.Weapon != null)
        {
            Console.WriteLine(
                $"{flow.Weapon} (level {flow.Weapon.Level}, needed {flow.Weapon.ExperienceNeeded}); base: {flow.Weapon.ScalingBase}, scale: {flow.Weapon.ScalingCurve}, quality: {flow.Weapon.Quality}");
        }

        yield return flow.Attacker.GetTraits().OnCombatStarts(flow);
        yield return flow.WeaponAttack?.Traits?.OnCombatStarts(flow);
        
        foreach (var skirmish in flow.Skirmishes)
        {
            yield return flow.Attacker.GetTraits().OnSkirmishStarts(skirmish);
            yield return flow.WeaponAttack?.Traits?.OnSkirmishStarts(skirmish);

            var (ox, oy) = (flow.Attacker.X, flow.Attacker.Y);
            var (x, y) = skirmish.Position;
            Positions.Swap((x, y), (ox, oy));
            DrawCombat();
            if (skirmish.Defender != null && skirmish.Defender is not Dummy)
            {
                yield return skirmish.Defender.GetTraits().OnSkirmishStarts(skirmish);

                yield return ResolveAttack(skirmish, skirmish.Attack());
                yield return new WaitForSeconds(0.5f);
                SineaterGame.Instance.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(45, 2), ' ');
                SineaterGame.Instance.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(22, 2), ' ');
                if (skirmish.Defender is Enemy { IsDead: true } e)
                {
                    //Party[0].AP.AddN<StatusSin>(5 - e.Wait);
                    e.Die();

                    SineaterGame.Instance.Layers["porsmol"].Clear();
                    var (i, j) = e.Icon;
                    var (u, v) = e.DeadIcon;
                    //_enemies.Remove(e);

                    for (int k = 0; k < 5; k++)
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
                        yield return new WaitForSeconds(0.01f);
                        SineaterGame.Instance.Layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(i, j, Color.Black, Color.Red));
                        yield return new WaitForSeconds(0.01f);
                    }

                    DrawCombat();

                    if (e.LastHit is PartyMember pm)
                    {
                        var transferable = e.Traits.Where(t => !(t is LimitedTrait)).ToList();
                        if (transferable.Count > 0)
                        {
                            var t = transferable[Rnd.Instance.Next(0, transferable.Count)];
                            yield return new ShowPopupWindowWithPortraitAndWaitForKey(pm.GetPortait(),
                                (_, bnd) => { bnd.Add($"The {e.LastHit.GetName()} acquires {t.Name.ToUpper()}!"); },
                                true);
                            yield return e.LastHit.AddTrait(t);
                        }
                    }

                    Draw(new GameTime());
                }

                yield return skirmish.GainExp();
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }

            yield return skirmish.Defender?.GetTraits().OnSkirmishEnds(skirmish);
            yield return flow.Attacker.GetTraits().OnSkirmishEnds(skirmish);
            yield return flow.WeaponAttack?.Traits?.OnSkirmishEnds(skirmish);
        }
        
        yield return flow.Attacker.GetTraits().OnCombatEnds(flow);
        yield return flow.WeaponAttack?.Traits?.OnCombatEnds(flow);

        flow.Attacker.IsDone = true;
        _confirmedCombatFlow = null;
    }
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return new WaitForSeconds(0.5f);
        //_presentation = EPresentationState.Done;
    }

    private ICharacter _currentEnemy = null;
    private CombatFlow? _confirmedCombatFlow = null;

    private Dictionary<int, (Weapon, WeaponAttack)> _attackOptions = [];
    
    private void UpdateAttackSelections()
    {
        _attackOptions.Clear();
        
        var chr = _game.Party.Characters[PlayerSelectedIndex];
        var opt = 0;
        
        if (chr.GetLeftWeapon() is { } lw)
        {
            foreach (var att in lw.GetAvailableAttacks())
            {
                opt++;
                _attackOptions[opt] = (lw, att.Thing);
            }
        }

        if (chr.GetRightWeapon() is { } rw)
        {
            foreach (var att in rw.GetAvailableAttacks())
            {
                opt++;
                _attackOptions[opt] = (rw, att.Thing);
            }
        }
    }
    
    private void CheckPlayerInputs()
    {
        var current = _game.Party.Characters[PlayerSelectedIndex];
        if (KB.HasBeenPressed(Keys.A))
        {
            var ability = current.Ability;
            if (ability != null)
            {
                if (ability.CanBeUsed(current, current.X, current.Y) && current.AP.Count<StatusStamina>() > 0)
                {
                    CoroutineHandler.Run(new ShowPopupWindowAndWaitForKey((game, layer) =>
                    {
                        layer.Add("The witch burns sin to open a domain!");
                    }, true));
                    CoroutineHandler.Run(ability.Use(this, current, current.X, current.Y));
                }
                else
                {
                    CoroutineHandler.Run(new ShowPopupWindowAndWaitForKey((game, layer) =>
                    {
                        layer.Add("Not enough sin to open this domain...");
                    }, true));
                }
            }
        }
        
        if (KB.HasBeenPressed(Keys.Enter))
        {
            CoroutineHandler.Run(Coroutine_EndTurn());
        }
        
        if (_confirmedCombatFlow != null && KB.HasBeenPressed(Keys.Escape))
        {
            _confirmedCombatFlow = null;
        }
        
        var choice = -1;
        if (KB.HasBeenPressed(Keys.D1))
        {
            choice = 1;
        }
        else if (KB.HasBeenPressed(Keys.D2))
        {
            choice = 2;
        }
        else if (KB.HasBeenPressed(Keys.D3))
        {
            choice = 3;
        }
        else if (KB.HasBeenPressed(Keys.D4))
        {
            choice = 4;
        }
        else if (KB.HasBeenPressed(Keys.D5))
        {
            choice = 5;
        }
        else if (KB.HasBeenPressed(Keys.D6))
        {
            choice = 6;
        }
        else if (KB.HasBeenPressed(Keys.D7))
        {
            choice = 7;
        }
        else if (KB.HasBeenPressed(Keys.D8))
        {
            choice = 8;
        }

        if (choice != -1 && _attackOptions.ContainsKey(choice))
        {
            var (wpn, atk) = _attackOptions[choice];
            var scored = Directions
                .Select(d => new CombatFlow(this, current, wpn, atk, (current.X, current.Y), d))
                .Select(cf => (cf, cf.Score()))
                .ToList();
            scored.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            _confirmedCombatFlow = scored[0].cf;
        }
        
        // MOVE
        if (PlayerSelectedIndex > -1)
        {
            if (_game.ActionPoints.Count<StatusStamina>() > 0 && !current.IsDone)
            {
                var up = KB.HasBeenPressed(Keys.Up);
                var down = KB.HasBeenPressed(Keys.Down);
                var left = KB.HasBeenPressed(Keys.Left);
                var right = KB.HasBeenPressed(Keys.Right);

                if (up || down || left || right)
                {
                    var dx = (left ? -1 : 0) + (right ? 1 : 0);
                    var dy = (up ? -1 : 0) + (down ? 1 : 0);
                    if ((dx == 0 || dy == 0) && (dx != 0 || dy != 0))
                    {
                        var x = current.X;
                        var y = current.Y;

                        if (_confirmedCombatFlow != null)
                        {
                            _confirmedCombatFlow.Direction = (dx, dy);
                        }
                        else if (Positions.IsCharacterAt(x + dx, y + dy) is { } c)
                        {
                            _confirmedCombatFlow = null;
                            // SWAP CHARACTERS
                            c.X = x;
                            c.Y = y;
                            current.X += dx;
                            current.Y += dy;
                            _game.ActionPoints.Spend(1);
                            
                            if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                            {
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                    .ApplyOnDomainStepped(this, current, current.X, current.Y, x, y));
                            }
                        }
                        else if (Positions.IsEnemyAt(x + dx, y + dy) is { } e)
                        {
                            // do nothing
                        }
                        else if (Structure.Map.IsWalkable(x + dx, y + dy))
                        {
                            _confirmedCombatFlow = null;
                            var oldX = current.X;
                            var oldY = current.Y;
                            current.X += dx;
                            current.Y += dy;
                            
                            if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                            {
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                    .ApplyOnDomainStepped(this, current, current.X, current.Y, oldX, oldY));
                            }

                            bool shouldCost = true;
                            if (this.Domains.Tiles.ContainsKey((current.X, current.Y)))
                            {
                                if (this.Domains.Tiles[(current.X, current.Y)] is DomainOfAction)
                                {
                                    shouldCost = false;
                                }
                            }

                            if (shouldCost)
                            {
                                var pm = SineaterGame.Instance.Party.Characters[SineaterGame.Instance.Party.Selected];
                                pm.Steps++;
                                if (pm.Steps > pm.Stats.Vigor)
                                {
                                    pm.Steps = 0;
                                    _game.ActionPoints.Spend(1);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
