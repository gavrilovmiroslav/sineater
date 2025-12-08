using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Bash : Move
{
    public override string Name { get; } = "Bash";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = character.Vig;
        var nondom = (character.IsRightHanded ? character.GetLeftWeapon() : character.GetRightWeapon());
        if (nondom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], []));
        }
        yield break;
    }
}