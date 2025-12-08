using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Chop : Move
{
    public override string Name { get; } = "Chop";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = character.Vig;
        var dom = (character.IsRightHanded ? character.GetRightWeapon() : character.GetLeftWeapon());
        if (dom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], [EStatus.Wound]));
        }
        yield break;
    }
}