using System.Collections;
using System.Collections.Generic;

namespace SINEATER.MoveLibrary;

[Move]
public class Protect : Move
{
    public override string Name { get; } = "Protect";
    public override string Description { get; } = "Requires 1 Fatigue and 1 Insanity.\nGain GUARD from all equipped weapons.";
    public override EStatus[] Costs { get; } = [ EStatus.Fatigue, EStatus.Insanity ]; // FI

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        List<Weapon> wpns = [];
        if (character.GetLeftWeapon() is {} wl) wpns.Add(wl);
        if (character.GetRightWeapon() is {} wr) wpns.Add(wr);
        if (wpns is [{ Weight: EWeightClass.Large }])
            wpns.Add(wpns[0]);

        foreach (var wpn in wpns)
        {
            character.Guard += wpn.Guard;
        }
        yield break;
    }
}