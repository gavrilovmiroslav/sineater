using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Schema;
using Microsoft.Xna.Framework;
using RogueSharp;

namespace SINEATER;

public class Domains(CombatMapScreen level)
{
    internal CombatMapScreen Level => level;
    public List<Domain> _domains = [];
    public Dictionary<(int, int), Domain> Tiles = [];

    public bool IsInDomain(int x, int y)
    {
        return Tiles.ContainsKey((x, y));
    }

    public Domain GetAt(int x, int y)
    {
        return Tiles[(x, y)];
    }
    
    public void Draw(CombatMapScreen level)
    {
        foreach (var dom in _domains)
        {
            dom.Draw(level);
        }
    }

    public IEnumerable Add(Domain domain)
    {
        _domains.Add(domain);
        yield return domain.ApplyOnDomainExpanded(Level);
    }
}

public class Domain(ICharacter caster, int x, int y, int radius)
{
    public bool ShouldClose = false;
    public ICharacter Caster { get; set; } = caster;
    public int X = x, Y = y;
    public int Radius = radius;
    public List<(int, int)> Tiles = [];

    public virtual void Update(CombatMapScreen level)
    {}
    
    public virtual void Draw(CombatMapScreen level)
    {}

    public void Close()
    {
        ShouldClose = true;
    }
    
    public virtual IEnumerable DefaultDomainOpening(CombatMapScreen level)
    {
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var map = level.Map;

        for (int k = 0; k < 10; k++)
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 22; j++)
                {
                    if (i == x && j == y) continue;
                    var fg = mrmo.GetFg(i, j + 2);
                    mrmo.Set(i, j + 2, Color.Lerp(fg, Color.Black, (float)k / 40.0f));
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

        foreach (var cell in map.GetCellsInCircle(x, y, Radius))
        {
            if (cell.X == x && cell.Y == y) continue;
            mrmo.Set(cell.X, cell.Y + 2, " ", Color.Black);
        }
        yield return new WaitForSeconds(0.5f);

        var border = map.GetBorderCellsInCircle(x, y, Radius).ToList();
        border.Shuffle();
        var raise = false;
        for (var i = 0; i < 15; i++)
        {
            foreach (var cell in border)
            {
                mrmo.Set(cell.X, cell.Y + 2, new Glyph(raise ? 12 : 13, 8, Color.Black, Color.Lerp(Color.OrangeRed, Color.White, Rnd.Instance.D10 / 10.0f)));
            }
            yield return new WaitForSeconds(0.01f);
            raise = !raise;
        }
        
        foreach (var cell in border)
        {
            mrmo.Set(cell.X, cell.Y + 2, Glyph.Bw(12 - 2, 8 + 6));
            yield return new WaitForSeconds(0.001f);
        }

        foreach (var cell in border)
        {
            mrmo.Set(cell.X, cell.Y + 2, " ", Color.White, Color.Black);
            yield return new WaitForSeconds(0.001f);
        }
    }
    
    public virtual IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        yield break;
    }
    
    public virtual IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y, int oldX, int oldY)
    {
        yield break;
    }

    protected IEnumerable Blink(CombatMapScreen level)
    {
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        for (var k = 0; k < 5; k++)
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 22; j++)
                {
                    mrmo.Set(i, j + 2, " ", Color.Black, Color.Black);
                }
            }
            yield return new WaitForSeconds(0.01f * (6 - k));
            level.UpdateFov();
            level.DrawCombat();
            yield return new WaitForSeconds(0.001f);
        }

        yield return new WaitForSeconds(0.15f);
    }

    public virtual IEnumerable ApplyOnDeath(CombatMapScreen combatMapScreen, int eX, int eY)
    {
        yield break;
    }
}

