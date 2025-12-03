using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Strike : Move
{
    public override string Name { get; } = "Strike";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [];

    public override IEnumerable PerformMove()
    {
        yield break;
    }
}