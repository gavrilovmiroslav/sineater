using System.Collections.Generic;
using System.Linq;
using RogueSharp;

namespace SINEATER;

public class DistanceMap
{
    private readonly Dictionary<(int, int), int> _distances = [];
    private readonly Dictionary<int, List<(int, int)>> _buckets = [];
    
    public DistanceMap(IMap<Cell> map, bool diagonals, int x, int y)
    {
        CreateDistanceMap(map, diagonals, x, y);
    }

    void CreateDistanceMap(IMap<Cell> map, bool diagonals, int mx, int my)
    {
        Queue<(int, int)> waitList = [];

        if (map.IsWalkable(mx, my))
        {
            waitList.Enqueue((mx, my));
            _distances[(mx, my)] = 0;
            _buckets[0] = [(mx, my)];
        }
        
        while (waitList.TryDequeue(out var xy))
        {
            var (x, y) = xy;
            var d = _distances[(x, y)];
            foreach (var adj in map.GetAdjacentCells(x, y, diagonals))
            {
                if (adj != null && adj.IsWalkable) 
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
}