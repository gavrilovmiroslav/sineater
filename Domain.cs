using System.Collections.Generic;

namespace SINEATER;

public class Domains
{
    public List<Domain> _domains = [];
    public Dictionary<(int, int), Domain> _tiles = [];

    public bool IsInDomain(int x, int y)
    {
        return _tiles.ContainsKey((x, y));
    }

    public Domain GetAt(int x, int y)
    {
        return _tiles[(x, y)];
    }
    
    public void Draw()
    {
        foreach (var dom in _domains)
        {
            dom.Draw();
        }
    }
}

public class Domain
{
    public ICharacter Caster;
    public int X, Y;
    public List<(int, int)> Tiles = [];
    
    public virtual void Draw()
    {}
}