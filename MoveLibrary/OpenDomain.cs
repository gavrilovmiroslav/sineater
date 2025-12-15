using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class OpenDomain : Move
{
    public override string Name { get; } = "Open Domain";
    public override string Description { get; } = "Requires 1 Sin.\nAttempts to open a destined astral domain.";
    public override EStatus[] Costs { get; } = [ EStatus.Sin ];

    protected override IEnumerable MoveAction(Character c, CombatMapScreen screen)
    {
        yield return new DomainExpansion().Use(screen, c, c.X, c.Y);
    }
}