public class DomainOfHealing(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    private bool _first = true;
    private float _time = 0;

    public override void Update(CombatMapScreen level)
    {
        var big = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        if (_first)
        {
            _first = false;
            return;
        }
        else if (Radius > 0 && !_first)
            Radius--;
        
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        var intersect = big.Except(circle).Select(c => ((int)c.X, (int)c.Y)).ToHashSet();
        foreach (var xy in intersect)
        {
            if (level.Domains.Tiles.ContainsKey(xy))
            {
                level.Domains.Tiles.Remove(xy);
            }
        }

        if (Radius == 0)
        {
            Close();
        }
    }

    public override IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y, int oldX, int oldY)
    {
        if (character.GetAP().Count<StatusWounds>() > 0)
        {
            character.GetAP().Reduce<StatusWounds>(1);
            for (var i = 0; i < 10; i++)
            {
                SineaterGame.Instance.Layers["mrmo"]
                    .Set(x, y + 2, "+", Color.Lerp(Color.Black, Color.Green, i / 10.0f));
                yield return new WaitForSeconds(0.001f);
            }
        }
        else
        {
            character.GetAP().Add<StatusInsanity>(1);
            for (var i = 0; i < 10; i++)
            {
                SineaterGame.Instance.Layers["mrmo"]
                    .Set(x, y + 2, "!", Color.Lerp(Color.Yellow, Color.DarkRed, i / 10.0f));
                yield return new WaitForSeconds(0.0001f);
            }
        }
    }

    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        yield return DefaultDomainOpening(level);
        
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        circle.Shuffle();

        var ew = level.Enemies[0].GetAP().Count<StatusWounds>();
        var pw = Caster.GetAP().Count<StatusWounds>();
        var wounds = pw + ew / 2;

        var walkable = circle.Where(c => level.Map.IsWalkable(c.X, c.Y)).ToList();
        foreach (var w in walkable)
        {
            level.Domains.Tiles[((int)w.X, (int)w.Y)] = this;
        }
        
        circle.Shuffle();
        
        for (int i = 0; i < 10; i++)
        {
            int c = 0;
            foreach (var cell in circle)
            {
                if (cell.X == x && cell.Y == y) continue;
                c++;
                var dist = Vector2.Distance(new Vector2(cell.X, cell.Y), new Vector2(x, y)) / Radius;
                mrmo.Set(
                    cell.X, cell.Y + 2, 
                    new Glyph(
                        (int)(i + cell.Y) % 3, 
                        57 + (int)(2 * i + cell.X) % 3,
                        Color.Black, 
                        Color.Lerp(Color.Black, Color.DarkRed, dist * (float)i / 10)));
                if (c > 3)
                {
                    c = 0;
                    yield return new WaitForSeconds(0.0001f);
                }
            }
            yield return new WaitForSeconds(0.001f);
        }

        yield return new WaitForSeconds(0.25f);

        yield return Blink(level);
    }

    public override void Draw(CombatMapScreen level)
    {
        _time += Rnd.Instance.Next(1, 3) * 0.01f;
        var t = (int)_time;
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        foreach (var cell in circle)
        {
            if (!level.IsInActivePartyMemberFOV.Contains((cell.X, cell.Y))) continue;
            var dist = Vector2.Distance(new Vector2(cell.X, cell.Y), new Vector2(x, y));
            var dx = 0;
            var fg = Color.DarkRed;
            if (!level.Map.IsWalkable(cell.X, cell.Y))
            {
                dx = 3;
                fg = Color.White;
            }
            mrmo.Set(
                cell.X, cell.Y + 2, 
                new Glyph(
                    dx + (int)(t + cell.X * t * 3.14f + cell.Y) % 3, 
                    57 + (int)(2 * t + cell.X * t * 1.28f + cell.Y) % 3,
                    Color.Black, 
                    Color.Lerp(Color.Black, fg, 0.5f + ((t + cell.X + cell.Y) % 10) / 10.0f)));
        }
    }
}

