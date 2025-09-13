using System;
using Microsoft.Xna.Framework;

namespace SINEATER;

public interface ILocation
{
    public Glyph GetIcon(int x, int y);
    public bool Transparent();
    public bool Walkable();
    public string GetName();
    public bool Visited();
    public void Visit();
}

public abstract class Location : ILocation
{
    private bool _visited = false;
    
    public virtual bool Transparent()
    {
        return true;
    }

    public virtual bool Walkable()
    {
        return true;
    }

    public virtual string GetName()
    {
        return "???";
    }

    public virtual bool Visited()
    {
        return _visited;        
    }

    public void Visit()
    {
        _visited = true;
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

    public override string GetName()
    {
        return "A forest!";
    }

    public override bool Transparent()
    {
        return false;
    }

    public override bool Visited()
    {
        return true;
    }
}

public class LocationTomb : Location
{
    private (int, int)[] _images = [ (3, 2), (10, 10) ];
    public override Glyph GetIcon(int x, int y)
    {
        var (u, v) = _images[(x + y) % _images.Length];
        return new Glyph(u, v, Color.Black, Color.DarkRed);
    }

    public override string GetName()
    {
        return "A tomb...";
    }
    
    public override bool Transparent()
    {
        return false;
    }
}

public class LocationTemple : Location
{
    public override Glyph GetIcon(int x, int y)
    {
        return new Glyph(8, 3, Color.Black, Color.DarkGoldenrod);
    }

    public override string GetName()
    {
        return "A temple...";
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
    
    public override string GetName()
    {
        return "A cave!";
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
        return Glyph.Bw(13, 65);
    }
    
    public override string GetName()
    {
        return "A traveller!";
    }
}

public class LocationTreasure : Location
{
    public override Glyph GetIcon(int x, int y)
    {
        return new Glyph(5, 66, Color.Black, Color.Gold);
    }
    
    public override string GetName()
    {
        return "Treasure!";
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

public class LocationGodhead : Location
{
    private readonly (int, int)[] _images = [ (0, 11), (1, 11), (2, 11), (3, 11), (4, 11), (5, 11) ];
    public override Glyph GetIcon(int x, int y)
    {
        var (u, v) = _images[(x + y) % _images.Length];
        var f = (int)((x + 1) * 3.14f + (y + 1) * 2.22f) % 10 / 10.0f;
        return new Glyph(u, v, Color.Black, Color.Lerp(Color.DarkRed, Color.DarkSlateGray, f));
    }

    public override bool Transparent()
    {
        return false;
    }

    public override bool Walkable()
    {
        return false;
    }
}