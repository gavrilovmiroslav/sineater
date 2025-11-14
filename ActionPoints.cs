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
    public static Glyph GetGlyph(this EStatus status, int index, int total)
    {
        var glyph = new Glyph(15, 63, Color.Green, Color.Green);

        switch (status)
        {
            case EStatus.Stamina:
                break;
            case EStatus.Void:
                glyph.Bg = Color.Black;
                glyph.Fg = Color.White;
                glyph.U = 4;
                glyph.V = 6;
                break;
            case EStatus.Wound:
                glyph.Bg = glyph.Fg = Color.Red;
                break;
            case EStatus.Fire:
                glyph.Bg = glyph.Fg = Color.Orange;
                break;
            case EStatus.Fatigue:
                glyph.Bg = glyph.Fg = Color.Pink;
                break;
            case EStatus.Insanity:
                glyph.Bg = glyph.Fg = Color.Yellow;
                break;
            case EStatus.Poison:
                glyph.Bg = glyph.Fg = Color.Purple;
                break;
            case EStatus.Sin:
                glyph.Bg = glyph.Fg = Color.White;
                break;
            case EStatus.Death:
                glyph.Bg = Color.Gray;
                glyph.Fg = Color.Pink;
                glyph.U = 1;
                glyph.V = 0;
                break;
            case EStatus.Frozen:
                glyph.Bg = glyph.Fg = Color.CadetBlue;
                break;
            case EStatus.Luck:
                glyph.Bg = glyph.Fg = Color.YellowGreen;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, null);
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
    public TextLayer Layer { get; set; }

    public AP(int width, TextLayer layer)
    {
        Width = width;
        Layer = layer;
        
        for (var i = 0; i < Width; i++)
        {
            _statuses.Add(EStatus.Stamina);
        }
    }
    
    public void Update(GameTime time)
    {
        
    }
    
    public void Draw(int x, int y, ICharacter? showDetails = null)
    {
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
            Layer.Set(x + i, y, View[i].GetGlyph(i, Width));
        }
    }

    public void Add(EStatus status, int amount)
    {
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

    public bool Spend(int amount)
    {
        if (View.Count(s => s == EStatus.Stamina) >= amount)
        {
            Add(EStatus.Void, amount);
            return true;
        }

        return false;
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
