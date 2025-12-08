using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class OpenDomain : Move
{
    public override string Name { get; } = "Open Domain";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [ EMoveCost.Sin ];

    protected override IEnumerable MoveAction(Character c, CombatMapScreen screen)
    {
        yield return new DomainExpansion().Use(screen, c, c.X, c.Y);
    }
}