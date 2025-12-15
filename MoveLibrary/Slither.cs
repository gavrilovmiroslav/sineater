using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Slither : Move
{
    public override string Name { get; } = "Slither";
    public override string Description { get; } = "+INIT + 6 movement.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 6 + character.Stats.Initiative;
        yield break;
    }
}