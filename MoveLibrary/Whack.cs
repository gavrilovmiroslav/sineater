using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Whack : Move
{
    public override string Name { get; } = "Whack";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];
    
    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft += 2;
        var dom = (character.IsRightHanded ? character.GetRightWeapon() : character.GetLeftWeapon());
        if (dom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], [EStatus.Fatigue], new StatsScaling(), null));
        }
        yield break;
    }
}