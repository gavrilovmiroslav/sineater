using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SINEATER;

[JsonObject(MemberSerialization.OptIn)]
public class Trait(string name, string shortName, string description) : IAbilitySource
{
    public string Name { get; set; } = name;
    public virtual string ShortName { get; set; } = shortName;
    public string Description { get; set; } = description;
    
    public static List<Type> All = [
        typeof(TraitBalanced),
        typeof(TraitHeavy),
        typeof(TraitPadded),
        typeof(TraitProficient),
        typeof(TraitSkilled),
        typeof(TraitSneaky),
        typeof(TraitWise),
    ];

    public IEnumerable YellName()
    {
        yield return new Present_Notify($"{Name}!");
    }

    public virtual IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyOnStartTurn(IScreen level, ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyOnEndTurn(ICharacter character)
    {
        yield break;
    }
    
    public virtual string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph.Bw(0, 0);
    }
}
public class ItemTrait(string name, string shortName, IItem item, string description) : Trait(name, shortName, description)
{
    
}
public class LimitedTrait(string name, string shortName, int duration, string description) : Trait(name, shortName, description)
{
    public int Duration = duration;

    public virtual IEnumerable ApplyOnExpires(ICharacter character)
    {
        yield break;
    }
    
    public override IEnumerable ApplyOnEndTurn(ICharacter character)
    {
        Duration--;
        if (Duration <= 0)
        {
            yield return ApplyOnExpires(character);
            character.GetTraits().Remove(this);
        }
    }
    
    public override string GetName()
    {
        return $"{Name} ({Duration})";
    }

    public override string ShortName => $"{shortName}{Duration}";
}