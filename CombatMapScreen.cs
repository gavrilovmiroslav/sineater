using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Content;

namespace SINEATER;

public enum ECombatState
{
    EnemyPhase,
    PlayerPhase,
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
    private string _title;
    private IMap? _map;
    private bool _rendered = false;
    private bool _debugView = false;
    private int _secondCounter = 0;
    private FieldOfView _fieldOfView;
    private ReadOnlyCollection<Cell>?[] _perspectives;
    private Color[,] _coloredMap;
    private bool[,] _visited;
    ReadOnlyCollection<Cell>? _fov = null;
    HashSet<(int, int)>? _isInActivePartyFOV = new();
    private Dictionary<Character, CombatState> _combatStates = new();
    private List<Character> _party = new();
    private List<Enemy> _enemies = new();
    private bool _showStats = false;
    private ECombatState _combatState = ECombatState.PlayerPhase;
    private EPresentationState _presentation = EPresentationState.Preparing;
    private int _playerSelectedIndex = -1;
    private Glyph[,] _groundGlyphs;
    private CoroutineHandler _coroutineHandler = new();
    private ActionPoints _enemyActionPoints;
    
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
    
    public CombatMapScreen(SineaterGame game, ETerrainKind kind, int width = -1, int height = -1, string title = "???")
    {
        _width = width;
        _height = height;
        _title = title;
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
        _coroutineHandler.Clear();
        _presentation = EPresentationState.Preparing;
        _combatState = ECombatState.PlayerPhase;
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

        // todo: move from here!
        _enemies.Clear();
        _enemies.Add(Enemy.Goblin());
        _enemies.Add(Enemy.Goblin());
        
        var hp = 0;
        foreach (var enemy in _enemies)
        {
            hp += enemy.Sin;
        }
        _enemyActionPoints = new ActionPoints(hp, _game.Layers["ascii"], new StatusStamina());
        foreach (var enemy in _enemies)
        {
            enemy.AP = _enemyActionPoints;
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

        List<Cell> toDeleteEntries = [];
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
                toDeleteEntries.Add(v);
                character = _game.Party.Characters[idx];
                _combatStates[character] = new CombatState(v.X, v.Y, Rnd.Instance.Next(character.Stats.Mod(EStat.Vigor), 5 + character.Stats.Vigor), character.Tint, 0);
                freeTiles.UnionWith(_map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
                freeTiles.RemoveWhere(t => entryPositions.Contains((t.X, t.Y)));
                idx++;
            }

            var tiles = new List<Cell>(vas);
            foreach (var t in toDeleteEntries)
            {
                tiles.Remove(t);
            }
            
            foreach (var t in freeTiles)
            {
                tiles.Remove(t);
            }
            
            if (tiles.Count < 2 * _enemies.Count)
            {
                Regenerate();
                return;
            }
            
            foreach (var enemy in _enemies)
            {
                var it = Rnd.Instance.Next(tiles.Count);
                v = tiles[it];
                enemy.X = v.X;
                enemy.Y = v.Y;
                tiles.RemoveAt(it);
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

        // TODO: move this away
        _party.Clear();
        foreach (var ch in _combatStates.Keys.OrderBy(ch => -_combatStates[ch].Initiative))
        {
            _party.Add(ch);
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

        _isInActivePartyFOV.Clear();
        foreach (var f in _fov) _isInActivePartyFOV.Add((f.X, f.Y));
        
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
                    _coloredMap[i, j] = Color.Lerp(Color.White, Color.Lerp(cs[0], cs[1], 0.5f), 0.5f);
                }
                else if (o == 1)
                {
                    var cs = Color.White;
                    if (c.R > 0) cs = _combatStates[_game.Party.Characters[0]].Tint;
                    if (c.G > 0) cs = _combatStates[_game.Party.Characters[1]].Tint;
                    if (c.B > 0) cs = _combatStates[_game.Party.Characters[2]].Tint;
                    if (c.A > 0) cs = _combatStates[_game.Party.Characters[3]].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, cs, 0.35f);
                }
            }
        }
    }

    public IEnumerable EnemyMoves()
    {
        _combatState = ECombatState.PlayerPhase;
        _presentation = EPresentationState.Preparing;
        _enemyActionPoints.Free(_enemies.Count);
                    
        var gm = new GoalMap<Cell>(_map, false);
        foreach (var (ch, cs) in _combatStates)
        {
            gm.AddGoal(cs.X, cs.Y, ch.Stats.Vigor);
        }
                    
        foreach (var enemy in _enemies)
        {
            gm.ClearObstacles();
            foreach (var e in _enemies.Where(e => e != enemy))
            {
                gm.AddObstacle(e.X, e.Y);
            }
            var path = gm.TryFindPath(enemy.X, enemy.Y);
                        
            if (path != null)
            {
                for (var i = 0; i < enemy.Stats.Will; i++)
                {
                    if (enemy.AP.Remaining == 0)
                    {
                        continue;
                    }

                    var next = path.StepForward();
                    if (IsCharacterAt(next.X, next.Y) is {} chr)
                    {
                        var (ex, ey) = enemy.Icon;
                        var (cx, cy) = chr.Job.GetImage();
                        for (int f = 0; f < 10; f++)
                        {
                            _game.Layers["mrmo"].Set(enemy.X, enemy.Y + _offsetY,
                                new Glyph(ex, ey, Color.Black, f % 2 == 0 ? Color.Red : enemy.Tint));
                            _game.Layers["mrmo"].Set(next.X, next.Y + _offsetY,
                                new Glyph(cx, cy, Color.Black, f % 2 == 1 ? Color.Red : chr.Tint));
                            yield return new WaitForSeconds(0.01f);
                        }
                        yield return new WaitForSeconds(0.5f);
                        yield return Attack(enemy, chr);
                    }
                    else
                    {
                        enemy.X = next.X;
                        enemy.Y = next.Y;
                        enemy.AP.Spend(1);
                    }

                    DrawGui();
                    DrawCombat();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else
            {
                Console.WriteLine("NO PATH!");
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

        if (_coroutineHandler.IsActive())
        {
            _coroutineHandler.Update();
        }
        else if (_presentation == EPresentationState.Preparing)
        {
            UpdateFov();
            
            switch (_combatState)
            {
                case ECombatState.EnemyPhase:
                    _coroutineHandler.Run(EnemyMoves());
                    
                    break;
                case ECombatState.PlayerPhase:
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
                        _game.ActionPoints.Free(st.Move);
                        st.Move = max;
                    }

                    _combatState = ECombatState.PlayerPhase;
                    _presentation = EPresentationState.Executing;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else if (_presentation == EPresentationState.Executing)
        {
            if (_enemies.Count == 0)
            {
                _coroutineHandler.Run(new FadeOutAndLeaveScreen(1));
            }
            
            switch (_combatState)
            {
                case ECombatState.EnemyPhase:
                    break;
                case ECombatState.PlayerPhase:
                    CheckPlayerInputs();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        
        }
        else if (_presentation == EPresentationState.Done)
        {
            switch (_combatState)
            {
                case ECombatState.EnemyPhase:
                    break;
                case ECombatState.PlayerPhase:
                    _combatState = ECombatState.EnemyPhase;
                    _presentation = EPresentationState.Preparing;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private void DrawCombat()
    {
        var index = 0;
        
        _game.Layers["mrmo"].SetRect(new Vector2(_offsetX, _offsetY), new Vector2(_fullWidth + _offsetX, _fullHeight + _offsetY), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(_offsetX, _offsetY), new Vector2(_fullWidth * 2 + _offsetX, _fullHeight * 2 + _offsetY), ' ');
        
        index = 0;
        if (_fov != null)
        {
            for (int i = 0; i < _fullWidth; i++)
            {
                for (int j = 0; j < _fullHeight; j++)
                {
                    if (_isInActivePartyFOV.Contains((i, j)))
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

        foreach (var enemy in _enemies)
        {
            if (!_isInActivePartyFOV.Contains((enemy.X, enemy.Y))) continue;
            var (ix, iy) = enemy.Icon;
            _game.Layers["mrmo"].Set(enemy.X + _offsetX, enemy.Y + _offsetY, new Glyph(ix, iy, Color.Black, enemy.Tint));
        }
        
        foreach (var (chr, cs) in _combatStates)
        {
            var (ix, iy) = chr.Job.GetImage();
            _game.Layers["mrmo"].Set(cs.X + _offsetX, cs.Y + _offsetY, new Glyph(ix, iy, Color.Black, 
                _combatStates[chr].Move > 0 ? _combatStates[chr].Tint : Color.DarkGray));
            index++;
        }
    }

    private void DrawGui()
    {
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
        
        _game.Layers["mrmo"].SetRect(new Vector2(2 + _fullWidth - 4, 0), new Vector2(2 + _fullWidth + 40, 2 + 10), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(2 * _fullWidth + 2, 0), new Vector2(2 * _fullWidth + 40, 2 + 6), ' ');

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
        _game.Layers["ascii"].Set(2 * _fullWidth - 1, 0, "CHAR       SEE MOV LH RH DF");
        
        var index = 0;
        foreach (var character in _party)
        {
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 1 + index,
                new Glyph(ix, iy, Color.Black, _combatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 1 + index, character.Job.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 1 + index, (5 + character.Stats.Mod(EStat.Clarity)).ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 1 + index, _combatStates[character].Move.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 14, 1 + index, character.LeftWeapon?.Attack.ToString() ?? "-",
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 17, 1 + index, character.RightWeapon?.Attack.ToString() ?? "-",
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 20, 1 + index, character.Armor?.Guard.ToString() ?? "-",
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            if (_playerSelectedIndex == index)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 1 + index, ">");
                if (_secondCounter < 400 || _secondCounter is > 800 and < 1200)
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
        foreach (var character in _party)
        {
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 1 + index,
                new Glyph(ix, iy, Color.Black, _combatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 1 + index, character.Job.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 1 + index, character.Stats.Will.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 1 + index, character.Stats.Clarity.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 1 + index, character.Stats.Poise.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 19, 1 + index, character.Stats.Vigor.ToString(),
                Color.Lerp(Color.White, _combatStates[character].Tint, 0.5f));

            if (_playerSelectedIndex == index)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 1 + index, ">");
                if (_secondCounter < 400 || _secondCounter is > 800 and < 1200)
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

        if (_enemies.Count > 0)
        {
            _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(40, 2), ' ');
            _game.Layers["ascii"].Set(1, 0, _title);
            _enemyActionPoints.Draw(_title.Length + 3, 0);
        }

        if (_coroutineHandler.IsActive()) return;
        DrawCombat();
        DrawGui();
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
    
    private IEnumerable Attack(ICharacter attacker, ICharacter defender)
    {
        _game.Layers["ascii"].Set(2 * _fullWidth + 2, 6, $"{attacker.GetName()} attacks {defender.GetName()}", Color.White, Color.Black);
        
        var attackDice = new List<(int, Weapon)>();
        foreach (var weapon in new[] { attacker.GetLeftWeapon(), attacker.GetRightWeapon() })
        {
            if (weapon != null)
            {
                for (int i = 0; i < weapon.Attack; i++)
                {
                    var d6 = Rnd.Instance.D6;
                    attackDice.Add((d6, weapon));
                }
            }
        }
        
        var defenseDice = new List<(int, Armor)>();
        if (defender.GetArmor() != null)
        {
            var armor = defender.GetArmor();
            for (int i = 0; i < armor.Guard; i++)
            {
                defenseDice.Add((Rnd.Instance.D6, armor));
            }
        }
        
        _game.Layers["ascii"].Set(2 * _fullWidth + 2, 7, $"{attackDice.Count} strikes, {defenseDice.Count} guards.", Color.White, Color.Black);
        
        attacker.ApplyOnAttackRoll(defender, ref attackDice, ref defenseDice);
        defender.ApplyOnRolledAttack(attacker, ref attackDice, ref defenseDice);

        attackDice.Sort((a, b) => -a.Item1.CompareTo(b.Item1));
        if (!defender.IsStunned())
        {
            Console.WriteLine("Defender isn't stunned, sorting!");
            defenseDice.Sort((a, b) => -a.Item1.CompareTo(b.Item1));
        }
        else
        {
            Console.WriteLine("Defender is stunned, no sorting!");
            defender.GetAP().Reduce<StatusStunned>(1);
        }

        int diceIdx = 0;
        var defenseDiceQueue = new Queue<(int, Armor)>(defenseDice);
        
        int startLine = 9;
        _game.Layers["mrmo"].Set(2 + _fullWidth - 3, startLine, "atk");
        _game.Layers["mrmo"].Set(2 + _fullWidth - 3, startLine + 1, "def");
        _game.Layers["mrmo"].Set(2 + _fullWidth - 3, startLine + 2, "hit");
        _game.Layers["mrmo"].Set(2 + _fullWidth - 3, startLine + 3, "dmg");
        
        int wounds = 0;
        List<int> hitDice = [];
        foreach (var (atk, weapon) in attackDice)
        {
            var attack = atk;
            bool critAttack = false, critDefense = false;
            if (defenseDiceQueue.TryDequeue(out var nextDefenseDie))
            {
                var waitingTime = 0.01f;
                for (int i = 0; i < 5; i++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));

                    yield return new WaitForSeconds(waitingTime);
                    waitingTime += 0.01f;
                }

                waitingTime = 0.01f;
                for (int i = 0; i < 5; i++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 1,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
                    
                    yield return new WaitForSeconds(waitingTime);
                    waitingTime += 0.01f;
                }

                if (Rnd.Instance.D100 <= 10 + defender.GetStats().Poise)
                {
                    defender.GetAP().Spend(1);
                    critDefense = true;
                    attack = 0;
                }

                var (def, armor) = nextDefenseDie;
                var defense = def;
                if (attack > 0 && Rnd.Instance.D100 <= 10 + attacker.GetStats().Clarity)
                {
                    attacker.GetAP().Spend(1);
                    critAttack = true;
                    defense = 0;
                }

                Console.WriteLine($"Att: {attack}, def: {defense}, critAtt: {critAttack}, critDef: {critDefense}");
                if (!critAttack && !critDefense)
                {
                    if (attack > 0 && !critAttack)
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine, new Glyph(attack - 1, 68, Color.Black, Color.White));

                    if (defense > 0 && !critDefense)
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 1, new Glyph(defense - 1, 68, Color.Black, Color.White));
                }
                else
                {
                    if (critAttack)
                    {
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine, new Glyph(8, 68, Color.Black, Color.Gold));
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 1, new Glyph(9, 68, Color.Black, Color.Gold));
                    }
                    else if (critDefense)
                    {
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 1, new Glyph(8, 68, Color.Black, Color.Gold));
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine, new Glyph(9, 68, Color.Black, Color.Gold));
                    }
                }

                defender.ApplyOnAttackBlocked(attacker, (attack, weapon), (defense, armor));
                if (defense > attack)
                {
                    hitDice.Add(0);
                    _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 2, new Glyph(9, 68, Color.Black, Color.DarkSlateGray));
                    defender.ApplyOnSuccessfulBlock(attacker, attack, weapon);
                }
                else if (defense == attack && attack > 0)
                {
                    hitDice.Add(0);
                    if (Rnd.Instance.D10 > armor.Quality + defender.GetStats().Mod(EStat.Poise))
                    {
                        _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 2, new Glyph(6, 68, Color.Black, Color.Orange));
                        armor.Guard--;
                    }
                }
                else if (attack > defense)
                {
                    hitDice.Add(attack - defense);
                    wounds = Math.Max(wounds, attack - defense);
                    _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 2, new Glyph((attack - defense) - 1, 68, Color.Black, Color.White));
                }
            }
            else
            {
                var waitingTime = 0.01f;
                for (int i = 0; i < 5; i++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
                    
                    yield return new WaitForSeconds(waitingTime);
                    waitingTime += 0.01f;
                }
                
                wounds = Math.Max(wounds, attack);
                _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine, new Glyph(attack - 1, 68, Color.Black, Color.White));
                _game.Layers["mrmo"].Set(2 + _fullWidth + diceIdx + 1, startLine + 2, new Glyph(attack - 1, 68, Color.Black, Color.White));
                hitDice.Add(attack);
            }
            
            diceIdx += 1;
            yield return new WaitForSeconds(0.15f);
        }
        
        int count = 0;
        if (wounds > 0)
        {
            for (int i = 0; i < diceIdx; i++)
            {
                if (hitDice[i] == wounds)
                {
                    count++;
                    int dmg = 1;
                    defender.ApplyOnWoundCounted(hitDice[i], i, ref dmg);
                    _game.Layers["mrmo"].Set(2 + _fullWidth + i + 1, startLine + 3,
                        new Glyph(dmg - 1, 68, Color.Black, Color.Red));
                }
            }
            
            defender.GetAP().Add<StatusWounds>(count);
            attacker.ApplyOnCausedWounds(defender, count);
            
            if (defender is Enemy enemy)
            {
                var max = defender.GetAP().Total - defender.GetStats().Vigor;
                var wnd = defender.GetAP().Count<StatusWounds>();
                var rnd = Rnd.Instance.Next(0, max);
                var isDead = rnd < wnd;
                Console.WriteLine($"RND(0, {max}) = {rnd} < {wnd}? {isDead}");
                
                if (isDead)
                {
                    _game.Layers["mrmo"].SetRect(new Vector2(10, 10), new Vector2(30, 15), ' ');
                    _game.Layers["ascii"].Set(30, 12, "BARK HERE", Color.Red, Color.Black);
                    Console.WriteLine("BARK HERE!");
                    yield return new WaitForKey(Keys.Space);

                    DrawCombat();
                    DrawGui();
                    
                    defender.GetAP().Reduce<StatusWounds>(rnd);
                    defender.GetAP().Add<StatusStunned>(1);
                    attacker.GetAP().Add<StatusSin>(enemy.Sin);
                    enemy.Die();
                }
            }
        }

        if (defender is Enemy { IsDead: true } e)
        {
            var (x, y) = e.DeadIcon;
            _enemies.Remove(e);
            _game.Layers["mrmo"].Set(e.X + _offsetX, e.Y + _offsetY, new Glyph(x, y, Color.Black, Color.White));
        }
        
        yield return new WaitForKey(Keys.Space);
    }

    private Enemy? IsEnemyAt(int x, int y)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.X == x && enemy.Y == y) return enemy;
        }

        return null;
    }

    private Character? IsCharacterAt(int x, int y)
    {
        foreach (var (chr, cs) in _combatStates)
        {
            if (cs.X == x && cs.Y == y) return chr;
        }

        return null;
    }

    private IEnumerable Coroutine_EndTurn()
    {
        yield return new WaitForSeconds(0.5f);
        _presentation = EPresentationState.Done;
    }
    
    private void CheckPlayerInputs()
    {
        if (KB.HasBeenPressed(Keys.Tab))
        {
            _playerSelectedIndex = (_playerSelectedIndex + 1) % 4;
            _secondCounter = 0;
        }

        if (KB.HasBeenPressed(Keys.Space))
        {
            _coroutineHandler.Run(Coroutine_EndTurn());
        }
        
        // MOVE
        if (_playerSelectedIndex > -1)
        {
            var current = _party[_playerSelectedIndex];
            if (_combatStates[current].Move > 0 && _game.ActionPoints.Remaining > 0)
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

                        if (IsCharacterAt(x + dx, y + dy) is { } c)
                        {
                            var cs = _combatStates[c];
                            cs.X = x;
                            cs.Y = y;
                            var pos = _combatStates[current];
                            pos.X += dx;
                            pos.Y += dy;
                        }
                        else if (IsEnemyAt(x + dx, y + dy) is { } e)
                        {
                            _coroutineHandler.Run(Attack(current, e));
                            _combatStates[current].Move = 0;
                        } 
                        else if (_map?.IsWalkable(x + dx, y + dy) ?? false)
                        {
                            var pos = _combatStates[current];
                            pos.X += dx;
                            pos.Y += dy;
                            _combatStates[current].Move--;
                            _game.ActionPoints.Spend(1);
                            UpdateFov();
                        }
                    }
                }
            }
        }
    }
}