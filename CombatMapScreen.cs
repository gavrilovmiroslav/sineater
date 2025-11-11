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
    private readonly Arch.Core.World _world;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    
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
        _world = Arch.Core.World.Create();
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

        foreach (var (tx, ty) in Structure.Treasure)
        {
            Map.SetCellProperties(tx, ty, false, false);
        }
        
        _rendered = false;

        for (var ci = 0; ci < 4; ci++)
        {
            _game.Party.Characters[ci].X = Structure.Starts[ci].Item1;
            _game.Party.Characters[ci].Y = Structure.Starts[ci].Item2;
        }

        foreach (var chr in _game.Party.Characters)
        {
            _world.Create(new FriendlyTeam(), new Combatant(chr), new LiveStats(new Stats(chr)), new Position(chr.X, chr.Y));
        }

        foreach (var enm in Structure.Enemies)
        {
            _world.Create(new EnemyTeam(), new Combatant(enm), new LiveStats(new Stats(enm)), new Position(enm.X, enm.Y));
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

            foreach (var cell in selfFov.ComputeFov(w.X, w.Y, w.Cla, true))
            {
                _fgs.Add((cell.X, cell.Y), w.Tint);
            }
            
            if (i == 0)
            {
                _fov.ComputeFov(w.X, w.Y, w.Cla, true);
            }
            else
            {
                _fov.AppendFov(w.X, w.Y, w.Cla, true);
            }
        }
    }
    
    internal void DrawCombat(bool onlyNow = false)
    {
        if (ShouldUpdateView)
        {
            UpdateCombatView();
            ShouldUpdateView = false;
        }

        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(_fullWidth - 1, _fullHeight + 2), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(_fullWidth * 2 - 2, _fullHeight * 2 + 2), ' ');
        
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
        
        for (var i = 0; i < _fullWidth; i++)
        {
            for (var j = 0; j < _fullHeight; j++)
            {
                if (!showMap && !_fov.IsInFov(i, j)) continue;
                var fg = Color.Black;
                foreach (var f in _fgs[(i, j)])
                {
                    fg = Color.Lerp(fg, f, 0.75f);
                }

                fg = Color.Lerp(fg, Color.White, _fgs[(i, j)].Count / 4.0f);
                
                if (Structure.Map.IsWalkable(i, j))
                {
                    var g = Glyph.Bw(_groundGlyphs[i, j].U, _groundGlyphs[i, j].V);
                    g.Fg = showMap ? Color.White : fg;
                    g.Bg = (i % 2 == j % 2) ? new Color(10, 0, 0, 1) : new Color(20, 10, 0, 1);

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
            var hasStamina = _game.ActionPoints.Count<StatusStamina>() > 0;
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
            Draw(chr.X, chr.Y, new Glyph(cu, cv, Color.Black, colors[chr.Level - 1]));
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
    
    private void DrawParty()
    {
        var h = 19;
        var index = 0;
        foreach (var character in _game.Party.Characters)
        {
            var (m, r) = character.Job.GetImage();
            var (u, v) = character.GetPortait();
            var (x, y) = _positions[index];
            var (xoff, yoff) = (_xoffsets[index], _offsets[index]);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 4 + yoff, $"CLA  WIL", character.Tint);
            _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{character.Cla}", Color.White);
            _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{character.Wil}", Color.White);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 5 + yoff, $"POI  VIG ", character.Tint);
            _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{character.Poi}", Color.White);
            _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{character.Vig}", Color.White);
            
            if (character.GetLeftWeapon() is {} lw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, $"{lw.Name}", character.Tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, "[LEFT ARM]", Color.LightGray);
            
            if (character.GetRightWeapon() is {} rw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, $"{rw.Name}", character.Tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, "[RIGHT ARM]", Color.LightGray);
            
            if (character.GetItem() is {} it)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, $"{it.Name}", character.Tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, "[EQUIPMENT]", Color.LightGray);
            
            if (index < 2)
            {
                _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait2"].Set(x * 2, y, new Glyph(u, v, Color.Black, character.Tint));
            }
            else
            {
                _game.Layers["portrait"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait"].Set(x * 2, y, new Glyph(u, v, Color.Black, character.Tint));
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
        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth + 40, 40), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth * 2 + 40, 40), ' ');

        DrawCombat();
    }

    private bool showMap = false;
    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.Tab))
        {
            showMap = !showMap;
        }
        
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }
    }
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return new WaitForSeconds(0.5f);
        //_presentation = EPresentationState.Done;
    }
    
    private void CheckPlayerInputs()
    {
        if (KB.HasBeenPressed(Keys.Space))
        {
            PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
            UpdateCombatView();
        }
        
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

                        if (Positions.IsCharacterAt(this, x + dx, y + dy) is { } c)
                        {
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
                        else if (Positions.IsEnemyAt(this, x + dx, y + dy) is { } e)
                        {
                            // do nothing
                        }
                        else if (Structure.Map.IsWalkable(x + dx, y + dy))
                        {
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
                                if (pm.Steps > pm.Vig)
                                {
                                    pm.Steps = 0;
                                    _game.ActionPoints.Spend(1);
                                }
                            }
                        }
                    }
                    
                    UpdateCombatView();
                }
            }
        }
    }
}
