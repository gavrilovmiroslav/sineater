using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using RogueSharp;

namespace SINEATER;

public class HeatMap
{
    private readonly Dictionary<(int, int), int> _heat = [];
    private readonly IMap<Cell> _map;
    
    public HeatMap(IMap<Cell> map)
    {
        _map = map;
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
    
    public int Get(int x, int y)
    {
        return _heat.GetValueOrDefault((x, y), -1);
    }

    public IEnumerable<(int, int)>? FindPath((int, int) entry, (int, int) goal, IEnumerable<(int, int)>? except = null)
    {
        var g = new GoalMap(_map, false);
        var (ex, ey) = entry;
        var (gx, gy) = goal;
        g.AddGoal(gx, gy, 100);
        g.ClearObstacles();
        if (except != null)
        {
            g.AddObstacles(except.Select(p => new Point(p.Item1, p.Item2)));
        }
        
        var paths = g.TryFindPaths(ex, ey);
        return paths?.OrderBy(p => p.Length)
            .First().Steps
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(gx, gy)) > 3)
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(ex, ey)) > 3)
            .Select(c => (c.X, c.Y));
    }
    
    public List<List<(int, int)>> FindPaths((int, int) entry, (int, int) goal)
    {
        var g = new GoalMap(_map, false);
        var (ex, ey) = entry;
        var (gx, gy) = goal;
        g.AddGoal(gx, gy, 100);
        g.ClearObstacles();
        var paths = g.TryFindPaths(ex, ey);
        if (paths == null) return [];
        
        var obv = paths.Select(p => p.Steps
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(gx, gy)) > 3)
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(ex, ey)) > 3).ToList()
            ).Select(l => l.Select(c => (c.X, c.Y)).ToList()).ToList();

        g.AddObstacles(obv.SelectMany(x => x).Select(x => new Point(x.X, x.Y)).ToList());
        paths = g.TryFindPaths(ex, ey);
        if (paths == null) return obv;
        
        var nonobv = paths.Select(p => p.Steps
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(gx, gy)) > 3)
            .Where(c => Vector2.Distance(new Vector2(c.X, c.Y), new Vector2(ex, ey)) > 3).ToList()
        ).Select(l => l.Select(c => (c.X, c.Y)).ToList()).ToList();
        
        return [..obv, ..nonobv];
    }

    public bool PaintPath((int, int) entry, (int, int) goal, int width = 1)
    {
        return PaintPath(entry, goal, [], width);
    }
    
    public bool PaintPath((int, int) entry, (int, int) goal, IEnumerable<(int, int)>? except, int width = 1)
    {
        var path = FindPath(entry, goal, except);
        if (path == null) return false;
        
        foreach (var c in path.SelectMany(c => _map.GetCellsInCircle(c.Item1, c.Item2, width)).Distinct())
        {
            if (_map.IsWalkable(c.X, c.Y))
            {
                if (_heat.ContainsKey((c.X, c.Y)))
                {
                    _heat[(c.X, c.Y)]++;
                }
                else
                {
                    _heat.Add((c.X, c.Y), 1);
                }
            }
        }
        
        return true;
    }
    
    public bool PaintPaths((int, int) entry, (int, int) goal, int width = 1)
    {
        var paths = FindPaths(entry, goal);
        if (paths.Count == 0) return false;

        foreach (var p in paths)
        {
            foreach (var c in p.SelectMany(c => _map.GetCellsInCircle(c.Item1, c.Item2, width)).Distinct())
            {
                if (_map.IsWalkable(c.X, c.Y))
                {
                    if (_heat.ContainsKey((c.X, c.Y)))
                    {
                        _heat[(c.X, c.Y)]++;
                    }
                    else
                    {
                        _heat.Add((c.X, c.Y), 1);
                    }
                }
            }
        }

        return true;
    }

    public IEnumerable<(int, int)> GetAll()
    {
        foreach (var (k, v) in this._heat)
        {
            if (v != 0)
            {
                yield return k;
            }
        }
    }
}