using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using SINEATER.Content;

namespace SINEATER;

public class ExplorationMapScreen : IScreen
{
    private readonly int _fullWidth = 26, _fullHeight = 15;
    private readonly int _offsetX = 4, _offsetY = 2;
    private SineaterGame _game;
    private CoroutineHandler _coroutineHandler = new();

    private bool _stats = false;
    private (int, int) _position;
    private HashSet<(int, int)> _history = [];
    private HashSet<(int, int)> _gossip = [];
    private Dictionary<(int, int), Trait?> _promised = [];
    private HashSet<(int, int)> _seen = [];
    private Dictionary<(int, int), ILocation> _locations = [];
    private Map<Cell> _map;
    private int _phase = 0;
    private FieldOfView<Cell> _fov;
    public float Time = 0.0f;
    private bool _debug = false;
    public List<Trait> UnusedTraits = [];

    public void UpdateFov(int sightRedux = 0)
    {
        var (px, py) = _position;
        var sight = SineaterGame.Instance.Party.WorldSight - sightRedux;
        if (sight <= 0) sight = 1;
        _seen.Clear();
        foreach (var s in _fov.ComputeFov(px, py, sight, true))
        {
            _history.Add((s.X, s.Y));
            _seen.Add((s.X, s.Y));
            _gossip.Remove((s.X, s.Y));
        }
    }

    public ExplorationMapScreen(SineaterGame game)
    {
        foreach (var typ in Trait.All)
        {
            UnusedTraits.Add((Trait)Activator.CreateInstance(typ));
        }
        UnusedTraits.Shuffle();
        
        _game = game;
        _game.World = new(_fullWidth, _fullHeight);
        _map = new Map(_fullWidth, _fullHeight);
        UpdateMap();
        _fov = new FieldOfView(_map);

        DrawDebugMap();
        _position = _game.World.Start;
        UpdateFov();
    }

    public void Initialize(SineaterGame game)
    {
    }

    public void Update(GameTime gameTime)
    {
        _game.Party.Selected = -1;
        if (_coroutineHandler.IsActive())
        {
            _coroutineHandler.Update();
        }
        else
        {
            if (KB.HasBeenPressed(Keys.I))
            {
                _game.ScreenStack.Push(new InventoryScreen(_game));
            }
            else if (KB.HasBeenPressed(Keys.O))
            {
                _game.ScreenStack.Push(new InventoryScreen(_game, true));
            };
            
            if (KB.HasBeenPressed(Keys.F10))
            {
                _debug = !_debug;
            }

            if (KB.HasBeenPressed(Keys.Tab))
            {
                _stats = !_stats;
            }

            var (x, y) = _position;
            if (KB.HasBeenPressed(Keys.Left))
            {
                _position = (x - 1, y);
            }

            if (KB.HasBeenPressed(Keys.Right))
            {
                _position = (x + 1, y);
            }

            if (KB.HasBeenPressed(Keys.Up))
            {
                _position = (x, y - 1);
            }

            if (KB.HasBeenPressed(Keys.Down))
            {
                _position = (x, y + 1);
            }

            if (_position != (x, y))
            {
                var (nx, ny) = _position;
                if (_position.Item1 < 0 || _position.Item2 < 0
                                        || _position.Item1 >= _fullWidth
                                        || _position.Item2 >= _fullHeight
                                        || _game.World.Map[nx, ny] == -2
                                        || _game.World.Map[nx, ny] == 4
                                        || _game.World.Map[nx, ny] == 8
                                        || _game.World.Map[nx, ny] == 10
                                        || _game.World.Map[nx, ny] == 11
                                        || !_map.IsWalkable(nx, ny))
                {
                    _position = (x, y);
                }
                else
                {
                    var t = _map.IsTransparent(nx, ny);
                    var w = _map.IsWalkable(nx, ny);
                    _map.SetCellProperties(nx, ny, true, true);
                    UpdateFov(!t ? 1 : 0);
                    _map.SetCellProperties(nx, ny, t, w);

                    if (_locations.ContainsKey((nx, ny)))
                    {
                        var l = _locations[(nx, ny)];
                        if (!l.Visited())
                        {
                            _coroutineHandler.Run(EnterLocation(l, nx, ny));
                            l.Visit();
                        }

                        if (l is LocationForest f)
                        {
                            if (Rnd.Instance.D100 < 25)
                            {
                                _game.ActionPoints.Add<StatusTired>(1);
                            }
                        } else if (l is LocationNPC npc)
                        {
                            _position = (x, y);
                        }
                    }
                    else
                    {
                        if (Rnd.Instance.D100 < 15)
                        {
                            _game.ActionPoints.Add<StatusTired>(1);
                        }
                    }
                }
            }
        }
    }

