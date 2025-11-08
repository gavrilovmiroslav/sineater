using System;
using System.Collections.Generic;
using System.Linq;
using RogueSharp;
using Wintellect.PowerCollections;

namespace SINEATER;

public class TiledStructure
{
    public readonly Dictionary<(int, int), int> TilesInRooms = [];
    public readonly MultiDictionary<int, (int, int)> RoomsByTiles = new(false);
    public readonly Dictionary<int, DistanceMap> Distances = [];

    public int MaxDistance()
    {
        var max = 0;
        foreach (var (r, dm) in Distances)
        {
            if (max < dm.MaxDistance())
            {
                max = dm.MaxDistance();
            }
        }

        return max;
    }
    
    public void Add((int, int) xy, int room)
    {
        TilesInRooms.Add(xy, room);
        RoomsByTiles.Add(room, xy);
    }

    public bool Contains((int, int) xy)
    {
        return TilesInRooms.ContainsKey(xy);
    }

    public List<(int, int)>? GetRoom(int n)
    {
        return RoomsByTiles[n].ToList();
    }

    public int GetDistance(int x, int y)
    {
        if (TilesInRooms.ContainsKey((x, y)))
        {
            var room = TilesInRooms[(x, y)];
            return Distances[room].Get(x, y);
        }
        else
        {
            return -1;
        }
    }
    
    public int Count => RoomsByTiles.Count;

    public void Initialize(IMap map, Func<IMap<Cell>, int, int, bool> pred)
    {
        for (int i = 0; i < Count; i++)
        {
            var tiles = GetRoom(i) ?? [];
            var (x, y) = tiles[0];
            Distances[i] = new(map, false, x, y, pred);
        }
    }
    
    public void Initialize(IMap map, Func<IMap<Cell>, int, int, bool> pred, IEnumerable<(int, int)> sources)
    {
        var xys = sources.ToList();
        for (int i = 0; i < Count; i++)
        {
            var tiles = GetRoom(i) ?? [];
            Distances[i] = new(map, false, xys, pred);
        }
    }
}

public record struct LevelStructure
{
    public readonly IMap Map;
    public readonly TiledStructure Walkables;
    public readonly TiledStructure Obstacles;
    public readonly List<TiledStructure> Rooms = [];
    public (int, int) Entry;
    public readonly List<(int, int)> Goals = [];
    public readonly HeatMap Heat;
    
    public LevelStructure(IMap map)
    {
        Map = map;
                
        var walkable = new Queue<(int, int)>();
        var obstacle = new Queue<(int, int)>();
        
        for (var i = 0; i < map.Width; i++)
        {
            for (var j = 0; j < map.Height; j++)
            {
                if (Map.IsWalkable(i, j))
                {
                    walkable.Enqueue((i, j));
                }
                else
                {
                    obstacle.Enqueue((i, j));
                }
            }
        }

        Walkables = new TiledStructure();
        var walkablePred = (IMap<Cell> m, int x, int y) => m.IsWalkable(x, y);
        var obstaclePred = (IMap<Cell> m, int x, int y) => !m.IsWalkable(x, y);
        UnionFind.Perform(walkable, map, walkablePred, ref Walkables);
        Walkables.Initialize(map, walkablePred);

        Obstacles = new TiledStructure();
        UnionFind.Perform(obstacle, map, obstaclePred, ref Obstacles);
        Obstacles.Initialize(map, obstaclePred, Walkables.TilesInRooms.Keys);
        
        Walkables.Initialize(map, walkablePred, Obstacles.TilesInRooms.Keys);
        if (Walkables.Count > 1)
        {
            Console.WriteLine("MORE THAN ONE ROOM! BE AFRAID");
        }
        
        var max = Walkables.MaxDistance();
        var dm = Walkables.Distances[0];
        var fov = new FieldOfView<Cell>(Map);
        var pred = (IMap<Cell> mp, int mx, int my) => dm.Get(mx, my) >= 2 && fov.IsInFov(mx, my);
        
        var start = dm.
            GetAllAt(max).
            OrderBy(e => dm.Flood(map, pred, e.Item1, e.Item2).ToList().Count)
            .First();

        Goals.Add(start);
        Walkables.Initialize(map, walkablePred, [start]);
        
        max = Walkables.MaxDistance();
        var far = Walkables.Distances[0].GetAllAt(max - (Rnd.Instance.D4 - 1)).ToList();
        Entry = far[Rnd.Instance.Next(0, far.Count)];
        Walkables.Initialize(map, walkablePred, [Entry]);
        
        Heat = new HeatMap(Map);
        Heat.PaintPaths(Entry, Goals[0], 1);
        Walkables.Initialize(map, walkablePred, [Entry, Goals[0], ..Heat.GetAll()]);
    }
}
