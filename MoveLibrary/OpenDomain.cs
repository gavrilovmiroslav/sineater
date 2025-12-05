using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class OpenDomain : Move
{
    public override string Name { get; } = "Open Domain";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [ MoveCost.Sin ];

    public override IEnumerable PerformMove(Character character)
    {
        yield break;
    }
}