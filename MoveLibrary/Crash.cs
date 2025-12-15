using System.Collections;
using System.Collections.Generic;

namespace SINEATER.MoveLibrary;

[Move]
public class Crash : Move
{
    //You get 1 attack with both weapons and greatly raised VIG scaling. Create 3 INSANITY.
    public override string Name { get; } = "Crash";
    public override string Description { get; } = "Requires 1 Fatigue and 1 Insanity.\n+VIG movement.\n+1 attack with both hands.\nLarge weapons count as two.\nGives 3 Insanity.";
    public override EStatus[] Costs { get; } = [ EStatus.Fatigue, EStatus.Insanity ]; // FI

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