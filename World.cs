using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class World
{
    private int[,] _map;
    private int _width, _height;
    private int _seed;
    
    public World(int width, int height)
    {
        _seed = Rnd.Instance.Next(0, 30);
        Init(width, height);
    }

    private void Init(int width, int height)
    {
        _width = width;
        _height = height;
        _map = new int[width, height];
        List<(int, int)> doors = [];
        
        var prevDoor = Rnd.Instance.Next(5, height - 5);
        var w4 = width / 4;
        for (var w = 0; w < 4; w++)
        {
            for (var i = w * w4; i <= (w + 1) * w4; i++)
            {
                for (var j = 0; j < 4 - w; j++)
                {
                    _map[i, j] = 8;
                }
                for (var j = 4 - (w + 1); j < height; j++)
                {
                    _map[i, j] = w;
                }
            }
        }

        var offset = Rnd.Instance.D6;
        var segment = 5 + Rnd.Instance.D6;
        
        for (var w = 0; w < 3; w++)
        {
            var dx = 0;
            var x = (w + 1) * w4;
            
            var first = true;
            List<(int, int)> curves = []; 
            List<(int, int)> shade = [];
            for (var j = 4 - (w + 2); j < height; j++)
            {
                if ((j + offset) % segment == 0 && x + 1 < width)
                {
                    curves.Add((x, j));
                    dx++; x++;
                }

                try
                {
                    if (first)
                    {
                        if (_map[x - 1, j] != 8)
                            _map[x - 1, j] = 5;
                        else
                            _map[x, j] = 5;
                    }
                    else
                    {
                        _map[x, j] = 4;
                        shade.Add((x - 1, j));
                        if (j > 3 && j < height - 2)
                            doors.Add((x, j));
                        if (x > 0)
                        {
                            for (int k = 0; k < dx; k++)
                                _map[x - k - 1, j] = w;
                        }
                    }
                }
                catch
                {
                    // ignored
                }
                
                first = false;
            }
            
            foreach (var (cx, cy) in curves)
            {
                try
                {
                    _map[cx, cy] = 10;
                    shade.Add((cx - 1, cy));
                    _map[cx + 1, cy] = 11;
                }
                catch
                {
                    //
                }
            }
            curves.Clear();
            
            foreach (var (sx, sy) in shade)
            {
                if (_map[sx, sy] != 10)
                    _map[sx, sy] = -1;
            }
            
            var ds = doors.Where(d =>
            {
                var (dox, doy) = d;
                return !(dox > 0 && _map[dox - 1, doy] == 10);
            }).ToList();

            var didx = prevDoor;
            do
            {
                didx = Rnd.Instance.Next(0, ds.Count());
            } while (Math.Abs(didx - prevDoor) < w + 1);
            var (dox, doy) = ds[didx];
            prevDoor = didx;
            _map[dox, doy] = 9;
            doors.Clear();
            offset += 2;
        }

        for (var j = 0; j < height; j++)
        {
            _map[width - 1, j] = 4;
            _map[width - 2, j] = -1;
        }

        var fd = 0;
        do
        {
            fd = Rnd.Instance.Next(3, height - 2);
        } while (Math.Abs(prevDoor - fd) <= 3);
        _map[width - 1, Rnd.Instance.Next(3, height - 3)] = 9;
        _map[0, Rnd.Instance.Next(3, height - 3)] = 6;

        for (var i = 0; i < _width; i++)
        {
            for (var j = 0; j < _height; j++)
            {
                if (_map[i, j] != 8) continue;
                if (_map[i, j + 1] != 8 || _map[i + 1, j + 1] == 5)
                {
                    _map[i, j] = -2;
                }
            }
        }
    }

    public static Color[] Colors =
    [
        new Color(0, 102, 51),
        new Color(0, 140, 0),
        new Color(89, 178, 0),
        new Color(163, 217, 0),
        new Color(255, 255, 0),
    ];
    
    public static Color[] Walls =
    [
        Color.White,
        Color.White,
        Color.White,
        Color.LightGray,
        Color.Gray,
    ];
    
    public void Draw(SineaterGame game, int ow, int oh)
    {
        for (var i = 0; i < _width; i++)
        {
            for (int j = 0; j < _height; j++)
            {
                switch (_map[i, j])
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(14, 54, Color.Black, Colors[_map[i, j]]));
                        break;
                    case 4:
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(15, 63, Color.Black, Walls[(int)(_seed + (i * 3.14f + j * 2.6f)) % Walls.Length]));
                        break;
                    case 5:
                        game.Layers["mrmo"].Set(i + ow - 1, j + oh, new Glyph(11, 7, Color.Black, Color.White));
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(15, 63, Color.Black, Color.White));
                        break;
                    case 10:
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(11, 7, Color.Black, Color.White));                       
                        break;
                    case 11:
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(15, 33, Color.Black, Color.White));                       
                        break;
                    case 6:
                        game.Layers["mrmo"].Set(i + ow, j + oh, "@");
                        break;
                    case 9:
                        // stepeniste
                        if (i != _width - 1)
                        {
                            game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(8, 8, Color.Black, Color.White));
                            game.Layers["mrmo"].Set(i + ow, j + oh - 1, new Glyph(11, 53, Color.Black, Color.Gray));
                            game.Layers["mrmo"].Set(i + ow, j + oh - 2, new Glyph(9, 8, Color.Black, Color.White));
                        }
                        else
                        {
                            game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(8, 8, Color.Black, Color.White));
                            game.Layers["mrmo"].Set(i + ow, j + oh - 1, new Glyph(11, 53, Color.Black, Color.White));
                            game.Layers["mrmo"].Set(i + ow, j + oh - 2, new Glyph(0, 14, Color.Black, Color.Lerp(Colors[4], Color.White, 0.5f)));
                            game.Layers["mrmo"].Set(i + ow, j + oh - 3, new Glyph(0, 13, Color.Black, Colors[4]));
                        }

                        break;
                    case -1:
                        var m = _map[i - 2, j];
                        if (m < 0) m = 0;
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(14, 54, Color.Black, Colors[m]));
                        game.Layers["mrmo"].Darken(i + ow, j + oh, 0.2f * (m + 0.5f));
                        break;
                    case -2:
                        game.Layers["mrmo"].Set(i + ow, j + oh, new Glyph(10, 29, Color.Black, Color.White));
                        break;
                    default:
                        game.Layers["mrmo"].Set(i + ow, j + oh, ' ');
                        break;
                }
            }
        }
        var w4 = _width / 4;
        var x = 3 * w4;
        for (var i = x - 1 ; i < _width; i++)
        {
            game.Layers["mrmo"].Set(i + ow, oh - 1, new Glyph(10, 29, Color.Black, Color.White));
        }
        for (var i = 0 ; i < _width; i++)
        {
            game.Layers["mrmo"].Set(i + ow, _height + 1, new Glyph(10, 27, Color.Black, Color.White));
        }
    }
}