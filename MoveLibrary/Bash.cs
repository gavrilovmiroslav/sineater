using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Bash : Move
{
    public override string Name { get; } = "Bash";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [];

    public override IEnumerable PerformMove(Character character)
    {
        yield break;
    }
}