using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Fly : Move
{
    public override string Name { get; } = "Fly";
    public override string Description { get; } = "+WIL x2 movement.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 2 * character.Stats.Will;
        yield break;
    }
}