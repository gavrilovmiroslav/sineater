namespace SINEATER;

public class Track(int current, int max = 9)
{
    private int _current = current;
    private int _max = max;

    public static implicit operator Track(int n)
    {
        return new Track(n);
    }

    public static implicit operator int(Track t)
    {
        return t._current;
    }

    public bool IsFull => _current == _max;

    public int Increase(int n)
    {
        _max += n;
        return _max;
    }
    
    public int Decrease(int n)
    {
        _max -= n;
        if (_max < 0)
        {
            _max = 0;
        }
        return _max;
    }
    
    public int Up(int n)
    {
        _current += n;
        if (_current > _max)
        {
            _current = _max;
        }
        
        return _current;
    }

    public int Up(int n, out int f)
    {
        _current += n;
        f = _current;
        if (_current > _max)
        {
            _current = _max;
        }
        
        return _current;
    }

    public int Down(int n)
    {
        _current -= n;
        if (_current < 0)
        {
            _current = 0;
        }

        return _current;
    }
    
    public int Down(int n, out int f)
    {
        _current -= n;
        f = _current;
        if (_current < 0)
        {
            _current = 0;
        }

        return _current;
    }

    public override string ToString()
    {
        return $"{_current}";
    }

    public void Reset()
    {
        _current = _max;
    }
}