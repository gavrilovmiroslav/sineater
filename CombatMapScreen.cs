using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Content;
using static SINEATER.Extensions;

namespace SINEATER;

public enum ECombatState
{
    CheckIfFinished,
    EnemyPlanningPhase,
    PlayerActionPhase,
    EnemyExecutionPhase,
}

public enum EPresentationState
{
    Preparing,
    Executing,
    Done
}

public enum ETerrainKind
{
    Tomb,
    Temple,
    Cave,
    Clearing,
    Ruin,
    Unknown
}

public class CombatState(int x, int y, int initiative, Color tint, int move)
{
    public int X { get; set; } = x;
    public int Y { get; set; } = y;
    public int Initiative { get; set; } = initiative;
    public Color Tint { get; set; } = tint;
    public int Move { get; set; } = move;
}

public class CombatMapScreen : IScreen
{
    private readonly int _fullWidth = 24, _fullHeight = 22;
    private readonly int _offsetX = 0, _offsetY = 2;
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    private IMap? _map;
    private bool _rendered = false;
    private bool _debugView = false;
    private int _secondCounter = 0;
    private FieldOfView _fieldOfView;
    private ReadOnlyCollection<Cell>?[] _perspectives;
    private Color[,] _coloredMap;
    private bool[,] _visited;
    ReadOnlyCollection<Cell>? _fov = null;
    HashSet<(int, int)>? _fov2 = new();
    private Dictionary<Character, CombatState> _combatStates = new();
    private List<Character> _turnOrder = new();
    private bool _showStats = false;
    private ECombatState _combatState = ECombatState.PlayerActionPhase;
    private EPresentationState _presentation = EPresentationState.Preparing;
    private int _playerSelectedIndex = -1;
    private Glyph[,] _groundGlyphs;
    
    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = Rnd.Instance.Next(3 * _fullWidth / 4, _fullWidth - 2);
            this._height = Rnd.Instance.Next(3 * _fullHeight / 4, _fullHeight - 2);
        }

        Regenerate();
    }
    
    private void Regenerate() => Regenerate(_kind);
    private int _extraFill = 0;
    
    public CombatMapScreen(SineaterGame game, ETerrainKind kind, int width = -1, int height = -1)
    {
        _width = width;
        _height = height;
        _coloredMap = new Color[_fullWidth, _fullHeight];
        _visited = new bool[_fullWidth, _fullHeight];
        
        _kind = kind;
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
        _presentation = EPresentationState.Preparing;
        _combatState = ECombatState.PlayerActionPhase;
        _combatStates.Clear();
        _kind = kind;
        var (a, b, c, d, e) = (0, 0, 0, _width, _height);
        switch (_kind)
        {
            case ETerrainKind.Tomb:
                (a, b, c, d, e) = (32, 2, 2, 22, 20);//36
                break;
            case ETerrainKind.Temple:
                (a, b, c, d, e) = (40, 1, 1, 22, 20); //45
                break;
            case ETerrainKind.Cave:
                (a, b, c) = (52, 3, 3); //47
                break;
            case ETerrainKind.Clearing:
                (a, b, c) = (54, 3, 1); //49
                break;
            case ETerrainKind.Ruin:
                (a, b, c) = (92, 2, 2);//89
                break;
            default:
                (a, b, c, d, e) = (Rnd.Instance.Next(1, 99), Rnd.Instance.D6, Rnd.Instance.D6, 22, 20);
                break;
        }

        a += _extraFill;
        _width = d;
        _height = e;
        Console.WriteLine($"Fill probability: {a}, iterations: {b}, cutoff: {c}, size: {_width} x {_height}");

        IMapCreationStrategy<Map>? mapCreationStrategy = null;
        
        if (_width > _fullWidth - 2 || _height > _fullHeight - 2)
        {
            throw new Exception($"MAP CAN'T BE LARGER THAN {_fullWidth - 2}x{_fullHeight - 2} (is {_width}x{_height})");
        }
        if (_kind == ETerrainKind.Ruin)
        {
            mapCreationStrategy = new RandomRoomsMapCreationStrategy<Map>(_width, _height, a, b, c, Rnd.Instance);
        }
        else
        {
            mapCreationStrategy = new CaveMapCreationStrategy<Map>( _width, _height, a, b, c, Rnd.Instance);
        }

        var inner = Map.Create(mapCreationStrategy);
        _map = Map.Create(new FilledMapCreationStrategy<Map>(_fullWidth, _fullHeight));
        _map.Copy(inner, 1 + Rnd.Instance.Next(0, _fullWidth - 2 - _width), Rnd.Instance.Next(0, 1 + (_fullHeight - 2 - _height)));

        _fieldOfView = new FieldOfView(_map);
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                var g = Glyph.Bw(0, 0);
                if (_map.IsWalkable(i, j))
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

        var vas = _map.GetAllCells().Where(t => t.IsWalkable).ToArray();
        if (vas.Length <= 50)
        {
            _extraFill++;
            Regenerate();
            return;
        }
        
        vas.Shuffle();
        var vs = vas.AsEnumerable().GetEnumerator();
        var idx = 0;

        List<(int, int)> entryPositions = [];
        if (vs.MoveNext())
        {
            var v = vs.Current;
            entryPositions.Add((v.X, v.Y));
            var character = _game.Party.Characters[idx];
            _combatStates[character] = new CombatState(v.X, v.Y, Rnd.Instance.Next(character.Stats.Mod(EStat.Vigor), 5 + character.Stats.Vigor), character.Tint, 0);
            var freeTiles = new HashSet<Cell>(_map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
            idx++;
            for (int i = 0; i < 3; i++)
            {
                if (freeTiles.Count == 0)
                {
                    if (vs.MoveNext())
                    {
                        freeTiles.Add(vs.Current);
                    }
                    else
                    {
                        Console.WriteLine("HAD TO REGENERATE FORCEFULLY INNER!");
                        Regenerate(true);
                        return;
                    }
                }
                v = freeTiles.ToArray()[Rnd.Instance.Next(freeTiles.Count)];
                entryPositions.Add((v.X, v.Y));
                character = _game.Party.Characters[idx];
                _combatStates[character] = new CombatState(v.X, v.Y, Rnd.Instance.Next(character.Stats.Mod(EStat.Vigor), 5 + character.Stats.Vigor), character.Tint, 0);
                freeTiles.UnionWith(_map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
                freeTiles.RemoveWhere(t => entryPositions.Contains((t.X, t.Y)));
                idx++;
            }
        }
        else
        {
            Console.WriteLine("HAD TO REGENERATE FORCEFULLY!");
            Regenerate(true);
            return;
        }

        _extraFill = 0;
        
        _turnOrder.Clear();
        foreach (var ch in _combatStates.Keys.OrderBy(ch => -_combatStates[ch].Initiative))
        {
            _turnOrder.Add(ch);
        }
    }

    private void UpdateFov()
    {
        _fov = null;
        foreach (var (chr, combatState) in _combatStates)
        {
            if (_fov == null)
            {
                _fov = _fieldOfView.ComputeFov(combatState.X, combatState.Y, 5 + chr.Stats.Mod(EStat.Clarity), true);
            }
            else
            {
                _fov = _fieldOfView.AppendFov(combatState.X, combatState.Y, 5 + chr.Stats.Mod(EStat.Clarity), true);    
            }
        }

        _fov2.Clear();
        foreach (var f in _fov) _fov2.Add((f.X, f.Y));
        
        _perspectives = new ReadOnlyCollection<Cell>?[4];
        int i = 0;
        foreach (var (chr, combatState) in _combatStates)
        {
            _perspectives[i] = _fieldOfView.ComputeFov(combatState.X, combatState.Y, 5 + chr.Stats.Mod(EStat.Clarity), true);
            i++;
        }

        for (i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                _coloredMap[i, j] = new Color(0, 0, 0, 0);
            }
        }
        
        foreach (var (chr, _) in _combatStates)
        {
            foreach (var cell in _perspectives[chr.Index])
            {
                _visited[cell.X, cell.Y] = true;
                switch (chr.Index)
                {
                    case 0: _coloredMap[cell.X, cell.Y].R++; break;
                    case 1: _coloredMap[cell.X, cell.Y].G++; break;
                    case 2: _coloredMap[cell.X, cell.Y].B++; break;
                    case 3: _coloredMap[cell.X, cell.Y].A++; break;
                }
            }
        }

        for (i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                var c = _coloredMap[i, j];
                var o = c.R + c.G + c.B + c.A;
                if (o > 2)
                {
                    _coloredMap[i, j] = Color.White;
                }
                else if (o == 2)
                {
                    var ci = 0;
                    var cs = new[] {Color.White, Color.White};
                    if (c.R > 0) cs[ci++] = _combatStates[_game.Party.Characters[0]].Tint;
                    if (c.G > 0) cs[ci++] = _combatStates[_game.Party.Characters[1]].Tint;
                    if (c.B > 0) cs[ci++] = _combatStates[_game.Party.Characters[2]].Tint;
                    if (c.A > 0) cs[ci++] = _combatStates[_game.Party.Characters[3]].Tint;
                    _coloredMap[i, j] = Color.Lerp(cs[0], cs[1], 0.5f);
                }
                else if (o == 1)
                {
                    var cs = Color.White;
                    if (c.R > 0) cs = _combatStates[_game.Party.Characters[0]].Tint;
                    if (c.G > 0) cs = _combatStates[_game.Party.Characters[1]].Tint;
                    if (c.B > 0) cs = _combatStates[_game.Party.Characters[2]].Tint;
                    if (c.A > 0) cs = _combatStates[_game.Party.Characters[3]].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, cs, 0.5f);
                }
            }
        }
    }
    
    public void Update(GameTime gameTime)
    {
        _secondCounter += gameTime.ElapsedGameTime.Milliseconds;
        if (_secondCounter > 1600)
        {
            _secondCounter = 0;
        }
        
        CheckInputs();

        if (_presentation == EPresentationState.Preparing)
        {
            UpdateFov();
            
            switch (_combatState)
            {
                case ECombatState.CheckIfFinished:
                    break;
                case ECombatState.EnemyPlanningPhase:
                    break;
                case ECombatState.PlayerActionPhase:
                    _playerSelectedIndex = 0;
                    int max = 0;
                    foreach (var (chr, st) in _combatStates)
                    {
                        if (max < chr.Stats.Will)
                        {
                            max = chr.Stats.Will;
                        }
                    }

                    max += 5;
                    foreach (var (chr, st) in _combatStates)
                    {
                        st.Move = max;
                    }
                    _presentation = EPresentationState.Executing;
                    break;
                case ECombatState.EnemyExecutionPhase:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (_presentation == EPresentationState.Executing)
        {
            switch (_combatState)
            {
                case ECombatState.CheckIfFinished:
                    break;
                case ECombatState.EnemyPlanningPhase:
                    break;
                case ECombatState.PlayerActionPhase:
                    CheckPlayerInputs();
                    break;
                case ECombatState.EnemyExecutionPhase:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DrawCombat()
    {
        var index = 0;
        
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                _game.Layers["mrmo"].Unset(i + _offsetX, j + _offsetY);
            }
        }
        
        index = 0;
        if (_fov != null)
        {
            //
            for (int i = 0; i < _fullWidth; i++)
            {
                for (int j = 0; j < _fullHeight; j++)
                {
                    if (_fov2.Contains((i, j)))
                    {
                        var g = Glyph.Bw(_groundGlyphs[i, j].U, _groundGlyphs[i, j].V);
                        g.Fg = _coloredMap[i, j];
                
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, g);
                    }
                    else if (_visited[i, j])
                    {
                        var g = _groundGlyphs[i, j];
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, new Glyph(g.U, g.V, Color.Black, Color.SlateGray));
                    }
                    else
                    {
                        _game.Layers["mrmo"].Unset(i + _offsetX, j + _offsetY);
                    }
                }
            }
        }
        
        foreach (var (chr, cs) in _combatStates)
        {
            var (ix, iy) = chr.Job.GetImage();
            _game.Layers["mrmo"].Set(cs.X + _offsetX, cs.Y + _offsetY, new Glyph(ix, iy, Color.Black, _combatStates[chr].Tint));
            index++;
        }
    }

    private void DrawGUI()
    {
        // HEADER
        _game.Layers["ascii"].Set(0, 0, "                                                                                                           ");
        _game.Layers["ascii"].Set(0, 0, "TEMPLE OF ELEMENTAL EVIL");
        // LOG
        
        for (int i = 0; i < 22; i++)
        {
            for (int j = 0; j < _fullHeight + _offsetY; j++)
            {
                _game.Layers["ascii"].Set(i + 2 * _fullWidth + 2, j, " ");
            }
        }

        for (int i = 0; i < 6; i++)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 2 + i, " ");
        }
        
        _game.Layers["mrmo"].SetRect(new Vector2(2 + _fullWidth - 4, 2), new Vector2(2 + _fullWidth + 23, 2 + 6), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(2 * _fullWidth + 2, 2), new Vector2(2 * _fullWidth + 23, 2 + 6), ' ');

        if (_showStats)
        {
            DrawStats();
        }
        else
        {
            DrawDetails();
        }
    }

    private void DrawDetails()
    {
        _game.Layers["ascii"].Set(2 * _fullWidth - 1, 0, "CHAR       SEE MOV");
        
        var index = 0;
        foreach (var character in _turnOrder)
        {
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 2 + index,
                new Glyph(ix, iy, Color.Black, _combatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 2 + index, character.Job.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 2 + index, (5 + character.Stats.Mod(EStat.Clarity)).ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 2 + index, _combatStates[character].Move.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            //
            // _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 2 + index, character.Stats.Poise.ToString(),
            //     Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            //
            // _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 19, 2 + index, character.Stats.Vigor.ToString(),
            //     Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            if (_playerSelectedIndex == index)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 2 + index, ">");
                if (_secondCounter < 400 || (_secondCounter > 800 && _secondCounter < 1200))
                {
                    _game.Layers["mrmo"].Set(_combatStates[character].X, _combatStates[character].Y + 1, 
                        new Glyph(12, 25, Color.Black, _combatStates[character].Tint));
                }
            }
            index++;
        }
    }

    private void DrawStats()
    {
        _game.Layers["ascii"].Set(2 * _fullWidth - 1, 0, "CHAR       WIL CLA POI VIG");
        
        var index = 0;
        foreach (var character in _turnOrder)
        {
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 2 + index,
                new Glyph(ix, iy, Color.Black, _combatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 2 + index, character.Job.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 2 + index, character.Stats.Will.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 2 + index, character.Stats.Clarity.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 2 + index, character.Stats.Poise.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 19, 2 + index, character.Stats.Vigor.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            if (_playerSelectedIndex == index)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 2 + index, ">");
                if (_secondCounter < 400 || (_secondCounter > 800 && _secondCounter < 1200))
                {
                    _game.Layers["mrmo"].Set(_combatStates[character].X, _combatStates[character].Y + 1, 
                        new Glyph(12, 25, Color.Black, _combatStates[character].Tint));
                }
            }
            index++;
        }
    }
    
    public void Draw(GameTime gameTime)
    {
        if (_map == null) return;
        DrawCombat();
        DrawGUI();
    }

    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }
        
        if (KB.HasBeenPressed(Keys.F1)) Regenerate(ETerrainKind.Tomb);
        if (KB.HasBeenPressed(Keys.F2)) Regenerate(ETerrainKind.Temple);
        if (KB.HasBeenPressed(Keys.F3)) Regenerate(ETerrainKind.Cave);
        if (KB.HasBeenPressed(Keys.F4)) Regenerate(ETerrainKind.Clearing);
        if (KB.HasBeenPressed(Keys.F5)) Regenerate(ETerrainKind.Ruin);
        if (KB.HasBeenPressed(Keys.F6)) Regenerate(ETerrainKind.Unknown);
        if (KB.HasBeenPressed(Keys.Escape)) Regenerate(true);

        _showStats = KB.IsPressed(Keys.LeftAlt);
    }

    private void CheckPlayerInputs()
    {
        if (KB.HasBeenPressed(Keys.Tab))
        {
            _playerSelectedIndex = (_playerSelectedIndex + 1) % 4;
            _secondCounter = 0;
        }

        // MOVE
        if (_playerSelectedIndex > -1)
        {
            var current = _turnOrder[_playerSelectedIndex];
            if (_combatStates[current].Move > 0)
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
                        var x = _combatStates[current].X;
                        var y = _combatStates[current].Y;
                        if (_map?.IsWalkable(x + dx, y + dy) ?? false)
                        {
                            var pos = _combatStates[current];
                            pos.X += dx;
                            pos.Y += dy;
                            _combatStates[current].Move--;
                            _game.Bar.Spend(1);
                            UpdateFov();
                        }
                    }
                }
            }
        }
    }
}