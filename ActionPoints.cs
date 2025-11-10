using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;

namespace SINEATER;

public enum EStatus
{
    Stamina,
    Void,
    Wound,
    Fire,
    Fatigue,
    Insanity,
    Poison,
    Sin,
    Death,
    Frozen,
    Luck,
}

public interface IStatus
{
    public EStatus Kind { get; }
    public int Width { get; set; }
    public AP ActionPoints { get; set; }
    
    public void Update(GameTime gameTime);
    public void Draw(int xMin, int xMax, int y);
    public EStatus ToStatus();
}

public abstract class Status : IStatus {
    public abstract EStatus Kind { get; }
    public int Width { get; set; }
    public AP ActionPoints { get; set; }

    public virtual void Update(GameTime gameTime) {}
    public virtual void Draw(int xMin, int xMax, int y) {}
    public abstract EStatus ToStatus();
}

public class AP
{
    private int _total;
    public int Total => _total;

    private TextLayer _layer;
    public TextLayer Layer => _layer;

    private readonly Status[] _pieces;

    public AP(int width, TextLayer layer)
    {
        _total = width;
        _layer = layer;
        _pieces = new Status[_total];
        for (var i = 0; i < _total; i++)
        {
            _pieces[i] = new StatusStamina() 
            {
                ActionPoints = this,
            };
        }
    }
    
    public bool FindLeftMost<S>(out int position) where S : Status
    {
        for (var i = 0; i < _total; i++)
        {
            if (_pieces[i] is S)
            {
                position = i;
                return true;
            }
        }

        position = -1;
        return false;
    }
    
    public bool FindRightMost<S>(out int position) where S : Status
    {
        for (var i = _total - 1; i >= 0; i--)
        {
            if (_pieces[i] is S)
            {
                position = i;
                return true;
            }
        }

        position = -1;
        return false;
    }

    public bool FindFirstAfterVoid(out int position)
    {
        Status? previous = null;
        for (var i = 0; i < _total; i++)
        {
            var current = _pieces[i];
            if (previous?.Kind == EStatus.Void && current.Kind != EStatus.Void)
            {
                position = i;
                return true;
            }

            previous = current;
        }

        position = -1;
        return false;
    }

    public void Add<S>(int index) where S : Status, new()
    {
        Status newPiece = new S()
        {
            ActionPoints = this,
        };
        
        while (true)
        {
            if (index < 0 || index >= _total) return;
            
            var p = _pieces[index];
            if (p.Kind is EStatus.Void or EStatus.Stamina)
            {
                _pieces[index] = newPiece;
                return;
            }
            else if (p.Kind == newPiece.Kind)
            {
                var start = index;
                while (_pieces[index].Kind == newPiece.Kind)
                {
                    index--;
                    if (index < 0 && start + 1 <= Total - 1)
                    {
                        _pieces[start + 1] = newPiece;
                        return;
                    }
                    else if (index < 0)
                    {
                        return;
                    }
                }

                var temp = _pieces[index];
                _pieces[index] = newPiece;
                index--;
                newPiece = temp;
            }
            else
            {
                index--;
                if (index < 0) return;
            }
        }        
    }
    
    public void Add<S>() where S: Status, new()
    {
        Add<S>(_total - 1);
    }

    public bool FindFirstVoid(out int position)
    {
        var j = 0;
        var found = true;
        while (_pieces[j].Kind != EStatus.Void)
        {
            j++;
            if (j >= Total - 1)
            {
                position = -1;
                return false;
            }
        }

        if (found)
        {
            position = j;
            return true;
        }
        else
        {
            position = -1;
            return false;
        }
    }

    public bool FindLastStamina(out int position)
    {
        var j = 0;
        var found = true;
        while (_pieces[j].Kind == EStatus.Stamina)
        {
            j++;
            if (j > Total - 1)
            {
                position = 39;
                return true;
            }
        }

        j--;

        if (found)
        {
            position = j;
            return true;
        }
        else
        {
            position = -1;
            return false;
        }
    }
    
    public void Gain(int n)
    {
        for (var i = 0; i < n; i++)
        {
            if (FindFirstVoid(out var p))
            {
                _pieces[p] = new StatusStamina()
                {
                    ActionPoints = this,
                };
            }
            else
            {
                return;
            }
        }
    }

    public void Spend(int n)
    {
        for (var i = 0; i < n; i++)
        {
            if (FindLastStamina(out var p))
            {
                _pieces[p] = new StatusVoid()
                {
                    ActionPoints = this,
                };
            }
            else
            {
                return;
            }
        }
    }

    public bool Contains<S>() where S: Status
    {
        for (var i = 0; i < Total; i++)
        {
            if (_pieces[i] is S) return true;
        }

        return false;
    }

    public int Count<S>() where S : Status
    {
        var count = 0;
        for (var i = 0; i < Total; i++)
        {
            if (_pieces[i] is S) count++;
        }

        return count;
    }

