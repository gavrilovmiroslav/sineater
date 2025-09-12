using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class World
{
    public int[,] Map;
    public Glyph[,] Glyphs;
    private int _width;
    public int Height;
    public (int, int) Start;
    private int _seed;
    
    public World(int width, int height)
    {
        _seed = Rnd.Instance.Next(0, 30);
        Init(width, height);
    }
    
    private void Init(int width, int height)
    {
        _width = width;
        Height = height;
        Map = new int[width, height];
        Glyphs = new Glyph[width, height];
        List<(int, int)> doors = [];
        
        var prevDoor = Rnd.Instance.Next(5, height - 5);
        var w4 = width / 4;
        for (var w = 0; w < 4; w++)
        {
            for (var i = w * w4; i <= (w + 1) * w4; i++)
            {
                for (var j = 0; j < 4 - w; j++)
                {
                    Map[i, j] = 8;
                }
                for (var j = 4 - (w + 1); j < height; j++)
                {
                    Map[i, j] = w;
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
                        if (Map[x - 1, j] != 8)
                            Map[x - 1, j] = 5;
                        else
                            Map[x, j] = 5;
                    }
                    else
                    {
                        Map[x, j] = 4;
                        shade.Add((x - 1, j));
                        if (j > 3 && j < height - 2)
                            doors.Add((x, j));
                        if (x > 0)
                        {
                            for (int k = 0; k < dx; k++)
                                Map[x - k - 1, j] = w;
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
                    Map[cx, cy] = 10;
                    shade.Add((cx - 1, cy));
                    Map[cx + 1, cy] = 11;
                }
                catch
                {
                    //
                }
            }
            curves.Clear();
            
            foreach (var (sx, sy) in shade)
            {
                if (Map[sx, sy] != 10)
                    Map[sx, sy] = -1;
            }
            
            var ds = doors.Where(d =>
            {
                var (dox, doy) = d;
                return !(dox > 0 && Map[dox - 1, doy] == 10);
            }).ToList();

            var didx = prevDoor;
            do
            {
                didx = Rnd.Instance.Next(0, ds.Count());
            } while (Math.Abs(didx - prevDoor) < w + 1);
            var (dox, doy) = ds[didx];
            prevDoor = didx;
            Map[dox, doy] = 9;
            doors.Clear();
            offset += 2;
        }

        for (var j = 0; j < height; j++)
        {
            Map[width - 1, j] = 4;
            Map[width - 2, j] = -1;
        }

        var fd = 0;
        do
        {
            fd = Rnd.Instance.Next(3, height - 2);
        } while (Math.Abs(prevDoor - fd) <= 3);
        Map[width - 1, Rnd.Instance.Next(3, height - 3)] = 9;
        Map[0, Rnd.Instance.Next(3, height - 3)] = 6;

        for (var i = 0; i < _width; i++)
        {
            for (var j = 0; j < Height; j++)
            {
                if (Map[i, j] != 8) continue;
                if (Map[i, j + 1] != 8 || Map[i + 1, j + 1] == 5)
                {
                    Map[i, j] = -2;
                }
            }
        }
        
        DrawInternal(Height);
    }
    
    public static Color[] Colors =
    [
        new Color(0, 102, 51),
        new Color(100, 140, 0),
        new Color(189, 178, 0),
        new Color(255, 25, 0),
    ];
    
    public static Color[] Walls =
    [
        Color.White,
        Color.White,
        Color.White,
        Color.LightGray,
        Color.Gray,
    ];
    
    public void DrawInternal(int oh)
    {
        for (var i = 0; i < _width; i++)
        {
            for (int j = 0; j < Height; j++)
            {
                switch (Map[i, j])
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        Glyphs[i, j] = new Glyph(14, 54, Color.Black,  Colors[Map[i, j]]);
                        break;
                    case 4:
                        Glyphs[i, j] = new Glyph(15, 63, Color.Black,
                            Walls[(int)(_seed + (i * 3.14f + j * 2.6f)) % Walls.Length]);
                        break;
                    case 5:
                        Glyphs[i - 1, j] = new Glyph(11, 7, Color.Black, Color.White);
                        Glyphs[i, j] = new Glyph(15, 63, Color.Black, Color.White);
                        break;
                    case 10:
                        Glyphs[i, j] = new Glyph(11, 7, Color.Black, Color.White);
                        break;
                    case 11:
                        Glyphs[i, j] = new Glyph(15, 33, Color.Black, Color.White);
                        break;
                    case 6:
                        Glyphs[i, j] = new Glyph(14, 54, Color.Black, Colors[0]);
                        Start = (i, j);
                        break;
                    case 9:
                        // stepeniste
                        if (i != _width - 1)
                        {
                            Glyphs[i, j] = new Glyph(8, 8, Color.Black, Color.White);
                            Glyphs[i, j - 1] = new Glyph(11, 53, Color.Black, Color.White);
                            Glyphs[i, j - 2] = new Glyph(9, 8, Color.Black, Color.White);
                        }
                        else
                        {
                            Glyphs[i, j] = new Glyph(8, 8, Color.Black, Color.White);
                            Glyphs[i, j - 1] = new Glyph(11, 53, Color.Black, Color.White);
                            Glyphs[i, j - 2] = new Glyph(0, 14, Color.Black, Color.White);
                            Glyphs[i, j - 3] = new Glyph(0, 13, Color.Black, Color.White);
                        }

                        break;
                    case -1:
                        var m = Map[i - 1, j];
                        if (m < 0) m = 0;
                        if (m > Colors.Length - 1) m = Colors.Length - 1;
                        Glyphs[i, j] = new Glyph(14, 54, Color.Black, Colors[m].Darken(0.2f * (m + 0.5f)));
                        break;
                    case -2:
                        Glyphs[i, j] = new Glyph(10, 29, Color.Black, Color.White);
                        break;
                    default:
                        Glyphs[i, j] = new Glyph(0, 0, Color.Black, Color.White);
                        break;
                }
            }
        }
    }
}