public class DomainOfAction(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    private Dictionary<(int, int), float> _steps = [];
    private (int, int) _waveCenter;
    private float _waveRadius = 0;
    private List<Cell> _waveCenters = [];
    private Dictionary<(int, int), (bool, bool)> _oldTransparency = [];

    private bool _first = true;
    public override void Update(CombatMapScreen level)
    {
        if (_first)
        {
            _first = false;
        }
        else if (Radius > 0)
        {
            Radius--;
            foreach (var (xy, tw) in _oldTransparency)
            {
                level.Map?.SetCellProperties(xy.Item1, xy.Item2, tw.Item1, tw.Item2);
            }
            
            foreach (var cell in level.Map?.GetCellsInCircle(x, y, Radius) ?? [])
            {
                level.Map?.SetCellProperties(cell.X, cell.Y, true, true);
            }
            level.UpdateFov();
        }
        else
        {
            Close();
        }
    }

    public override IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y, int oldX, int oldY)
    {
        character.GetAP().Reduce(1);
        
        if (!_steps.ContainsKey((oldX, oldY)))
        {
            if (!(oldY == y + 1 && oldX == x))
            {
                _steps[(oldX, oldY)] = 3;
            }
        }

        yield break;
    }

    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        _waveCenters = level.Map?.GetBorderCellsInCircle(x, y, Radius + 1).ToList() ?? [];
        _waveCenters.Shuffle();
        
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        yield return DefaultDomainOpening(level);

        List<(int, int)> alreadyFound = [];
        for (int i = 0; i < 2; i++)
        {
            _waveRadius = 0;
            var c = _waveCenters[Rnd.Instance.Next(0, _waveCenters.Count)];
            _waveCenter.Item1 = c.X;
            _waveCenter.Item2 = c.Y;

            for (int k = 0; k < Radius * 4; k++)
            {
                var w = level.Map.GetBorderCellsInCircle(_waveCenter.Item1, _waveCenter.Item2, (int)_waveRadius)
                    .ToHashSet();
                var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();

                foreach (var cell in circle)
                {
                    if (cell.X == x && cell.Y == y) continue;
                    var dist = Vector2.Distance(new Vector2(cell.X, cell.Y), new Vector2(x, y)) / Radius;
                    if (w.Contains(cell))
                    {
                        mrmo.Set(cell.X, cell.Y + 2, ".", new Color(0.9f - Rnd.Instance.Next01() * 0.1f, 0.9f - Rnd.Instance.Next01() * 0.1f, 1.0f - Rnd.Instance.Next01() * 0.1f));
                        alreadyFound.Add((cell.X, cell.Y));
                    }
                    else if (alreadyFound.Contains((cell.X, cell.Y)))
                    {
                        mrmo.Set(cell.X, cell.Y + 2, ".", new Color(Rnd.Instance.Next01() * 0.1f, Rnd.Instance.Next01() * 0.1f, 1.0f - (0.5f * dist)));
                    }
                }

                yield return new WaitForSeconds(0.1f);
                
                _waveRadius++;
            }
        }

        foreach (var cell in level.Map?.GetCellsInCircle(x, y, Radius) ?? [])
        {
            mrmo.Set(cell.X, cell.Y + 2, " ", Color.Black);
            _oldTransparency[(cell.X, cell.Y)] = (level.Map.IsTransparent(cell.X, cell.Y), level.Map.IsWalkable(cell.X, cell.Y));
            level.Map.SetCellProperties(cell.X, cell.Y, true, true);
            level.Domains.Tiles[(cell.X, cell.Y)] = this;
        }

        yield return Blink(level);
        
        level.UpdateFov();
    }

    public override void Draw(CombatMapScreen level)
    {
        if (_waveRadius >= Radius * 3)
        {
            _waveRadius = 0;
        }
        
        if (_waveRadius == 0)
        {
            var c = _waveCenters[Rnd.Instance.Next(0, _waveCenters.Count)];
            _waveCenter.Item1 = c.X;
            _waveCenter.Item2 = c.Y;
        }
        
        var w = level.Map.GetBorderCellsInCircle(_waveCenter.Item1, _waveCenter.Item2, (int)_waveRadius).ToHashSet();
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        
        foreach (var cell in circle)
        {
            var dist = Vector2.Distance(new Vector2(cell.X, cell.Y), new Vector2(x, y)) / Radius;
            if (w.Contains(cell))
            {
                mrmo.Set(cell.X, cell.Y + 2, ".", new Color(0.2f * dist + Rnd.Instance.Next(0, 2) * 0.1f, 0.2f * dist + Rnd.Instance.Next(0, 2) * 0.1f, 0.5f * dist));
            }
            else
            {
                mrmo.Set(cell.X, cell.Y + 2, ".", new Color(0.1f, 0.1f, 1.0f - (0.5f * dist)));
            }
        }

        List<(int, int)> toRemove = [];
        foreach (var xy in _steps.Keys)
        {
            var dist = Vector2.Distance(new Vector2(xy.Item1, xy.Item2), new Vector2(x, y)) / Radius;
            var f = _steps[(xy.Item1, xy.Item2)] / 3.0f;
            f = 1.0f - MathF.Pow(1 - f, 3);
            var dark = new Color(0.1f, 0.1f, 1.0f - (0.5f * dist));
            mrmo.Set(xy.Item1, xy.Item2 + 2, ".", Color.Lerp(dark, Color.White, f));
            _steps[(xy.Item1, xy.Item2)] -= 0.1f;
            if (_steps[(xy.Item1, xy.Item2)] <= 0.0f)
            {
                toRemove.Add((xy.Item1, xy.Item2));
            }
        }

        foreach (var rem in toRemove)
        {
            _steps.Remove(rem);
        }
        
        var xys = circle.Select(c => (c.X, c.Y)).ToHashSet();
        foreach (var chr in SineaterGame.Instance.Party.Characters)
        {
            var (cx, cy) = (chr.X, chr.Y);
            if (xys.Contains((cx, cy)) && xys.Contains((cx, cy + 1)))
            {
                var (u, v) = chr.Job.GetImage();
                var color = SineaterGame.Instance.Layers["mrmo"].GetFg(cx, cy + 3);
                SineaterGame.Instance.Layers["mrmo"].Set(cx, cy + 3, new Glyph(u, v + 5, Color.Black, color));
            }
        }
        foreach (var chr in level.Enemies)
        {
            var (cx, cy) = (chr.X, chr.Y);
            if (xys.Contains((cx, cy)) && xys.Contains((cx, cy + 1)))
            {
                var (u, v) = chr.Icon;
                var color = SineaterGame.Instance.Layers["mrmo"].GetFg(cx, cy + 3);
                SineaterGame.Instance.Layers["mrmo"].Set(cx, cy + 3, new Glyph(u, v + 5, Color.Black, color));
            }
        }
        
        _waveRadius += 0.1f;
    }
}

