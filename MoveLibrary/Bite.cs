using System.Collections;

namespace SINEATER.MoveLibrary;

[Move]
public class Bite : Move
{
    IEnumerable Attack(Character atk, Attack attack, Character def, CombatMapScreen screen)
    {
        def.HP -= 1;
        if (def.HP <= 0)
        {
            def.Die();
        }

        yield return new WaitForSeconds(0.5f);
    }
    
    public override string Name { get; } = "Bite";
    public override string Description { get; } = "";
    public override EMoveCost[] Costs { get; } = [];

    protected override IEnumerable MoveAction(Character character, CombatMapScreen screen)
    {
        character.MovementLeft = 5;
        character.Attacks.Add(new Attack([], [], new StatsScaling()));
        yield break;
    }
}