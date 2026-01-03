using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SINEATER;

[AttributeUsage(AttributeTargets.Class)]
public class MoveAttribute : Attribute {}

public abstract class Move
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract EStatus[] Costs { get; }

    public bool CanPerform(Character character, CombatMapScreen screen)
    {
        return false;// character.CanPay(Costs);
    }

    public IEnumerable<IEnumerable> Perform(Character character, CombatMapScreen screen, bool realResources = true)
    {
        if (CanPerform(character, screen))
        {
            if (realResources)
            {
                //yield return character.Pay(Costs);
            }

            yield return MoveAction(character, screen);            
        }

        yield break;
    }
    
    protected abstract IEnumerable MoveAction(Character character, CombatMapScreen screen);
}

public class Moves
{
    public Dictionary<string, Move> Library = [];

    public Move Get(string name)
    {
        if (Library.TryGetValue(name, out Move move))
        {
            return move;
        }
        else
        {
            Console.WriteLine($"!!!! MOVE {name} MISSING!");
            return Library["Whack"];
        }
    }
    
    public Moves()
    {
        foreach (var mv in AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(t => t.GetTypes())
                     .Where(t => t.IsClass && t.BaseType == typeof(Move)))
        {
            var instance = mv.Assembly.CreateInstance(mv.FullName) as Move;
            Library.Add(instance.Name, instance);
        }
    }
}