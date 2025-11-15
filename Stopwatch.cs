using System;

namespace SINEATER;

public class Stopwatch
{
    private readonly System.Diagnostics.Stopwatch _stopwatch = new();
    private long _startTime = 0;
    
    public void Start()
    {
        _stopwatch.Start();
        _startTime = _stopwatch.ElapsedMilliseconds;
    }

    public long Stop()
    {
        _stopwatch.Stop();
        var end = _stopwatch.ElapsedMilliseconds;
        var dt = end - _startTime;
        _stopwatch.Restart();
        _startTime = _stopwatch.ElapsedMilliseconds;
        return dt;
    }

    public void Lap(string msg)
    {
        _stopwatch.Stop();
        var end = _stopwatch.ElapsedMilliseconds;
        var dt = end - _startTime;
        Console.WriteLine($"[{dt}ms] {msg}");
        _stopwatch.Restart();
        _startTime = _stopwatch.ElapsedMilliseconds;
    }

    public void End()
    {
        _stopwatch.Stop();
    }
}