using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using CommunityToolkit.HighPerformance.Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Content;
using SINEATER.SinMod;
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
    public int Sin;
    public ETerrainKind Terrain;
}

public class CombatMapScreen : IScreen
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private readonly int _fullWidth = 20, _fullHeight = 20;
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    public LevelStructure Structure;
    private bool _rendered = false;
    private bool _debugView = false;
    private int _time = 0;
    public int PlayerSelectedIndex = 0;
    private Glyph[,] _groundGlyphs;
    internal CoroutineHandler CoroutineHandler = new();
    internal FieldOfView<Cell> _fov;
    private readonly CombatConfig? _config;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    private List<string> _submenu = [];
    private int _submenuSelection = 0;
    private (int, int) _submenuDelta = (0, 0);
    
    internal (int, int) DrawOffset { get; set; } = (8, 1);
    internal bool ShouldUpdateView = true;

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

    public CombatMapScreen(SineaterGame game, CombatConfig? config = null, int width = -1, int height = -1, string title = "???")
    {
        _config = config;
        _width = width;
        _height = height;

        Domains = new(this);
        
        _kind = _config?.Terrain ?? ETerrainKind.Cave;
        _game = game;
        _groundGlyphs = new Glyph[_fullWidth, _fullHeight];
        Initialize(game);
        Regenerate(_width == -1 || _height == -1);
    }

    public void Initialize(SineaterGame game)
    {
        _game = game;
    }
    
    private void Regenerate(ETerrainKind kind)
    {
        ShouldUpdateView = true;
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
        map.Copy(inner, 1, 1);

        Structure = new LevelStructure(map);
        _fov = new(Map);
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

        Map.SetCellProperties(Structure.Goals[0].Item1, Structure.Goals[0].Item2, false, false);
        
        foreach (var (tx, ty) in Structure.Treasure)
        {
            Map.SetCellProperties(tx, ty, false, false);
        }
        
        _rendered = false;

        for (var ci = 0; ci < 4; ci++)
        {
            _game.Party.Characters[ci].X = Structure.Starts[ci].Item1;
            _game.Party.Characters[ci].Y = Structure.Starts[ci].Item2;
            _game.Party.Characters[ci].SetOrigin();
        }
        
        
    }
    
    public void Update(GameTime gameTime)
    {
        if (KB.HasBeenPressed(Keys.U))
        {
            var dof = DrawOffset;
            dof.Item1--;
            DrawOffset = dof;
        }
        
        if (KB.HasBeenPressed(Keys.I))
        {
            var dof = DrawOffset;
            dof.Item1++;
            DrawOffset = dof;
        }
        
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }

        if (Keyboard.GetState().IsKeyDown(Keys.F1))
        {
            Regenerate();
        }

        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }

        CheckInputs();
        CheckPlayerInputs();
    }
    
    internal (int, int)? GetUV(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetUV(x + ox, y + oy);
    }

    internal Color GetFg(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetFg(x + ox, y + oy);
    }
    
    internal void Draw(int x, int y, Glyph g)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, g);
    }
    
    internal void Draw(int x, int y, string s)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s);
    }
    
    internal void Draw(int x, int y, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c);
    }
    
    internal void Draw(int x, int y, string s, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c);
    }
    
    internal void Draw(int x, int y, string s, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c, b);
    }

    internal void Draw(int x, int y, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c, b);
    }

    void UpdateCombatView()
    {
        var selfFov = new FieldOfView(Map);
        _fgs.Clear();
        
        for (var i = 0; i < 4; i++)
        {
            var w = _game.Party.Characters[i];

            w.Fov = selfFov.
                ComputeFov(w.X, w.Y, 2 * w.Cla, true).
                Select(Predicate.CellToPosition).ToHashSet();
            
            foreach (var (x, y) in w.Fov)
            {
                _fgs.Add((x, y), w.Tint);
            }
            
            if (i == 0)
            {
                _fov.ComputeFov(w.X, w.Y, 2 * w.Cla, true);
            }
            else
            {
                _fov.AppendFov(w.X, w.Y, 2 * w.Cla, true);
            }

            if (i == PlayerSelectedIndex)
            {
                CalculateZone(w);
            }
        }

        foreach (var enemy in Structure.Enemies.Where(e => !e.Active && _fov.IsInFov(e.X, e.Y)))
        {
            if (enemy.SleepyTime < 0)
            {
                enemy.SleepyTime = enemy.Level * 4;
            }
            else
            {
                enemy.SleepyTime--;
                if (enemy.SleepyTime <= 0)
                {
                    var dist = new DistanceMap(Structure.Map, true, enemy.X, enemy.Y, Predicate.Walkable);
                    var p = _game.Party.Characters[PlayerSelectedIndex];
                    var d = dist.Get(p.X, p.Y);
            
                    var roll = Rnd.Instance.Next(0, d);
                    enemy.Active = roll <= enemy.Cla * 2;
                }
            }
        }
    }

    private void MarkDone(PartyMember w)
    {
        w.IsDone = true;

        if (_game.Party.Characters.All(ch => ch.IsDone))
        {
            CoroutineHandler.Run(Coroutine_EndTurn());
        }
        else
        {
            PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
            ShouldUpdateView = true;
        }
    }

    private IEnumerable RunEnemyMoves()
    {
        // TODO: this shouldn't be the behaviour of EVERY enemy
        var goals = new DistanceMap(Structure.Map, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))], Predicate.Walkable);
        foreach (var enemy in Structure.Enemies.Where(enemy => enemy.Active).OrderByDescending(enemy => enemy.Level))
        {
            var wounds = enemy.AP.Count(EStatus.Wound);
            var roll = Rnd.Instance.Next(0, enemy.AP.Width);
            if (roll < wounds)
            {
                enemy.HP--;
                for (var i = 0; i < 3; i++)
                {
                    var (u, v) = enemy.GetIcon(true);
                    Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.DarkMagenta, Color.White));
                    yield return new WaitForSeconds(0.01f);
                    Draw(enemy.X, enemy.Y, new Glyph(u, v + 4, Color.Black, enemy.Tint));
                    yield return new WaitForSeconds(0.01f);
                }

                if (enemy.HP <= 0)
                {
                    enemy.Die();
                    Structure.Enemies.Remove(enemy);
                    UpdateCombatView();
                    DrawCombat();
                }

                continue;
            }

            for (int ev = 0; ev < enemy.Vig; ev++)
            {
                if (enemy.IsDone) break;
                if (_game.Party.Characters[0].AP.Count(EStatus.Void) == 0)
                {
                    break;
                }

                var dist = goals.Get(enemy.X, enemy.Y);
                
                if (dist == 1)
                {
                    var next = Structure.Map.GetAdjacentCells(enemy.X, enemy.Y, false).Select(c =>
                        Positions.GetCharAt(this, c.X, c.Y)).Where(c => c is PartyMember).ToList();
                    if (next.Count > 0)
                    {
                        var def = next[Rnd.Instance.Next(0, next.Count)] as PartyMember;
                        var (a, b) = def.Job.GetImage(true);
                        Draw(def.X, def.Y, new Glyph(a, b, Color.Black, def.Tint));
                        for (var i = 0; i < 3; i++)
                        {
                            var (u, v) = enemy.GetIcon(true);
                            Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.Black, Color.White));
                            yield return new WaitForSeconds(0.01f);
                            Draw(enemy.X, enemy.Y, new Glyph(u, v + 4, Color.Black, enemy.Tint));
                            yield return new WaitForSeconds(0.01f);
                        }
                        yield return Coroutine_Attack(enemy, def);
                    }
                }
                else
                {
                    var next = Structure.Map.GetAdjacentCells(enemy.X, enemy.Y, false).Where(c =>
                        Positions.GetCharAt(this, c.X, c.Y) == null && Structure.Map.IsWalkable(c.X, c.Y) &&
                        goals.Get(c.X, c.Y) < dist).ToList();
                    if (next.Count > 0)
                    {
                        if (_fov.IsInFov(enemy.X, enemy.Y))
                        {
                            for (var i = 0; i < 3; i++)
                            {
                                var (u, v) = enemy.GetIcon(true);
                                Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.Black, enemy.Tint));
                                yield return new WaitForSeconds(0.01f);
                                Draw(enemy.X, enemy.Y, new Glyph(u, v + 4, Color.Black, enemy.Tint));
                                yield return new WaitForSeconds(0.01f);
                            }
                        }

                        var choice = next[Rnd.Instance.Next(0, next.Count - 1)];
                        enemy.X = choice.X;
                        enemy.Y = choice.Y;
                        _game.Party.Characters[0].AP.Unspend(1);
                        DrawCombat();
                        yield return new WaitForSeconds(0.05f);
                    }
                    else
                    {
                        enemy.IsDone = true;
                    }
                }
            }
        }

        yield break;
    }

    private IEnumerable ResetPartyMembers()
    {
        foreach (var pm in _game.Party.Characters)
        {
            pm.SetOrigin();
            pm.IsDone = false;
            CalculateZone(pm);
        }
        yield break;
    }
    
    private void CalculateZone(PartyMember w)
    {
        w.Zone.Clear();
        var dis = new DistanceMap(Structure.Map, false, [w.Origin], Predicate.Walkable);
        var walkRadius = (int)Math.Max(1, w.Vig + w.Poi + w.Wil - w.Weight);
        w.Zone = dis.GetAllBeneath(walkRadius + 1).ToHashSet();
        w.Zone.IntersectWith(Structure.Map.
            GetCellsInCircle(w.Origin.Item1, w.Origin.Item2, walkRadius).
            Select(Predicate.CellToPosition).ToHashSet());
    }

    internal void DrawCombat(bool onlyNow = false)
    {
        if (ShouldUpdateView)
        {
            UpdateCombatView();
            ShouldUpdateView = false;
        }

        foreach (var layer in SineaterGame.LayerNames)
        {
            _game.Layers[layer].Clear();
        }
        
        _game.ActionPoints.Draw(DrawOffset.Item1 * 2 + 1, 26);

        MultiDictionary<int, PartyMember> xs = new(false);
        foreach (var w in _game.Party.Characters)
        {
            var g = w.Job.GetImage();
            xs.Add(w.X, w);
        }

        foreach (var x in xs.Keys.Order())
        {
            var y = 25 - xs[x].Count;
            int n = 0;
            foreach (var pm in xs[x].OrderBy(p => p.Y))
            {
                var (u, v) = pm.Job.GetImage();
                Draw(x, y + n, new Glyph(u, v, Color.Black, pm.Tint));
                n++;
            }
        }
        
        var selected = _game.Party.Characters[PlayerSelectedIndex];
        
        for (var i = 0; i < _fullWidth; i++)
        {
            for (var j = 0; j < _fullHeight; j++)
            {
                var fg = Color.Black;
                var bg = Color.Black;
                foreach (var f in _fgs[(i, j)])
                {
                    fg = Color.Lerp(fg, f, 0.75f);
                }

                fg = Color.Lerp(fg, Color.White, _fgs[(i, j)].Count / 4.0f);
                
                if (Structure.Map.IsWalkable(i, j))
                {
                    var g = Glyph.Bw(_groundGlyphs[i, j].U, _groundGlyphs[i, j].V);
                    g.Fg = showMap ? Color.White : Color.Lerp(fg, Color.White, 0.5f);
                    bg = (i % 2 == j % 2) ? new Color(0, 0, 0, 1) : new Color(20, 0, 10, 1);
                    if (selected.Zone.Contains((i, j)))
                    {
                        bg = (i % 2 == j % 2) ? Party.Zones[PlayerSelectedIndex] : Color.Lerp(Party.Zones[PlayerSelectedIndex], Color.Black, 0.5f);
                        if (!selected.Fov.Contains((i, j)))
                        {
                            bg = Color.Lerp(bg, Color.Black, 0.5f);
                        }
                    }
                    else
                    {
                        if (!selected.Fov.Contains((i, j)))
                        {
                            bg = Color.Black;
                            g.Fg = Color.Black;
                        }
                        else
                        {
                            // -1..1
                            // *0.5 = -0.5..0.5
                            // +0.5 = 0..1
                            g.Fg = Color.Lerp(g.Fg, Color.Black,
                                (MathF.Sin((i % 2 == j % 2 ? Single.Pi : 0) + _time * 0.0001f) * 0.5f + 0.5f));
                        }
                    }
                    g.Bg = bg;

                    Draw(i, j, g);
                }
                else
                {
                    var g = _groundGlyphs[i, j];
                    Draw(i, j, new Glyph(g.U, g.V, Color.Black, showMap ? Color.White : fg));
                }
            }
        }
        
        foreach (var domain in Domains._domains)
        {
            domain.Draw(this);
        }
        
        foreach (var chr in _game.Party.Characters)
        {
            if (!showMap && !_fov.IsInFov(chr.X, chr.Y))
                continue;
            
            var (ix, iy) = chr.Job.GetImage();
            if (chr == selected)
            {
                iy -= 4;
            }
            var hasStamina = _game.ActionPoints.Count(EStatus.Stamina) > 0;
            if (chr.IsDone || !hasStamina)
            {
                Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, Color.DarkGray));
            }
            else
            {
                Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, chr.Tint));
            }
        }

        var max = Structure.Walkables.MaxDistance();
        var dm = Structure.Walkables.Distances[0];
        var pred = (IMap<Cell> mp, int mx, int my) => dm.Get(mx, my) >= 2 && _fov.IsInFov(mx, my);
        
        var (gx, gy) = Structure.Goals[0];
        Draw(gx, gy, new Glyph(13, 60, Color.Black, Color.Lerp(Color.Red, Color.Yellow, Rnd.Instance.Next01())));

        var colors = new List<Color>() { Color.Yellow, Color.OrangeRed, Color.Red, Color.Purple };
        
        foreach (var chr in Structure.Enemies.Where(chr => showMap || _fov.IsInFov(chr.X, chr.Y)))
        {
            var (cu, cv) = chr.Icon;
            Draw(chr.X, chr.Y, new Glyph(cu, cv, Color.Black, chr.Active ? colors[chr.Level - 1] : Color.Gray));
        }
        
        foreach (var chr in Structure.Treasure.Where(chr => showMap || _fov.IsInFov(chr.Item1, chr.Item2)))
        {
            Draw(chr.Item1, chr.Item2, "?", Color.White);
        }

        DrawParty();
    }

    private readonly List<int> _offsets =
    [
        1, 1, 0, 0
    ];
    
    private readonly List<int> _xoffsets =
    [
        0, 0, 0, 0
    ];
    
    private readonly List<(int, int)> _positions = [
        (0, 0), (3, 0), (0, 3), (3, 3)
    ];

    private readonly List<string> _positionStats =
    [
        "WIL", "CLA", "POI", "VIG"
    ];
    
    private void DrawParty((PartyMember?, int?, int?, int?, int?)? change = null)
    {
        var (cha, cwil, ccla, cvig, cpoi) = change ?? (null, null, null, null, null);
        var h = 19;
        var index = 0;
        foreach (var character in _game.Party.Characters)
        {
            var (m, r) = character.Job.GetImage();
            var (u, v) = character.GetPortait();
            var (x, y) = _positions[index];
            var (xoff, yoff) = (_xoffsets[index], _offsets[index]);
            var tint = character.Tint;
            if (character != _game.Party.Characters[PlayerSelectedIndex])
            {
                tint = Color.Lerp(tint, Color.Black, 0.75f);
            }
            
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y - 1 + yoff, $"{character.Job.GetShortName()}", tint);
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y + yoff, $"{_positionStats[index]}", tint);
            var hp = $"HP{character.HP}";
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -11 - hp.Length : 0), 5 * y + yoff + 1, hp, Color.White);
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -11 - hp.Length : 0), 5 * y + yoff + 1, $"HP", tint);
            
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 4 + yoff, $"WIL  CLA  ", tint);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 5 + yoff, $"VIG  POI  ", tint);
            
            if (character == cha)
            {
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{cwil ?? character.Wil}", cwil == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{ccla ?? character.Cla}", ccla == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{cvig ?? character.Vig}", cvig == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{cpoi ?? character.Poi}", cpoi == null ? Color.White : Color.Yellow);
            }
            else
            {
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{character.Wil}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{character.Cla}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{character.Vig}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{character.Poi}", Color.White);
            }

            if (character.GetLeftWeapon() is {} lw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, $"{lw.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, "[LEFT ARM]", Color.Gray);
            
            if (character.GetRightWeapon() is {} rw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, $"{rw.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, "[RIGHT ARM]", Color.Gray);
            
            if (character.GetItem() is {} it)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, $"{it.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, "[EQUIPMENT]", Color.Gray);
            
            if (index < 2)
            {
                _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait2"].Set(x * 2, y, new Glyph(u, v, Color.Black, tint));
            }
            else
            {
                _game.Layers["portrait"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait"].Set(x * 2, y, new Glyph(u, v, Color.Black, tint));
            }

            index++;
        }
    }
    
    public bool SkipGUI { get; set; } = false;

    private int _offset = 96;
    
    public void Draw(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();

        DrawCombat();
        DrawSubmenu();
    }

    private void DrawSubmenu()
    {
        if (_submenu.Count > 0)
        {
            var len = _submenu.Select(s => s.Length).Max() + 2;
            var (x, y) = (15, 19);
            _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
            _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count), Sides.Ascii, Corners.Ascii);

            for (int i = 0; i < _submenu.Count; i++)
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {_submenu[i]}");
            }
            _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");

            if (_submenu[_submenuSelection] == "ATTACK")
            {
                var (px, py) = (
                    _game.Party.Characters[PlayerSelectedIndex].X,
                    _game.Party.Characters[PlayerSelectedIndex].Y);
                
                DrawSubmenuAttackInfo(px + _submenuDelta.Item1, py + _submenuDelta.Item2);
            }
        }
    }

    private void DrawSubmenuAttackInfo(int x, int y)
    {
        var attacker = _game.Party.Characters[PlayerSelectedIndex];
        Character? defender = null;
        if (Positions.IsCharacterAt(this, x, y) is { } c)
        {
            defender = c;
        }
        else if (Positions.IsEnemyAt(this, x, y) is { } e)
        {
            defender = e;
        }

        if (defender is { } d)
        {
            var attack = Combat.Attack(attacker, defender);
            var hpDamage = 0;
            var woundDamage = 0;
            var poiseDamage = 0;
            
            foreach (var atk in attack)
            {
                if (atk is DealHPDamage dmg)
                {
                    hpDamage += dmg.Amount;
                }
                else if (atk is DealWoundDamage dmw)
                {
                    woundDamage += dmw.Amount;
                }
                else if (atk is DealPoiseDamage dmp)
                {
                    poiseDamage += dmp.Amount;
                }
            }
            
            if (d is Enemy e)
                DrawSubmenuAttackEnemy(e, hpDamage, woundDamage, poiseDamage);
        }
    }

    private void DrawSubmenuAttackEnemy(Enemy enemy, int dhp = 0, int dwnd = 0, int dpoi = 0)
    {
        var woundsBefore = enemy.AP.Count(EStatus.Wound);
        var woundsAfter = woundsBefore + dwnd;
        
        var chanceBefore = Math.Clamp((int)(100 * (woundsBefore / (float)enemy.AP.Width)), 0, 100);
        var chanceAfter = Math.Clamp((int)(100 * (woundsAfter / (float)enemy.AP.Width)), 0, 100);

        var (m, r) = enemy.Icon;
        var (u, v) = enemy.GetPortait();
        var (x, y) = (2, 2);
        var (xoff, yoff) = (0, 1);
        _game.Layers["ascii"].SetRect(new Vector2(20 * x - 3, 5 * y - 3 + yoff), new Vector2(20 * x + 14, 5 * y + yoff + 12), ' ');
        _game.Layers["ascii"].SetBox(new Vector2(20 * x - 4, 5 * y - 4 + yoff), new Vector2(20 * x + 15, 5 * y + yoff + 13), Sides.Ascii, Corners.Ascii);
        _game.Layers["ascii"].Set(20 * x - 2, 4 * y + yoff - 1, $"{enemy.GetName()}", enemy.Tint);
        var hp = $"HP{enemy.HP}";
        var len = hp.Length;
        _game.Layers["ascii"].Set(20 * x + 2 - len, 5 * y + yoff - 2, hp, Color.White);
        _game.Layers["ascii"].Set(20 * x + 2 - len, 5 * y + yoff - 2, $"HP", enemy.Tint);

        if (dwnd > 0 && (_time > 600))
        {
            _game.Layers["ascii"].Set(20 * x + 5 + len - 10, 4 * y + yoff + 13, $"+{dwnd} WND", Color.OrangeRed);
            _game.Layers["ascii"].Set(20 * x + 5 + len - 10, 4 * y + yoff + 14, $"{chanceBefore}~{chanceAfter} HP DOWN%", Color.OrangeRed);
        }

        if (dhp > 0 && (_time > 600))
        {
            _game.Layers["ascii"].Set(20 * x + 6 - len, 5 * y + yoff - 2, new Glyph(9, 5, Color.Black, Color.OrangeRed));
            _game.Layers["ascii"].Set(20 * x + 8 - len, 5 * y + yoff - 2, $"{Math.Max(0, enemy.HP - dhp)}", Color.OrangeRed);
        }

        _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 4 + yoff, $"WIL  CLA", enemy.Tint);
        _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{enemy.Wil}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{enemy.Cla}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 5 + yoff, $"VIG  POI ", enemy.Tint);
        _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{enemy.Vig}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{enemy.Poi}", Color.White);
        
        if (enemy.GetLeftWeapon() is {} lw)
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 7 + yoff, $"{lw.Name}", enemy.Tint);
        else
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 7 + yoff, "[LEFT ARM]", Color.Gray);
        
        if (enemy.GetRightWeapon() is {} rw)
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 8 + yoff, $"{rw.Name}", enemy.Tint);
        else
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 8 + yoff, "[RIGHT ARM]", Color.Gray);
        
        if (enemy.GetItem() is {} it)
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 9 + yoff, $"{it.Name}", enemy.Tint);
        else
            _game.Layers["ascii"].Set(20 * x - 2 + xoff, 5 * y + 9 + yoff, "[EQUIPMENT]", Color.Gray);
        
        _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
        _game.Layers["portrait2"].Set(x * 2, y, new Glyph(u, v, Color.Black, enemy.Tint));
    }

    private bool showMap = false;
    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.F12))
        {
            showMap = !showMap;
        }
    }
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return RunEnemyMoves();
        yield return ResetPartyMembers();
    }

    private bool _inspectMode = false;
    
    private void CheckPlayerInputs()
    {
        if (_inspectMode)
        {
            if (KB.HasBeenPressed(Keys.Escape))
            {
                _inspectMode = false;
            }
            return;
        }
        
        var current = _game.Party.Characters[PlayerSelectedIndex];
        if (KB.HasBeenPressed(Keys.A))
        {
            var ability = current.Ability;
            if (ability != null)
            {
                if (ability.CanBeUsed(current, current.X, current.Y) && current.AP.Count(EStatus.Stamina) > 0)
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
        
        if (_submenu.Count > 0)
        {
            if (KB.HasBeenPressed(Keys.Up))
            {
                if (_submenuSelection == 0)
                {
                    _submenuSelection = _submenu.Count - 1;
                }
                else
                {
                    _submenuSelection--;
                }
            }
            else if (KB.HasBeenPressed(Keys.Down))
            {
                if (_submenuSelection == _submenu.Count - 1)
                {
                    _submenuSelection = 0;
                }
                else
                {
                    _submenuSelection++;
                }
            }
            else if (KB.HasBeenPressed(Keys.Space))
            {
                var opt = _submenu[_submenuSelection];
                _submenu.Clear();
                DrawCombat();
                
                if (opt == "SWAP")
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    if (Positions.IsCharacterAt(this, x, y) is { } c)
                    {
                        c.X = current.X;
                        c.Y = current.Y;
                        current.X = x;
                        current.Y = y;
                        _game.ActionPoints.Spend(1);

                        if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                        {
                            DrawCombat();
                            CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                .ApplyOnDomainStepped(this, current, current.X, current.Y, x, y));
                        }
                    }
                    else if (Positions.IsEnemyAt(this, x, y) is { } e)
                    {
                        e.X = current.X;
                        e.Y = current.Y;
                        current.X = x;
                        current.Y = y;
                        _game.ActionPoints.Spend(1);

                        if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                        {
                            DrawCombat();
                            CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                .ApplyOnDomainStepped(this, current, current.X, current.Y, x, y));
                        }
                    }
                }
                else if (opt == "ATTACK")
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    if (Positions.IsCharacterAt(this, x, y) is { } c)
                    {
                        CoroutineHandler.Run(Coroutine_Attack(current, c));
                    }
                    else if (Positions.IsEnemyAt(this, x, y) is { } e)
                    {
                        CoroutineHandler.Run(Coroutine_Attack(current, e));
                    }
                }
                else if (opt == "FORTIFY")
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    current.X = x;
                    current.Y = y;
                    _game.ActionPoints.Unspend(current.Vig);
                    current.Temp.Reset();
                    MarkDone(current);
                }
                else if (opt == "EXERT")
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    current.X = x;
                    current.Y = y;
                    _game.ActionPoints.Spend((int)Math.Ceiling(current.Weight));
                    current.SetOrigin();
                    CalculateZone(current);
                }
                else if (opt == "CYCLE")
                {
                    PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
                    ShouldUpdateView = true;
                }
                else if (opt == "CONSUME")
                {
                    CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
                    Muse.SetTravelMood();
                }
                else if (opt == "INSPECT")
                {
                    _inspectMode = true;
                }
                else if (opt == "CANCEL")
                {
                }
            }
        }
        // MOVE
        else if (PlayerSelectedIndex > -1)
        {
            if (KB.HasBeenPressed(Keys.Tab))
            {
                PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
                ShouldUpdateView = true;
            }

            if (KB.HasBeenPressed(Keys.Space))
            {
                _submenuDelta = (0, 0);
                StartSubmenu(["CYCLE", "FORTIFY", "INSPECT"]);
            }
            
            if (_game.ActionPoints.Count(EStatus.Stamina) > 0 && !current.IsDone)
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

                        if (Positions.IsCharacterAt(this, x + dx, y + dy) is { } _)
                        {
                            _submenuDelta = (dx, dy);
                            List<string> opts = ["SWAP"];
                            if (current.Vig > 0)
                            {
                                opts.Add("ATTACK");
                            }
                            StartSubmenu(opts.ToArray());
                        }
                        else if (Positions.IsEnemyAt(this, x + dx, y + dy) is { } e)
                        {
                            // ENEMY
                            _submenuDelta = (dx, dy);
                            List<string> opts = [];
                            if (current.Vig > 0)
                            {
                                opts.Add("ATTACK");
                            }
                            
                            if (e.Poi < current.Poi)
                            {
                                opts.Add("SWAP");
                            }
                            StartSubmenu(opts.ToArray());
                        }
                        else if (Structure.Map.IsWalkable(x + dx, y + dy))
                        {
                            var nx = x + dx;
                            var ny = y + dy;

                            if (Domains.Tiles.ContainsKey((nx, ny)))
                            {
                                current.X = nx;
                                current.Y = ny;
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[(nx, ny)]
                                    .ApplyOnDomainStepped(this, current, nx, ny, x, y));
                            }

                            if (!current.Zone.Contains((nx, ny)))
                            {
                                _submenuDelta = (dx, dy);
                                StartSubmenu(["FORTIFY", "EXERT"]);
                            }
                            else
                            {
                                current.X += dx;
                                current.Y += dy;
                            }
                        }
                        else if (Structure.Treasure.Contains((current.X + dx, current.Y + dy)))
                        {
                            var (tx, ty) = (current.X + dx, current.Y + dy);
                            Structure.Treasure.Remove((tx, ty));
                            Structure.Map.SetCellProperties(tx, ty, true, true);
                            CalculateZone(current);
                            _game.ActionPoints.Spend(1);
                            MarkDone(current);
                        }
                        else if (Structure.Goals[0] == (current.X + dx, current.Y + dy))
                        {
                            _submenuDelta = (dx, dy);
                            StartSubmenu(["CONSUME"]);
                        }
                    }
                    
                    UpdateCombatView();
                }
            }
        }
    }

    private void StartSubmenu(string[] opts)
    {
        _submenuSelection = 0;
        foreach (var opt in opts)
        {
            _submenu.Add(opt);
        }
        _submenu.Add("CANCEL");
    }
    
    IEnumerable Coroutine_Attack(Character attacker, Character defender)
    {
        var guv = (0, 0);
        if (defender is PartyMember pm) guv = pm.Job.GetImage();
        else if (defender is Enemy en) guv = en.Icon;
        
        for (int i = 0; i < 5; i++)
        {
            var (gu, gv) = guv;
            Draw(defender.X, defender.Y, new Glyph(gu, gv, Color.Black, defender.Tint));
            yield return new WaitForSeconds(0.01f);
            Draw(defender.X, defender.Y, new Glyph(gu, gv - 4, Color.Black, defender.Tint));
            yield return new WaitForSeconds(0.01f);
        }
        Draw(defender.X, defender.Y, new Glyph(5, 31, Color.Black, Color.OrangeRed));
        yield return new WaitForSeconds(0.1f);
        Draw(defender.X, defender.Y, new Glyph(5, 30, Color.Black, Color.Red));
        yield return new WaitForSeconds(0.1f);
        Draw(defender.X, defender.Y, new Glyph(guv.Item1, guv.Item2, Color.Black, defender.Tint));
        
        if (defender is not PartyMember)
            defender.AP.Draw(DrawOffset.Item1 * 2 + 1, 20, defender);
        
        yield return new WaitForSeconds(0.5f);
        foreach (var effect in Combat.Attack(attacker, defender))
        {
            yield return new WaitForSeconds(0.2f);
            _game.Layers["ascii"].SetRect(new Vector2(20, 20), new Vector2(60, 24), ' ');
            if (effect is DealFatigueDamage ftg)
            {
                defender.AP.Add(EStatus.Fatigue, ftg.Amount);
                _game.Layers["ascii"].Set(50, 21, $"+{ftg.Amount} Fatigue");
            }
            else if (effect is DealWoundDamage wnd)
            {
                var woundsBefore = Math.Clamp((int)(100 * (defender.AP.Count(EStatus.Wound) / (float)defender.AP.Width)), 0, 100);
                defender.AP.Add(EStatus.Wound, wnd.Amount);
                var totalWounds = Math.Clamp((int)(100 * (defender.AP.Count(EStatus.Wound) / (float)defender.AP.Width)), 0, 100);
                
                _game.Layers["ascii"].Set(30, 21, $"+{wnd.Amount} WND ({woundsBefore}~{totalWounds} HP DOWN%)");
            }
            else if (effect is DealPoiseDamage poi)
            {
                defender.Temp.Poise -= poi.Amount;
                _game.Layers["ascii"].Set(40, 21, $"-{poi.Amount} Poise");
            }
            else if (effect is DealHPDamage hp)
            {
                defender.HP -= hp.Amount;
                if (defender.HP < 0) defender.HP = 0;
                _game.Layers["ascii"].Set(20, 21, $"-{hp.Amount} HP");
            }
            
            if (defender is not PartyMember)
                defender.AP.Draw(DrawOffset.Item1 * 2 + 1, 20, defender);
            
            yield return new WaitForSeconds(0.5f);

            var wounds = defender.AP.Count(EStatus.Wound);
            var superCrit = wounds >= defender.AP.Width && defender.HP > 1;
            var roll = Rnd.Instance.Next(0, defender.AP.Width);
            if (roll <= wounds || superCrit)
            {
                var nextHP = defender.HP;
                var damage = 0;
                if (superCrit)
                {
                    damage = nextHP - 1;
                }
                else if (roll < Math.Max(1, wounds / 5))
                {
                    damage = 3;
                }
                else
                {
                    damage = 1;
                }
                nextHP -= damage;
                if (nextHP < 0) nextHP = 0;

                for (var i = 0; i < 5; i++)
                {
                    if (superCrit)
                    {
                        _game.Layers["ascii"].Set(30, 22, $"HP BREAK!", i % 2 == 0 ? Color.White : Color.Red);
                    }
                    else if (damage == 1)
                    {
                        _game.Layers["ascii"].Set(30, 22, $"-{damage} HP!", i % 2 == 0 ? Color.White : Color.Red);
                    }
                    else if (damage == 3)
                    {
                        _game.Layers["ascii"].Set(30, 22, $"CRIT! -{damage} HP!", i % 2 == 0 ? Color.White : Color.Red);
                    }

                    if (i % 2 == 0)
                    {
                        _game.Layers["ascii"].Set(DrawOffset.Item1 * 2 + 1, 21, $"HP {defender.HP}    ");
                    }
                    else
                    {
                        _game.Layers["ascii"].Set(DrawOffset.Item1 * 2 + 1, 21, $"HP {nextHP}    ");
                    }
                    yield return new WaitForSeconds(0.005f);
                }
                _game.Layers["ascii"].Set(DrawOffset.Item1 * 2 + 1, 21, $"HP {nextHP}    ");
                yield return new WaitForSeconds(0.01f);
                defender.HP = nextHP;
            }

            if (defender.HP <= 0)
            {
                defender.Die();
                _game.Layers["ascii"].Set(30, 23, $"{defender.GetName()} dies!");
            }
        }
        yield return new WaitForSeconds(0.5f);
        if (defender is Enemy { IsDead: true } e)
        {
            Structure.Enemies.Remove(e);
            UpdateCombatView();
        }

        if (attacker is PartyMember m)
        {
            var nextVig = attacker.Vig - 1;
            for (int i = 0; i < 10; i++)
            {
                if (i % 2 == 0)
                {
                    DrawParty((m, null, null, nextVig, null));
                }
                else
                {
                    DrawParty();
                }

                yield return new WaitForSeconds(0.01f);
            }
        }

        attacker.Temp.Vigor--;
        if (attacker.Vig == 0)
        {
            if (attacker is PartyMember p)
            {
                MarkDone(p);
            }
            else if (attacker is Enemy n)
            {
                n.NoMove = true;
            }
        }
    }
}
