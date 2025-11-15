using System;
using RogueSharp;

namespace SINEATER;

public static class Predicate
{
    public static Func<IMap<Cell>, int, int, bool> Walkable = (IMap<Cell> m, int x, int y) => m.IsWalkable(x, y);
    public static Func<IMap<Cell>, int, int, bool> Obstacle = (IMap<Cell> m, int x, int y) => !m.IsWalkable(x, y);
    
    public static Func<Cell, (int, int)> CellToPosition = cell => (cell.X, cell.Y);
}