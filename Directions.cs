using System.Collections.Generic;

namespace SINEATER;

public static class Directions
{
    public static Dictionary<(int, int), (int, int)> Images { get; } = new()
    {
        { ( 1,  0), (10, 58) },
        { (-1,  0), ( 9, 58) },
        { ( 0,  1), ( 8, 58) },
        { ( 0, -1), ( 7, 58) },
        { ( 0,  0), ( 9, 59) },
    };

    public static (int X, int Y) GoForwards((int X, int Y) position, (int X, int Y) direction, int n = 1)
    {
        return (position.X + direction.X * n, position.Y + direction.Y * n);
    }
    
    public static (int X, int Y) GoBackwards((int X, int Y) position, (int X, int Y) direction, int n = 1)
    {
        return (position.X - direction.X * n, position.Y - direction.Y * n);
    }
    
    public static (int, int) GoLeft((int, int) position, (int, int) direction)
    {
        switch (direction)
        {
            case (0, 1): return GoForwards(position, (1, 0));
            case (0, -1): return GoForwards(position, (-1, 0));
            case (1, 0): return GoForwards(position, (0, -1));
            case (-1, 0): return GoForwards(position, (0, 1));
            default: return position;
        }
    }
    
    public static (int, int) GoRight((int, int) position, (int, int) direction)
    {
        switch (direction)
        {
            case (0, 1): return GoForwards(position, (-1, 0));
            case (0, -1): return GoForwards(position, (1, 0));
            case (1, 0): return GoForwards(position, (0, 1));
            case (-1, 0): return GoForwards(position, (0, -1));
            default: return position;
        }
    }
}