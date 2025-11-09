using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
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
    public readonly List<Enemy> Enemies = [];
    public readonly List<(int, int)> Treasure = [];
    public readonly HeatMap Heat;
    
    public LevelStructure(IMap map)
    {
        Enemies.Clear();
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
        
        var watch = new Stopwatch();
        
        var max = Walkables.MaxDistance();
        for (int w = 0; w < Walkables.Distances.Count; w++)
        {
            var dm = Walkables.Distances[w];
            var fov = new FieldOfView<Cell>(Map);
            var pred = (IMap<Cell> mp, int mx, int my) => dm.Get(mx, my) >= 2 && fov.IsInFov(mx, my);

            var heap = dm.GetAllAt(max).OrderBy(e => dm.Flood(map, pred, e.Item1, e.Item2).ToList().Count);
            if (!heap.Any()) continue;
            var start = heap.First();

            Goals.Clear();
            Goals.Add(start);
            Walkables.Initialize(map, walkablePred, [start]);

            Heat = new HeatMap(Map);
            Heat.PaintPaths(Entry, Goals[0], Color.Green);

            max = Walkables.MaxDistance();
            var far = Walkables.Distances[w].GetAllAt(max - (Rnd.Instance.D4 - 1)).ToList();
            if (far.Count == 0) continue;
            
            Entry = far[Rnd.Instance.Next(0, far.Count)];
            Walkables.Initialize(map, walkablePred, [Entry]);
            Heat.PaintPaths(Entry, Goals[0], Color.Blue);
            Walkables.Initialize(map, walkablePred, [Entry, ..Goals, ..Heat.GetAll()]);

            var n = 0;

            do
            {
                far = Walkables.Distances[w].GetAllAt(Walkables.Distances[w].MaxDistance()).ToList();
                var (x, y) = far[Rnd.Instance.Next(0, far.Count)];
                if (Heat.Get(x, y) != Color.Black) break;
                Goals.Add((x, y));
                Heat.PaintPaths(Entry, (x, y), new Color(0.2f, 0.2f, 0.0f));

                Walkables.Initialize(map, walkablePred, [Entry, ..Goals]);
                n++;
                if (n > 5) break;
            } while (true);

            Walkables.Initialize(map, walkablePred, [Entry]);
            var entrySight = fov.ComputeFov(Entry.Item1, Entry.Item2, 6, false);
            foreach (var (dx, dy) in Walkables.Distances[w].GetAllAt(2))
            {
                entrySight = fov.AppendFov(dx, dy, 6, false);
            }

            var sight = entrySight.Select(c => (c.X, c.Y)).ToList();
            watch.Start();

            var goalSet = new HashSet<(int, int)>();
            foreach (var g in Goals)
            {
                goalSet.Add(g);
            }
            
            Stack<(int, int, ECrewChoice)> crew = [];
            for (var i = 0; i < Goals.Count; i++)
            {
                var res = 300;
                watch.Lap($"goal {i}");
                var (gx, gy) = Goals[i];

                var places = Heat.GetAll()
                    .Where(e => !sight.Contains(e))
                    .Where(e => !goalSet.Contains(e))
                    .OrderBy(xy =>
                        Vector2.Distance(new Vector2(xy.Item1, xy.Item2), new Vector2(gx, gy))).GetEnumerator();

                int spawned = 0;
                while (res > 0)
                {
                    Console.WriteLine(res);
                    Enemy enm;
                    if (crew.Count == 0)
                    {
                        for (var l = 5; l > 0; l--)
                        {
                            if (!Bestiary.Levels.ContainsKey(l)) continue;
                            enm = Bestiary.Levels[l].ToList()[Rnd.Instance.Next(0, Bestiary.Levels[l].Count)]();
                            var cost = (1 + enm.Crew) * l * 10;
                            if (res < cost) continue;

                            res -= cost;
                            places.MoveNext();
                            var (x, y) = places.Current;
                            enm.X = x;
                            enm.Y = y;
                            if (Rnd.Instance.D100 > 30)
                            {
                                Enemies.Add(enm);
                                spawned++;
                                Console.WriteLine($"{enm.Name} (-{cost}) at {x}, {y}");
                                if (enm.CrewChoice != ECrewChoice.None)
                                {
                                    crew.Push((enm.Crew, l, enm.CrewChoice));
                                    //watch.Lap($"done with {enm.Name}");
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        var cost = 0;
                        var (c, l, ch) = crew.Pop();
                        var substack = new Queue<(int, int, ECrewChoice)>();
                        for (int m = 0; m < c; m++)
                        {
                            switch (ch)
                            {
                                case ECrewChoice.None:
                                    break;

                                case ECrewChoice.Minions:
                                    l = l - 1;

                                    if (Bestiary.Levels.ContainsKey(l))
                                    {
                                        enm = Bestiary.Levels[l].ToList()[
                                            Rnd.Instance.Next(0, Bestiary.Levels[l].Count)]();

                                        cost = (1 + enm.Crew) * l * 10;

                                        if (res > cost)
                                        {
                                            res -= cost;
                                            places.MoveNext();
                                            var (x, y) = places.Current;
                                            enm.X = x;
                                            enm.Y = y;
                                            if (Rnd.Instance.D100 > 30)
                                            {
                                                Enemies.Add(enm);
                                                spawned++;
                                                Console.WriteLine($"{enm.Name} (-{cost}) at {x}, {y}");
                                                if (enm.CrewChoice != ECrewChoice.None)
                                                {
                                                    substack.Enqueue((enm.Crew, l, enm.CrewChoice));
                                                }
                                            }
                                        }
                                    }

                                    break;
                                case ECrewChoice.Companion:
                                    enm = Bestiary.Levels[l].ToList()[Rnd.Instance.Next(0, Bestiary.Levels[l].Count)]();
                                    cost = (1 + enm.Crew) * 10;
                                    if (res > cost)
                                    {
                                        res -= cost;
                                        places.MoveNext();
                                        var (x, y) = places.Current;
                                        enm.X = x;
                                        enm.Y = y;
                                        if (Rnd.Instance.D100 > 30)
                                        {
                                            Enemies.Add(enm);
                                            Console.WriteLine($"{enm.Name} (-{cost}) at {x}, {y}");
                                            spawned++;
                                            if (enm.CrewChoice != ECrewChoice.None)
                                            {
                                                substack.Enqueue((enm.Crew, l, enm.CrewChoice));
                                            }
                                        }
                                    }

                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }

                        while (substack.Count > 0)
                        {
                            crew.Push(substack.Dequeue());
                        }
                    }
                }

                Console.WriteLine($"Spawned {spawned} enemies!");
            }

            for (var i = 0; i < 5; i++)
            {
                Walkables.Initialize(map, walkablePred, [..Enemies.Select(e => (e.X, e.Y)), ..Treasure, Goals[0]]);
                var en = new Vector2(Entry.Item1, Entry.Item2);
                var t = Walkables.Distances[w].GetAllAt(1)
                    .OrderByDescending(t => Vector2.Distance(new Vector2(t.Item1, t.Item2), en)).ToList();
                if (t.Count > 0)
                {
                    Treasure.Add(t[Rnd.Instance.Next(0, t.Count)]);
                }
                else
                {
                    t = Walkables.Distances[w].GetAllAt(3)
                        .OrderByDescending(t => Vector2.Distance(new Vector2(t.Item1, t.Item2), en)).ToList();
                    if (t.Count > 0)
                    {
                        Treasure.Add(t[Rnd.Instance.Next(0, t.Count)]);
                    }
                }
            }
            
            for (var i = 0; i < 3; i++)
            {
                Walkables.Initialize(map, walkablePred, [Entry, ..Enemies.Select(e => (e.X, e.Y)), ..Treasure, Goals[0]]);
                var en = new Vector2(Entry.Item1, Entry.Item2);
                var t = Walkables.Distances[w].GetAllAt(Walkables.Distances[w].MaxDistance())
                    .OrderByDescending(t => Vector2.Distance(new Vector2(t.Item1, t.Item2), en)).ToList();
                if (t.Count > 0)
                {
                    Treasure.Add(t[Rnd.Instance.Next(0, t.Count)]);
                }
            }
        }

        watch.End();
    }
}
