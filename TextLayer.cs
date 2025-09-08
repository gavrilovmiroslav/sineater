using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadRex;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER;

public record struct Glyph(int U, int V, Color Bg, Color Fg)
{
    public static Glyph Bw(int u, int v)
    {
        return  new Glyph(u, v, Color.Black, Color.White);
    }
}

public static class RexColorExtensions {
    public static Color ToXNA(this SadRex.Color color)
    {
        return new Color(color.R, color.G, color.B);
    }
}

public record struct Corners<T>(T TopLeft, T TopRight, T BottomLeft, T BottomRight);

public class TextLayer(Texture2D font, Vector2 screen, Vector2 tileSize, Vector2 mapSize, Vector2 edge, int scale, Vector2 offset)
{
    private Vector2 _offset = Vector2.Zero;
    private readonly Dictionary<int, Glyph> _glyphs = new();
    private readonly Dictionary<char, (int, int)> _chars = new();
    private readonly HashSet<(int, int)> _flips = new();
    
    public (int, int) Char(char c) => _chars[c];
    
    public void Map(char c, int x, int y)
    {
        _chars.Add(c, (x, y));
    }

    public void Map(string s, int x, int y)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < s.Length; i++)
        {
            Map(chars[i], x + i, y);
        }
    }
    
    private (int, int) FromPosition(int p)
    {
        int w = (int)screen.X;
        int y = p / w;
        int x = p % w;
        return (x, y);
    }
    
    private int ToPosition(int x, int y)
    {
        return x + y * (int)screen.X;
    }

    public void SetOffset(int u, int v)
    {
        _offset.X = u;
        _offset.Y = v;
    }

    public void SetFlip(int u, int v, SpriteEffects flip)
    {
        if (flip != SpriteEffects.None)
        {
            _flips.Add((u, v));
        }
        else
        {
            _flips.Remove((u, v));
        }
    }
    
    public void Unset(int x, int y)
    {
        if (x < 0 || x >= screen.X) return;
        if (y < 0 || y >= screen.Y) return;
        _glyphs.Remove(ToPosition(x, y));
    }
    
    public void Set(int x, int y, Glyph glyph)
    {
        if (x < 0 || x >= screen.X) return;
        if (y < 0 || y >= screen.Y) return;
        _glyphs[ToPosition(x, y)] = glyph;
    }

    public void Set(int x, int y, char c)
    {
        if (x < 0 || x >= screen.X) return;
        if (y < 0 || y >= screen.Y) return;
        _glyphs[ToPosition(x, y)] = Glyph.Bw(_chars[c].Item1, _chars[c].Item2);
    }

    IEnumerable<Vector2> Line(int x, int y, int x2, int y2)
    {
        var w = x2 - x;
        var h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        dx1 = w switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => dx1
        };
        dy1 = h switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => dy1
        };
        dx2 = w switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => dx2
        };
        var longest = Math.Abs(w);
        var shortest = Math.Abs(h);
        
        if (!(longest > shortest))
        {
            longest = Math.Abs(h);
            shortest = Math.Abs(w);
            dy2 = h switch
            {
                < 0 => -1,
                > 0 => 1,
                _ => dy2
            };
            dx2 = 0;
        }

        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            yield return new Vector2(x, y);
            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
    }

    public void SetLine(Vector2 start, Vector2 end, char c)
    {
        foreach (var place in Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y))
        {
            Set((int)place.X, (int)place.Y, c);
        }
    }
    
    public void SetLine(Vector2 start, Vector2 end, Glyph g)
    {
        foreach (var place in Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y))
        {
            Set((int)place.X, (int)place.Y, g);
        }
    }
    
    public void SetLine(Vector2 start, Vector2 end, Func<Vector2, float, Glyph> action)
    {
        var dm = Vector2.Distance(start, end);
        foreach (var place in Line((int)start.X, (int)start.Y, (int)end.X, (int)end.Y))
        {
            var d = Vector2.Distance(start, place); 
            Set((int)place.X, (int)place.Y, action(place, d / dm));
        }
    }

    public void SetRect(Vector2 start, Vector2 end, Glyph g)
    {
        for (var x = start.X; x <= end.X; x++)
        {
            for (var y = start.Y; y <= end.Y; y++)
            {
                Set((int)x, (int)y, g);
            }
        }
    }
    
    public void SetRect(Vector2 start, Vector2 end, char c)
    {
        for (var x = start.X; x <= end.X; x++)
        {
            for (var y = start.Y; y <= end.Y; y++)
            {
                Set((int)x, (int)y, c);
            }
        }
    }

    public void SetRect(Vector2 start, Vector2 end, Func<Vector2, float, Glyph> action)
    {
        var dm = Vector2.Distance(start, end);
        for (var x = start.X; x <= end.X; x++)
        {
            for (var y = start.Y; y <= end.Y; y++)
            {
                var place = new Vector2((int)x, (int)y);
                var d = Vector2.Distance(start, place); 
                Set((int)place.X, (int)place.Y, action(place, d / dm));
            }
        }
    }
    
    public void SetFrame(Vector2 start, Vector2 end, char h, char v)
    {
        var places = new[] { new Vector2(0, (int)start.Y), new Vector2(0, (int)end.Y) };
        
        for (var x = start.X; x <= end.X; x++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].X = x;
                Set((int)places[i].X, (int)places[i].Y, h);
            }
        }

        places[0].X = start.X;
        places[1].X = end.X;
        for (var y = start.Y + 1; y <= end.Y - 1; y++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].Y = y;
                Set((int)places[i].X, (int)places[i].Y, v);
            }
        }
    }
    
    public void SetFrame(Vector2 start, Vector2 end, Glyph h, Glyph v)
    {
        var places = new[] { new Vector2(0, (int)start.Y), new Vector2(0, (int)end.Y) };
        
        for (var x = start.X; x <= end.X; x++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].X = x;
                Set((int)places[i].X, (int)places[i].Y, h);
            }
        }

        places[0].X = start.X;
        places[1].X = end.X;
        for (var y = start.Y + 1; y <= end.Y - 1; y++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].Y = y;
                Set((int)places[i].X, (int)places[i].Y, v);
            }
        }
    }
    
    public void SetFrame(Vector2 start, Vector2 end, Func<Vector2, float, Glyph> h, Func<Vector2, float, Glyph> v)
    {
        var dm = Vector2.Distance(start, end);
        var places = new[] { new Vector2(0, (int)start.Y), new Vector2(0, (int)end.Y) };
        
        for (var x = start.X; x <= end.X; x++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].X = x;
                var d = Vector2.Distance(start, places[i]);
                Set((int)places[i].X, (int)places[i].Y, h(places[i], d / dm));
            }
        }

        places[0].X = start.X;
        places[1].X = end.X;
        for (var y = start.Y + 1; y <= end.Y - 1; y++)
        {
            for (int i = 0; i < 2; i++)
            {
                places[i].Y = y;
                var d = Vector2.Distance(start, places[i]);
                Set((int)places[i].X, (int)places[i].Y, v(places[i], d / dm));
            }
        }
    }

    private void SetCorners(Vector2 start, Vector2 end, Corners<Glyph> corners)
    {
        Set((int)start.X, (int)start.Y, corners.TopLeft);
        Set((int)end.X, (int)start.Y, corners.TopRight);
        Set((int)start.X, (int)end.Y, corners.BottomLeft);
        Set((int)end.X, (int)end.Y, corners.BottomRight);
    }

    private void SetCorners(Vector2 start, Vector2 end, Corners<char> corners)
    {
        Set((int)start.X, (int)start.Y, corners.TopLeft);
        Set((int)end.X, (int)start.Y, corners.TopRight);
        Set((int)start.X, (int)end.Y, corners.BottomLeft);
        Set((int)end.X, (int)end.Y, corners.BottomRight);
    }

    public void SetBox(Vector2 start, Vector2 end, Glyph h, Glyph v, Corners<Glyph> corners)
    {
        SetFrame(start, end, h, v);
        SetCorners(start, end, corners);
    }
    
    public void SetBox(Vector2 start, Vector2 end, Glyph h, Glyph v, Corners<char> corners)
    {
        SetFrame(start, end, h, v);
        SetCorners(start, end, corners);
    }
    
    public void Set(int x, int y, string s)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < s.Length; i++)
        {
            if (!_chars.ContainsKey(chars[i])) continue;
            
            var (u, v) = _chars[chars[i]];
            Set(x + i, y, Glyph.Bw(u, v));
        }
    }

    public void Set(int x, int y, string s, Color fg)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < s.Length; i++)
        {
            if (!_chars.ContainsKey(chars[i])) continue;
            
            var (u, v) = _chars[chars[i]];
            Set(x + i, y, new Glyph(u, v, Color.Black, fg));
        }
    }
    
    public void Set(int x, int y, string s, Color fg, Color bg)
    {
        var chars = s.ToCharArray();
        for (int i = 0; i < s.Length; i++)
        {
            if (!_chars.ContainsKey(chars[i])) continue;
            
            var (u, v) = _chars[chars[i]];
            Set(x + i, y, new Glyph(u, v, bg, fg));
        }
    }

    public void SetRex(int sx, int sy, Image rex)
    {
        for (var i = 0; i < rex.Width; i++)
        {
            for (var j = 0; j < rex.Height; j++)
            {
                foreach (var layer in rex.Layers)
                {
                    if (!layer[i, j].IsTransparent())
                    {
                        var c = layer[i, j].Character;
                        if (c == 32) continue; 
                        var y = c / (int)mapSize.X;
                        var x = c % (int)mapSize.X;
                        var b = layer[i, j].Background;
                        var f = layer[i, j].Foreground;
                        Set(sx + i, sy + j, new Glyph(x, y, b.ToXNA(), f.ToXNA()));
                    }
                }
            }
        }
    }
    
    public void SetRex(int sx, int sy, Image rex, int layerIndex)
    {
        var layer = rex.Layers[layerIndex];
        for (var i = 0; i < rex.Width; i++)
        {
            for (var j = 0; j < rex.Height; j++)
            {
                if (layer[i, j].IsTransparent() || layer[i, j].Background == SadRex.Color.Transparent) continue;
                
                var c = layer[i, j].Character;
                if (c == 32) continue; 
                var y = c / (int)mapSize.X;
                var x = c % (int)mapSize.X;
                var b = layer[i, j].Background;
                var f = layer[i, j].Foreground;
                Set(sx + i, sy + j, new Glyph(x, y, b.ToXNA(), f.ToXNA()));
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        var pos = Vector2.Zero;
        var tx = (int)tileSize.X;
        var ty = (int)tileSize.Y;
        var mx = mapSize.X - 1;
        var my = mapSize.Y - 1;
        var ox = (int)_offset.X;
        var oy = (int)_offset.Y;
        
        var src = new Rectangle
        {
            X = (int) (mx * (tx + ox)),
            Y = (int) (my * (ty + oy)),
            Width = tx,
            Height = ty
        };

        spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        foreach (var (xy, glyph) in _glyphs)
        {
            var (x, y) = FromPosition(xy);
            pos.X = (edge.X + x) * tx * scale;
            pos.Y = (edge.Y + y) * ty * scale;
            spriteBatch.Draw(font, pos + offset, src, glyph.Bg, 0.0f, Vector2.Zero, scale, 
                _flips.Contains((glyph.U, glyph.V)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0.0f);
        }
        spriteBatch.End();

        spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.AlphaBlend);
        foreach (var (xy, glyph) in _glyphs)
        {
            var (x, y) = FromPosition(xy);
            src.X = glyph.U * (tx + ox);
            src.Y = glyph.V * (ty + oy);
            pos.X = (edge.X + x) * tx * scale;
            pos.Y = (edge.Y + y) * ty * scale;
            spriteBatch.Draw(font, pos + offset, src, glyph.Fg, 0.0f, Vector2.Zero, scale, 
                _flips.Contains((glyph.U, glyph.V)) ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0.0f);
        }
        spriteBatch.End();
    }
}