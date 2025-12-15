using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Steal : Move
{
    public override string Name { get; } = "Steal";
    public override string Description { get; } = "+WIL movement.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft += character.Wil;
        yield break;
    }
}