public class DomainOfDarkness(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    private float _dx;
    private float _dy;
    private float _t = 0;
    private long _seed;
    private int _shrinking = radius;
    
    private Dictionary<(int, int), Glyph> _glyphs = [];

    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        _seed = Rnd.Instance.Next(0, 10000);
        _dx = Rnd.Instance.Next01() - 0.5f;
        _dy = Rnd.Instance.Next01() - 0.5f;
        
        var mrmo = SineaterGame.Instance.Layers["mrmo"];

        level.DrawCombat();
        yield return new WaitForSeconds(0.5f);
        
        HashSet<(int, int)> drawn = [];
        var tR = (Radius + 2) / 10.0f;
        var tA = 0.9f;
        
        for (int i = 1; i <= Radius; i++)
        {
            for (int n = 0; n < 10; n++)
            {
                foreach (var cell in level.Map?.GetBorderCellsInCircle(x, y, i + 1) ?? [])
                {
                    mrmo.Set(cell.X, cell.Y + 2, " ", Color.White, new Color(0, 0, (int)(25 * (float)i / Radius)));
                    mrmo.Set(cell.X, cell.Y + 2, new Glyph(n % 2 == 0 ? 12 : 13, 8, Color.Black, Color.Lerp(Color.OrangeRed,
                        Color.Blue, n / 10.0f)));
                }

                yield return new WaitForSeconds(0.05f);
            }
            
            var bcells = level.Map?.GetCellsInCircle(x, y, i + 1) ?? [];
            foreach (var cell in bcells)
            {
                if (drawn.Contains((cell.X, cell.Y))) continue;
                mrmo.Set(cell.X, cell.Y + 2, " ", Color.Yellow, new Color(0, 0, (int)(25 * (float)i / Radius)));
            }

            var cells = level.Map?.GetCellsInCircle(x, y, i) ?? [];
            foreach (var cell in cells)
            {
                if (!drawn.Contains((cell.X, cell.Y)))
                {
                    var xnoise = OpenSimplex2S.Noise3_ImproveXY(_seed, cell.X * 0.1f + _dx, cell.Y * 0.1f + _dy, -1);
                    var ynoise = OpenSimplex2S.Noise3_ImproveXY(_seed, cell.X * 0.1f + _dx, cell.Y * 0.1f + _dy, 1);
                    var xy = (cell.X, cell.Y);
                    drawn.Add(xy);
                    var d20 = Rnd.Instance.D100 / 2;
                    if (level.Map.IsWalkable(cell.X, cell.Y) && d20 <= 4)
                    {
                        _glyphs[xy] = new Glyph(Rnd.Instance.D4 - 1, 20,
                            new Color((int)(xnoise * 25), (int)(ynoise * 25), (int)(25 * (float)i / Radius)), Color.Yellow);
                        mrmo.Set(cell.X, cell.Y + 2, _glyphs[xy]);
                    }
                    else if (level.Map.IsWalkable(cell.X, cell.Y) && d20 == 5)
                    {
                        _glyphs[xy] = new Glyph(13 + Rnd.Instance.D2, 40 + Rnd.Instance.D2,
                            new Color((int)(xnoise * 25), (int)(ynoise * 25), (int)(25 * (float)i / Radius)), Color.Yellow);
                        mrmo.Set(cell.X, cell.Y + 2, _glyphs[xy]);
                    }
                    else
                    {
                        _glyphs[xy] = new Glyph(0, 0, new Color((int)(xnoise * 25), (int)(ynoise * 25), (int)(25 * (float)i / Radius)), Color.Yellow);
                        mrmo.Set(cell.X, cell.Y + 2, _glyphs[xy]);
                    }
                }
            }

            yield return new WaitForSeconds(tR);
            tR *= tA;
            tA *= 0.75f;
        }
        
        foreach (var cell in level.Map?.GetCellsInCircle(x, y, Radius) ?? [])
        {
            level.Domains.Tiles[(cell.X, cell.Y)] = this;
        }

        foreach (var enemy in level.Enemies)
        {
            if (level.Domains.Tiles.ContainsKey((enemy.X, enemy.Y)))
            {
                enemy.Behaviors.Insert(0, new BehaviorYearnForLight());
            }
        }
        
        yield return new WaitForSeconds(1f);
        yield return Blink(level);
    }

    public override void Update(CombatMapScreen level)
    {
        _shrinking--;
        if (_shrinking == 0)
        {
            ShouldClose = true;
            return;
        }
        
        HashSet<(int, int)> stars = [];
        foreach (var (xy, g) in _glyphs)
        {
            if (g.V != 0)
            {
                stars.Add(xy);
                foreach (var c in level.Map.GetCellsInCircle(xy.Item1, xy.Item2, _shrinking))
                {
                    if (c != null)
                        stars.Add((c.X, c.Y));
                }
            }
        }

        List<(int, int)> toRemove = [];
        foreach (var (xy, g) in _glyphs)
        {
            if (!stars.Contains(xy))
            {
                toRemove.Add(xy);
            }
        }

        foreach (var xy in toRemove)
        {
            _glyphs.Remove(xy);
            if (level.Domains.Tiles.ContainsKey(xy) && level.Domains.Tiles[xy] == this)
            {
                level.Domains.Tiles.Remove(xy);
            }
        }
        
        foreach (var enemy in level.Enemies)
        {
            if (level.Domains.Tiles.ContainsKey((enemy.X, enemy.Y)))
            {
                enemy.Behaviors.Insert(0, new BehaviorYearnForLight());
            }
        }
    }

    public override void Draw(CombatMapScreen level)
    {
        _dx += 0.01f;
        _dy += 0.01f;
        _t += 0.001f;
        
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        foreach (var ((cx, cy), g) in _glyphs)
        {
            var xnoise = OpenSimplex2S.Noise3_ImproveXY(_seed, cx * 0.1f + _dx, cy * 0.1f + _dy, MathF.Cos(MathF.PI + _t));
            var ynoise = OpenSimplex2S.Noise3_ImproveXY(_seed, cx * 0.1f + _dx, cy * 0.1f + _dy, MathF.Sin(MathF.PI / 2 + _t));
            var dist = Vector2.Distance(new Vector2(cx, cy), new Vector2(x, y)) / Radius;
            if (level.IsInActivePartyMemberFOV?.Contains((cx, cy)) ?? false)
            {
                var fg = Color.White;
                var color = new Color((int)(xnoise * 15), (int)(ynoise * 15),
                    (int)(35 * dist - xnoise * 10 - ynoise * 10));
                if (level.Map?.IsWalkable(cx, cy) ?? false)
                {
                    if (g.V != 0)
                    {
                        fg = Color.Lerp(Color.White, Color.Red, MathF.Abs(MathF.Sin(_t * 10)));
                        color = Color.Lerp(color, Color.DarkRed, MathF.Abs(MathF.Cos(_t * 10)));
                    }
                    mrmo.Set(cx, cy + 2, g);
                    mrmo.Set(cx, cy + 2, fg, color);
                }
                else
                {
                    mrmo.Set(cx, cy + 2, color.Lighten(0.1f), color);
                }
            }
        }
    }

    
    public override IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y, int oldX, int oldY)
    {
        var ap = character.GetAP();
        if (_glyphs[(x, y)].V != 0)
        {
            if (ap.Count<StatusFrozen>() > 0)
            {
                ap.Reduce<StatusFrozen>(4);
            }
            else
            {
                ap.Add<StatusFire>(1);
            }
        }
        else
        {
            if (ap.Count<StatusFire>() > 0)
            {
                character.GetAP().Reduce<StatusFire>(1);
            }
            else
            {
                character.GetAP().Add<StatusFrozen>(1);
            }
        }

        yield return new WaitForSeconds(0.01f);
    }
}


