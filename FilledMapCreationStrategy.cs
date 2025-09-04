using RogueSharp;
using RogueSharp.MapCreation;

namespace SINEATER;

public class FilledMapCreationStrategy<TMap, TCell>(int width, int height) : IMapCreationStrategy<TMap, TCell>
    where TMap : IMap<TCell>, new()
    where TCell : ICell
{
    public TMap CreateMap()
    {
        var map = new TMap();
        map.Initialize(width, height);
        map.Clear(false, false);
        return map;
    }
}

public class FilledMapCreationStrategy<TMap>(int width, int height) : 
    FilledMapCreationStrategy<TMap, Cell>(width, height),
    IMapCreationStrategy<TMap>,
    IMapCreationStrategy<TMap, Cell>
    where TMap : IMap<Cell>, new()
{ }