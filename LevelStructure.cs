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

    public void Initialize(LevelStructure map, Func<LevelStructure, int, int, bool> pred)
    {
        for (int i = 0; i < Count; i++)
        {
            var tiles = GetRoom(i) ?? [];
            var (x, y) = tiles[0];
            Distances[i] = new(map, false, x, y, pred);
        }
    }
    
    public void Initialize(LevelStructure map, Func<LevelStructure, int, int, bool> pred, IEnumerable<(int, int)> sources)
    {
        var xys = sources.ToList();
        for (int i = 0; i < Count; i++)
        {
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
    public (int X, int Y) Entry;
    public readonly List<(int X, int Y)> Starts = [];
    public readonly List<(int X, int Y)> Goals = [];
    public readonly List<Enemy> Enemies = [];
    public readonly List<(int X, int Y)> Treasure = [];
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
        
        UnionFind.Perform(walkable, this, Predicate.Walkable, ref Walkables);
        Walkables.Initialize(this, Predicate.Walkable);

        Obstacles = new TiledStructure();
        UnionFind.Perform(obstacle, this, Predicate.Obstacle, ref Obstacles);
        Obstacles.Initialize(this, Predicate.Obstacle, Walkables.TilesInRooms.Keys);
        
        Walkables.Initialize(this, Predicate.Walkable, Obstacles.TilesInRooms.Keys);
        var largest = 0;
        if (Walkables.Count > 1)
        {
            var count = 0;
            for (int i = 0; i < Walkables.Count; i++)
            {
                if (Walkables.RoomsByTiles[i].Count > count)
                {
                    largest = i;
                    count = Walkables.RoomsByTiles[i].Count;
                }
            }
        }
        Console.WriteLine($"Largest room = {largest}");
        
        Goals.Clear();
        
        for (int w = 0; w < Walkables.Distances.Count; w++)
        {
            if (w == largest) 
            {
                var dm = Walkables.Distances[w];
                var max = dm.MaxDistance();
                var fov = new FieldOfView<Cell>(Map);

                var pred = (LevelStructure s, int mx, int my) => dm.Get(mx, my) >= 2 && fov.IsInFov(mx, my);
                var s = this;
                var heap = dm.GetAllAt(max).OrderBy(e => dm.Flood(s, pred, e.Item1, e.Item2).ToList().Count).ToList();
                if (heap.Count == 0) continue;
                var start = heap.First();

                Goals.Add(start);
                Walkables.Initialize(this, Predicate.Walkable, [start]);

                Heat = new HeatMap(Map);

                max = Walkables.MaxDistance();
                var far = Walkables.Distances[w].GetAllAt(max - (Rnd.Instance.D4 - 1)).ToList();
                if (far.Count == 0) continue;

                Entry = far[Rnd.Instance.Next(0, far.Count)];
                Heat.PaintPaths(Entry, Goals[0], Color.Green);

                Walkables.Initialize(this, Predicate.Walkable, [Entry]);
                Heat.PaintPaths(Entry, Goals[0], Color.Blue);
                Walkables.Initialize(this, Predicate.Walkable, [Entry, ..Goals, ..Heat.GetAll()]);

                var n = 0;

                do
                {
                    far = Walkables.Distances[w].GetAllAt(Walkables.Distances[w].MaxDistance()).ToList();
                    var (x, y) = far[Rnd.Instance.Next(0, far.Count)];
                    if (Heat.Get(x, y) != Color.Black) break;
                    Goals.Add((x, y));
                    Heat.PaintPaths(Entry, (x, y), new Color(0.2f, 0.2f, 0.0f));

                    Walkables.Initialize(this, Predicate.Walkable, [Entry, ..Goals]);
                    n++;
                    if (n > 5) break;
                } while (true);

                Walkables.Initialize(this, Predicate.Walkable, [Entry]);
                var entrySight = fov.ComputeFov(Entry.Item1, Entry.Item2, 6, false);
                foreach (var (dx, dy) in Walkables.Distances[w].GetAllAt(2))
                {
                    entrySight = fov.AppendFov(dx, dy, 6, false);
                }

                var sight = entrySight.Select(c => (c.X, c.Y)).ToList();

                var goalSet = new HashSet<(int, int)>();
                foreach (var g in Goals)
                {
                    goalSet.Add(g);
                }

                Stack<(int, int, ECrewChoice)> crew = [];
                for (var i = 0; i < Goals.Count; i++)
                {
                    var res = 300;
                    var (gx, gy) = Goals[i];

                    var places = Heat.GetAll()
                        .Where(e => !sight.Contains(e))
                        .Where(e => !goalSet.Contains(e))
                        .OrderBy(xy =>
                            Vector2.Distance(new Vector2(xy.Item1, xy.Item2), new Vector2(gx, gy))).GetEnumerator();

                    var spawned = 0;
                    HashSet<(int, int)> usedEnemySpots = [];
                    
                    while (res > 0)
                    {
                        Enemy enm;
                        if (crew.Count == 0)
                        {
                            for (var l = 5; l > 0; l--)
                            {
                                if (!Bestiary.Levels.ContainsKey(l)) continue;
                                enm = Bestiary.Levels[l].ToList()[Rnd.Instance.Next(0, Bestiary.Levels[l].Count)]();
                                var cost = (enm.Stats.Score + enm.HP) * enm.Sin;
                                if (res < cost)
                                {
                                    res -= 10;
                                    continue;
                                }

                                res -= cost;
                                do
                                {
                                    places.MoveNext();
                                } while (usedEnemySpots.Contains(places.Current));
                                var (x, y) = places.Current;
                                usedEnemySpots.Add((x, y));
                                enm.X = x;
                                enm.Y = y;
                                if (Rnd.Instance.D100 > 30)
                                {
                                    Enemies.Add(enm);
                                    spawned++;
                                    if (enm.CrewChoice != ECrewChoice.None)
                                    {
                                        crew.Push((enm.Crew, l, enm.CrewChoice));
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

                                            //cost = (1 + enm.Crew) * l * 10;
                                            cost = (enm.Stats.Score + enm.HP) * enm.Sin;
                                            if (res > cost)
                                            {
                                                res -= cost;
                                                
                                                do
                                                {
                                                    places.MoveNext();
                                                } while (usedEnemySpots.Contains(places.Current));
                                                var (x, y) = places.Current;
                                                usedEnemySpots.Add((x, y));
                                                enm.X = x;
                                                enm.Y = y;
                                                if (Rnd.Instance.D100 > 30)
                                                {
                                                    Enemies.Add(enm);
                                                    spawned++;
                                                    if (enm.CrewChoice != ECrewChoice.None)
                                                    {
                                                        substack.Enqueue((enm.Crew, l, enm.CrewChoice));
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                res -= 10;
                                            }
                                        }

                                        break;
                                    case ECrewChoice.Companion:
                                        enm = Bestiary.Levels[l].ToList()[
                                            Rnd.Instance.Next(0, Bestiary.Levels[l].Count)]();
                                        //cost = (1 + enm.Crew) * 10;
                                        cost = (enm.Stats.Score + enm.HP) * enm.Sin;
                                        if (res > cost)
                                        {
                                            res -= cost;
                                            do
                                            {
                                                places.MoveNext();
                                            } while (usedEnemySpots.Contains(places.Current));
                                            var (x, y) = places.Current;
                                            usedEnemySpots.Add((x, y));
                                            enm.X = x;
                                            enm.Y = y;
                                            if (Rnd.Instance.D100 > 30)
                                            {
                                                Enemies.Add(enm);
                                                spawned++;
                                                if (enm.CrewChoice != ECrewChoice.None)
                                                {
                                                    substack.Enqueue((enm.Crew, l, enm.CrewChoice));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            res -= 10;
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
                    Walkables.Initialize(this, Predicate.Walkable, [..Enemies.Select(e => (e.X, e.Y)), ..Treasure, Goals[0]]);
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
                    Walkables.Initialize(this, Predicate.Walkable,
                        [Entry, ..Enemies.Select(e => (e.X, e.Y)), ..Treasure, Goals[0]]);
                    var en = new Vector2(Entry.Item1, Entry.Item2);
                    var t = Walkables.Distances[w].GetAllAt(Walkables.Distances[w].MaxDistance())
                        .OrderByDescending(t => Vector2.Distance(new Vector2(t.Item1, t.Item2), en)).ToList();
                    if (t.Count > 0)
                    {
                        Treasure.Add(t[Rnd.Instance.Next(0, t.Count)]);
                    }
                }
            }
            else
            {
                var dm = Walkables.RoomsByTiles[w].ToList() ?? [];
                dm.Shuffle();
                for (var i = 0; i < Math.Min(dm.Count - 1, 1 + Rnd.Instance.D6); i++)
                {
                    Console.WriteLine($"ADDED SECONDARY TREASURE TO {dm[i]}");
                    Treasure.Add(dm[i]);
                }
            }
        }
        
        Walkables.Initialize(this, Predicate.Walkable, [Entry]);
        var wd = Walkables.Distances[largest];
        Starts.Add(Entry);

        for (var i = 2; i <= 5; i++)
        {
            if (Starts.Count < 4)
            {
                var treasure = Treasure;
                foreach (var xy in wd.GetAllAt(i).Where(t => !treasure.Contains(t)))
                {
                    Starts.Add(xy);
                    if (Starts.Count == 4) break;
                }
            }
            if (Starts.Count == 4) break;
        }

        if (Starts.Count < 4)
        {
            foreach (var xy in wd.GetAllAt(1))
            {
                Starts.Add(xy);
                if (Starts.Count == 4) break;
            }
        }
    }
}
