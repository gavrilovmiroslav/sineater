
using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Pray : Move
{
    public override string Name { get; } = "Pray";
    public override string Description { get; } = "Requires 1 Fatigue.\nDouble CLA for one turn.";
    public override EStatus[] Costs { get; } = [ EStatus.Fatigue ];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.Bonus.Clarity = character.Stats.Clarity;
        yield break;
    }
}