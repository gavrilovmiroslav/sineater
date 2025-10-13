using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;

namespace SINEATER;

public class Coroutine
{
    internal IEnumerator _enumerator;
    internal Coroutine? _waitingOn = null;

    public Coroutine(IEnumerable method)
    {
        _enumerator = method.GetEnumerator();
    }

    protected Coroutine()
    {
    }

    public static void Consume(IEnumerable en)
    {
        foreach (var e in en) {}
    }
}

public class CoroutineHandler
{
    private List<Coroutine> _coroutines = [];

    public bool IsActive()
    {
        return _coroutines.Count > 0;
    }

    public void Run(IEnumerable cor)
    {
        _coroutines.Add(new Coroutine(cor));
    }
    
    public void Run(Coroutine cor)
    {
        _coroutines.Add(cor);
    }

    public void Update()
    {
        List<Coroutine> toAdd = [];
        List<Coroutine> toDelete = [];
        
        foreach (var cor in _coroutines.Where(cor => cor._waitingOn == null))
        {
            if (cor._enumerator.MoveNext())
            {
                var val = cor._enumerator.Current;
                if (val is Coroutine dep)
                {
                    cor._waitingOn = dep;
                    toAdd.Add(dep);
                }
                else if (val is IEnumerable enm)
                {
                    cor._waitingOn = new Coroutine(enm);
                    toAdd.Add(cor._waitingOn);
                }
            }
            else
            {
                toDelete.Add(cor);
            }
        }

        foreach (var cor in toDelete)
        {
            _coroutines.Remove(cor);
            foreach (var next in _coroutines)
            {
                if (next._waitingOn == cor)
                {
                    next._waitingOn = null;
                }
            }
        }

        foreach (var cor in toAdd)
        {
            _coroutines.Add(cor);
        }
    }

    public void Clear()
    {
        _coroutines.Clear();
    }
}


public class WaitForSeconds(float seconds) : IEnumerable
{
    private int _waitTimeMillis = (int)(seconds * 1000);
    private int _currentTime = 0;
    
    public IEnumerator GetEnumerator()
    {
        while (true)
        {
            _currentTime += SineaterGame.DeltaTime;
            if (_currentTime < _waitTimeMillis)
            {
                yield return null;
            }
            else
            {
                break;
            }
        }
    }
}

public class WaitForKey(Keys key) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        while (true)
        {
            if (KB.HasBeenPressed(key))
            {
                break;
            }

            yield return null;
        }
    }
}

public class FadeOutAndLeaveScreen(float seconds) : IEnumerable
{
    private int _waitTimeMillis = (int)(seconds * 1000);
    private int _currentTime = 0;
    
    public IEnumerator GetEnumerator()
    {
        while (true)
        {
            var dt = SineaterGame.DeltaTime;
            var factor = (float)dt / (float)_waitTimeMillis;
            _currentTime += dt;
            if (_currentTime < _waitTimeMillis)
            {
                foreach (var (_, layer) in SineaterGame.Instance.Layers)
                {
                    layer.Darken(factor);
                }
                yield return null;
            }
            else
            {
                break;
            }
        }
        
        SineaterGame.Instance.ScreenStack.TryPop(out var _);
        if (SineaterGame.Instance.ScreenStack.TryPeek(out var screen))
        {
            screen.Draw(new GameTime());
        }
    }
}

public class FadeOutAndLoadScreen(float seconds, IScreen screen) : IEnumerable
{
    private int _waitTimeMillis = (int)(seconds * 1000);
    private int _currentTime = 0;
    
    public IEnumerator GetEnumerator()
    {
        while (true)
        {
            var dt = SineaterGame.DeltaTime;
            var factor = (float)dt / (float)_waitTimeMillis;
            _currentTime += dt;
            if (_currentTime < _waitTimeMillis)
            {
                foreach (var (_, layer) in SineaterGame.Instance.Layers)
                {
                    layer.Darken(factor);
                }
                yield return null;
            }
            else
            {
                break;
            }
        }
        
        SineaterGame.Instance.ScreenStack.Push(screen);
    }
}

