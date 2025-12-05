using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Steal : Move
{
    public override string Name { get; } = "Steal";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [];

    public override IEnumerable PerformMove(Character character)
    {
        yield break;
    }
}