public class DomainOfFatigue(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    private int _t = 0;
    internal List<(int, int, ICharacter)> _totems = [];
    Dictionary<(int, int), int> _shadows = [];
    Dictionary<(int, int), Color> _shadowColors = [];
    Dictionary<ICharacter, int> _moves = [];
    private int _turns = 3;

    public override IEnumerable ApplyOnDeath(CombatMapScreen level, int eX, int eY)
    {
        List<(int, int, ICharacter)> toRemove = [];
        foreach (var (tx, ty, t) in _totems)
        {
            if (tx == eX && ty == eY)
            {
                toRemove.Add((tx, ty, t));
            }
        }

        foreach (var rem in toRemove)
        {
            _totems.Remove(rem);
        }

        level.DrawCombat();
        yield break;
    }

    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        List<(int, int)> shadows = [];
        level.DrawCombat();
        var oldRadius = Radius;
        Radius = 2;
        yield return DefaultDomainOpening(level);
        Radius = oldRadius;
        
        var mrmo = SineaterGame.Instance.Layers["mrmo"];

        level.DrawCombat();

        var goals = new GoalMap(level.Map, true);
        shadows.Shuffle();
         for (int c = 1; c < 4 + character.Stats.Mod(EStat.Clarity); c++)
         {
             foreach (var cell in level.Map?.GetBorderCellsInCircle(x, y, c) ?? [])
             {
                 if (!cell.IsWalkable)
                     continue;
                 goals.ClearGoals();
                 goals.AddGoal(cell.X, cell.Y, 100);
                 var path = goals.TryFindPath(x, y);
                 if (path == null || path.Length > c * 2) continue;
                 
                 var sidx = 5 + Rnd.Instance.D4;
                 Tiles.Add((cell.X, cell.Y));
                 level.Domains.Tiles[(cell.X, cell.Y)] = this;
                 _shadows[(cell.X, cell.Y)] = sidx;
                 _shadowColors[(cell.X, cell.Y)] = Color.LightGray;
                 mrmo.Set(cell.X, cell.Y + 2,
                     new Glyph(sidx, 72, Color.Black, _shadowColors[(cell.X, cell.Y)]));
                 yield return new WaitForSeconds(0.01f);
             }
         }

        yield return new WaitForSeconds(1f);
        yield return Blink(level);
    }

    public IEnumerable SkullTotem(DomainOfFatigue domain, CombatMapScreen level, ICharacter c, int x, int y)
    {
        c.Render = false;
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        mrmo.Set(x, y + 2, new Glyph(13, 71, Color.Black, Color.White));
        yield return new WaitForSeconds(0.5f);
        
        for (int i = 0; i < 3; i++)
        {
            mrmo.Set(x, y + 1, new Glyph(13 + i, 71, Color.Black, Color.White));
            mrmo.Set(x, y + 2, new Glyph(13 + i, 72, Color.Black, Color.White));
            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(2.0f);
        domain._totems.Add((x, y, c));
    }

    public IEnumerable Disappear(CombatMapScreen level)
    {
        yield return Blink(level);
        yield return new WaitForSeconds(0.01f);
        
        _totems.Clear();

        foreach (var (tx, ty) in level.Domains.Tiles.Keys.ToList())
        {
            if (level.Domains.Tiles[(tx, ty)] == this)
            {
                level.Domains.Tiles.Remove((tx, ty));
            }
        }
        
        _shadows.Clear();
        _shadowColors.Clear();
        
        foreach (var e in level.Enemies)
        {
            if (e.Traits.Any(t => t is TraitProne))
            {
                e.Traits.RemoveAll(t => t is TraitProne);
            }
            e.Render = true;
        }

        foreach (var c in SineaterGame.Instance.Party.Characters)
        {
            if (c.Traits.Any(t => t is TraitProne))
            {
                c.Traits.RemoveAll(t => t is TraitProne);
            }
            c.Render = true;
        }
        
        level.DrawCombat();
        Close();
    }
    
    public override IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y, int oldX, int oldY)
    {
        if (!_moves.ContainsKey(character))
        {
            _moves[character] = 0;
        }
        
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        _moves[character]++;
        
        var uv = mrmo.GetUV(oldX, oldY + 2);
        var fg = mrmo.GetFg(oldX, oldY + 2);
        mrmo.Set(oldX, oldY + 2, $"{character.Stats.Clarity - _moves[character]}");
        yield return new WaitForSeconds(0.3f);
        if (uv.HasValue)
        {
            var (u, v) = uv.Value;
            mrmo.Set(oldX, oldY + 2, new Glyph(u, v, Color.Black, fg));
        }

        if (_moves[character] == character.Stats.Clarity)
        {
            yield return SkullTotem(this, level, character, x, y);
            if (character is PartyMember c)
            {
                yield return c.AddTrait(new TraitProne(3));
                _moves[character] = 0;
            }
            else if (character is Enemy e)
            {
                e.IsDone = true;
                yield return e.AddTrait(new TraitProne(3));
                _moves[character] = 0;
            }
        }
        
        yield return new WaitForSeconds(0.1f);
    }

    public override void Update(CombatMapScreen level)
    {
        _moves.Clear();
        _turns--;
        if (_turns == 0)
        {
            level.CoroutineHandler.Run(Disappear(level));
        }
    }

    public override void Draw(CombatMapScreen level)
    {
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var shadowKeys = _shadows.Keys.ToList();
        shadowKeys.Sort();
        
        for (int j = 0; j < _shadows.Keys.Count; j++)
        {
            var (sx, sy) = shadowKeys[j];
            var s = 6 + (_shadows[(sx, sy)] - 6 + _t) % 6;
            mrmo.Set(sx, sy + 2, new Glyph(s, 72, 
                Color.Black, Color.Lerp(_shadowColors[(sx, sy)], Color.MediumPurple, MathF.Pow(s / 12.0f, 3))));
        }

        foreach (var (tx, ty, _) in _totems)
        {
            mrmo.Set(tx, ty + 1, new Glyph(15, 71, Color.Black, Color.White));
            mrmo.Set(tx, ty + 2, new Glyph(15, 72, Color.Black, Color.White));
        }
    }
}