public class ShowPopupWindowAndWaitForKey(Action<SineaterGame, TextLayerBox> content, bool clear = false) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new ShowPopupAndWaitForKey(new Vector2(5, 8), new Vector2(28, 16), content);
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].SetRect(new Vector2(5, 8), new Vector2(28, 16), ' ');
        game.Layers["ascii"].SetRect(new Vector2(5, 8), new Vector2(28 * 2, 16), ' ');
    }
}

public class ShowPopupAndWaitForKey(Vector2 start, Vector2 end, Action<SineaterGame, TextLayerBox> content) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].SetRect(start, end, ' ');
        game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        {
            Top = Glyph.Bw(10, 27),
            Bottom = Glyph.Bw(10, 29),
            Left = Glyph.Bw(9, 28),
            Right = Glyph.Bw(11, 28),
        }, new Corners<Glyph>()
        {
            BottomLeft = Glyph.Bw(11, 30), 
            BottomRight = Glyph.Bw(10, 30), 
            TopLeft = Glyph.Bw(11, 31), 
            TopRight = Glyph.Bw(10, 31),
        });
        content(game, game.Layers["ascii"].Bounds(
            new Vector2(start.X * 2 + 4, start.Y + 1), 
            new Vector2(end.X * 2 - 1, end.Y - 2)));
        game.Layers["ascii"].Set((int)end.X * 2 - 10, (int)end.Y - 2, "<  OK >");
        yield return new WaitForKey(Keys.Space);
        game.Layers["ascii"].Set((int)end.X * 2 - 10, (int)end.Y - 2, "< ... >");
    }
}

public class ShowPopupAndWaitForSeconds(float time, Vector2 start, Vector2 end, Action<SineaterGame, TextLayerBox> content) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].SetRect(start, end, ' ');
        game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        {
            Top = Glyph.Bw(10, 27),
            Bottom = Glyph.Bw(10, 29),
            Left = Glyph.Bw(9, 28),
            Right = Glyph.Bw(11, 28),
        }, new Corners<Glyph>()
        {
            BottomLeft = Glyph.Bw(11, 30), 
            BottomRight = Glyph.Bw(10, 30), 
            TopLeft = Glyph.Bw(11, 31), 
            TopRight = Glyph.Bw(10, 31),
        });
        content(game, game.Layers["ascii"].Bounds(
            new Vector2(start.X * 2 + 4, start.Y + 1), 
            new Vector2(end.X * 2 - 1, end.Y - 2)));
        yield return new WaitForSeconds(time);
    }
}

public class ShowPopupWindowWithPortraitAndWaitForKey((int, int) portrait, Action<SineaterGame, TextLayerBox> content, bool flip = false) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var (u, v) = portrait;
        var start = new Vector2(5, 5);
        var end = new Vector2(23 + 5, 16);
            
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].SetRect(start, end, ' ');
        // 10,11 25,26
        // 10,11 39,40,41
        game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        {
            Top = Glyph.Bw(10, 27),
            Bottom = Glyph.Bw(10, 29),
            Left = Glyph.Bw(9, 28),
            Right = Glyph.Bw(11, 28),
        }, new Corners<Glyph>()
        {
            BottomLeft = Glyph.Bw(11, 30), 
            BottomRight = Glyph.Bw(10, 30), 
            TopLeft = Glyph.Bw(11, 31), 
            TopRight = Glyph.Bw(10, 31),
        });
        
        content(game, game.Layers["ascii"].Bounds(
            new Vector2(start.X * 2 + 15, start.Y + 1), 
            new Vector2(end.X * 2 - 1, end.Y - 2)));
        game.Layers["ascii"].Set((int)end.X * 2 - 10, (int)end.Y - 2, "<OK>");
        game.Layers["portrait"].SetFlip(u, v, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        game.Layers["portrait"].Set(1, 2, Glyph.Bw(u, v));
        yield return new WaitForKey(Keys.Space);
    }
}