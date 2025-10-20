using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Channels;
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
    public static CombatMapScreen? Level = null;
    
    internal static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
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
    public ReadOnlyCollection<Cell>?[] Perspectives;
    private Color[,] _coloredMap;
    public bool[,] Visited;
    ReadOnlyCollection<Cell>?[] _fovs = new ReadOnlyCollection<Cell>?[4];
    public readonly HashSet<(int, int)> IsInActivePartyMemberFOV = [];
    public readonly HashSet<(int, int)> IsInActivePartyFOV = [];
    private List<PartyMember> _party = [];
    public List<PartyMember> Party => _party;
    private List<Enemy> _enemies = [];
    private List<Enemy> _enemiesSortedByDistance = [];
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
        Level = this;
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
        var count = 5 + Rnd.Instance.D6 + (_config.Reward != null ? 4 : 2);
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

        for (var i = 0; i < 13; i++)
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
            character.X = v.X;
            character.Y = v.Y;
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
                character.X = v.X;
                character.Y = v.Y;
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
        int n = 0;
        foreach (var chr in _game.Party.Characters)
        {
            _fovs[n] = null;
            if (_fovs[n] == null && chr.Stats.Clarity > 0)
            {
                _fovs[n] = _fieldOfView.ComputeFov(chr.X, chr.Y, chr.Stats.Clarity, true);
            }
            else if (chr.Stats.Clarity > 0)
            {
                _fovs[n] = _fieldOfView.AppendFov(chr.X, chr.Y, chr.Stats.Clarity, true);
            }

            n++;
        }

        IsInActivePartyFOV.Clear();
        IsInActivePartyMemberFOV.Clear();

        if (SineaterGame.Instance.Party.Selected > -1)
        {
            if (_fovs[SineaterGame.Instance.Party.Selected] != null)
            {
                foreach (var f in _fovs[SineaterGame.Instance.Party.Selected]!)
                    IsInActivePartyMemberFOV.Add((f.X, f.Y));
            }
        }

        for (int fi = 0; fi < 4; fi++)
        {
            if (_fovs[fi] != null)
            {
                foreach (var f in _fovs[fi]!)
                    IsInActivePartyFOV.Add((f.X, f.Y));
            }   
        }
        
        Perspectives = new ReadOnlyCollection<Cell>?[4];
        int i = 0;
        foreach (var chr in _game.Party.Characters)
        {
            Perspectives[i] = _fieldOfView.ComputeFov(chr.X, chr.Y, chr.Stats.Clarity, true);
            i++;
        }

        for (i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                _coloredMap[i, j] = new Color(0, 0, 0, 0);
            }
        }
        
        foreach (var chr in _game.Party.Characters)
        {
            foreach (var cell in Perspectives[chr.Index])
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
                    if (c.R > 0) cs[ci++] = _game.Party.Characters[0].Tint;
                    if (c.G > 0) cs[ci++] = _game.Party.Characters[1].Tint;
                    if (c.B > 0) cs[ci++] = _game.Party.Characters[2].Tint;
                    if (c.A > 0) cs[ci++] = _game.Party.Characters[3].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, Color.Lerp(cs[0], cs[1], 0.5f), 0.5f);
                }
                else if (o == 1)
                {
                    var cs = Color.White;
                    if (c.R > 0) cs = _game.Party.Characters[0].Tint;
                    if (c.G > 0) cs = _game.Party.Characters[1].Tint;
                    if (c.B > 0) cs = _game.Party.Characters[2].Tint;
                    if (c.A > 0) cs = _game.Party.Characters[3].Tint;
                    _coloredMap[i, j] = Color.Lerp(Color.White, cs, 0.35f);
                }
            }
        }

        _enemiesSortedByDistance.Clear();
        Dictionary<Enemy, float> distances = [];
        foreach (var e in _enemies)
        {
            if (IsInActivePartyFOV.Contains((e.X, e.Y)))
            {
                var d = _game.Party.Characters
                    .Select(p => Vector2.Distance(new Vector2(e.X, e.Y), new Vector2(p.X, p.Y))).Min();

                var df = IsInActivePartyMemberFOV.Contains((e.X, e.Y)) ? 0.5f : 1;
                distances[e] = d * df;
                _enemiesSortedByDistance.Add(e);
            }
        }
        _enemiesSortedByDistance.Sort((a, b) => distances[a].CompareTo(distances[b]));
    }

    public IEnumerable EnemyMoves()
    {
        _combatState = ECombatState.PlayerPhase;
        _presentation = EPresentationState.Preparing;

        foreach (var enemy in _enemies.Where(e => IsInActivePartyFOV.Contains((e.X, e.Y))))
        {
            _currentEnemy = enemy;
            //DrawCharacterCard(_currentEnemy, 1, 1, false);
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
            
            _game.Layers["portrait"].Clear();
            _game.Layers["porsmol"].Clear();
            _game.Layers["mrmo"].SetRect(new Vector2(_fullWidth, 0), new Vector2(_fullWidth + 10, 40), ' ');
            _game.Layers["ascii"].SetRect(new Vector2(2 * _fullWidth - 2, 0), new Vector2(_fullWidth * 2 + 20, 40), ' ');
            _confirmedCombatFlow = null;
        }
        
        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();
        _game.Layers["mrmo"].SetRect(new Vector2(_fullWidth, 0), new Vector2(_fullWidth + 10, 40), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(2 * _fullWidth - 2, 0), new Vector2(_fullWidth * 2 + 20, 40), ' ');
        _confirmedCombatFlow = null;
        _currentEnemy = null;
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
                    foreach (var chr in _game.Party.Characters)
                    {
                        _game.ActionPoints.Free(chr.Stats.Will);
                        
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
                SinMod.System.GetLabelledInstance("bgm")?.SetParam("BGMusicMood", 0);
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
                    CoroutineHandler.Run(new DeathsDoor(_game, this));
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
                _game.ActionPoints.DrawCursor(w.X * 2 + 1, 25);
            }
        }

        index = 0;
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                if (IsInActivePartyMemberFOV.Contains((i, j)))
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
        
        foreach (var domain in Domains._domains)
        {
            domain.Draw(this);
        }
        
        foreach (var ((x, y), item) in Floor)
        {
            if (!IsInActivePartyMemberFOV.Contains((x, y))) continue;
            _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, item.GetIcon());
        }
        
        foreach (var enemy in _enemies)
        {
            if (!enemy.Render) continue;
            if (!IsInActivePartyFOV.Contains((enemy.X, enemy.Y))) continue;
            var (ix, iy) = enemy.Icon;
            var c = enemy.Tint;
            if (!IsInActivePartyMemberFOV.Contains((enemy.X, enemy.Y))) c = c.Darken(0.75f);
            if (enemy.Traits.Count > 0) c = Color.Lerp(c, Color.Gold, 0.6f);
            _game.Layers["mrmo"].Set(enemy.X + _offsetX, enemy.Y + _offsetY, new Glyph(ix, iy, Color.Black, c));
        }
        
        foreach (var chr in _game.Party.Characters)
        {
            if (!chr.Render) continue;
            var (ix, iy) = chr.Job.GetImage();
            _game.Layers["mrmo"].Set(chr.X + _offsetX, chr.Y + _offsetY, new Glyph(ix, iy, Color.Black, 
                _game.ActionPoints.Remaining > 0 ? chr.Tint : Color.DarkGray));
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
                _game.Layers["mrmo"].Set(config.X, config.Y + _offsetY, "_", config.Owner.GetTint());
            }
        }
    }
    
    private void DrawCharacterCard(ICharacter? chr, int h = 12, int dp = 0, bool header = true)
    {
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
        if (chr is PartyMember)
        {
            if (header)
            {
                _game.Layers["ascii"].Set(2 * _fullWidth - 1, h, "NAME       WIL CLA POI VIG");
            }
            else
            {
                h--;
            }

            _game.Layers["mrmo"].Set(2 + _fullWidth - 3, h + 1,
                new Glyph(ix, iy, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 2, h + 1, chr.GetName(),
                Color.Lerp(Color.White, tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 7, h + 1, chr.Stats.Will.ToString(),
                Color.Lerp(Color.White, tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 11, h + 1, chr.Stats.Clarity.ToString(),
                Color.Lerp(Color.White, tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 15, h + 1, chr.Stats.Poise.ToString(),
                Color.Lerp(Color.White, tint, 0.5f));

            _game.Layers["ascii"].Set(2 * _fullWidth + 4 + 19, h + 1, chr.Stats.Vigor.ToString(),
                Color.Lerp(Color.White, tint, 0.5f));
            
            var ph = (h + 1) / 2 + dp;
            var (u, v) = chr.GetPortait(); 
            _game.Layers["porsmol"].Set(10, ph, new Glyph(u, v, Color.Black, tint));

            (u, v) = ItemLibrary.EmptyUv;
            _game.Layers["porsmol"].Set(11, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 7, $"[1]: --");
            if (chr.GetLeftWeapon() is { } lw)
            {
                (u, v) = lw.Picture;
                var att = lw.Attack;
                if (lw is Shield rws) att = rws.Defense;
                _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 7, $"[1]: {lw.GetName()} ({att}D6)");
            }
            _game.Layers["porsmol"].Set(11, ph, new Glyph(u, v, Color.Black, tint));
        
            (u, v) = ItemLibrary.EmptyUv;
            _game.Layers["porsmol"].Set(12, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 8, $"[2]: --");
            if (chr.GetRightWeapon() is { } rw)
            {
                (u, v) = rw.Picture;
                var att = rw.Attack;
                if (rw is Shield rws) att = rws.Defense;
                _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 8, $"[2]: {rw.GetName()} ({att}D6)");
            }
            _game.Layers["porsmol"].Set(12, ph, new Glyph(u, v, Color.Black, tint));
        
            (u, v) = ItemLibrary.EmptyUv;
            _game.Layers["porsmol"].Set(13, ph, new Glyph(u, v, Color.Black, tint));
            _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 5, $"GUARD: --");
            if (chr.GetArmor() is { } ar)
            {
                (u, v) = ar.Picture;
                _game.Layers["ascii"].Set(2 * _fullWidth + 3, h + 5, $"{ar.GetName()} ({ar.Guard})");
            }
            _game.Layers["porsmol"].Set(13, ph, new Glyph(u, v, Color.Black, tint));
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
        if (Map == null) return;

        if (CoroutineHandler.IsActive())
        {
            // if (_enemies.Count > 0)
            // {
            //     _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(40, 2), ' ');
            //     _game.Layers["ascii"].Set(1, 1, _title);
            // }

            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();
        _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth + 40, 40), ' ');
        _game.Layers["ascii"].SetRect(new Vector2(0, 0), new Vector2(2 + _fullWidth * 2 + 40, 40), ' ');

        DrawCombat();
        ICharacter selected = SineaterGame.Instance.Party.Characters[SineaterGame.Instance.Party.Selected];
        if (_combatState == ECombatState.EnemyPhase)
        {
            selected = _currentEnemy;
            if (selected == null) return;
        }
        
        DrawCharacterCard(selected, 1, 1);
        var h = 11;
        foreach (var e in _enemiesSortedByDistance)
        {
            if (h == 11)
            {
                DrawCharacterCard(e, h, 1, true);
                h += 2;
            }
            else
            {
                DrawCharacterCard(e, h, 1, false);
                h += 1;
            }
        }
        if (!CoroutineHandler.IsActive())
        {
            if (_time < 400 || _time is > 800 and < 1200)
            {
                if (selected is PartyMember ps)
                {
                    var (gx, gy) = ps.Job.GetImage();

                    _game.Layers["mrmo"].Set(ps.X, ps.Y + 2,
                        new Glyph(gx, gy - 4, Color.Black, ps.Tint));
                }
                else if (selected is Enemy e)
                {
                    var (gx, gy) = e.Icon;

                    _game.Layers["mrmo"].Set(e.X, e.Y + 2,
                        new Glyph(gx, gy - 4, Color.Black, e.Tint));
                }
                
                if (_confirmedCombatFlow != null)
                {
                    var step = 1;
                    foreach (var skirmish in _confirmedCombatFlow.Skirmishes)
                    {
                        if (skirmish.Defender != null)
                        {
                            var (x, y) = (skirmish.Defender.X, skirmish.Defender.Y);
                            _game.Layers["mrmo"].Set(x, y + 2, $"{step}", skirmish.Defender is Dummy ? Color.Gray : Color.Red);
                        }
                        else
                        {
                            var (x, y) = skirmish.Position;
                            _game.Layers["mrmo"].Set(x, y + 2, $"{step}", Color.White);
                        }
                        step++;
                    }
                }
            }
        }

        if (RangedActionConfig != null)
        {
            DrawTargetting();
        }
    }

    private void CheckInputs()
    {
        if (KB.HasBeenPressed(Keys.D))
        {
            _debugView = !_debugView;
            _rendered = false;
        }

        if (KB.HasBeenPressed(Keys.C))
        {
            _game.ScreenStack.Push(new CharacterSheetScreen(_game));
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
                var cs = config.Owner as PartyMember;
                CoroutineHandler.Run(new FlyingObject(cs.X, cs.Y, config));
                var foundInInventory = false;
                var inventory = _game.Party.Characters[PlayerSelectedIndex].Inventory;
                for (int i = 0; i < inventory.Items.Length; i++)
                {
                    if (inventory.Items[i] == config.Source)
                    {
                        inventory.Items[i] = null;
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
                    if (!IsInActivePartyMemberFOV?.Contains((config.X, config.Y)) ?? false) return;
                    RangedActionConfig = config;
                }
            }
        }
    }

    private IEnumerable CombatAlgebra(SkirmishFlow flow, IPresentation step)
    {
        if (step is Present_Notify notif)
        {
            if (notif.WaitKey)
            {
                yield return new ShowPopupAndWaitForKey(new Vector2(2, 15), new Vector2(18, 22), (_, bnd) =>
                {
                    bnd.Newline();
                    bnd.Add(notif.Message);
                    bnd.Newline();
                });
            }
            else
            {
                yield return new ShowPopupAndWaitForSeconds(0.25f, new Vector2(2, 15), new Vector2(18, 22), (_, bnd) =>
                {
                    bnd.Newline();
                    bnd.Add(notif.Message);
                    bnd.Newline();
                });
            }
        }
        else if (step is Present_AttackRolled atk)
        {
            DrawCharacterCard(flow.Defender, 11, 0);
            yield return new WaitForSeconds(0.25f);

            for (int i = 0; i < 10; i++)
            {
                for (int d = 0; d < flow.AttackDiceRolled.Count; d++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + d + 1, 21,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Lerp(Color.Gray, Color.White, i / 5.0f)));
                }

                yield return new WaitForSeconds(0.1f);
            }

            for (int i = 0; i < flow.AttackDiceRolled.Count; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + i + 1, 21,
                    new Glyph(flow.AttackDiceRolled[i].Value - 1, 68, Color.Black, Color.Green));
                for (int d = i + 1; d < flow.AttackDiceRolled.Count; d++)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + d + 1, 21,
                        new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.White));
                }
                yield return new WaitForSeconds(0.3f);
            }
        }
        else if (step is Present_Crit crit)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + crit.index + 1, 21,
                new Glyph(8, 68, Color.Black, Color.Gold));
            flow.Attacker.GetAP().Free(flow.Weapon.OpeningsPerCrit);
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_ArmorDent dent)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dent.index + 1, 21,
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
            _game.Layers["mrmo"].Set(2 + _fullWidth + brk.index + 1, 21,
                new Glyph(6, 68, Color.Black, Color.Red));
            if (flow.Defender.GetArmor().Guard < 0)
            {
                flow.Defender.RemoveArmor();
            }
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_GuardBreak grd)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + grd.index + 1, 21,
                new Glyph(10, 68, Color.Black, Color.Red));
            yield return new WaitForSeconds(0.2f);
        }
        else if (step is Present_DealDamage dmg)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dmg.index + 1, 21,
                new Glyph(dmg.damage - 1, 68, Color.Black, Color.Red));
            if (flow.Defender is PartyMember p)
            {
                p.GetAP().Add<StatusWounds>(dmg.damage);
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
        
        /*var dw = 1;
        var dh = 0;

        if (step is CombatFlow_Notify notif)
        {
            if (notif.WaitKey)
            {
                yield return new ShowPopupAndWaitForKey(new Vector2(2, 8), new Vector2(18, 15), (_, bnd) =>
                {
                    bnd.Newline();
                    bnd.Add(notif.Message);
                    bnd.Newline();
                });
            }
            else
            {
                yield return new ShowPopupAndWaitForSeconds(0.25f, new Vector2(2, 8), new Vector2(18, 15), (_, bnd) =>
                {
                    bnd.Newline();
                    bnd.Add(notif.Message);
                    bnd.Newline();
                });
            }
        }
        else if (step is CombatFlow_PresentAttacker att)
        {
            // _game.Layers["ascii"].SetRect(new Vector2(4 + 2 * _fullWidth, 21), new Vector2(4 + 2 * _fullWidth + 6, 25), ' ');
            // _game.Layers["ascii"].SetRect(new Vector2(4 + 2 * _fullWidth, 21), new Vector2(4 + 2 * _fullWidth + 6, 25), ' ');
            // _game.Layers["mrmo"].SetRect(new Vector2(2 + _fullWidth + dw + 0 + 1, 21),
            //     new Vector2(2 + _fullWidth + dw + 10 + 1, 25), ' ');

            var (ua, va) = att.Attacker.GetPortait();
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 21, "ATK");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 22, "DEF");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 23, "HIT");
            _game.Layers["ascii"].Set(4 + 2 * _fullWidth, 24, "DMG");
        }
        else if (step is CombatFlow_PresentDefender def)
        {
            // var (ud, vd) = def.Defender.GetPortait();
            // _game.Layers["mrmo"].Set(2 + _fullWidth + dw + 1, 9 + dh - 2, "vs");
            // _game.Layers["porsmol"].Set(12, 3, new Glyph(ud, vd, Color.Black, def.Defender.GetTint()));
            // if (def.Defender is PartyMember chr)
            // {
            //     var cs = CombatStates[chr];
            //     var (u, v) = chr.Job.GetImage();
            //     _game.Layers["mrmo"].Set(cs.X, cs.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
            // }
            // else if (def.Defender is Enemy enm)
            // {
            //     var (u, v) = enm.Icon;
            //     _game.Layers["mrmo"].Set(enm.X, enm.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
            // }
        }
        else if (step is CombatFlow_PresentRollingAttackDie ra)
        {
            for (int i = 0; i < 5; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + ra.Index + 1, 21 + dh,
                    new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));

                yield return new WaitForSeconds(0.01f);
            }
        }
        else if (step is CombatFlow_PresentRollingDefenseDie rd)
        {
            for (int i = 0; i < 5; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + rd.Index + 1, 22 + dh,
                    new Glyph(Rnd.Instance.D6 - 1, 68, Color.Black, Color.Gray));

                yield return new WaitForSeconds(0.01f);
            }
        }
        else if (step is CombatFlow_PresentAttackDie a)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dw + a.Index + 1, 21 + dh,
                new Glyph(a.Value - 1, 68, Color.Black, Color.Gray));
        }
        else if (step is CombatFlow_PresentDefenseDie d)
        {
            _game.Layers["mrmo"].Set(2 + _fullWidth + dw + d.Index + 1, 22 + dh,
                new Glyph(d.Value - 1, 68, Color.Black, Color.Gray));
        }
        else if (step is CombatFlow_DefenderArmorDented)
        {
            if (flow.Defender.GetArmor() is {} armor)
            {
                armor.Guard--;
                if (armor.Guard < 0) armor.Guard = 0;
            }
        }
        else if (step is CombatFlow_PresentStrike strike)
        {
            for (int i = 0; i <= 10; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + strike.Index + 1, 21 + dh,
                    new Glyph(strike.Attack.Value - 1, 68, Color.Black, Color.Lerp(Color.Gold, Color.Gray, (float)i / 10.0f)));

                if (strike.Defense != null)
                {
                    _game.Layers["mrmo"].Set(2 + _fullWidth + dw + strike.Index + 1, 22 + dh,
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
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 23 + dh,
                    new Glyph(9, 68, Color.Black, Color.DarkGray));
            }
            else if (hit.Value == -1)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 23 + dh,
                    new Glyph(6, 68, Color.Black, Color.CadetBlue));
            }
            else
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + hit.Index + 1, 23 + dh,
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
                    _game.Layers["mrmo"].Set(2 + _fullWidth + dw + damaging.Index + 1, 23 + dh,
                        new Glyph(die.Value - 1, 68, Color.Black, Color.Lerp(Color.Red, Color.Gray, (float)i / 10.0f)));
                }
            }
            yield return new WaitForSeconds(0.01f);
        }
        else if (step is CombatFlow_PresentDamageDie dmg)
        {
            for (int i = 0; i <= 10; i++)
            {
                _game.Layers["mrmo"].Set(2 + _fullWidth + dw + dmg.Index + 1, 24 + dh,
                    new Glyph(dmg.Value - 1, 68, Color.Black, Color.Lerp(Color.Red, Color.DarkRed, (float)i / 10.0f)));
            }
            yield return new WaitForSeconds(0.1f);
        }
        else if (step is CombatFlow_TotalIncomingDamage inc)
        {
            if (flow.Defender is Enemy e)
            {
                e.LastHit = flow.Attacker;
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
        else if (step is CombatFlow_DefenderApplyWounds aw)
        {
            flow.Defender.GetAP().Add<StatusWounds>(aw.Count);
        }
        else if (step is CombatFlow_DefenderStumble s)
        {
            flow.Defender.Stats.Poise = Math.Max(0, flow.Defender.Stats.Poise - 1);
            if (flow.Defender.Stats.Poise == 0)
            {
                flow.Defender.Stats.Poise = flow.Defender.HP;
            }
        }
        else if (step is CombatFlow_DefenderApplyStatus ass)
        {
            flow.Defender.AddTrait(ass.trait);
        }*/
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
        yield return flow.Weapon?.Traits.OnCombatStarts(flow);
        
        foreach (var skirmish in flow.Skirmishes)
        {
            yield return flow.Attacker.GetTraits().OnSkirmishStarts(skirmish);
            yield return flow.Weapon?.Traits.OnSkirmishStarts(skirmish);

            var (ox, oy) = (flow.Attacker.X, flow.Attacker.Y);
            var (x, y) = skirmish.Position;
            Positions.Swap((x, y), (ox, oy));
            UpdateFov(true);
            DrawCombat();
            if (skirmish.Defender != null && skirmish.Defender is not Dummy)
            {
                yield return skirmish.Defender.GetTraits().OnSkirmishStarts(skirmish);

                yield return ResolveAttack(skirmish, skirmish.Attack());
                if (skirmish.Defender is Enemy { IsDead: true } e)
                {
                    Party[0].AP.Add<StatusSin>(e.Sin);
                    e.Die();

                    DrawCombat();
                    SineaterGame.Instance.Layers["porsmol"].Clear();
                    var (i, j) = e.Icon;
                    var (u, v) = e.DeadIcon;
                    _enemies.Remove(e);

                    for (int k = 0; k < 5; k++)
                    {
                        SineaterGame.Instance.Layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(u, v, Color.Black, Color.Red));
                        yield return new WaitForSeconds(0.01f);
                        SineaterGame.Instance.Layers["mrmo"].Set(e.X, e.Y + 2, new Glyph(i, j, Color.Black, Color.Red));
                        yield return new WaitForSeconds(0.01f);
                    }

                    if (e.LastHit is PartyMember chr)
                    {
                        yield return new ShowPopupWindowWithPortraitAndWaitForKey(e.LastHit.GetPortait(), (_, bnd) =>
                        {
                            bnd.Newline();
                            bnd.Add($"{chr.GetName()} dispatches the {e.GetName()}.");
                            bnd.Newline();
                            bnd.Add($"  {chr.GetRandomBark()}");
                            bnd.Newline();
                        }, true);
                    }
                    else
                    {
                        yield return new ShowPopupWindowAndWaitForKey((_, bnd) =>
                        {
                            bnd.Newline();
                            bnd.Add($"{e.GetName()} dies.");
                            bnd.Newline();
                            bnd.Newline();
                        }, true);
                    }

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
                yield return new WaitForSeconds(0.5f);
            }

            yield return skirmish.Defender?.GetTraits().OnSkirmishEnds(skirmish);
            yield return flow.Attacker.GetTraits().OnSkirmishEnds(skirmish);
            yield return flow.Weapon?.Traits.OnSkirmishEnds(skirmish);
        }
        
        yield return flow.Attacker.GetTraits().OnCombatEnds(flow);
        yield return flow.Weapon?.Traits.OnCombatEnds(flow);

        _confirmedCombatFlow = null;
    }
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return new WaitForSeconds(0.5f);
        _presentation = EPresentationState.Done;
    }

    private ICharacter _currentEnemy = null;
    private CombatFlow? _confirmedCombatFlow = null;
    
    private void CheckPlayerInputs()
    {
        var current = _party[PlayerSelectedIndex];
        if (KB.HasBeenPressed(Keys.Space))
        {
            if (_confirmedCombatFlow == null)
            {
                var p = PlayerSelectedIndex;
                PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
                _game.Party.Selected = PlayerSelectedIndex;
                UpdateFov(true);
            }
            else
            {
                CoroutineHandler.Run(Attack(_confirmedCombatFlow));
            }
        }

        if (KB.HasBeenPressed(Keys.A))
        {
            var ability = current.Ability;
            if (ability != null)
            {
                if (ability.CanBeUsed(current, current.X, current.Y) && current.AP.Remaining > 0)
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
        
        if (KB.HasBeenPressed(Keys.OemComma))
        {
            
            var x = current.X;
            var y = current.Y;
            if (Floor.ContainsKey((x, y)))
            {
                CoroutineHandler.Run(Floor[(x, y)].ApplyItemPickedUp(this, x, y, current));
            }
        }
        
        if (_confirmedCombatFlow != null && KB.HasBeenPressed(Keys.Escape))
        {
            _confirmedCombatFlow = null;
        }
        else if (_confirmedCombatFlow == null && current.GetLeftWeapon() != null && KB.HasBeenPressed(Keys.D1))
        {
            var scored = Directions
                .Select(d => new CombatFlow(this, current, current.GetLeftWeapon(), (current.X, current.Y), d))
                .Select(cf => (cf, cf.Score()))
                .ToList();
            foreach (var s in scored) Console.WriteLine(s);
            scored.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            _confirmedCombatFlow = scored[0].cf;
        }
        else if (_confirmedCombatFlow == null && current.GetRightWeapon() != null && KB.HasBeenPressed(Keys.D2))
        {
            var scored = Directions
                .Select(d => new CombatFlow(this, current, current.GetRightWeapon(), (current.X, current.Y), d))
                .Select(cf => (cf, cf.Score()))
                .ToList();
            foreach (var s in scored) Console.WriteLine(s);
            scored.Sort((a, b) => b.Item2.CompareTo(a.Item2));
            _confirmedCombatFlow = scored[0].cf;
        }
        
        // MOVE
        if (PlayerSelectedIndex > -1)
        {
            if (_game.ActionPoints.Remaining > 0)
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
                        }
                        else if (Positions.IsEnemyAt(x + dx, y + dy) is { } e)
                        {
                            // do nothing
                        }
                        else if (Map?.IsWalkable(x + dx, y + dy) ?? false)
                        {
                            _confirmedCombatFlow = null;
                            var oldX = current.X;
                            var oldY = current.Y;
                            current.X += dx;
                            current.Y += dy;
                            _game.ActionPoints.Spend(1);
                            UpdateFov(true);
                            if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                            {
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                    .ApplyOnDomainStepped(this, current, current.X, current.Y, oldX, oldY));
                            }
                        }
                        UpdateFov(true);
                    }
                }
            }
        }
    }
}
