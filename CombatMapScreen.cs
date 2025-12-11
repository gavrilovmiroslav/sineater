using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Input;
using SINEATER.SinMod;
using Wintellect.PowerCollections;

namespace SINEATER;

public enum ETerrainKind
{
    Tomb,
    Temple,
    Cave,
    Clearing,
    Ruin,
}

public record struct CombatParameters(int Resources, int MinLevel, int MaxLevel, string Reward);

public class CombatConfig
{
    public ETerrainKind Terrain;
    public CombatParameters Params;
}

public class CombatMapScreen : Screen
{
    private static readonly (int X, int Y)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private ETerrainKind _kind;
    public LevelStructure Structure;
    public AP TotalAP;
    private bool _rendered = false;
    private bool _detailedView = false;
    private Glyph[,] _groundGlyphs;
    internal FieldOfView<Cell> _fov;
    private readonly CombatConfig? _config;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    
    internal bool ShouldUpdateView = true;

    public Domains Domains;
    public IMap? Map => Structure.Map;
    public List<Character> InitiativeOrder = [];
    public Character? InitiativeCurrent => InitiativeOrder.First();
    
    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = _fullWidth - 2;
            this._height = _fullHeight - 2;
        }

        Regenerate();
    }
    
    private void Regenerate() => Regenerate(_kind);

    public CombatMapScreen(SineaterGame game, CombatConfig? config = null, int width = -1, int height = -1, string title = "???") : base(game)
    {
        _config = config;
        _width = width;
        _height = height;

        Domains = new(this);
        
        _kind = _config?.Terrain ?? ETerrainKind.Cave;
        _game = game;
        _groundGlyphs = new Glyph[_fullWidth, _fullHeight];
        Regenerate(_width == -1 || _height == -1);

        TotalAP = new AP(game.Party.Characters[0].AP, Structure.EnemyActionPoints);
        foreach (var player in game.Party.Characters)
        {
            player.AP = TotalAP;
        }

        foreach (var enemy in Structure.Enemies)
        {
            enemy.AP = TotalAP;
        }
    }

    public override void Initialize(SineaterGame game)
    {}
    
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

        Structure = new LevelStructure(map, _config);
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

        Map.SetCellProperties(Structure.Goals[0].X, Structure.Goals[0].Y, false, false);
        
        foreach (var (tx, ty) in Structure.Treasure)
        {
            Map.SetCellProperties(tx, ty, false, false);
        }
        
        _rendered = false;

        for (var ci = 0; ci < 4; ci++)
        {
            _game.Party.Characters[ci].X = Structure.Starts[ci].X;
            _game.Party.Characters[ci].Y = Structure.Starts[ci].Y;
            _game.Party.Characters[ci].SetOrigin();
            InitiativeOrder.Add(_game.Party.Characters[ci]);
        }

        foreach (var enm in Structure.Enemies)
        {
            InitiativeOrder.Add(enm);
        }

        InitiativeOrder.Sort((ca, cb) => ca.Stats.Initiative - cb.Stats.Initiative);
    }
    
    public override void Update(GameTime gameTime)
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
        
        if (InitiativeCurrent is PartyMember player)
        {
            if (player.IsDone)
            {
                InitiativeNext();
            }
            else if (!CheckSubmenuInputs())
            {
                CheckPlayerInputs();
            }
        }
        else if (InitiativeCurrent is Enemy enemy)
        {
            if (enemy.Active)
            {
                var f = new FieldOfView(Map);
                Dictionary<Move, int> good = [];
                var playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))], Predicate.Walkable);
                
                foreach (var move in enemy.AvailableMoves)
                {
                    good[move] = 0;
                    var dup = enemy.Copy();
                    move.Perform(dup, this).Consume();
                    
                    f.ComputeFov(dup.X, dup.Y, dup.Cla, false);
                    var dist = playerDist.Get(dup.X, dup.Y);
                    if (dup.Attacks.Count == 0)
                    {
                        // move closer
                        good[move] += Math.Max(1, dist - dup.MovementLeft);
                    }
                    else
                    {
                        if (dist < dup.MovementLeft)
                        {
                            good[move] += 5 * dup.Attacks.Count + dist;
                        }
                        else
                        {
                            good[move] += dist;
                        }
                    }
                }

                if (good.Count == 0)
                {
                    enemy.Active = false;
                    InitiativeNext();
                }
                else
                {
                    var (best, quality) = good.OrderByDescending(a => a.Value).First();
                    Console.WriteLine($"BEST MOVE: {best.Name} {quality}");
                    
                    CoroutineHandler.Run(DoEnemyMove(enemy, best));
                    
                    // if (InputManager.Instance.IsActionActive(EInputAction.Confirm))
                    // {
                    //     InitiativeNext();
                    // }
                }
            }
            else
            {
                Console.WriteLine("ENEMY DOES NOTHING FOR NOW");
                InitiativeNext();
            }
        }
    }

    IEnumerable DoEnemyMove(Enemy enemy, Move best)
    {
        var playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))], Predicate.Walkable);
        best.Perform(enemy, this).Consume();

        if (enemy.MovementLeft > 0)
        {
            var originalDist = playerDist.Get(enemy.X, enemy.Y);
            var dist = originalDist;
            //if (enemy.Attacks.Count > 0)
            {
                // move all the way to player
                for (var i = 0; i < Math.Min(dist, enemy.MovementLeft); i++)
                {
                    var (x, y, newDist) = playerDist
                        .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
                            => !(_game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2))
                               && (!Structure.Enemies.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2) &&
                                   xyd.Item3 < dist));
                    if (newDist > 0)
                    {
                        dist = newDist;
                        enemy.X = x;
                        enemy.Y = y;
                    }

                    DrawCombat();
                    yield return new WaitForSeconds(0.05f);
                    playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))],
                        Predicate.Walkable);
                }
            
                dist = playerDist.Get(enemy.X, enemy.Y);
                if (dist == 1) // todo: dist >= attack.range
                {
                    foreach (var atk in enemy.Attacks)
                    {
                        var (px, py, pd) = playerDist
                            .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
                                => _game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2));
                        var player = _game.Party.Characters.First(e => e.X == px && e.Y == py);
                        yield return DoAttack(enemy, player);
                    }
                }

                yield return new WaitForSeconds(0.1f);
                enemy.MovementLeft -= originalDist;
                
                for (var i = 0; i < enemy.MovementLeft; i++)
                {
                    var (x, y, newDist) = playerDist
                        .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
                            => !(_game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2))
                               && (!Structure.Enemies.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2) &&
                                   xyd.Item3 > dist));
                    if (newDist > 0)
                    {
                        dist = newDist;
                        enemy.X = x;
                        enemy.Y = y;
                    }

                    DrawCombat();
                    yield return new WaitForSeconds(0.05f);
                    playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))],
                        Predicate.Walkable);
                }
            }
        }

        enemy.Attacks.Clear();
        InitiativeNext();
    }

    private void InitiativeNext()
    {
        var loop = 0;
        while (true)
        {
            loop++;
            var last = InitiativeOrder[0];
            InitiativeOrder.Remove(last);
            InitiativeOrder.Add(last);

            if ((InitiativeCurrent?.Stats.Initiative ?? 0) > last.Stats.Initiative)
            {
                foreach (var ch in SineaterGame.Instance.Party.Characters)
                {
                    ch.ForceRestart();
                }
        
                foreach (var dom in Domains._domains)
                {
                    dom.Update(this);
                }
            }
            
            if (InitiativeCurrent is Enemy { Active: true })
            {
                break;
            }
            else if (InitiativeCurrent is PartyMember { IsDone: false })
            {
                break;
            }

            if (loop > 100)
            {
                Console.WriteLine("STUCK IN LOOP!");
                Environment.Exit(0);
            }
        }
        UpdateCombatView();
    }

    public void UpdateCombatView()
    {
        var selfFov = new FieldOfView(Map);
        _fgs.Clear();
        
        for (var i = 0; i < 4; i++)
        {
            var w = _game.Party.Characters[i];

            w.Fov = selfFov.
                ComputeFov(w.X, w.Y, 4 * w.Cla, true).
                Select(Predicate.CellToPosition).ToHashSet();
            
            foreach (var (x, y) in w.Fov)
            {
                _fgs.Add((x, y), w.Tint);
            }
            
            if (i == 0)
            {
                _fov.ComputeFov(w.X, w.Y, 4 * w.Cla, true);
            }
            else
            {
                _fov.AppendFov(w.X, w.Y, 4 * w.Cla, true);
            }
        }
        
        if (InitiativeCurrent is PartyMember current)
        {
            CalculateZone(current);
        }
    }

    private void UpdateEnemyActivation()
    {
        if (InitiativeCurrent is PartyMember player)
        {
            var enemyFov = new FieldOfView(Structure.Map);
            //                                                          not active and player sees them
            foreach (var enemy in Structure.Enemies.Where(e => !e.Active && _fov.IsInFov(e.X, e.Y)))
            {
                //if (enemy.ShouldWakeUp)
                {
                    enemy.Active = true;
                    continue;
                }
            }
        }
    }

    // RunEnemyMoves -> RunUpkeep
    private IEnumerable RunEnemyMoves()
    {
        foreach (var ch in SineaterGame.Instance.Party.Characters)
        {
            ch.ForceRestart();
        }
        
        foreach (var dom in Domains._domains)
        {
            dom.Update(this);
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
    
    public void CalculateZone(PartyMember w)
    {
        w.Zone.Clear();
        var dis = new DistanceMap(Structure, false, [w.Origin], Predicate.Walkable);
        var walkRadius = w.MovementLeft;
        w.Zone = dis.GetAllBeneath(walkRadius + 1).ToHashSet();
        w.Zone.IntersectWith(Structure.Map.
            GetCellsInCircle(w.Origin.X, w.Origin.Y, walkRadius).
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
        
        TotalAP.Draw(DrawOffset.X + 1, 27);

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

        if (InitiativeCurrent is {} selected)
        {
            if (selected is PartyMember pm)
            {
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
                            if (pm.Zone.Contains((i, j)))
                            {
                                bg = (i % 2 == j % 2)
                                    ? Party.Zones[selected.Index]
                                    : Color.Lerp(Party.Zones[selected.Index], Color.Black, 0.5f);
                                if (!pm.Fov.Contains((i, j)))
                                {
                                    bg = Color.Lerp(bg, Color.Black, 0.5f);
                                }
                            }
                            else
                            {
                                if (!pm.Fov.Contains((i, j)))
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
                                        (MathF.Sin((i % 2 == j % 2 ? Single.Pi : 0) + _time * 0.001f) * 0.5f + 0.5f));
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
                
                if (chr.IsDone)
                {
                    Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, Color.DarkGray));
                }
                else
                {
                    Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, chr.Tint));
                }
            }
            
            var dm = Structure.Walkables.Distances[0];
            var (gx, gy) = Structure.Goals[0];
            Draw(gx, gy, new Glyph(13, 60, Color.Black, Color.Lerp(Color.Red, Color.Yellow, Rnd.Instance.Next01())));

            var colors = new List<Color>() { Color.Yellow, Color.OrangeRed, Color.Red, Color.Purple };

            foreach (var chr in Structure.Enemies.Where(chr => showMap || _fov.IsInFov(chr.X, chr.Y)))
            {
                var (cu, cv) = chr.Icon;
                Draw(chr.X, chr.Y, new Glyph(cu, cv, Color.Black, chr.Active ? colors[chr.Level - 1] : Color.Gray));
            }

            foreach (var chr in Structure.Treasure.Where(chr => showMap || _fov.IsInFov(chr.X, chr.Y)))
            {
                Draw(chr.X, chr.Y, "?", Color.White);
            }

            DrawParty();
        }
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
            if (character != InitiativeCurrent)
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
    public bool ShouldHardUpdate { get; set; } = true;

    private int _offset = 96;
    
    public override void Draw(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();

        if (ShouldHardUpdate)
        {
            UpdateCombatView();
            ShouldHardUpdate = false;
        }
        
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

            for (var i = 0; i < _submenu.Count; i++)
            {
                var name = _submenu[i];
                if (name.EndsWith("*"))
                {
                    _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {name[..^1]}", Color.Gray);    
                }
                else
                {
                    _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {name}");
                }
            }

            if (_submenu[_submenuSelection].EndsWith("*"))
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, "x", Color.Gray);
                // TODO: add requirements here
            }
            else
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }

            if (_submenu[_submenuSelection] == "ATTACK")
            {
                var index = (InitiativeCurrent as PartyMember).Index;
                var (px, py) = (
                    _game.Party.Characters[index].X,
                    _game.Party.Characters[index].Y);
                
                DrawSubmenuAttackInfo(px + _submenuDelta.X, py + _submenuDelta.Y);
            }
        }
    }

    private void DrawSubmenuAttackInfo(int x, int y)
    {
        if (InitiativeCurrent is PartyMember attacker)
        {
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
                // TODO
                //DrawSubmenuAttackEnemy(defender, damage);
            }
        }
    }
    
    private bool showMap = false;
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return RunEnemyMoves();
        yield return ResetPartyMembers();
    }

    private bool _inspectMode = false;

    private void StartActionSubmenu(Character current)
    {
        _submenuDelta = (0, 0);
                    
        StartSubmenu([
            ..current.CurrentMoves.Select(n => n.Name.ToUpper() + (current.CanPay(n.Costs) ? "" : "*")),
            "END TURN"
        ]);
    }
    
    private void CheckPlayerInputs()
    {
        if (_inspectMode)
        {
            if (InputM.IsActive(EInputAction.ExitInspect))
            {
                _inspectMode = false;
            }
            return;
        }

        if (InitiativeCurrent is PartyMember current)
        {
            if (InputM.IsActive(EInputAction.EndTurn))
            {
                CoroutineHandler.Run(Coroutine_EndTurn());
                return;
            }
            // MOVE
            if (current.SelectedMove == null && current.HasTurn)
            {
                if (current.MovementLeft == 0 || InputM.IsActive(EInputAction.ActionsMenu))
                {
                    StartActionSubmenu(current);
                }
            }
            else if (InputM.IsActive(EInputAction.ActionsMenu))
            {
                List<string> opts = [];
                _submenuDelta = (0, 0);

                opts.Add("END TURN");
                StartSubmenu(opts.ToArray());
            }

            if (current is { MovementLeft: > 0 })
            {
                var up = InputM.IsActive(EInputAction.MoveUp);
                var down = InputM.IsActive(EInputAction.MoveDown);
                var left = InputM.IsActive(EInputAction.MoveLeft);
                var right = InputM.IsActive(EInputAction.MoveRight);

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
                            c.X = current.X;
                            c.Y = current.Y;
                            current.X = x + dx;
                            current.Y = y + dy;
                            _game.PartyActionPoints.Spend(1);

                            if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                            {
                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                    .ApplyOnDomainStepped(this, current, current.X, current.Y, x, y));
                            }
                        }
                        else if (Positions.IsEnemyAt(this, x + dx, y + dy) is { } e)
                        {
                            if (current is { HasTurn: true, Attacks.Count: 0 })
                            {
                                StartActionSubmenu(current);
                            }
                            else if (current.Attacks.Count > 0)
                            {
                                // ENEMY
                                _submenuDelta = (dx, dy);
                                List<string> opts = [];
                                if (current.Vig > 0)
                                {
                                    opts.Add("ATTACK");
                                }

                                if (current.CanSwapEnemies)
                                {
                                    opts.Add("SWAP");
                                }

                                if (opts.Count == 1)
                                {
                                    SubmenuActivate(opts.First());
                                }
                                else
                                {
                                    StartSubmenu(opts.ToArray());
                                }
                            }
                        }
                        else if (current.MovementLeft > 0 && Structure.Map.IsWalkable(x + dx, y + dy))
                        {
                            var nx = x + dx;
                            var ny = y + dy;

                            if (Domains.Tiles.ContainsKey((nx, ny)))
                            {
                                current.X = nx;
                                current.Y = ny;

                                current.MovementLeft--;
                                current.SetOrigin();
                                CalculateZone(current);
                                if (current is { MovementLeft: 0, Attacks.Count: 0, HasTurn: false })
                                {
                                    current.Done();
                                }

                                DrawCombat();
                                CoroutineHandler.Run(Domains.Tiles[(nx, ny)]
                                    .ApplyOnDomainStepped(this, current, nx, ny, x, y));
                            }
                            else if (current.Zone.Contains((nx, ny)))
                            {
                                current.X += dx;
                                current.Y += dy;

                                current.MovementLeft--;
                                current.SetOrigin();
                                CalculateZone(current);
                                DrawCombat();
                                if (current is { MovementLeft: 0, Attacks.Count: 0, HasTurn: false })
                                {
                                    current.Done();
                                }

                                UpdateEnemyActivation();
                            }
                        }
                        else if (Structure.Treasure.Contains((current.X + dx, current.Y + dy)))
                        {
                            var (tx, ty) = (current.X + dx, current.Y + dy);
                            Structure.Treasure.Remove((tx, ty));
                            Structure.Map.SetCellProperties(tx, ty, true, true);
                            CalculateZone(current);
                            _game.PartyActionPoints.Spend(1);
                            current.Done();
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

    private void StartSubmenu(string[] opts, bool cancel = true)
    {
        _submenuSelection = 0;
        foreach (var opt in opts)
        {
            _submenu.Add(opt);
        }
        
        if (cancel)
            _submenu.Add("CANCEL");
    }

    IEnumerable CoStartAttack(Character attacker, Attack attack, Character defender, CombatMapScreen screen)
    {
        var guv = (0, 0);
        if (defender is PartyMember pm) guv = pm.Job.GetImage();
        else if (defender is Enemy en) guv = en.Icon;
        
        for (var i = 0; i < 5; i++)
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
    }
    
    void CoDamageDealing(Character attacker, Attack attack, Character defender, CombatMapScreen screen)
    {
        var dmg = Math.Max(1, attack.Weapons.Sum(w => w.Attack));
        if (defender.Guard > 0)
        {
            defender.Guard -= dmg;
            if (defender.Guard < 0)
            {
                defender.HP += defender.Guard;
            }
        }
        else
        {
            defender.HP -= dmg;
        }

        if (defender.HP <= 0) defender.Die();
    }

    IEnumerable CoAfterDamage(Character attacker, Attack attack, Character defender, CombatMapScreen screen)
    {
        var guv = (0, 0);
        if (defender is PartyMember pm) guv = pm.Job.GetImage();
        else if (defender is Enemy en) guv = en.Icon;
        yield return new WaitForSeconds(0.5f);
        if (defender is Enemy { IsDead: true } e)
        {
            for (int i = 0; i < 5; i++)
            {
                var (gu, gv) = guv;
                Draw(defender.X, defender.Y, new Glyph(gu, gv, Color.Black, defender.Tint));
                yield return new WaitForSeconds(0.02f);
                Draw(defender.X, defender.Y, " ");
                yield return new WaitForSeconds(0.02f);
            }

            var ap = _game.Party.Characters[0].AP;
            ap.Add(EStatus.Sin, e.Level);

            Structure.Enemies.Remove(e);
            InitiativeOrder.Remove(e);
            UpdateCombatView();
        }

        if (attacker is PartyMember m)
        {
            var nextVig = attacker.Vig;
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
        
        for (int i = 0; i < 5; i++)
        {
            var (gu, gv) = attacker.Job.GetImage();
            Draw(attacker.X, attacker.Y, new Glyph(gu, gv, Color.Black, attacker.Tint));
            yield return new WaitForSeconds(0.01f);
            Draw(attacker.X, attacker.Y, ".", attacker.Tint, Color.Black);
            yield return new WaitForSeconds(0.01f);
        }
        
        attacker.Done();
    }
    
    IEnumerable CoAttack(Character attacker, Attack attack, Character defender, CombatMapScreen screen)
    {
        yield return CoStartAttack(attacker, attack, defender, screen);

        CoDamageDealing(attacker, attack, defender, screen);

        yield return CoAfterDamage(attacker, attack, defender, screen);
        
        DrawCombat();
    }

    private IEnumerable DoAttack(Character a, Character b)
    {
        var att = a.Attacks.First();
        a.Attacks = a.Attacks[1..];
        if (att.AttackProc != null)
        {
            yield return att.AttackProc(a, att, b, this);
        }
        else
        {
            yield return CoAttack(a, att, b, this);
        }
    }

    public override void SubmenuActivate(string opt)
    {
        DrawCombat();
        if (InitiativeCurrent is PartyMember current)
        {
            switch (opt)
            {
                case "SWAP":
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    if (Positions.IsAnyCharacterAt(this, x, y) is { } c)
                    {
                        c.X = current.X;
                        c.Y = current.Y;
                        current.X = x;
                        current.Y = y;

                        if (Domains.Tiles.ContainsKey(((int)current.X, (int)current.Y)))
                        {
                            DrawCombat();
                            CoroutineHandler.Run(Domains.Tiles[((int)current.X, (int)current.Y)]
                                .ApplyOnDomainStepped(this, current, current.X, current.Y, x, y));
                        }
                    }

                    break;
                }
                case "END TURN":
                    current.MovementLeft = 0;
                    current.Attacks.Clear();
                    current.SetOrigin();
                    CalculateZone(current);
                    current.Done();
                    break;
                case "ATTACK":
                {
                    var (dx, dy) = _submenuDelta;
                    var (x, y) = (current.X + dx, current.Y + dy);
                    if (Positions.IsCharacterAt(this, x, y) is { } c)
                    {
                        CoroutineHandler.Run(DoAttack(current, c));
                    }
                    else if (Positions.IsEnemyAt(this, x, y) is { } e)
                    {
                        CoroutineHandler.Run(DoAttack(current, e));
                    }

                    break;
                }
                case "CONSUME":
                {
                    var playerAP = new AP(TotalAP, 10);
                    foreach (var player in SineaterGame.Instance.Party.Characters)
                    {
                        player.AP = playerAP;
                    }

                    CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
                    Muse.SetTravelMood();
                    break;
                }
                case "CANCEL":
                    break;
                default:
                {
                    var moves = current.CurrentMoves;
                    var index = moves.FindIndex(w => w.Name.ToUpper() == opt);
                    if (index != -1)
                    {
                        current.SelectedMove = moves[index].Name;
                        CoroutineHandler.Run(moves[index].Perform(current, this));
                        CoroutineHandler.Run(new CoUpdateScreen(current, this));
                    }

                    break;
                }
            }
        }
    }
}

public class CoUpdateScreen(PartyMember c, CombatMapScreen s) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        s.ShouldHardUpdate = true;
        yield break;
    }
}
