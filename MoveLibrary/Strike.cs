using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Strike : Move
{
    public override string Name { get; } = "Strike";
    public override string Description { get; } = "+2 movement.\n+1 dominant-hand weapon.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft += 2;
        var dom = (character.IsRightHanded ? character.GetRightWeapon() : character.GetLeftWeapon());
        if (dom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], []));
        }
        yield break;
    }
}