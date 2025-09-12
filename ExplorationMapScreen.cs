using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using SINEATER.Content;

namespace SINEATER;

public class ExplorationMapScreen : IScreen
{
    private readonly int _fullWidth = 26, _fullHeight = 15;
    private readonly int _offsetX = 4, _offsetY = 2;
    private SineaterGame _game;

    private (int, int) _position;
    private HashSet<(int, int)> _history = [];
    private HashSet<(int, int)> _seen = [];
    private Dictionary<(int, int), ILocation> _locations = [];
    private Map<Cell> _map;
    private int _phase = 0;
    private FieldOfView<Cell> _fov;
    public float Time = 0.0f;
    private bool _debug = false;

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
        }
    }

    public ExplorationMapScreen(SineaterGame game)
    {
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
        if (KB.HasBeenPressed(Keys.Tab))
        {
            _debug = !_debug;
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
            }
        }
    }

    public void Draw(GameTime gameTime)
    {
        _game.Layers["ascii"].Clear();
        _game.Layers["mrmo"].Clear();

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
                        _game.Layers["mrmo"].Set(i + _offsetX, j + _offsetY, new Glyph(1 + ((int)Time + j) % 2, 38,
                            Color.Black, Color.Lerp(Color.MediumPurple, Color.Purple, (float)j / (float)_fullHeight)));
                    }
                }
            }
        }

        foreach (var (px, py) in _history)
        {
            if (_game.World.Glyphs[px, py] != null)
                _game.Layers["mrmo"].Set(px + _offsetX, py + _offsetY, _game.World.Glyphs[px, py].Recolored(Color.Black,
                    Color.Lerp(Color.MediumPurple, Color.Purple, (float)py / (float)_fullHeight)));
        }

        var (plx, ply) = _position;
        var sight = SineaterGame.Instance.Party.WorldSight;

        foreach (var (sx, sy) in _seen)
        {
            _game.Layers["mrmo"].Set(sx + _offsetX, sy + _offsetY,
                _game.World.Glyphs[sx, sy]);    
        }

        var (x, y) = _position;
        _game.Layers["mrmo"].Set(x + _offsetX, y + _offsetY, "@");

        if (_debug) DrawDebugMap();
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
            Console.WriteLine(levels[l].Count);
            switch (l)
            {
                case 0:
                    for (int i = 0; i < 20; i++)
                    {
                        var (x, y) = levels[l][i];
                        _locations.Add((x, y), new LocationForest());
                    }
                    levels[l].RemoveRange(0, 30);

                    int d = Rnd.Instance.D2 + 2;
                    for (int i = 0; i < d; i++)
                    {
                        var (x, y) = levels[l][i];
                        _locations.Add((x, y), new LocationCave());
                    }
                    levels[l].RemoveRange(0, d);
                    
                    d = Math.Min(Rnd.Instance.D4, levels[l].Count);
                    for (int i = 0; i < d; i++)
                    {
                        var (x, y) = levels[l][i];
                        _locations.Add((x, y), new LocationHill());
                    }
                    levels[l].RemoveRange(0, d);

                    d = Math.Min(Rnd.Instance.D6, levels[l].Count);
                    for (int i = 0; i < d; i++)
                    {
                        var (x, y) = levels[l][i];
                        _locations.Add((x, y), new LocationNPC());
                    }
                    levels[l].RemoveRange(0, d);

                    d = Math.Min(Rnd.Instance.D4, levels[l].Count);
                    for (int i = 0; i < d; i++)
                    {
                        var (x, y) = levels[l][i];
                        _locations.Add((x, y), new LocationPillar());
                    }
                    levels[l].Clear();
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
            }
        }

        foreach (var ((px, py), l) in _locations)
        {
            _map.SetCellProperties(px, py, l.Transparent(), l.Walkable());
            _game.World.Glyphs[px, py] = l.GetIcon(px, py);
        }
    }
}