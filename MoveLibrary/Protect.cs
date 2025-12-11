using System.Collections;
using System.Collections.Generic;

namespace SINEATER.MoveLibrary;

[Move]
public class Protect : Move
{
    public override string Name { get; } = "Protect";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = []; //[ EMoveCost.Fatigue, EMoveCost.Insanity ]; // FI

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