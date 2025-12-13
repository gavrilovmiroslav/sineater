using System.Collections;
using System.Collections.Generic;

namespace SINEATER.MoveLibrary;

[Move]
public class Crash : Move
{
    //You get 1 attack with both weapons and greatly raised VIG scaling. Create 3 INSANITY.
    public override string Name { get; } = "Crash";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];//[ EMoveCost.Fatigue, EMoveCost.Insanity ]; // FI

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft += character.Vig;
        List<Weapon> wpns = [];
        if (character.GetLeftWeapon() is {} wl) wpns.Add(wl);
        if (character.GetRightWeapon() is {} wr) wpns.Add(wr);
        if (wpns is [{ Weight: EWeightClass.Large }])
            wpns.Add(wpns[0]);
        
        character.Attacks.Add(new Attack(wpns, [ EStatus.Insanity, EStatus.Insanity, EStatus.Insanity ]));
        yield break;
    }
}