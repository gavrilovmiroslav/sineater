using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    }
}