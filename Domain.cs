using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using RogueSharp;

namespace SINEATER;

public class Domains(CombatMapScreen level)
{
    internal CombatMapScreen Level => level;
    public List<Domain> _domains = [];
    public Dictionary<(int, int), Domain> _tiles = [];

    public bool IsInDomain(int x, int y)
    {
        return _tiles.ContainsKey((x, y));
    }

    public Domain GetAt(int x, int y)
    {
        return _tiles[(x, y)];
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
    public ICharacter Caster { get; set; } = caster;
    public int X = x, Y = y;
    public int Radius = radius;
    public List<(int, int)> Tiles = [];

    public virtual void Update(CombatMapScreen level)
    {}
    
    public virtual void Draw(CombatMapScreen level)
    {}

    public virtual IEnumerable DefaultDomainOpening(CombatMapScreen level)
    {
        var cla = radius;
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

        foreach (var cell in map.GetCellsInCircle(x, y, cla))
        {
            if (cell.X == x && cell.Y == y) continue;
            mrmo.Set(cell.X, cell.Y + 2, " ", Color.Black);
        }
        yield return new WaitForSeconds(0.5f);

        var border = map.GetBorderCellsInCircle(x, y, cla).ToList();
        border.Shuffle();
        var raise = false;
        for (var i = 0; i < 5; i++)
        {
            foreach (var cell in border)
            {
                mrmo.Set(cell.X, cell.Y + 2, new Glyph(raise ? 12 : 13, 8, Color.Black, Color.Lerp(Color.OrangeRed, Color.White, Rnd.Instance.D10 / 10.0f)));
                yield return new WaitForSeconds(0.001f);
            }

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
    
    public virtual IEnumerable ApplyOnDomainStepped(CombatMapScreen level, ICharacter character, int x, int y)
    {
        yield break;
    }
}

public class DomainOfHealing(ICharacter character, int x, int y, int radius) : Domain(character, x, y, radius)
{
    private float _time = 0;
    private List<Cell> _eyes = [];

    public override void Update(CombatMapScreen level)
    {
        var big = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        if (Radius > 0)
            Radius--;
        
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        var intersect = big.Except(circle).Select(c => ((int)c.X, (int)c.Y)).ToHashSet();
        foreach (var xy in intersect)
        {
            if (level.Domains._tiles.ContainsKey(xy))
            {
                level.Domains._tiles.Remove(xy);
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
            level.Domains._tiles[((int)w.X, (int)w.Y)] = this;
        }
        
        for (int w = 0; w < Math.Min(wounds * 3 / 4, walkable.Count * 3 / 4); w++)
        {
            _eyes.Add(walkable[w]);
        }
        
        circle.Shuffle();
        
        for (int i = 0; i < 20; i++)
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
            level.DrawCombat();
            yield return new WaitForSeconds(0.001f);
        }

        yield return new WaitForSeconds(0.15f);
    }

    public override void Draw(CombatMapScreen level)
    {
        _time += Rnd.Instance.Next(1, 3) * 0.01f;
        var t = (int)_time;
        var mrmo = SineaterGame.Instance.Layers["mrmo"];
        var circle = level.Map.GetCellsInCircle(x, y, Radius).ToList();
        foreach (var cell in circle)
        {
            if (!level.IsInActivePartyFOV.Contains((cell.X, cell.Y))) continue;
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

        foreach (var eye in _eyes)
        {
            mrmo.Set(
                eye.X, eye.Y + 2, 
                new Glyph(
                    6, 
                    58 + (int)(t + eye.Y * t * 1.2f + eye.X * t * 3.14f) % 2,
                    Color.Black, 
                    Color.Lerp(Color.Red, Color.Black, 0.2f + ((t + eye.X + eye.Y) % 10) / 70.0f)));
        }
    }
}