    private IEnumerable EnterLocation(ILocation l, int x, int y)
    {
        if (l is LocationCave cave)
        {
            yield return new ShowPopupWindowWithPortraitAndWaitForKey(_game.Party.Characters[0].GetPortait(),
                (_, bnd) =>
                {
                    bnd.Add($"{_locations[(x, y)].GetName()} You go in to explore.");
                }, true);

            Trait? p = null;
            if (_promised.ContainsKey((x, y)))
            {
                p = _promised[(x, y)];
            }
            _game.ActionPoints.Reduce<StatusTired>(_game.ActionPoints.Count<StatusTired>() / 2);
            yield return new FadeOutAndLoadScreen(1, new CombatMapScreen(_game, new CombatConfig() { Terrain = ETerrainKind.Cave, Phase = _phase, Reward = p }));
        }
        else if (l is LocationTemple temple)
        {
            yield return new ShowPopupWindowAndWaitForKey(
                (_, bnd) =>
                {
                    bnd.Add($"{_locations[(x, y)].GetName()} You hear voices from within...");
                });
        
            Trait? p = null;
            if (_promised.ContainsKey((x, y)))
            {
                p = _promised[(x, y)];
            }
            _game.ActionPoints.Reduce<StatusTired>(_game.ActionPoints.Count<StatusTired>() / 2);
            yield return new FadeOutAndLoadScreen(1, new CombatMapScreen(_game, new CombatConfig() { Terrain = ETerrainKind.Cave, Phase = _phase, Reward = p }));
        }
        else if (l is LocationTomb tomb)
        {
            yield return new ShowPopupWindowAndWaitForKey(
                (_, bnd) =>
                {
                    bnd.Add($"{_locations[(x, y)].GetName()} It might contain some precious bones.");
                });
        
            Trait? p = null;
            if (_promised.ContainsKey((x, y)))
            {
                p = _promised[(x, y)];
            }
            _game.ActionPoints.Reduce<StatusTired>(_game.ActionPoints.Count<StatusTired>() / 2);
            yield return new FadeOutAndLoadScreen(1, new CombatMapScreen(_game, new CombatConfig() { Terrain = ETerrainKind.Cave, Phase = _phase, Reward = p }));
        }
        else if (l is LocationTreasure treasure)
        {
            yield return new ShowPopupWindowAndWaitForKey(
                (_, bnd) =>
                {
                    IItem pot = Rnd.Instance.D4 < 2 ? new PotionBloodReliquary() : new GhylagsTear();
                    bnd.Add($"{_locations[(x, y)].GetName()} You found...");
                    bnd.Newline();
                    bnd.Add($"  a {pot}!");
                    _game.Inventory.Put(pot);
                    _locations.Remove((x, y));
                });
        }
        else if (l is LocationNPC npc)
        {
            yield return new ShowPopupWindowAndWaitForKey(
                (_, bnd) =>
                {
                    var gossip = _locations
                        .Where(xy => !_history.Contains(xy.Key))
                        .Where(xy => !(xy.Value is LocationGodhead or LocationPillar or LocationForest))
                        .ToList();
                    if (gossip.Count > 0)
                    {
                        gossip.Shuffle();
                        var ((x, y), lo) = gossip.First();
                        _seen.Add((x, y));
                        _history.Add((x, y));
                        for (int i = -1; i < 2; i++)
                        {
                            if (x + i >= 0
                                && y + i >= 0
                                && x + i < _fullWidth
                                && y + i < _fullHeight)
                            {
                                _seen.Add((x + i, y));
                                _history.Add((x + i, y));
                                _gossip.Add((x + i, y));
                                _seen.Add((x, y + i));
                                _history.Add((x, y + i));
                                _gossip.Add((x, y + i));
                            }
                        }

                        if (lo is not LocationNPC && lo is not LocationTreasure && UnusedTraits.Count > 0 && Rnd.Instance.D100 < 80)
                        {
                            var t = UnusedTraits[0];
                            UnusedTraits.RemoveAt(0);
                            _promised[(x, y)] = t;
                        }
                        Draw(new GameTime());
                        
                        bnd.Add($"{_locations[(x, y)].GetName()} You camp together. You learn about ");
                        if (lo is LocationNPC _)
                        {
                            bnd.Add($"their friend", Color.Gold);
                        }
                        else if (lo is LocationCave _)
                        {
                            bnd.Add($"a cave", Color.Gold);
                        }
                        else if (lo is LocationTemple _)
                        {
                            bnd.Add($"a temple", Color.Gold);
                        }
                        else if (lo is LocationTreasure _)
                        {
                            bnd.Add($"an unknown treasure", Color.Gold);
                        }
                        else if (lo is LocationTomb _)
                        {
                            bnd.Add($"an ancient tomb", Color.Gold);
                        }
                        bnd.Add(".");
                        
                        if (_promised.ContainsKey((x, y)))
                        {
                            bnd.Add("They mention a ");
                            bnd.Add(_promised[(x, y)].Name.ToUpper(), Color.DarkRed);
                            bnd.Add(" opponent there...");
                        } 
                    }
                });
            
            _locations.Remove((x, y));
            _game.World.Glyphs[x, y].U = 14;
            _game.World.Glyphs[x, y].V = 81;
        }
    }

