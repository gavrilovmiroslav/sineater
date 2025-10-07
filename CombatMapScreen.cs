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

public class CombatConfig
{
    public int Phase;
    public Trait? Reward;
    public int Sin;
    public ETerrainKind Terrain;
}

public enum EStatDisplay
{
    Stats = 0,
    Details = 1,
    Equipment = 2,
}

public struct RangedTargetting
{
    public IAbilitySource Source;
    public ICharacter Owner;
    public int X, Y;
}

public class CombatMapScreen : IScreen
{
    private readonly int _fullWidth = 24, _fullHeight = 22;
    private readonly int _offsetX = 0, _offsetY = 2;
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    private string _title;
    public IMap? Map;
    private bool _rendered = false;
    private bool _debugView = false;
    private int _time = 0;
    private FieldOfView _fieldOfView;
    private ReadOnlyCollection<Cell>?[] _perspectives;
    private Color[,] _coloredMap;
    public bool[,] Visited;
    ReadOnlyCollection<Cell>? _fov = null;
    HashSet<(int, int)>? _isInActivePartyFOV = new();
    public HashSet<(int, int)>? IsInActivePartyFOV => _isInActivePartyFOV;
    public Dictionary<PartyMember, CombatState> CombatStates = new();
    private List<PartyMember> _party = new();
    public List<PartyMember> Party => _party;
    private List<Enemy> _enemies = new();
    public List<Enemy> Enemies => _enemies;
    public Dictionary<(int, int), IItem> Floor = new();
    private EStatDisplay _showStats = EStatDisplay.Stats;
    private ECombatState _combatState = ECombatState.PlayerPhase;
    private EPresentationState _presentation = EPresentationState.Preparing;
    public int PlayerSelectedIndex = 0;
    private Glyph[,] _groundGlyphs;
    internal CoroutineHandler CoroutineHandler = new();
    private ActionPoints _enemyActionPoints;
    public RangedTargetting? RangedActionConfig = null;

