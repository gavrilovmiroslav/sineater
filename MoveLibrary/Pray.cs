
using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Pray : Move
{
    public override string Name { get; } = "Pray";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [ EMoveCost.Fatigue ];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.Bonus.Clarity = character.Stats.Clarity;
        yield break;
    }
}