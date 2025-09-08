using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace SINEATER;

public interface IBarPiece
{
    public int Width { get; set; }
    public ActionPoints ActionPoints { get; set; }
    
    public void Update(GameTime gameTime);
    public void Draw(int xMin, int xMax, int y);
}

public class BarPiece : IBarPiece {
    public int Width { get; set; }
    public ActionPoints ActionPoints { get; set; }

    public virtual void Update(GameTime gameTime) {}
    public virtual void Draw(int xMin, int xMax, int y) {}
}

public class ActionPoints(int width, TextLayer layer, IBarPiece def)
{
    public TextLayer Layer => layer;
    private readonly List<IBarPiece> _pieces = new();
    public (int, int) Points => (_empty - _spent, _empty);
    
    private int _empty = width;
    private int _spent = 0;

    public int Remaining => _empty - _spent;
    public bool Spend(int n)
    {
        if (_empty - _spent == 0) return false;
        
        if (n > _empty) n = _empty;
        _spent += n;

        return true;
    }

    public void Free(int n)
    {
        if (n > _spent) n = _spent;
        _spent -= n;
    }
    
    public void Add<T>(int w) where T : class, IBarPiece, new()
    {
        if (_empty - _spent == 0) return;
        
        T piece = null;
        foreach (var p in _pieces)
        {
            if (p is T)
            {
                piece = p as T;
                break;
            }
        }

        if (w > _empty)
        {
            w = _empty;
        }
        _empty -= w;
        
        if (piece != null)
        {
            piece.Width += w;
            return;
        }
        
        var t = new T
        {
            Width = w,
            ActionPoints = this
        };
        _pieces.Add(t);
    }

    public bool Contains<T>() where T : class, IBarPiece
    {
        foreach (var p in _pieces)
        {
            if (p is T)
            {
                return true;
            }
        }

        return false;
    }
    
    public void Reduce<T>(int w) where T : class, IBarPiece, new()
    {
        T piece = null;
        foreach (var p in _pieces)
        {
            if (p is T barPiece)
            {
                piece = barPiece;
                break;
            }
        }
        
        if (piece != null)
        {
            piece.Width -= w;
            _empty += w;

            if (piece.Width == 0)
            {
                _pieces.Remove(piece);
            }
        }
    }
    
    public void Draw(int x, int y)
    {
        def.ActionPoints = this;
        def.Width = _empty - _spent;

        for (var i = x - 1; i <= x + width; i++)
        {
            layer.Unset(i, y - 1);
            layer.Unset(i, y);
            layer.Unset(i, y + 1);
            layer.Unset(i, y + 2);
        }
        
        layer.Set(x - 1, y, Glyph.Bw(3, 6));
        for (var i = 0; i <= width; i++)
            layer.Set(x + i, y, Glyph.Bw(4, 6));
        
        var xMin = x;
        var xMax = xMin + def.Width;
        def.Draw(xMin, xMax - 1, y);
        xMin += _empty;
        
        foreach (var piece in _pieces)
        {
            xMax = xMin + piece.Width - 1;
            piece.Draw(xMin, xMax, y);
            if (piece.Width > 1)
            {
                layer.Set(xMin, y + 1, Glyph.Bw(0, 6));
            }

            xMin = xMax + 1;
        }
        
        layer.Set(x + width, y, Glyph.Bw(20, 5));
    }

    public void Update(GameTime gameTime)
    {
        def.Update(gameTime);
        foreach (var bar in _pieces)
        {
            bar.Update(gameTime);
        }
    }
}

public static class Bars
{
    public static (int, int) Offset(int min, int max)
    {
        var l = max - min;
        var ux = l == 0 ? 0 : 1;
        var uy = l == 0 ? 1 : 1;
        return (ux, uy);
    }
}

public class StatusStamina : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }

    public override void Draw(int xMin, int xMax, int y)
    {
        var len = Math.Max(xMax - xMin, 1);
        var dx = 1.0f / (float)len;
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(17, 5, Color.Black, Color.Lerp(Color.LightGreen, Color.Green, (i - xMin) * dx)));
        }

        var (ap, tot) = ActionPoints.Points;
        ActionPoints.Layer.Set(xMin, y + 1, $"{ap}/{tot}");
    }
}

public class StatusWounds : BarPiece
{
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(18, 5, Color.Black, i % 2 == 0 ? Color.Red : Color.DarkRed));
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(3, 0, Color.Black, Color.Red));
    }
}