    public void Reduce(int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (FindFirstAfterVoid(out var position))
            {
                _pieces[i] = new StatusVoid()
                {
                    ActionPoints = this,
                };
                
                if (FindLastStamina(out var stamina) && stamina < Total)
                {
                    if (_pieces[stamina + 1] is StatusVoid)
                    {
                        _pieces[stamina + 1] = new StatusStamina()
                        {
                            ActionPoints = this,
                        };
                    }
                }
            }
        }
    }
    
    public void Reduce<S>(int n) where S: Status
    {
        for (int i = 0; i < n; i++)
        {
            if (FindLeftMost<S>(out var position))
            {
                _pieces[position] = new StatusVoid()
                {
                    ActionPoints = this,
                };
                
                if (FindLastStamina(out var stamina) && stamina < Total)
                {
                    if (_pieces[stamina + 1] is StatusVoid)
                    {
                        _pieces[stamina + 1] = new StatusStamina()
                        {
                            ActionPoints = this,
                        };
                    }
                }
            }
        }
    }
    
    public void Draw(int x, int y)
    {
        var layer = _layer;
        for (var i = x - 1; i < x + Total; i++)
        {
            layer.Unset(i, y - 1);
            layer.Unset(i, y);
            layer.Unset(i, y + 1);
            layer.Unset(i, y + 2);
        }
    
        layer.Set(x - 1, y, Glyph.Bw(3, 6));
        for (var i = 0; i < Total; i++)
            layer.Set(x + i, y, Glyph.Bw(4, 6));
        layer.Set(x + Total, y, Glyph.Bw(20, 5));
        
        for (int i = 0; i < Total; i++)
        {
            var current = _pieces[i];
            var j = i + 1;
            if (j < Total)
            {
                while (_pieces[j].Kind == current.Kind)
                {
                    if (j == Total - 1) break;
                    j++;
                }

                current.Draw(x + i, x + j, y);
                i = j;
            }
            else
            {
                current.Draw(x + i, x + i, y);
            }

        }
    }

    public void Update(GameTime gameTime)
    {
        foreach (var bar in _pieces)
        {
            bar.Update(gameTime);
        }
    }

    public Status GetAt(int at)
    {
        return this._pieces[at];
    }

    public void DrawCursor(int i, int y)
    {
        Layer.Set(i, y - 1, new Glyph(13, 5, Color.Black, Color.White));
    }
    
    public (int, int) Points => (Count<StatusStamina>(), Total);

    public void AddN<S>(int n) where S: Status, new()
    {
        for (int i = 0; i < n; i++)
        {
            Add<S>();
        }
    }
}

// public class ActionPoints(int width, TextLayer layer, IStatus def)
// {
//     public int Total { get; } = width;
//     public TextLayer Layer => layer;
//     private readonly List<IStatus> _pieces = new();
//     public (int, int) Points => (Remaining, _empty);
//     
//     private int _empty = width;
//     private int _spent = 0;
//
//     public int Remaining => Math.Max(0, _empty - _spent);
//     public bool Spend(int n)
//     {
//         if (_empty - _spent == 0) return false;
//         
//         if (n > _empty) n = _empty;
//         _spent += n;
//
//         return true;
//     }
//
//     public void Free(int n)
//     {
//         if (n > _spent) n = _spent;
//         _spent -= n;
//     }
//     
//     public void Add<T>(int w) where T : class, IStatus, new()
//     {
//         if (_empty - _spent == 0) return;
//         
//         T piece = null;
//         foreach (var p in _pieces)
//         {
//             if (p is T)
//             {
//                 piece = p as T;
//                 break;
//             }
//         }
//
//         if (w > _empty)
//         {
//             w = _empty;
//         }
//         _empty -= w;
//         
//         if (piece != null)
//         {
//             piece.Width += w;
//             return;
//         }
//         
//         var t = new T
//         {
//             Width = w,
//         };
//         _pieces.Add(t);
//     }
//
//     public bool Contains<T>() where T : class, IStatus
//     {
//         foreach (var p in _pieces)
//         {
//             if (p is T)
//             {
//                 return true;
//             }
//         }
//
//         return false;
//     }
//
//     public int Count<T>() where T : class, IStatus
//     {
//         int n = 0;
//         foreach (var p in _pieces)
//         {
//             if (p is T)
//             {
//                 n += p.Width;
//             }
//         }
//
//         return n;
//     }
//
//     public void Reduce(int n)
//     {
//         for (int i = 0; i < n; i++)
//         {
//             if (_pieces.Count == 0) return;
//             var piece = _pieces.Last();
//             
//             piece.Width -= 1;
//             _empty += 1;
//
//             if (piece.Width == 0)
//             {
//                 _pieces.Remove(piece);
//             }
//         }
//     }
//     
//     public void Reduce<T>(int w) where T : class, IStatus, new()
//     {
//         T piece = null;
//         foreach (var p in _pieces)
//         {
//             if (p is T barPiece)
//             {
//                 piece = barPiece;
//                 break;
//             }
//         }
//         
//         if (piece != null)
//         {
//             if (w > piece.Width) w = piece.Width;
//             piece.Width -= w;
//             _empty += w;
//
//             if (piece.Width == 0)
//             {
//                 _pieces.Remove(piece);
//             }
//         }
//     }
//     
//     public void Draw(int x, int y)
//     {
//         for (var i = x - 1; i <= x + width; i++)
//         {
//             layer.Unset(i, y - 1);
//             layer.Unset(i, y);
//             layer.Unset(i, y + 1);
//             layer.Unset(i, y + 2);
//         }
//         
//         layer.Set(x - 1, y, Glyph.Bw(3, 6));
//         for (var i = 0; i <= width; i++)
//             layer.Set(x + i, y, Glyph.Bw(4, 6));
//
//         for (int i = 0; i < Total; i++)
//         {
//             var current = _pieces[i];
//             var j = i + 1;
//             while (_pieces[j].Kind == current.Kind)
//             {
//                 if (j == Total - 1) break;
//                 j++;
//             }
//             current.Draw(x + i, x + j - 1, y);
//             i = j;
//         }
//         
//         // var xMin = x;
//         // var xMax = xMin + def.Width;
//         // def.Draw(xMin, xMax - 1, y);
//         // xMin += _empty;
//         //
//         // foreach (var piece in _pieces)
//         // {
//         //     xMax = xMin + piece.Width - 1;
//         //     piece.Draw(xMin, xMax, y);
//         //     if (piece.Width > 1)
//         //     {
//         //         layer.Set(xMin, y + 1, Glyph.Bw(0, 6));
//         //     }
//         //
//         //     xMin = xMax + 1;
//         // }
//         //
//         // layer.Set(x + width, y, Glyph.Bw(20, 5));
//     }
//
//     public void Update(GameTime gameTime)
//     {
//         def.Update(gameTime);
//         foreach (var bar in _pieces)
//         {
//             bar.Update(gameTime);
//         }
//     }
//
//     public EStatus GetAt(int at)
//     {
//         var cursor = 0;
//         var next = this.Remaining;
//         if (at >= cursor && at <= next) return def.ToStatus();
//         cursor = next + 1;
//         next += this._spent;
//         if (at >= cursor && at <= next) return EStatus.Void;
//         cursor = next + 1;
//         foreach (var p in _pieces)
//         {
//             next += p.Width;
//             if (at >= cursor && at <= next) return p.ToStatus();
//             cursor = next + 1;
//         }
//
//         throw new Exception("CAN'T BE THIS!");
//     }
//
//     public void DrawCursor(int i, int y)
//     {
//         Layer.Set(i, y - 1, "v");
//     }
// }

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

