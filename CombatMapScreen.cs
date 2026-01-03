using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Http;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using RogueSharp.MapCreation;
using SINEATER.Input;
using SINEATER.SinMod;
using Wintellect.PowerCollections;

namespace SINEATER;

public enum EOrder
{
    Player = 0,
    CPU = 1,
}

public enum EAction
{
    Move = 0,
    Special = 1,
    Attack = 2,
    Rest = 3
}

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

    private static readonly (EOrder Side, EAction Action)[] TurnOrder = [
        (EOrder.Player, EAction.Move),
        (EOrder.Player, EAction.Special),
        (EOrder.Player, EAction.Attack),
        (EOrder.Player, EAction.Rest),
        (EOrder.CPU, EAction.Move),
        (EOrder.CPU, EAction.Special),
        (EOrder.CPU, EAction.Attack),
        (EOrder.CPU, EAction.Rest),
    ];

    public int TurnOrderIndex = 0;
    
    private ETerrainKind _kind;
    public LevelStructure Structure;
    
    private bool _rendered = false;
    private bool _detailedView = false;
    private Glyph[,] _groundGlyphs;
    internal FieldOfView<Cell> _fov;
    private readonly CombatConfig? _config;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    
    internal bool ShouldUpdateView = true;

    public Domains Domains;
    public IMap? Map => Structure.Map;
    //public List<Character> InitiativeOrder = [];
    //public Character? InitiativeCurrent => InitiativeOrder.First();
    
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
            
            //InitiativeOrder.Add(_game.Party.Characters[ci]);
        }

        // foreach (var enm in Structure.Enemies)
        // {
        //     InitiativeOrder.Add(enm);
        // }
        //
        // InitiativeOrder.Sort((ca, cb) => ca.Stats.Initiative - cb.Stats.Initiative);
        
        
    }

    private void Next()
    {
        TurnOrderIndex = (TurnOrderIndex + 1) % TurnOrder.Length;
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

        var (currentSide, currentAction) = TurnOrder[TurnOrderIndex];
        
        if (currentSide == EOrder.Player)
        {
            if (InputM.IsActive(EInputAction.EndTurn))
            {
                Next();
            }
            else if (!CheckSubmenuInputs())
            {
                CheckPlayerInputs();
            }
        }
        else
        {
            Next();
            // if (enemy.Active)
            // {
            //     var f = new FieldOfView(Map);
            //     Dictionary<Move, int> good = [];
            //     var playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))], Predicate.Walkable);
            //     
            //     foreach (var move in enemy.AvailableMoves)
            //     {
            //         good[move] = 0;
            //         var dup = enemy.Copy();
            //         if (move.CanPerform(dup, this))
            //         {
            //             move.Perform(dup, this, false).Consume();
            //
            //             f.ComputeFov(dup.X, dup.Y, dup.Cla, false);
            //             var dist = playerDist.Get(dup.X, dup.Y);
            //             if (dup.Attacks.Count == 0)
            //             {
            //                 // move closer
            //                 good[move] += Math.Max(1, dist - dup.MovementLeft) + move.Costs.Length;
            //             }
            //             else
            //             {
            //                 if (dist < dup.MovementLeft)
            //                 {
            //                     good[move] += 5 * dup.Attacks.Count + dist + move.Costs.Length;
            //                 }
            //                 else
            //                 {
            //                     good[move] += dist + move.Costs.Length;
            //                 }
            //             }
            //         }
            //     }
            //
            //     if (good.Count == 0)
            //     {
            //         enemy.Active = false;
            //         InitiativeNext();
            //     }
            //     else
            //     {
            //         var (best, quality) = good.OrderByDescending(a => a.Value).First();
            //         CoroutineHandler.Run(DoEnemyMove(enemy, best));
            //     }
            // }
            // else
            // {
            //     Console.WriteLine("ENEMY DOES NOTHING FOR NOW");
            //     InitiativeNext();
            // }
        }
    }

    IEnumerable DoEnemyMove(Enemy enemy, Move best)
    {
        // var (u, v) = enemy.Icon;
        // for (var id = 0; id < 5; id++)
        // {
        //     Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.Black, Color.White));
        //     yield return new WaitForSeconds(0.02f);
        //     Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.Black, Color.Red));
        //     yield return new WaitForSeconds(0.02f);
        // }
        //
        // var (ox, oy) = DrawOffset;
        // _game.Layers["ascii"].Set(ox + enemy.X + 2, oy + enemy.Y, $"{enemy.Name} performs {best.Name}.");
        // yield return new WaitForInput(EInputAction.Confirm);
        //
        // Draw(enemy.X, enemy.Y, new Glyph(u, v, Color.Black, enemy.Tint));
        // var playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))], Predicate.Walkable);
        // best.Perform(enemy, this).Consume();
        //
        // if (enemy.MovementLeft > 0)
        // {
        //     var originalDist = playerDist.Get(enemy.X, enemy.Y);
        //     var dist = originalDist;
        //     if (enemy.Attacks.Count > 0)
        //     {
        //         // move all the way to player
        //         for (var i = 0; i < Math.Min(dist, enemy.MovementLeft); i++)
        //         {
        //             var (x, y, newDist) = playerDist
        //                 .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
        //                     => !(_game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2))
        //                        && (!Structure.Enemies.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2) &&
        //                            xyd.Item3 < dist));
        //             if (newDist > 0)
        //             {
        //                 dist = newDist;
        //                 enemy.X = x;
        //                 enemy.Y = y;
        //             }
        //
        //             DrawCombat();
        //             yield return new WaitForSeconds(0.05f);
        //             playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))],
        //                 Predicate.Walkable);
        //         }
        //     
        //         dist = playerDist.Get(enemy.X, enemy.Y);
        //         if (dist == 1) // todo: dist >= attack.range
        //         {
        //             foreach (var atk in enemy.Attacks)
        //             {
        //                 var (px, py, pd) = playerDist
        //                     .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
        //                         => _game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2));
        //                 var player = _game.Party.Characters.First(e => e.X == px && e.Y == py);
        //                 yield return DoAttack(enemy, player);
        //             }
        //         }
        //
        //         yield return new WaitForSeconds(0.1f);
        //         enemy.MovementLeft -= originalDist;
        //         
        //         for (var i = 0; i < enemy.MovementLeft; i++)
        //         {
        //             var (x, y, newDist) = playerDist
        //                 .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
        //                     => !(_game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2))
        //                        && (!Structure.Enemies.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2) &&
        //                            xyd.Item3 > dist));
        //             if (newDist > 0)
        //             {
        //                 dist = newDist;
        //                 enemy.X = x;
        //                 enemy.Y = y;
        //             }
        //
        //             DrawCombat();
        //             yield return new WaitForSeconds(0.05f);
        //             playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))],
        //                 Predicate.Walkable);
        //         }
        //     }
        //     else
        //     {
        //         // move all the way to player
        //         for (var i = 0; i < Math.Min(dist, enemy.MovementLeft + 1); i++)
        //         {
        //             var (x, y, newDist) = playerDist
        //                 .GetAllAdjacent(enemy.X, enemy.Y).FirstOrDefault(xyd
        //                     => !(_game.Party.Characters.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2))
        //                        && (!Structure.Enemies.Any(e => e.X == xyd.Item1 && e.Y == xyd.Item2) &&
        //                            xyd.Item3 < dist));
        //             if (newDist > 0)
        //             {
        //                 dist = newDist;
        //                 enemy.X = x;
        //                 enemy.Y = y;
        //             }
        //
        //             DrawCombat();
        //             yield return new WaitForSeconds(0.05f);
        //             playerDist = new DistanceMap(Structure, false, [.._game.Party.Characters.Select(p => (p.X, p.Y))],
        //                 Predicate.Walkable);
        //         }
        //     }
        // }
        //
        // enemy.Attacks.Clear();
        //InitiativeNext();
        yield return null;
    }

    // private void InitiativeNext()
    // {
    //     var loop = 0;
    //     while (true)
    //     {
    //         loop++;
    //         var last = InitiativeOrder[0];
    //         InitiativeOrder.Remove(last);
    //         InitiativeOrder.Add(last);
    //
    //         if ((InitiativeCurrent?.Stats.Initiative ?? 0) > last.Stats.Initiative)
    //         {
    //             foreach (var ch in SineaterGame.Instance.Party.Characters)
    //             {
    //                 ch.ForceRestart(this);
    //             }
    //     
    //             foreach (var dom in Domains._domains)
    //             {
    //                 dom.Update(this);
    //             }
    //             
    //             UpdateEnemyActivation();
    //         }
    //         
    //         if (InitiativeCurrent is Enemy { Active: true })
    //         {
    //             break;
    //         }
    //         else if (InitiativeCurrent is PartyMember { IsDone: false })
    //         {
    //             break;
    //         }
    //
    //         if (loop > 100)
    //         {
    //             Console.WriteLine("STUCK IN LOOP!");
    //             Environment.Exit(0);
    //         }
    //     }
    //     UpdateCombatView();
    // }

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
        
        // if (InitiativeCurrent is PartyMember current)
        // {
        //     CalculateZone(current);
        // }
    }

    private void UpdateEnemyActivation()
    {
        // if (InitiativeCurrent is PartyMember player)
        // {
        //     var dist = new DistanceMap(Structure, false, [(player.X, player.Y)], Predicate.Walkable);
        //     var enemyFov = new FieldOfView(Structure.Map);
        //     //                                                          not active and player sees them
        //     foreach (var enemy in Structure.Enemies.Where(e => !e.Active && _fov.IsInFov(e.X, e.Y)))
        //     {
        //         var d = Math.Min(9, dist.Get(enemy.X, enemy.Y));
        //         enemyFov.ComputeFov(enemy.X, enemy.Y, 2 * enemy.Stats.Clarity, false);
        //         if (enemyFov.IsInFov(player.X, player.Y))
        //         {
        //             if (Rnd.Instance.Next(1, d) < (int)player.Loudness)
        //             {
        //                 enemy.Active = true;
        //                 CoroutineHandler.Run(new CoWakeUpEnemy(enemy, this));
        //             }
        //         }
        //     }
        // }
    }

    // RunEnemyMoves -> RunUpkeep
    private IEnumerable RunEnemyMoves()
    {
        foreach (var ch in SineaterGame.Instance.Party.Characters)
        {
            ch.ForceRestart(this);
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
        
        //_game.Party.Characters[0].AP.Draw(DrawOffset.X + 1, 27);

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

        // if (InitiativeCurrent is {} selected)
        // {
        //     for (var i = 0; i < _fullWidth; i++)
        //     {
        //         for (var j = 0; j < _fullHeight; j++)
        //         {
        //             var fg = Color.Black;
        //             var bg = Color.Black;
        //             foreach (var f in _fgs[(i, j)])
        //             {
        //                 fg = Color.Lerp(fg, f, 0.75f);
        //             }
        //
        //             fg = Color.Lerp(fg, Color.White, _fgs[(i, j)].Count / 4.0f);
        //
        //             if (Structure.Map.IsWalkable(i, j))
        //             {
        //                 var g = Glyph.Bw(_groundGlyphs[i, j].U, _groundGlyphs[i, j].V);
        //                 g.Fg = _showMap ? Color.White : Color.Lerp(fg, Color.White, 0.5f);
        //                 bg = (i % 2 == j % 2) ? new Color(0, 0, 0, 1) : new Color(20, 0, 10, 1);
        //
        //                 if (selected is PartyMember pm1 && pm1.Zone.Contains((i, j)))
        //                 {
        //                     bg = (i % 2 == j % 2)
        //                         ? Party.Zones[selected.Index]
        //                         : Color.Lerp(Party.Zones[selected.Index], Color.Black, 0.5f);
        //                     if (!pm1.Fov.Contains((i, j)))
        //                     {
        //                         bg = Color.Lerp(bg, Color.Black, 0.5f);
        //                     }
        //                 }
        //                 else
        //                 {
        //                     if (selected is PartyMember pm2 && !pm2.Fov.Contains((i, j)))
        //                     {
        //                         bg = Color.Black;
        //                         g.Fg = Color.Black;
        //                     }
        //                     else
        //                     {
        //                         // -1..1
        //                         // *0.5 = -0.5..0.5
        //                         // +0.5 = 0..1
        //                         g.Fg = Color.Lerp(g.Fg, Color.Black,
        //                             (MathF.Sin((i % 2 == j % 2 ? Single.Pi : 0) + _time * 0.001f) * 0.5f + 0.5f));
        //                     }
        //                 }
        //
        //                 g.Bg = bg;
        //
        //                 Draw(i, j, g);
        //             }
        //             else
        //             {
        //                 var g = _groundGlyphs[i, j];
        //                 Draw(i, j, new Glyph(g.U, g.V, Color.Black, _showMap ? Color.White : fg));
        //             }
        //         }
        //     }
        //     
        //     foreach (var domain in Domains._domains)
        //     {
        //         domain.Draw(this);
        //     }
        //
        //     foreach (var chr in _game.Party.Characters)
        //     {
        //         if (!_showMap && !_fov.IsInFov(chr.X, chr.Y))
        //             continue;
        //
        //         var (ix, iy) = chr.Job.GetImage();
        //         if (chr == selected)
        //         {
        //             iy -= 4;
        //         }
        //         
        //         if (chr.IsDone)
        //         {
        //             Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, Color.DarkGray));
        //         }
        //         else
        //         {
        //             Draw(chr.X, chr.Y, new Glyph(ix, iy, Color.Black, chr.Tint));
        //         }
        //     }
        //     
        //     var dm = Structure.Walkables.Distances[0];
        //     var (gx, gy) = Structure.Goals[0];
        //     Draw(gx, gy, new Glyph(13, 60, Color.Black, Color.Lerp(Color.Red, Color.Yellow, Rnd.Instance.Next01())));
        //
        //     var colors = new List<Color>() { Color.Red, Color.OrangeRed, Color.Orange, Color.YellowGreen, Color.Green };
        //
        //     foreach (var chr in Structure.Enemies.Where(chr => _showMap || _fov.IsInFov(chr.X, chr.Y)))
        //     {
        //         var (cu, cv) = chr.Icon;
        //         Draw(chr.X, chr.Y, new Glyph(cu, cv, Color.Black, chr.Active ? Color.White : Color.Gray));
        //     }
        //
        //     foreach (var chr in Structure.Treasure.Where(chr => !Domains.IsInDomain(chr.X, chr.Y) && (_showMap || _fov.IsInFov(chr.X, chr.Y))))
        //     {
        //         Draw(chr.X, chr.Y, new Glyph(5, 66, Color.Black, Color.Gold));
        //     }
        //     
        //     foreach (var chr in Structure.SpentTreasure.Where(chr => !Domains.IsInDomain(chr.X, chr.Y) && (_showMap || _fov.IsInFov(chr.X, chr.Y))))
        //     {
        //         Draw(chr.X, chr.Y, new Glyph(6, 66, Color.Black, Color.Gold));
        //     }
        //
        //     DrawParty();
        // }
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
    
    public void DrawParty((PartyMember?, int?, int?, int?, int?)? change = null, IEnumerable<PartyMember>? toDraw = null, Color? colorOverride = null)
    {
        var drawSet = (toDraw ?? _game.Party.Characters).ToHashSet();
        var (cha, cwil, ccla, cvig, cpoi) = change ?? (null, null, null, null, null);
        var h = 19;
        var index = 0;
        
        foreach (var character in _game.Party.Characters)
        {
            if (drawSet.Contains(character))
            {
                var (m, r) = character.Job.GetImage();
                var (u, v) = character.GetPortait();
                var (x, y) = _positions[index];
                var (xoff, yoff) = (_xoffsets[index], _offsets[index]);
                var tint = character.Tint;
                // if (character != InitiativeCurrent)
                // {
                //     tint = Color.Lerp(tint, Color.Black, 0.75f);
                // }

                if (colorOverride is { } color)
                {
                    tint = color;
                }

                _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y - 1 + yoff,
                    $"{character.Job.GetShortName()}", tint);
                _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y + yoff, $"{_positionStats[index]}",
                    tint);

                _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 4 + yoff, $"WIL  CLA  ", tint);
                _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 5 + yoff, $"VIG  POI  ", tint);

                if (character == cha)
                {
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{cwil ?? character.Wil}",
                        cwil == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{ccla ?? character.Cla}",
                        ccla == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{cvig ?? character.Vig}",
                        cvig == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{cpoi ?? character.Poi}",
                        cpoi == null ? Color.White : Color.Yellow);
                }
                else
                {
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{character.Wil}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{character.Cla}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{character.Vig}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{character.Poi}", Color.White);
                }


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
            }

            index++;
        }
    }
    
    public bool ShouldHardUpdate { get; set; } = true;

    private readonly int _offset = 96;
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
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

    public override void PostDraw(SpriteBatch batch, GameTime gameTime)
    {
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
            }
            else
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }
        }
    }
    
    private IEnumerable Coroutine_EndTurn()
    {
        yield return RunEnemyMoves();
        yield return ResetPartyMembers();
    }

    private bool _inspectMode = false;
    public Character? AttackTarget = null;
    
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

    public override void SubmenuActivate(string opt)
    {
        DrawCombat();
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
