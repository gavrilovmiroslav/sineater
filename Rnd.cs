using System;
using System.Collections.Generic;
using RogueSharp.Random;

namespace SINEATER;

public class Rnd : IRandom
{
    public static readonly Rnd Instance = new();
    
    private readonly Random _rand;
    private readonly int _seed;
    
    public int Seed => _seed;

    public Rnd(int seed = 0)
    {
        _seed = seed;
        _rand = new Random(Guid.NewGuid().GetHashCode());
    }

    public float Next01()
    {
        return (float)Next() / (float)int.MaxValue;
    }
    
    public int Next()
    {
        return _rand.Next();
    }

    public int Next(int maxValue)
    {
        return Next(0, maxValue);
    }

    public int Next(int min, int max)
    {
        if (min >= max) return min;
        return _rand.Next(min, max);
    }

    public RandomState Save()
    {
        return new RandomState()
        {
            Seed = [_seed], NumberGenerated = Next()
        };
    }

    public void Restore(RandomState state)
    {
    }

    public int D2 => Next(1, 3);
    public int D4 => Next(1, 5);
    public int D6 => Next(1, 7);
    public int D8 => Next(1, 9);
    public int D10 => Next(1, 11);
    public int D12 => Next(1, 13);
    public int D20 => Next(1, 21);
    public int D100 => Next(1, 101);

    public int[] Bag(Func<int, bool> filter, params int[] dice)
    {
        var result = new List<int>();
        for (var i = 0; i < dice.Length; i++)
        {
            for (var n = 1; n < dice[i]; n++)
                if (filter(n)) result.Add(n);
        }

        var end = result.ToArray();
        end.Shuffle();
        return end;
    }
}