
using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Pray : Move
{
    public override string Name { get; } = "Pray";
    public override string Description { get; } = "Requires 1 Fatigue.\nDouble CLA for one turn.\nGives 1 Insanity.";
    public override EStatus[] Costs { get; } = [ EStatus.Fatigue ];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.Bonus.Clarity = character.Stats.Clarity;
        character.AP.Add(EStatus.Insanity, 1);
        yield break;
    }
}