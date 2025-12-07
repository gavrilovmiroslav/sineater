
using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Pray : Move
{
    public override string Name { get; } = "Pray";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [ MoveCost.Fatigue ];

    public override IEnumerable PerformMove(Character character, CombatMapScreen screen)
    {
        character.Bonus.Clarity = character.Stats.Clarity;
        yield break;
    }
}