using System;
using Microsoft.Xna.Framework;

namespace SINEATER;

public interface ILocation
{
    public Glyph GetIcon(int x, int y);
    public bool Transparent();
    public bool Walkable();
}

public abstract class Location : ILocation
{
    public virtual bool Transparent()
    {
        return true;
    }

    public virtual bool Walkable()
    {
        return true;
    }
    
    public abstract Glyph GetIcon(int x, int y);
}

public class LocationForest : Location
{
    private static readonly (int, int)[] _trees = [ (11, 65), (11, 64), (12, 64), (14, 64), (15, 64)];
    public override Glyph GetIcon(int x, int y)
    {
        var (u, v) = _trees[(x + y) % _trees.Length];
        var c = Color.Lerp(Color.Green, Color.Orange, (float)x / 26.0f);
        return new Glyph(u, v, Color.Black, Color.Lerp(Color.White, c, MathF.Min(1.0f, 0.2f + ((float)(x * 27.61f + y * 14.42f) % 100) / 100.0f)));
    }

    public override bool Transparent()
    {
        return false;
    }
}

public class LocationHill : Location
{
    private static (int, int)[] _images = [ (13, 64), (10, 65)];
    public override Glyph GetIcon(int x, int y)
    {
        var (u, v) = _images[(x + y) % _images.Length];
        var c = Color.Lerp(Color.Brown, Color.Red, (float)x / 26.0f);
        return new Glyph(u, v, Color.Black, Color.Lerp(Color.White, c, MathF.Min(1.0f, 0.2f + ((float)(x * 27.61f + y * 14.42f) % 100) / 100.0f)));
    }

    public override bool Transparent()
    {
        return false;
    }
}

public class LocationCave : Location
{
    public override Glyph GetIcon(int x, int y)
    {
        return Glyph.Bw(0, 49);
    }
    
    public override bool Transparent()
    {
        return false;
    }
}

public class LocationNPC : Location
{
    public override Glyph GetIcon(int x, int y)
    {
        return Glyph.Bw(15, 17);
    }
}

public class LocationPillar : Location
{
    public override bool Transparent()
    {
        return false;
    }
    
    public override bool Walkable()
    {
        return false;
    }

    public override Glyph GetIcon(int x, int y)
    {
        return new Glyph(3, 14, Color.Black, Color.DarkGray);
    }
}