public class StatusStamina : Status
{
    private float _time = 0;

    public override EStatus Kind => EStatus.Stamina;

    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }

    public override void Draw(int xMin, int xMax, int y)
    {
        var len = Math.Max(xMax - xMin, 1);
        var dx = 1.0f / len;
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(17, 5, Color.Black, Color.Lerp(Color.LightGreen, Color.Green, (i - xMin) * dx)));
        }

        var (ap, tot) = ActionPoints.Points;
        ActionPoints.Layer.Set(xMin, y + 1, $"{ap}/{tot}");
    }

    public override EStatus ToStatus()
    {
        return EStatus.Stamina;
    }
}

public class StatusLuck : Status
{
    public override EStatus Kind => EStatus.Luck;
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(18, 5, Color.Black, Color.White));
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(3, 0, Color.Black, Color.Red));
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Luck;
    }
}

public class StatusWounds : Status
{
    public override EStatus Kind => EStatus.Wound;
    
    public override void Draw(int xMin, int xMax, int y)
    {
        var (ux, uy) = Bars.Offset(xMin, xMax);
        for (int i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(18, 5, Color.Black, i % 2 == 0 ? Color.Red : Color.DarkRed));
        }
        ActionPoints.Layer.Set(xMin + ux, y + uy, new Glyph(3, 0, Color.Black, Color.Red));
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Wound;
    }
}

public class StatusFire : Status
{
    public override EStatus Kind => EStatus.Fire;
    
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.002f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Fire;
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

public class StatusFatigue : Status
{
    public override EStatus Kind => EStatus.Fatigue;
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.0005f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Fatigue;
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

public class StatusInsanity : Status
{
    private float _time = 0;
    
    public override EStatus Kind => EStatus.Insanity;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Insanity;
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

public class StatusPoison : Status
{
    public override EStatus Kind => EStatus.Poison;
    
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.001f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Poison;
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

public class StatusSin : Status
{
    public override EStatus Kind => EStatus.Sin;
    
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.003f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Sin;
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

public class StatusDeath : Status
{
    public override EStatus Kind => EStatus.Death;
    
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.01f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Death;
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

public class StatusFrozen : Status
{
    public override EStatus Kind => EStatus.Frozen;
    
    private float _time = 0;
    
    public override void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds * 0.007f;
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Frozen;
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


public class StatusVoid : Status
{
    public override EStatus Kind => EStatus.Void;
    
    public override void Update(GameTime gameTime)
    {
    }
    
    public override EStatus ToStatus()
    {
        return EStatus.Void;
    }
    
    public override void Draw(int xMin, int xMax, int y)
    {
        for (var i = xMin; i <= xMax; i++)
        {
            ActionPoints.Layer.Set(i, y, new Glyph(4, 6, Color.Black, Color.White));
        }
    }
}
