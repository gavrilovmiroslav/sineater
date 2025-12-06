using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Walk : Move
{
    public override string Name { get; } = "Walk";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [];

    public override IEnumerable PerformMove(Character character, CombatMapScreen screen)
    {
        character.MovesLeft = character.Stats.Initiative;
        yield break;
    }
}