using System;
using RogueSharp;

namespace SINEATER;

public static class Predicate
{
    public static Func<LevelStructure, int, int, bool> Walkable = (LevelStructure s, int x, int y) => s.Map.IsWalkable(x, y);
    public static Func<LevelStructure, int, int, bool> Obstacle = (LevelStructure s, int x, int y) => !s.Map.IsWalkable(x, y);
    
    public static Func<Cell, (int, int)> CellToPosition = cell => (cell.X, cell.Y);
}