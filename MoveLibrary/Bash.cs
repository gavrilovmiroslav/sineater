using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Bash : Move
{
    public override string Name { get; } = "Bash";
    public override string Description { get; } = "+1 movement.\n+1 non-dominant hand attack.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft += 1;
        var nondom = (character.IsRightHanded ? character.GetLeftWeapon() : character.GetRightWeapon());
        if (nondom is {} wpn)
        {
            character.Attacks.Add(new Attack([wpn], []));
        }
        yield break;
    }
}