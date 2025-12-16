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
        if (!character.Hits.IsFull && character.Hits % 2 == 0)
        {
            character.Hits.Up(1);
            var (u, v) = character.Job.GetImage();
            for (int i = 1; i < 5; i++)
            {
                screen.Draw(character.X, character.Y, new Glyph(u, v, Color.Pink, Color.White));
                if (character is PartyMember pm1)
                    screen.DrawParty(toDraw: [pm1], colorOverride: Color.Pink);
                yield return new WaitForSeconds(0.01f * (10 - i));
                
                screen.Draw(character.X, character.Y, "+", Color.White, Color.Pink);
                if (character is PartyMember pm2)
                    screen.DrawParty(toDraw: [pm2]);
                yield return new WaitForSeconds(0.01f * (10 - i));
            }
        }

        yield break;
    }
}