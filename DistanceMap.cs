using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;

namespace SINEATER;

public class DistanceMap
{
    private readonly Dictionary<(int, int), int> _distances = [];
    private readonly Dictionary<int, List<(int, int)>> _buckets = [];
    
    public DistanceMap(IMap<Cell> map, bool diagonals, int x, int y, Func<IMap<Cell>, int, int, bool> pred)
    {
        CreateDistanceMap(map, diagonals, [(x, y)], pred);
    }
    
    public DistanceMap(IMap<Cell> map, bool diagonals, IEnumerable<(int, int)> sources, Func<IMap<Cell>, int, int, bool> pred)
    {
        CreateDistanceMap(map, diagonals, sources, pred);
    }

    public IEnumerable<(int, int)> Flood(IMap<Cell> map, Func<IMap<Cell>, int, int, bool> pred, int x, int y, bool diagonals = false)
    {
        List<(int, int)> result = [];
        Queue<(int, int)> waitList = [];
        HashSet<Cell> visited = [];
        
        waitList.Enqueue((x, y));
        if (pred(map, x, y))
        {
            result.Add((x, y));
        }

        while (waitList.TryDequeue(out var xy))
        {
            var (a, b) = xy;
            foreach (var adj in map.GetAdjacentCells(a, b, diagonals).Where(adj => !visited.Contains(adj)))
            {
                if (adj != null && pred(map, adj.X, adj.Y))
                {
                    result.Add((adj.X, adj.Y));
                    visited.Add(adj);
                    waitList.Enqueue((adj.X, adj.Y));
                }
            }
        }

        return result;
    }
    
    void CreateDistanceMap(IMap<Cell> map, bool diagonals, IEnumerable<(int, int)> sources, Func<IMap<Cell>, int, int, bool> pred)
    {
        Queue<(int, int)> waitList = [];

        foreach (var (mx, my) in sources)
        {
            if (pred.Invoke(map, mx, my))
            {
                waitList.Enqueue((mx, my));
                _distances[(mx, my)] = 0;
                _buckets[0] = [(mx, my)];
            }
            else
            {
                waitList.Enqueue((mx, my));
            }
        }

        while (waitList.TryDequeue(out var xy))
        {
            var (x, y) = xy;
            var d = _distances.ContainsKey((x, y)) ? _distances[(x, y)] : 0;
            foreach (var adj in map.GetAdjacentCells(x, y, diagonals))
            {
                if (adj != null && pred(map, adj.X, adj.Y)) 
                {
                    var axy = (adj.X, adj.Y);
                    if (!_distances.ContainsKey(axy))
                    {
                        waitList.Enqueue(axy);
                        _distances[axy] = d + 1;
                        if (!_buckets.ContainsKey(d + 1))
                        {
                            _buckets[d + 1] = [];
                        }
                        
                        _buckets[d + 1].Add(axy);
                    }
                }
            }
        }
    }

    public int MaxDistance()
    {
        if (_distances.Values.Count == 0) return 0;
        return _distances.Values.Max();
    }
    
    public int Get(int x, int y)
    {
        return _distances.GetValueOrDefault((x, y), -1);
    }

    public IEnumerable<(int, int)> GetAllAt(int distance)
    {
        if (_buckets.TryGetValue(distance, out var value))
        {
            return value;
        }

        return [];
    }

    public IEnumerable<(int, int)> GetAll()
    {
        foreach (var (k, v) in _buckets)
        {
            foreach (var e in v)
            {
                yield return e;
            }
        }
    }

    public IEnumerable<(int, int)> GetAllBeneath(int distance)
    {
        for (var i = 0; i < distance; i++)
        {
            foreach (var t in GetAllAt(i))
                yield return t;
        }
    }
}