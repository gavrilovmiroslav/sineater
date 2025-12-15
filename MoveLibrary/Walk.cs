using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Walk : Move
{
    public override string Name { get; } = "Walk";
    public override string Description { get; } = "+INIT + 2 movement.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 2 + character.Stats.Initiative;
        yield break;
    }
}