using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

public static class EStatusExtensions
{
    private static int[] _frames = [0, 1, 0, 1, 2, 1, 0, 0, 1, 2, 3, 2, 1, 0, 1, 2, 3, 4, 4, 3, 2, 1, 0];
    private static int[] _voids = [0, 0, 0, 1, 2, 3, 4, 5, 6, 0, 0, 0, 0, 0];
    
    public static Glyph GetGlyph(this EStatus status, int index, int time)
    {
        var glyph = new Glyph(0, _frames[(index + time) % _frames.Length], Color.Black, Color.White);
        
        switch (status)
        {
            case EStatus.Void:
                glyph.U = 1;
                glyph.V = _voids[(index + time) % _voids.Length];
                break;
            case EStatus.Stamina:
                glyph.U = 2;
                break;
            case EStatus.Wound:
                glyph.U = 3;
                break;
            case EStatus.Insanity:
                glyph.U = 4;
                break;
            case EStatus.Frozen:
                glyph.U = 5;
                break;
            case EStatus.Poison:
                glyph.U = 6;
                break;
            case EStatus.Sin:
                glyph.U = 7;
                break;
            default:
                glyph.U = 0;
                break;
        }
        
        return glyph;
    }
}

public class AP
{
    private int _start = 0;
    private readonly List<EStatus> _statuses = [];
    
    public List<EStatus> View => _statuses[_start..(_start + Width)];
    
    public int Width { get; set; }
    public int Empty { get; set; }
    public TextLayer Layer { get; set; }

    public AP Copy()
    {
        var ap = new AP(Width, Layer, Empty, this);
        return ap;
    }

    private AP(int width, TextLayer layer, int empty, AP original)
    {
        Width = width;
        Layer = layer;
        Empty = empty;
        
        _start = original._start;
        foreach (var status in original._statuses)
        {
            _statuses.Add(status);
        }
    }

    public AP(AP left, AP right)
    {
        Width = left.Width + right.Width;
        Layer = left.Layer;
        Empty = left.Empty;
        
        for (var i = 0; i < left.Width; i++)
        {
            _statuses.Add(left.GetAt(i));
        }
        
        for (var i = 0; i < right.Width; i++)
        {
            _statuses.Add(right.GetAt(i));
        }
    }
    
    public AP(int width, TextLayer layer, int empty = 0)
    {
        Width = width;
        Layer = layer;
        Empty = empty;
        
        for (var i = 0; i < Width; i++)
        {
            _statuses.Add(EStatus.Stamina);
        }
        
        Add(EStatus.Void, empty);
    }
    
    public void Update(GameTime time)
    {
        
    }

    private float t = 0;
    public void Draw(int x, int y, ICharacter? showDetails = null)
    {
        t += 0.05f;
        
        if (showDetails != null)
        {
            var name = showDetails.GetName();
            Layer.SetRect(new Vector2(x, y), new Vector2(x + name.Length + 1, y + 1), ' ');
            Layer.Set(x, y, name);
            Layer.Set(x, y + 1, $"HP {showDetails.HP}");
            x += name.Length + 1;
        }
        for (int i = 0; i < Width; i++)
        {
            Layer.Set(x + i, y, View[i].GetGlyph((int)(i * i * 2.9f), (int)t));
        }
    }

    public void Add(EStatus status, int amount)
    {
        if (amount == 0) return;
        if (amount < 0)
        {
            Reduce(status, -amount);
            return;
        }
        
        if (status != EStatus.Void)
        {
            var voids = Count(EStatus.Void);
            Reduce(EStatus.Void, amount <= voids ? amount : voids);
        }

        if (Count(status) > 0)
        {
            var index = View.FindLastIndex(s => s == status);
            for (var i = 0; i < amount; i++)
            {
                _statuses.Insert(index + _start, status);
            }
        }
        else
        {
            for (var i = 0; i < amount; i++)
            {
                _statuses.Add(status);
            }
        }

        _start += amount;
    }
    
    public void Reduce(EStatus status, int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            if (_start > 0)
            {
                var index = View.FindLastIndex(s => s == status);
                if (index != -1)
                {
                    _statuses.RemoveAt(index + _start);
                    _start--;
                }
                else
                {
                    break;
                }
            }
            else
            {
                var index = View.FindLastIndex(s => s == status);
                if (index != -1)
                {
                    _statuses[index] = EStatus.Stamina;
                }
                else
                {
                    break;
                }
            }
        }
    }

    public void Spend(int amount)
    {
        var stam = View.Count(s => s == EStatus.Stamina);
        Add(EStatus.Void, Math.Min(amount, stam));
    }
    
    public void Unspend(int amount)
    {
        var voids = View.Count(s => s == EStatus.Void);
        Reduce(EStatus.Void, Math.Min(voids, amount));
    }

    public int Count(EStatus status)
    {
        return View.Count(s => s == status);
    }

    public EStatus GetAt(int x)
    {
        return View[x];
    }
}