public class StatusFire : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.002f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            var dt = MathF.Sin(_time) * 0.5f + 0.5f;
            var t = (float)Math.Clamp(dt, 0.2, 0.8);
            ActionPoints.Layer.Set(i, y, new Glyph(17, 5, Color.Black, Color.Lerp(Color.Yellow, Color.OrangeRed, i % 2 == 0 ? t : 1 - t + Rnd.Instance.Next01() * 0.2f)));
            ActionPoints.Layer.Set(i, y - 1, new Glyph(10 + ((int)(i + dt * 3)) % 3, 0, Color.Black, Color.Lerp(Color.Yellow, Color.OrangeRed, i % 2 == 0 ? t : 1 - t + Rnd.Instance.Next01() * 0.2f)));
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(7, 0, Color.Black, Color.OrangeRed));
    }
}

public class StatusTired : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.0005f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            var t = (float)Math.Clamp(MathF.Sin(_time) * 0.5f + 0.5f, 0.2, 0.8);
            ActionPoints.Layer.Set(i, y, new Glyph(17, 5, Color.Black, Color.Lerp(Color.Pink, Color.CornflowerBlue, i % 2 == 0 ? t : 1 - t)));
            var idx = (int)(i * 6.28f + _time) % 18;
            if (idx < 6)
            {
                ActionPoints.Layer.Set(i, y - 1,
                    new Glyph(17 + idx, 0, Color.Black,
                        Color.Lerp(Color.Pink, Color.CornflowerBlue,
                            i % 2 == 0 ? t : 1 - t + Rnd.Instance.Next01() * 0.2f)));
            }
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(6, 0, Color.Black, Color.CadetBlue));
    }
}

public class StatusInsanity : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        var d = 360.0f / (xMax - xMin + 1);
        for (var i = xMin; i <= xMax; i++)
        {
            var t = (int)((MathF.Sin(_time) * 0.5f + 0.5f) * 360 + d * i) % 360;
            var f = ((int)((_time * 100 % 30) / 10) + 2) % 8;
            var c = HSB.New(255, t, 0.5f, 0.6f);
            ActionPoints.Layer.Set(i, y, new Glyph(27 + (i + f) % 3, 6, Color.Black, c));
        }
        
        var color = HSB.New(255, (int)((MathF.Sin(_time) * 0.5f + 0.5f) * 360) % 360, 0.5f, 0.7f);
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(8, 0, Color.Black, color));
    }
}

public class StatusPoison : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            var dt = MathF.Sin(_time) * 0.5f + 0.5f;
            var t = (float)Math.Clamp(dt, 0.2, 0.8);
            var idx = (int)(i * 3.12f + _time) % 12;
            ActionPoints.Layer.Set(i, y, new Glyph(18, 5, Color.Black, Color.Lerp(Color.Black, Color.DarkViolet, i % 2 == 0 ? t : 1 - t)));
            if (idx < 4)
            {
                ActionPoints.Layer.Set(i, y - 1,
                    new Glyph(13 + idx, 0, Color.Black,
                        Color.Lerp(Color.Black, Color.DarkViolet,
                            i % 2 == 0 ? t : 1 - t + Rnd.Instance.Next01() * 0.2f)));
            }
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(4, 0, Color.Black, Color.DarkViolet));
    }
}

public class StatusSin : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.003f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        var t = 45 + MathF.Sin(_time) * 5;
        var b = (1.5f + MathF.Sin(_time)) * 0.33f;
        var c = HSB.New(255, t, 0.7f, b);
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(15, 5, Color.Black, c));
        }
        
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(5, 0, Color.Black, 
            HSB.New(255, t, 0.5f, 0.6f)));
    }
}

public class StatusStunned : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.01f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        var d = 60.0f / (xMax - xMin + 1);
        for (int i = xMin; i <= xMax; i++)
        {
            var t = (30 + (int)(MathF.Sin(_time) * 30) + (int)(i * d)) % 60;
            var c = HSB.New(255, t, 0.7f, 0.6f);
            ActionPoints.Layer.Set(i, y, new Glyph(18, 5, Color.Black, c));
        }
        
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(1, 0, Color.Black, 
            HSB.New(255, (int)(30 + (int)(MathF.Sin(_time) * 30)) % 60, 0.5f, 0.6f)));
    }
}

public class StatusFrozen : BarPiece
{
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.007f;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        var d = 60.0f / (xMax - xMin + 1);
        for (int i = xMin; i <= xMax; i++)
        {
            var t = 180 + ((int)(MathF.Sin(_time) * 30) + (int)(i * d * 27.2f)) % 60;
            if (t > 190)
            {
                var c = HSB.New(255, t, 0.7f, 0.6f);
                ActionPoints.Layer.Set(i, y, new Glyph(15, 5, Color.Black, c));
            }
            else
            {
                ActionPoints.Layer.Set(i, y, new Glyph(15, 5, Color.Black, Color.White));
            }
        }
        
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(2, 0, Color.Black, 
            HSB.New(255, 180 + (int)((int)(MathF.Sin(_time) * 30)) % 60, 0.5f, 0.6f)));
    }
}
