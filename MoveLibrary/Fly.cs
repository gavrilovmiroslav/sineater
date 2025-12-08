using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Fly : Move
{
    public override string Name { get; } = "Fly";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 2 * character.Stats.Will;
        yield break;
    }
}