public class DomainOfFire(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    IEnumerable ClearScreen(CombatMapScreen level, IMap map, TextLayer mrmo, int r)
    {
        level.DrawCombat();
        for (int i = 0; i < 24; i++)
        {
            for (int j = 0; j < 22; j++)
            {
                if (i == x && j == y) continue;
                var fg = mrmo.GetFg(i, j + 2);
                mrmo.Set(i, j + 2, Color.Lerp(fg, Color.Black, 1.0f / 4.0f));
            }
        }
        
        foreach (var cell in map.GetCellsInCircle(x, y, r))
        {
            if (cell.X == x && cell.Y == y) continue;
            mrmo.Set(cell.X, cell.Y + 2, " ", Color.Black);
        }

        yield return new WaitForSeconds(0.01f);
    }
    
    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var map = level.Map;

        for (int k = 0; k < 10; k++)
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 22; j++)
                {
                    if (i == x && j == y) continue;
                    var fg = mrmo.GetFg(i, j + 2);
                    mrmo.Set(i, j + 2, Color.Lerp(fg, Color.Black, (float)k / 40.0f));
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return ClearScreen(level, map, mrmo, Radius);
        
        yield return new WaitForSeconds(0.5f);

        for (var r = Radius; r >= 1; r--)
        {
            yield return ClearScreen(level, map, mrmo, r);
            var border = map.GetBorderCellsInCircle(x, y, r).ToList();
            border.Shuffle();
            var raise = false;
            for (var i = 0; i < 15; i++)
            {
                foreach (var cell in border)
                {
                    mrmo.Set(cell.X, cell.Y + 2,
                        new Glyph(raise ? 12 : 13, 8, 
                            Color.Lerp(Color.Black, Color.Red, 1.0f - (float)r / Radius),
                            Color.Lerp(Color.OrangeRed, Color.White, Rnd.Instance.D10 / 10.0f)));
                }
                yield return new WaitForSeconds(0.01f * r);
                raise = !raise;
            }
        }

        Dictionary<(int, int), ICharacter> chars = [];
        foreach (var ch in SineaterGame.Instance.Party.Characters)
        {
            chars[(ch.X, ch.Y)] = ch; 
        }
        
        foreach (var ch in level.Enemies)
        {
            chars[(ch.X, ch.Y)] = ch; 
        }
        
        var distances = new DistanceMap(map, false, x, y);
        for (var i = 1; i < distances.MaxDistance(); i++)
        {
            level.DrawCombat();
            foreach (var (rx, ry) in distances.GetAllAt(i - 2))
            {
                mrmo.Set(rx, ry + 2, ".", Color.White, Color.Black);
            }

            for (var n = 0; n < 3; n++)
            {
                foreach (var (rx, ry) in distances.GetAllAt(i - 1))
                {
                    mrmo.Set(rx, ry + 2, "^", Color.Yellow, Color.Lerp(Color.Red, Color.Black, n / 3.0f));
                }

                yield return new WaitForSeconds(0.001f);
            }

            foreach (var (rx, ry) in distances.GetAllAt(i))
            {
                mrmo.Set(rx, ry + 2, new Glyph(12, 8, Color.OrangeRed, Color.White));

                if (level.IsInActivePartyMemberFOV?.Contains((rx, ry)) ?? false)
                {
                    if (chars.ContainsKey((rx, ry)))
                    {
                        yield return chars[(rx, ry)].AddTrait(new TraitCritical(3));
                    }
                }
                
                level.Visited[rx, ry] = true;
            }
            
            foreach (var (rx, ry) in distances.GetAllAt(i + 1))
            {
                mrmo.Set(rx, ry + 2, ".", Color.Yellow, Color.Black);
            }

            yield return new WaitForSeconds(0.005f);
        }
    }
}

public class DomainOfControl(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    public override IEnumerable ApplyOnDomainExpanded(CombatMapScreen level)
    {
        yield return DefaultDomainOpening(level);
        yield return new WaitForSeconds(1f);
        yield return Blink(level);
    }
}