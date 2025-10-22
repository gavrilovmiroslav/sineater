using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SINEATER;

[DataContract]
public class Trait(string name, string shortName, string description) : IAbilitySource
{
    [DataMember]
    public string Name { get; set; } = name;
    [DataMember]
    public virtual string ShortName { get; set; } = shortName;
    [DataMember]
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

    public virtual IEnumerable ApplyOnReceived(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyOnStartTurn(CombatMapScreen level, ICharacter character)
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
[DataContract]
public class LimitedTrait(string name, string shortName, int duration, string description) : Trait(name, shortName, description)
{
    [DataMember]
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