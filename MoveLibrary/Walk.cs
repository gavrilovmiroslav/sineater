using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Walk : Move
{
    public override string Name { get; } = "Walk";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = character.Stats.Initiative;
        yield break;
    }
}