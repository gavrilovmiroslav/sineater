using System;
using System.Collections.Generic;
using RogueSharp;

namespace SINEATER;

public static class UnionFind
{
    public static void Perform(Queue<(int, int)> queue, IMap map, Func<IMap, int, int, bool> pred, ref TiledStructure structure)
    {
        int index = 0;
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            if (structure.Contains((x, y)))
            {
                continue;
            }
            
            structure.Add((x, y), index);
            var walk = new Queue<(int, int)>();
            walk.Enqueue((x, y));

            while (walk.Count > 0)
            {
                var (ox, oy) = walk.Dequeue();
                for (var dx = -1; dx < 2; dx++)
                {
                    for (var dy = -1; dy < 2; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var (nx, ny) = (ox + dx, oy + dy);
                        if (nx >= 0 && nx < map.Width && ny >= 0 && ny < map.Height)
                        {
                            if (!structure.Contains((nx, ny)) && pred.Invoke(map, nx, ny))
                            {
                                structure.Add((nx, ny), index);
                                walk.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }
            
            index++;
        }
    }
}