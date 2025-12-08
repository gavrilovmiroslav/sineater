using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Bite : Move
{
    public override string Name { get; } = "Bite";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 3;
        character.Attacks.Add(new Attack([], []));
        yield break;
    }
}