    private void DrawParty()
    {
        var h = 19;
        var index = 0;
        foreach (var character in _game.Party.Characters)
        {
            var (m, r) = character.Job.GetImage();
            var (u, v) = character.GetPortait();
            _game.Layers["mrmo"].Set(10 * index, h - 1, new Glyph(m, r, Color.Black, character.Tint));
            _game.Layers["ascii"].Set(20 * index + 4, h - 1, $"{index + 1}. {character.Job}", character.Tint);
            for (int i = 0; i < character.Traits.Count; i++)
            {
                _game.Layers["ascii"].Set(20 * index + (index > 1 ? -2 : 12), h + i, $"[{character.Traits[i].ShortName}]", character.Tint);
            }

            _game.Layers["portrait"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
            _game.Layers["portrait"].Set(index * 2, 4, new Glyph(u, v, Color.Black, character.Tint));
            index++;
        }
    }
    
    private void DrawStats()
    {
        var w = 10;
        var h = 19;
        _game.Layers["ascii"].Set(w - 1, h + 0, "CHAR  NAME   WIL CLA POI VIG  SEE MOV LH RH DF SKLS", Color.CadetBlue);
        
        var index = 0;
        foreach (var character in _game.Party.Characters)
        {
            var c = character.Tint;
            var (ix, iy) = character.Job.GetImage();
            _game.Layers["mrmo"].Set(2 + w / 2 - 3, h + 1 + index, new Glyph(ix, iy, Color.Black, c));
            _game.Layers["ascii"].Set(w + 2, h + 1 + index, $"{index + 1}. {character.Job}", c);
            _game.Layers["ascii"].Set(w + 5 + 8, h + 1 + index, character.Stats.Will.ToString(), c);
            _game.Layers["ascii"].Set(w + 5 + 12, h + 1 + index, character.Stats.Clarity.ToString(), c);
            _game.Layers["ascii"].Set(w + 5 + 16, h + 1 + index, character.Stats.Poise.ToString(), c);
            _game.Layers["ascii"].Set(w + 5 + 20, h + 1 + index, character.Stats.Vigor.ToString(), c);
            
            _game.Layers["ascii"].Set(w + 6 + 7 + 17, h + 1 + index, (5 + character.Stats.Mod(EStat.Clarity)).ToString(), c);
            _game.Layers["ascii"].Set(w + 6 + 11 + 17, h + 1 + index, (character.Stats.Will + 5).ToString(), c);
            _game.Layers["ascii"].Set(w + 5 + 14 + 18, h + 1 + index, character.LeftWeapon?.Attack.ToString() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 14 + 19, h + 1 + index, character.LeftWeapon?.Weight.Short() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 17 + 18, h + 1 + index, character.RightWeapon?.Attack.ToString() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 17 + 19, h + 1 + index, character.RightWeapon?.Weight.Short() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 20 + 18, h + 1 + index, character.Armor?.Guard.ToString() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 20 + 19, h + 1 + index, character.Armor?.Weight.Short() ?? "-", c);
            _game.Layers["ascii"].Set(w + 5 + 20 + 22, h + 1 + index, character.GetTraits().Count.ToString(), c);
            
            index++;
        }
    }
    
    public void Draw(GameTime gameTime)
    {
        if (_coroutineHandler.IsActive()) return;
        _game.Layers["ascii"].Clear();
        _game.Layers["mrmo"].Clear();
        _game.Layers["portrait"].Clear();

        Time += SineaterGame.DeltaTime * 0.001f;
        for (var i = 3; i < _fullHeight; i++)
        {
            _game.World.Glyphs[0, i] = new Glyph(14, 54, Color.Black,
                Color.Lerp(Color.LightBlue, Color.Blue, ((Time + i * 2.7f) % 10) / 10.0f));
        }

        // draw next wall
        for (var p = _phase - 1; p <= _phase; p++)
        {
            if (p < 0) continue;
            var min = (p + 1) * (_fullWidth / 4) - 3;
            if (min < 0) min = 0;
            var max = (p + 1) * (_fullWidth / 4) + 4;
            if (max >= _fullWidth) max = _fullWidth - 1;
            for (int i = min; i < max; i++)
            {
                for (int j = 0; j < _fullHeight; j++)
                {
                    if (_game.World.Map[i, j] == 8) continue;
                    if (_game.World.Map[i, j] == 4
                        || _game.World.Map[i, j] == 11
                        || _game.World.Map[i, j] == 5
                        || _game.World.Map[i, j] == 9)
                    {
                        var c = Color.Lerp(Color.MediumPurple, Color.Purple, (float)j / (float)_fullHeight);
                        var f = (float)i / (float)_fullWidth;
                        c = Color.Lerp(Color.DarkGreen, c, f);
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, new Glyph(1 + ((int)Time + j) % 2, 38,
                            Color.Black, c));
                    }
                }
            }
        }

        foreach (var (px, py) in _history)
        {
            if (_game.World.Glyphs[px, py] != null)
            {
                var c = Color.Lerp(Color.MediumPurple, Color.Purple, (float)py / (float)_fullHeight);
                var f = (float)px / (float)_fullWidth;
                c = Color.Lerp(Color.DarkGreen, c, f);
                _game.Layers["mrmo"].Set(px + _offsetX, py + _offsetY, _game.World.Glyphs[px, py].Recolored(Color.Black,
                    c));
            }
        }

        foreach (var (sx, sy) in _seen)
        {
            _game.Layers["mrmo"].Set(sx + _offsetX, sy + _offsetY,
                _game.World.Glyphs[sx, sy]);
        }

        foreach (var (gx, gy) in _gossip)
        {
            _game.Layers["mrmo"].Set(gx + _offsetX, gy + _offsetY,
                _game.World.Glyphs[gx, gy].Recolored(Color.Black, Color.Gold));
        }
        
        var (x, y) = _position;
        var (u, v) = _game.Party.Characters[0].Job.GetImage();
        _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, Glyph.Bw(u, v));