    public Domains Domains;
    
    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = Rnd.Instance.Next(3 * _fullWidth / 4, _fullWidth - 4);
            this._height = Rnd.Instance.Next(3 * _fullHeight / 4, _fullHeight - 2);
        }

        Regenerate();
    }
    
    private void Regenerate() => Regenerate(_kind);
    private int _extraFill = 0;
    private readonly CombatConfig? _config;

    public CombatMapScreen(SineaterGame game, CombatConfig? config = null, int width = -1, int height = -1, string title = "???")
    {
        _config = config;
        _width = width;
        _height = height;
        _title = title;
        _coloredMap = new Color[_fullWidth, _fullHeight];
        Visited = new bool[_fullWidth, _fullHeight];
            
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
        CoroutineHandler.Clear();
        _presentation = EPresentationState.Preparing;
        _combatState = ECombatState.PlayerPhase;
        CombatStates.Clear();
        _kind = kind;
        var (a, b, c, d, e) = (0, 0, 0, _width, _height);
        switch (_kind)
        {
            case ETerrainKind.Tomb:
                (a, b, c, d, e) = (36, 2, 2, 22, 20);//36
                break;
            case ETerrainKind.Temple:
                (a, b, c, d, e) = (40, 1, 1, 22, 20); //45
                break;
            case ETerrainKind.Cave:
                (a, b, c) = (52, 4, 7); //47
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

        //******************************************************************
        // todo: move from here!
        _enemies.Clear();
        var count = Rnd.Instance.D2 + (_config.Reward != null ? 4 : 2);
        var chosen = Rnd.Instance.Next(0, count);
        for (var i = 0; i < count; i++)
        {
            var en = Bestiary.Goblin();
            _enemies.Add(en);
            if (i == chosen && _config.Reward != null)
            {
                en.Traits.Add(_config.Reward);
            }
        }

        for (var i = 0; i < 3; i++)
        {
            _enemies.Add(Bestiary.Bat());
        }
        
        _enemies.Add(Bestiary.Hobgoblin());
        //******************************************************************
        
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
        
        var inner = RogueSharp.Map.Create(mapCreationStrategy);
        Map = RogueSharp.Map.Create(new FilledMapCreationStrategy<Map>(_fullWidth, _fullHeight));
        Map.Copy(inner, 1 + Rnd.Instance.Next(0, _fullWidth - 2 - _width), Rnd.Instance.Next(0, 1 + (_fullHeight - 2 - _height)));

        _fieldOfView = new FieldOfView(Map);
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                var g = Glyph.Bw(0, 0);
                if (Map.IsWalkable(i, j))
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

        var vas = Map.GetAllCells().Where(t => t.IsWalkable).ToArray();
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
            CombatStates[character] = new CombatState(v.X, v.Y, Rnd.Instance.Next(character.Stats.Mod(EStat.Vigor), 5 + character.Stats.Vigor), character.Tint, 0);
            _party.Add(character);
            var freeTiles = new HashSet<Cell>(Map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
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
                CombatStates[character] = new CombatState(v.X, v.Y, Rnd.Instance.Next(character.Stats.Mod(EStat.Vigor), 5 + character.Stats.Vigor), character.Tint, 0);
                _party.Add(character);
                freeTiles.UnionWith(Map.GetAdjacentCells(v.X, v.Y).Where(t => t.IsWalkable));
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
    }

    public void UpdateFov(bool onlyOneChar = false)
    {
        _fov = null;
        foreach (var (chr, combatState) in CombatStates)
        {
            if (onlyOneChar && _game.Party.Characters[PlayerSelectedIndex] != chr) continue;
            if (_fov == null && chr.Stats.Clarity > 0)
            {
                _fov = _fieldOfView.ComputeFov(combatState.X, combatState.Y, chr.Stats.Clarity, true);
            }
            else if (chr.Stats.Clarity > 0)
            {
                _fov = _fieldOfView.AppendFov(combatState.X, combatState.Y, chr.Stats.Clarity, true);    
            }
        }

        _isInActivePartyFOV.Clear();
        if (_fov == null) return;
        foreach (var f in _fov) _isInActivePartyFOV.Add((f.X, f.Y));
        
        _perspectives = new ReadOnlyCollection<Cell>?[4];
        int i = 0;
        foreach (var (chr, combatState) in CombatStates)
        {
            _perspectives[i] = _fieldOfView.ComputeFov(combatState.X, combatState.Y, chr.Stats.Clarity, true);
            i++;
        }

        for (i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                _coloredMap[i, j] = new Color(0, 0, 0, 0);
            }
        }
        
        foreach (var (chr, _) in CombatStates)
        {
            foreach (var cell in _perspectives[chr.Index])
            {
                Visited[cell.X, cell.Y] = true;
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
                    if (c.R > 0) cs[ci++] = CombatStates[_game.Party.Characters[0]].Tint;
                    if (c.G > 0) cs[ci++] = CombatStates[_game.Party.Characters[1]].Tint;
                    if (c.B > 0) cs[ci++] = CombatStates[_game.Party.Characters[2]].Tint;
                    if (c.A > 0) cs[ci++] = CombatStates[_game.Party.Characters[3]].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, Color.Lerp(cs[0], cs[1], 0.5f), 0.5f);
                }
                else if (o == 1)
                {
                    var cs = Color.White;
                    if (c.R > 0) cs = CombatStates[_game.Party.Characters[0]].Tint;
                    if (c.G > 0) cs = CombatStates[_game.Party.Characters[1]].Tint;
                    if (c.B > 0) cs = CombatStates[_game.Party.Characters[2]].Tint;
                    if (c.A > 0) cs = CombatStates[_game.Party.Characters[3]].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, cs, 0.35f);
                }
            }
        }
    }

    public IEnumerable EnemyMoves()
    {
        _combatState = ECombatState.PlayerPhase;
        _presentation = EPresentationState.Preparing;

        foreach (var enemy in _enemies)
        {
            if (enemy.Stats.Clarity == 0)
            {
                yield return new BehaviorBlind().Do(enemy, this, enemy.X, enemy.Y);
                continue;
            }
            
            var beh = enemy.Behaviors[0];
            enemy.Behaviors.RemoveAt(0);
            if (!beh.ShouldFizzleOut())
            {
                enemy.Behaviors.Add(beh);
            }

            yield return beh.Do(enemy, this, enemy.X, enemy.Y);
        }
    }
    
    public void Update(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }
        
        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }

        if (RangedActionConfig == null)
        {
            CheckInputs();
        }
        else
        {
            CheckTargetingInputs();
            return;
        }

        if (_presentation == EPresentationState.Preparing)
        {
            switch (_combatState)
            {
                case ECombatState.EnemyPhase:
                    foreach (var e in _enemies)
                    {
                        e.IsDone = false;

                        for (int i = e.Traits.Count - 1; i >= 0; i--)
                            CoroutineHandler.Run(e.Traits[i].ApplyOnStartTurn(this, e));
                    }

                    CoroutineHandler.Run(EnemyMoves());
                    
                    break;
                case ECombatState.PlayerPhase:
                    PlayerSelectedIndex = 0;
                    foreach (var (chr, st) in CombatStates)
                    {
                        _game.ActionPoints.Free(st.Move);
                        st.Move = chr.Stats.Will + chr.Stats.Mod(EStat.Clarity);
                        
                        for (int i = chr.Traits.Count - 1; i >= 0; i--)
                            CoroutineHandler.Run(chr.Traits[i].ApplyOnStartTurn(this, chr));
                    }

                    _combatState = ECombatState.PlayerPhase;
                    _presentation = EPresentationState.Executing;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            UpdateFov(true);
        }
        else if (_presentation == EPresentationState.Executing)
        {
            if (_enemies.Count == 0)
            {
                CoroutineHandler.Run(new FadeOutAndLeaveScreen(1));
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
                    CoroutineHandler.Run(new Frenzy(_game, this));
                    
                    foreach (var enemy in _enemies)
                    {
                        for (int i = enemy.Traits.Count - 1; i >= 0; i--)
                            CoroutineHandler.Run(enemy.Traits[i].ApplyOnEndTurn(enemy));
                    }
                    
                    foreach (var chr in _game.Party.Characters)
                    {
                        for (int i = chr.Traits.Count - 1; i >= 0; i--)
                            CoroutineHandler.Run(chr.Traits[i].ApplyOnEndTurn(chr));
                    }

                    List<Domain> toClose = [];
                    foreach (var domain in Domains._domains)
                    {
                        domain.Update(this);
                        if (domain.ShouldClose) toClose.Add(domain);
                    }

                    foreach (var close in toClose)
                    {
                        Domains._domains.Remove(close);

                        foreach (var (tx, ty) in Domains.Tiles.Keys.ToList())
                        {
                            if (Domains.Tiles[(tx, ty)] == close)
                            {
                                Domains.Tiles.Remove((tx, ty));
                            }
                        }
                    }
                    
                    this.DrawCombat();
                    _combatState = ECombatState.EnemyPhase;
                    _presentation = EPresentationState.Preparing;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal void DrawCombat(bool onlyNow = false)
    {
        var index = 0;
        
        _game.Layers["mrmo"].SetRect(new Vector2(_offsetX, _offsetY), new Vector2(_fullWidth + _offsetX - 1, _fullHeight + _offsetY), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(_offsetX, _offsetY), new Vector2(_fullWidth * 2 + _offsetX - 2, _fullHeight * 2 + _offsetY), ' ');
        
        _game.ActionPoints.Draw(1, 25);

        foreach (var w in _game.Party.Characters)
        {
            if (w.Job == ECharacterClass.Witch)
            {
                _game.ActionPoints.DrawCursor(CombatStates[w].X * 2 + 1, 25);
            }
        }

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
                    else if (Visited[i, j])
                    {
                        if (onlyNow) continue;
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
        
        foreach (var domain in Domains._domains)
        {
            domain.Draw(this);
        }
        
        foreach (var ((x, y), item) in Floor)
        {
            if (!_isInActivePartyFOV.Contains((x, y))) continue;
            _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, item.GetIcon());
        }
        
        foreach (var enemy in _enemies)
        {
            if (!enemy.Render) continue;
            if (!_isInActivePartyFOV.Contains((enemy.X, enemy.Y))) continue;
            var (ix, iy) = enemy.Icon;
            var c = enemy.Tint;
            if (enemy.Traits.Count > 0) c = Color.Lerp(c, Color.Gold, 0.6f);
            _game.Layers["mrmo"].Set(enemy.X + _offsetX, enemy.Y + _offsetY, new Glyph(ix, iy, Color.Black, c));
        }
        
        foreach (var (chr, cs) in CombatStates)
        {
            if (!chr.Render) continue;
            var (ix, iy) = chr.Job.GetImage();
            _game.Layers["mrmo"].Set(cs.X + _offsetX, cs.Y + _offsetY, new Glyph(ix, iy, Color.Black, 
                CombatStates[chr].Move > 0 ? CombatStates[chr].Tint : Color.DarkGray));
            index++;
        }
    }

    public bool SkipGUI { get; set; } = false;

    private void DrawTargetting()
    {
        UpdateFov(true);
        DrawCombat(true);
        if (RangedActionConfig.HasValue)
        {
            var config = RangedActionConfig.Value;
            if ((_time / 200) % 2 == 0)
            {
                _game.Layers["mrmo"].Set(config.X, config.Y + _offsetY, "_", CombatStates[config.Owner as PartyMember].Tint);
            }
        }
    }

    public void DrawGui()
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
        
        _game.Layers["mrmo"].SetRect(new Vector2(2 + _fullWidth - 4, 0), new Vector2(2 + _fullWidth + 40, 6), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(2 * _fullWidth * 2 + 2, 0), new Vector2(2 * _fullWidth * 2 + 40, 6), ' ');

        if (_showStats == EStatDisplay.Stats)
        {
            DrawStats();
        }
        else if (_showStats == EStatDisplay.Details)
        {
            DrawDetails();
        }
        else
        {
            DrawLoadout();
        }
    }

    private void DrawLoadout()
    {
        _game.Layers["ascii"].Set(2 * _fullWidth - 1, 0, "CHAR       SKILLS");
        var index = 0;
        foreach (var character in _party)
        {
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, 1 + index,
                new Glyph(ix, iy, Color.Black, CombatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 1 + index, character.Job.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            var traits = string.Join(" ", character.Traits.Select(t => t.ShortName));
            if (traits.Length == 0) traits = "--";
            _game.Layers["ascii"].Set(2 * _fullWidth + 10, 1 + index, traits,
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            if (PlayerSelectedIndex == index && !CoroutineHandler.IsActive())
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 1 + index, ">");
                if (_time < 400 || _time is > 800 and < 1200)
                {
                    _game.Layers["mrmo"].Set(CombatStates[character].X, CombatStates[character].Y + 1, 
                        new Glyph(12, 25, Color.Black, CombatStates[character].Tint));
                }
            }
            index++;
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
                new Glyph(ix, iy, Color.Black, CombatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 1 + index, character.Job.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 1 + index, character.Stats.Clarity.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 1 + index, CombatStates[character].Move.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 14, 1 + index, character.LeftWeapon?.Attack.ToString() ?? "-",
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            if (character.LeftWeapon is Shield leftShield)
            {
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 14, 1 + index, leftShield.Defense.ToString() ?? "-",
                    Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 1 + index, "G", Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            }
            else
            {
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 1 + index,
                    character.LeftWeapon?.Weight.Short() ?? "-",
                    Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            }

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 17, 1 + index, character.RightWeapon?.Attack.ToString() ?? "-",
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            if (character.RightWeapon is Shield rightShield)
            {
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 17, 1 + index, rightShield.Defense.ToString() ?? "-",
                    Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 1 + index, "G", Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            }
            else
            {
                _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 18, 1 + index,
                    character.RightWeapon?.Weight.Short() ?? "-",
                    Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            }

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 20, 1 + index, character.Armor?.Guard.ToString() ?? "-",
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 21, 1 + index, character.Armor?.Weight.Short() ?? "-",
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));

            if (PlayerSelectedIndex == index && !CoroutineHandler.IsActive())
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 1 + index, ">");
                if (_time < 400 || _time is > 800 and < 1200)
                {
                    _game.Layers["mrmo"].Set(CombatStates[character].X, CombatStates[character].Y + 1, 
                        new Glyph(12, 25, Color.Black, CombatStates[character].Tint));
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
                new Glyph(ix, iy, Color.Black, CombatStates[character].Tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, 1 + index, character.Job.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, 1 + index, character.Stats.Will.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, 1 + index, character.Stats.Clarity.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, 1 + index, character.Stats.Poise.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));
            
            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 19, 1 + index, character.Stats.Vigor.ToString(),
                Color.Lerp(Color.White, CombatStates[character].Tint, 0.5f));

            if (PlayerSelectedIndex == index && !CoroutineHandler.IsActive())
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth - 4, 1 + index, ">");
                if (_time < 400 || _time is > 800 and < 1200)
                {
                    _game.Layers["mrmo"].Set(CombatStates[character].X, CombatStates[character].Y + 1, 
                        new Glyph(12, 25, Color.Black, CombatStates[character].Tint));
                }
            }
            index++;
        }
    }
    
    public void Draw(GameTime gameTime)
    {
        if (Map == null) return;

        if (CoroutineHandler.IsActive())
        {
            if (_enemies.Count > 0)
            {
                _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(40, 2), ' ');
                _game.Layers["ascii"].Set(1, 1, _title);
                _enemyActionPoints.Draw(_title.Length + 3, 1);
            }
            DrawGui();
            return;
        }
        
        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();
        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth + 40, 40), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth * 2 + 40, 40), ' ');
        
        DrawCombat();
        DrawGui();
        
        if (RangedActionConfig != null)
        {
            DrawTargetting();
        }
        
        if (_enemies.Count > 0)
        {
            _enemyActionPoints.Draw(_title.Length + 3, 1);
        }
    }

    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }

        if (KB.HasBeenPressed(Keys.I))
        {
            _game.ScreenStack.Push(new InventoryScreen(_game));
        }
        else if (KB.HasBeenPressed(Keys.O))
        {
            _game.ScreenStack.Push(new InventoryScreen(_game, true));
        }
        
        if (KB.HasBeenPressed(Keys.Tab))
        {
            _showStats = (EStatDisplay) (((int)_showStats + 1) % 3);
        }
    }

    private void CheckTargetingInputs()
    {
        if (RangedActionConfig.HasValue)
        {
            if (KB.HasBeenPressed(Keys.Escape))
            {
                RangedActionConfig = null;
                return;
            }
            
            var config = RangedActionConfig.Value;
            if (KB.HasBeenPressed(Keys.Enter))
            {
                var cs = CombatStates[config.Owner as PartyMember];
                CoroutineHandler.Run(new FlyingObject(cs.X, cs.Y, config));
                var foundInInventory = false;
                for (int i = 0; i < _game.Inventory.Items.Length; i++)
                {
                    if (_game.Inventory.Items[i] == config.Source)
                    {
                        _game.Inventory.Items[i] = null;
                        foundInInventory = true;
                        break;
                    }
                }

                if (!foundInInventory)
                {
                    var chr = config.Owner as PartyMember;
                    if (chr.GetLeftWeapon() == config.Source)
                    {
                        chr.LeftWeapon = null;
                    }
                    else if (chr.GetRightWeapon() == config.Source)
                    {
                        chr.RightWeapon = null;
                    }
                    else if (chr.GetArmor() == config.Source)
                    {
                        chr.Armor = null;
                    }
                }

                cs.Move = 0;
                return;
            }
            
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
                    config.X += dx;
                    config.Y += dy;
                    if (!Map?.IsWalkable(config.X, config.Y) ?? false) return;
                    if (!_isInActivePartyFOV?.Contains((config.X, config.Y)) ?? false) return;
                    RangedActionConfig = config;
                }
            }
        }
    }

    private IEnumerable CombatAlgebra(CombatFlow flow, ICombatFlowStep step)
    {
        var dw = 1;
        var dh = 0;
        
        if (step is CombatFlow_Notify notif)
        {
            Console.WriteLine(notif.Message);
            yield return new WaitForSeconds(0.1f);
        }
        else if (step is CombatFlow_PresentAttacker att)
        {
            var (ua, va) = att.Attacker.GetPortait();
            _game.Layers["porsmol"].Set(10, 3, new Glyph(ua, va, Color.Black, att.Attacker.GetTint()));
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 9, "ATK");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 10, "DEF");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 11, "HIT");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 12, "DMG");
            
            if (att.Attacker is PartyMember chr)
            {
                var cs = CombatStates[chr];
                var (u, v) = chr.Job.GetImage();
                _game.Layers["mrmo"].Set(cs.X, cs.Y + 2, new Glyph(u, v, Color.Black, Color.White));
            }
            else if (att.Attacker is Enemy enm)
            {
                var (u, v) = enm.Icon;
                _game.Layers["mrmo"].Set(enm.X, enm.Y + 2, new Glyph(u, v, Color.Black, Color.White));
            }
        }
        else if (step is CombatFlow_PresentDefender def)
        {
            var (ud, vd) = def.Defender.GetPortait();
            _game.Layers["mrmo"].Set(2 + _fullWidth + dw + 1, 9 + dh - 2, "vs");
            _game.Layers["porsmol"].Set(12, 3, new Glyph(ud, vd, Color.Black, def.Defender.GetTint()));
            if (def.Defender is PartyMember chr)
            {
                var cs = CombatStates[chr];
                var (u, v) = chr.Job.GetImage();
                _game.Layers["mrmo"].Set(cs.X, cs.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
            }
            else if (def.Defender is Enemy enm)
            {
                var (u, v) = enm.Icon;
                _game.Layers["mrmo"].Set(enm.X, enm.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
            }
        }
        else if (step is CombatFlow_PresentRollingAttackDie ra)
        {
            for (int i = 0; i < 5; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + ra.Index + 1, 9 + dh,
                    new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
            
                yield return new WaitForSeconds(0.01f);
            }
        }
        else if (step is CombatFlow_PresentRollingDefenseDie rd)
        {
            for (int i = 0; i < 5; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + rd.Index + 1, 10 + dh,
                    new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));
            
                yield return new WaitForSeconds(0.01f);
            }
        }
        else if (step is CombatFlow_PresentAttackDie a)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dw + a.Index + 1, 9 + dh,
                new Glyph(a.Value - 1, 68, Color.Black, Color.Gray));
        }
        else if (step is CombatFlow_PresentDefenseDie d)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dw + d.Index + 1, 10 + dh,
                new Glyph(d.Value - 1, 68, Color.Black, Color.Gray));
        }
        else if (step is CombatFlow_PresentStrike strike)
        {
            for (int i = 0; i <= 10; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + strike.Index + 1, 9 + dh,
                    new Glyph(strike.Attack.Value - 1, 68, Color.Black, Color.Lerp(Color.Gold, Color.Gray, (float)i / 10.0f)));

                if (strike.Defense != null)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + dw + strike.Index + 1, 10 + dh,
                        new Glyph(strike.Defense.Value - 1, 68, Color.Black,
                            Color.Lerp(Color.Gold, Color.Gray, (float)i / 10.0f)));
                }

                yield return new WaitForSeconds(0.01f);
            }
        }
        else if (step is CombatFlow_PresentHitDie hit)
        {
            if (hit.Value == 0)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 11 + dh,
                    new Glyph(9, 68, Color.Black, Color.DarkGray));
            }
            else if (hit.Value == -1)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 11 + dh,
                    new Glyph(6, 68, Color.Black, Color.CadetBlue));
            }
            else
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 11 + dh,
                    new Glyph(hit.Value - 1, 68, Color.Black, Color.Gray));
            }
        }
        else if (step is CombatFlow_PresentDamagingHitDie damaging)
        {
            var die = flow.HitDice[damaging.Index];
            if (die != null)
            {
                for (int i = 0; i <= 10; i++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + dw + damaging.Index + 1, 11 + dh,
                        new Glyph(die.Value - 1, 68, Color.Black, Color.Lerp(Color.Red, Color.Gray, (float)i / 10.0f)));
                }
            }
            yield return new WaitForSeconds(0.01f);
        }
        else if (step is CombatFlow_PresentDamageDie dmg)
        {
            for (int i = 0; i <= 10; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + dmg.Index + 1, 12 + dh,
                    new Glyph(dmg.Value - 1, 68, Color.Black, Color.Lerp(Color.Red, Color.DarkRed, (float)i / 10.0f)));
            }
            yield return new WaitForSeconds(0.1f);
        }
        else if (step is CombatFlow_TotalIncomingDamage inc)
        {
            var ap = flow.Defender.GetAP();
            var stats = flow.Defender.Stats;
            if (inc.TotalDamage > 0)
            {
                var wnd = ap.Count<StatusWounds>();
                var min = Math.Max(inc.TotalDamage, wnd);
                var effect = Rnd.Instance.Next(min, inc.TotalDamage + wnd);
                if (effect < 3)
                {
                    ap.Add<StatusWounds>(inc.TotalDamage);
                }
                else if (effect < 4)
                {
                    ap.Add<StatusWounds>((int)Math.Ceiling(inc.TotalDamage * 1.5f));
                }
                else if (effect < 7)
                {
                    ap.Add<StatusWounds>(inc.TotalDamage);
                    flow.Defender.Stats.Poise = Math.Max(0, flow.Defender.Stats.Poise - 1);
                    Console.WriteLine($"{flow.Defender.GetName()} loses poise ({flow.Defender.Stats.Poise})!");
                    if (flow.Defender.Stats.Poise == 0)
                    {
                        flow.Defender.Stats.Poise = flow.Defender.HP;
                        Console.WriteLine($"{flow.Defender.GetName()} recovers a bit to poise {flow.Defender.Stats.Poise}");
                        MaybeDie(flow, ap);
                    }
                }
                else
                {
                    ap.Add<StatusWounds>(inc.TotalDamage);
                    MaybeDie(flow, ap);
                }
                
                // if (applied < inc.TotalDamage)
                // {
                //     var diff = inc.TotalDamage - applied;
                //     ap.Reduce(diff);
                //     ap.Add<StatusWounds>(diff);
                // }
                // if (totalWounds - woundsBefore > stats.Vigor && flow.Defender is Enemy enm)
                // {
                //     ap.Reduce<StatusWounds>(stats.Vigor);
                //     flow.Attacker.GetAP().Add<StatusSin>(enm.Sin);
                //     if (flow.Attacker is PartyMember _)
                //         enm.Die();
                // }
                // else if (totalWounds > 0 && flow.Defender is Enemy enemy)
                // {
                //     var min = 0;
                //     if (ap.Remaining <= 0) min = 3;
                //     if (Rnd.Instance.Next(min, totalWounds) >= enemy.HP)
                //     {
                //         ap.Reduce<StatusWounds>(enemy.HP);
                //         flow.Attacker.GetAP().Add<StatusSin>(enemy.Sin);
                //         if (flow.Attacker is PartyMember _)
                //             enemy.Die();
                //     }
                // }
            }
        } 
        else if (step is CombatFlow_PresentArmorDestroyed pad)
        {
            flow.Defender.RemoveArmor();
        }
        else if (step is CombatFlow_ShatteredLeftWeapon lw)
        {
            flow.Attacker.EquipLeftWeapon(null);
        }
        else if (step is CombatFlow_ShatteredRightWeapon rw)
        {
            flow.Attacker.EquipRightWeapon(null);
        }
    }

    private void MaybeDie(CombatFlow flow, ActionPoints ap)
    {
        flow.Defender.HP--;
        Console.WriteLine($"{flow.Defender.GetName()} loses health ({flow.Defender.HP})!");
        if (flow.Defender.HP <= 0)
        {
            if (flow.Defender is Enemy enm)
            {
                Console.WriteLine($"{flow.Defender.GetName()} dies!");
                enm.Die();
                ap.Reduce(enm.Sin);
            }
            else if (flow.Defender is PartyMember _)
            {
                var rnd = Rnd.Instance.D6;
                var hpGain = Rnd.Instance.D4;
                flow.Defender.HP += hpGain;

                switch (rnd)
                {
                    case <= 2:
                        ap.Add<StatusDeath>(hpGain);
                        break;
                    case <= 3:
                        flow.Defender.AddTrait(new TraitCrippledLeftHand());
                        break;
                    case <= 4:
                        flow.Defender.AddTrait(new TraitCrippledRightHand());
                        break;
                    default:
                        flow.Defender.AddTrait(new TraitParalyzed());
                        break;
                }
            }
        }
    }

    private IEnumerable ResolveAttack(CombatFlow flow, IEnumerable log)
    {
        foreach (var part in log)
        {
            if (part is IEnumerable enm)
            {
                yield return ResolveAttack(flow, enm);
            }
            else if (part is ICombatFlowStep step) 
            {
                yield return CombatAlgebra(flow, step);
            }
            else
            {
                yield return part;
            }
        }
    }

    public IEnumerable Attack(ICharacter attacker, ICharacter defender)
    {
        var flow = new CombatFlow(attacker, defender);
        yield return ResolveAttack(flow, flow.Attack());
        
        if (defender is Enemy { IsDead: true } e)
        {
            _game.Layers["porsmol"].Clear();
            var (u, v) = e.DeadIcon;
            _enemies.Remove(e);
            _game.Layers["mrmo"].Set(e.X + _offsetX, e.Y + _offsetY, new Glyph(u, v, Color.Black, Color.Red));
            yield return new ShowPopupWindowAndWaitForKey((_, bnd) =>
            {
                bnd.Newline();
                bnd.Add($"The {attacker.GetName()} kills the {defender.GetName()}:");
                bnd.Newline();
                bnd.Newline();
                if (attacker is PartyMember chr)
                {
                    bnd.Add($"  {chr.GetRandomBark()}");
                }

                bnd.Newline();
                bnd.Newline();
            }, true);
            if (attacker is PartyMember chr)
            {
                var transferable = e.Traits.Where(t => !(t is LimitedTrait)).ToList();
                if (transferable.Count > 0)
                {
                    var t = transferable[Rnd.Instance.Next(0, transferable.Count)];
                    yield return new ShowPopupWindowAndWaitForKey(
                        (_, bnd) => { bnd.Add($"The {attacker.GetName()} acquires {t.Name.ToUpper()}!"); }, true);
                    yield return attacker.AddTrait(t);
                }
            }

            if (Domains.Tiles.ContainsKey((e.X, e.Y)))
            {
                yield return Domains.Tiles[(e.X, e.Y)].ApplyOnDeath(this, e.X, e.Y);
            }
            Draw(new GameTime());
        }
    }

    public Enemy? IsEnemyAt(int x, int y)
    {
        foreach (var enemy in _enemies)
        {
            if (enemy.X == x && enemy.Y == y) return enemy;
        }

        return null;
    }

    public PartyMember? IsCharacterAt(int x, int y)
    {
        foreach (var (chr, cs) in CombatStates)
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
        if (KB.HasBeenPressed(Keys.Space))
        {
            var p = PlayerSelectedIndex;
            while (true)
            {
                PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
                _game.Party.Selected = PlayerSelectedIndex;
                UpdateFov(true);
                if (CombatStates[_game.Party.Characters[PlayerSelectedIndex]].Move > 0)
                {
                    _time = 0;
                    break;
                }
                if (p == PlayerSelectedIndex) break;
            }
        }

        if (KB.HasBeenPressed(Keys.A))
        {
            var chr = _game.Party.Characters[PlayerSelectedIndex];
            var ability = chr.Ability;
            if (ability != null && ability.CanBeUsed(chr) && chr.AP.Remaining > 0)
            {
                CoroutineHandler.Run(ability.Use(this, chr, CombatStates[chr].X, CombatStates[chr].Y));
            }
        }
        
        if (KB.HasBeenPressed(Keys.Enter))
        {
            CoroutineHandler.Run(Coroutine_EndTurn());
        }

        if (KB.HasBeenPressed(Keys.OemComma))
        {
            var current = _party[PlayerSelectedIndex];
            var x = CombatStates[current].X;
            var y = CombatStates[current].Y;
            if (Floor.ContainsKey((x, y)))
            {
                CoroutineHandler.Run(Floor[(x, y)].ApplyItemPickedUp(this, x, y, current));
            }
        }
        
        // MOVE
        if (PlayerSelectedIndex > -1)
        {
            var current = _party[PlayerSelectedIndex];
            if (CombatStates[current].Move > 0 && _game.ActionPoints.Remaining > 0)
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
                        var x = CombatStates[current].X;
                        var y = CombatStates[current].Y;

                        if (IsCharacterAt(x + dx, y + dy) is { } c)
                        {
                            var cs = CombatStates[c];
                            cs.X = x;
                            cs.Y = y;
                            var pos = CombatStates[current];
                            pos.X += dx;
                            pos.Y += dy;
                        }
                        else if (IsEnemyAt(x + dx, y + dy) is { } e)
                        {
                            CoroutineHandler.Run(Attack(current, e));
                            CombatStates[current].Move = 0;
                        } 
                        else if (Map?.IsWalkable(x + dx, y + dy) ?? false)
                        {
                            var oldX = CombatStates[current].X;
                            var oldY = CombatStates[current].Y;
                            var pos = CombatStates[current];
                            pos.X += dx;
                            pos.Y += dy;
                            CombatStates[current].Move--;
                            _game.ActionPoints.Spend(1);
                            UpdateFov(true);
                            if (Domains.Tiles.ContainsKey(((int)pos.X, (int)pos.Y)))
                            {
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[((int)pos.X, (int)pos.Y)]
                                    .ApplyOnDomainStepped(this, current, pos.X, pos.Y, oldX, oldY));
                            }
                        }
                    }
                }
            }
        }
    }
}
