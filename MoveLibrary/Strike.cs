using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Strike : Move
{
    public override string Name { get; } = "Strike";
    public override string Description { get; } = "";
    public override MoveCost[] Costs { get; } = [];

    public override IEnumerable PerformMove(Character character, CombatMapScreen screen)
    {
        var dom = (character.IsRightHanded ? character.GetRightWeapon() : character.GetLeftWeapon());
        if (dom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], new StatsScaling(), new StatusScaling(), null));
        }
        else
        {
            character.Attacks.Add(new Attack([], new StatsScaling(), new StatusScaling(), null));
        }
        yield break;
    }
}