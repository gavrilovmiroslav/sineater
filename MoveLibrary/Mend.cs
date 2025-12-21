using System;
using System.Collections;
using Microsoft.Xna.Framework;

namespace SINEATER.MoveLibrary;

[Move]
public class Mend : Move
{
    public override string Name { get; } = "Mend";
    public override string Description { get; } = "Heals 1 health at the start of every activation.";
    public override EStatus[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        yield break;
    }
}