        if (_debug) DrawDebugMap();
        
        if (_stats)
            DrawStats();
        else
            DrawParty();
    }

    public void DrawDebugMap()
    {
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                var l = _game.World.Map[i, j];
                var n = Math.Abs(l);
                var str = n.ToString();
                if (_game.World.Map[i, j] < 0)
                {
                    if (str.Length > 1)
                    {
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, (n % 10).ToString(), Color.Red);
                    }
                    else
                    {
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, str, Color.Purple);
                    }
                }
                else
                {
                    if (str.Length > 1)
                    {
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, (n % 10).ToString(), Color.Yellow);
                    }
                    else
                    {
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, str, Color.White);
                    }
                }
                //}
            }
        }
    }

    public void UpdateMap()
    {
        List<(int, int)>[] levels = [[], [], [], []];
        for (int i = 0; i < _fullWidth; i++)
        {
            for (int j = 0; j < _fullHeight; j++)
            {
                var m = _game.World.Map[i, j];
                if (m is >= 0 and < 4 or 6 or -1 or 9)
                {
                    if (m is >= 0 and < 4 or -1)
                    {
                        if (m != -1)
                        {
                            levels[m].Add((i, j));
                        }
                        else
                        {
                            if (_game.World.Map[i + 1, j] != 9)
                            {
                                var v = _game.World.Map[i - 1, j];
                                if (v == -2) v = _game.World.Map[i - 1, j + 1];
                                levels[v].Add((i, j));
                            }
                        }
                    }

                    _map.SetCellProperties(i, j, true, true);
                }
                else
                {
                    _map.SetCellProperties(i, j, true, false);
                }
            }
        }

        for (int l = 0; l < 4; l++)
        {
            levels[l].Shuffle();
            switch (l)
            {
                case 0:
                    levels[l].RemoveAll(xy => xy.Item1 == 0);
                    GenBeach(ref levels[l]);
                    break;
                case 1:
                    GenCity(ref levels[l]);
                    break;
                case 2:
                    GenForest(ref levels[l]);
                    break;
                case 3:
                    GenRed(ref levels[l]);
                    break;
            }
        }

        foreach (var ((px, py), l) in _locations)
        {
            _map.SetCellProperties(px, py, l.Transparent(), l.Walkable());
            _game.World.Glyphs[px, py] = l.GetIcon(px, py);
        }
    }

    private void GenBeach(ref List<(int, int)> level)
    {
        for (int i = 0; i < 20; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationForest());
        }

        level.RemoveRange(0, 30);
        level.Shuffle();

        int d = Rnd.Instance.D2 + 2;
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationCave());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D2, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTomb());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D2, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTemple());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationNPC());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTreasure());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationPillar());
        }

        level.Clear();
    }
    
    private void GenCity(ref List<(int, int)> level)
    {
        var (ax, ay) = level.Min();
        var (bx, by) = level.Max();
        for (int i = ax; i <= bx; i += 2)
        {
            for (int j = ay; j <= by; j += 2)
            {
                if (level.Contains((i, j)))
                {
                    if (Rnd.Instance.D100 >= 59) continue;
                    if (j + 2 < _fullHeight && _game.World.Map[i - 1, j + 2] == 9) continue;
                    if (_game.World.Map[i - 1, j] == 9) continue;
                    
                    _locations.Add((i, j), new LocationPillar());
                    level.Remove((i, j));
                }
            }
        }
        
        var d = Math.Min(Rnd.Instance.D4 - 1, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationNPC());
        }
        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTreasure());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
        
        d = Math.Min(Rnd.Instance.D6, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTemple());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
        
        d = Math.Min(Rnd.Instance.D6, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTomb());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
    }
    
    private void GenForest(ref List<(int, int)> level)
    {
        int d = level.Count * 3 / 4;
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationForest());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Rnd.Instance.D2 + 2;
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationCave());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D2, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTomb());
        }

        level.RemoveRange(0, d);
        level.Shuffle();
        
        d = Math.Min(Rnd.Instance.D4 - 1, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationNPC());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4 - 1, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTreasure());
        }

        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationPillar());
        }

        level.Clear();
    }

    private void GenRed(ref List<(int, int)> level)
    {
        var (ax, ay) = level.Min();
        var (bx, by) = level.Max();
        for (int i = ax; i <= bx; i += 2)
        {
            for (int j = ay; j <= by; j += 2)
            {
                if (level.Contains((i, j)))
                {
                    if (Rnd.Instance.D100 >= 49) continue;
                    if (j + 2 < _fullHeight && _game.World.Map[i - 1, j + 2] == 9) continue;
                    if (_game.World.Map[i - 1, j] == 9) continue;
                    _locations.Add((i, j), new LocationGodhead());

                    level.Remove((i, j));
                }
            }
        }
        
        var d = Math.Min(Rnd.Instance.D4 - 1, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationNPC());
        }
        level.RemoveRange(0, d);
        level.Shuffle();

        d = Math.Min(Rnd.Instance.D4, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTreasure());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
        
        d = Math.Min(Rnd.Instance.D6, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTemple());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
        
        d = Math.Min(Rnd.Instance.D6, level.Count);
        for (int i = 0; i < d; i++)
        {
            var (x, y) = level[i];
            _locations.Add((x, y), new LocationTomb());
        }
        level.RemoveRange(0, d);
        level.Shuffle();
    }
}