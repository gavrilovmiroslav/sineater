using Newtonsoft.Json;
using SINEATER.Localization;
using SINEATER.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SINEATER;

public interface IItem : IAbilitySource
{
    public (int, int) Picture { get; }
    public string Name { get; }
    public Glyph Glyph { get; }
    public EWeightClass Weight { get; }
    public EElement Element { get; }
    public List<ITrait> Traits { get; set; }

    public IEnumerable ApplyItemEquipped(ICharacter character);
    public IEnumerable ApplyItemUnequipped(ICharacter character);

    public virtual void AffectOffense(ref Damage dmg)
    {
        foreach (var trait in Traits)
            trait.AffectOffense(ref dmg);
    }

    public virtual void AffectDefense(ref Damage dmg)
    {
        foreach (var trait in Traits)
            trait.AffectDefense(ref dmg);
    }

    public virtual void AffectDamage(ref Damage dmg)
    {
        foreach (var trait in Traits)
            trait.AffectDamage(ref dmg);
    }

    public virtual void AffectStatuses(ref Damage dmg)
    {
        foreach (var trait in Traits)
            trait.AffectStatuses(ref dmg);
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class Item(string name, (int, int) uv, EElement element = EElement.None, EWeightClass weight = EWeightClass.Medium) : ICloneable, IItem
{
    public void Copy(Item original)
    {
        this.Name = original.Name;
        this.Picture = original.Picture;
    }
    
    public static Item Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR ITEM {name}");
        return new Item($"!{name}", (0, 0));
    }
    
    ~Item()
    {
        if (ItemLibrary.InstancedItems.ContainsKey(Name))
        {
            ItemLibrary.InstancedItems.Remove(Name, this);
        }
    }

#region Serialization
    [JsonProperty]
    public string Name { get; set; } = name;
    [JsonProperty]
    public LocalizedString LocaName { get; set; } = new LocalizedString(name);
    [JsonProperty]
    public virtual (int, int) Picture { get; set; } = (uv.Item1, uv.Item2);
    [JsonProperty]
    public virtual EWeightClass Weight { get; set; } = weight;
    [JsonProperty]
    public virtual EElement Element { get; set; } = element;

    public List<ITrait> Traits { get; set; } = [];

    #endregion // Serialization

    public virtual Glyph Glyph => Glyph.Bw(0, 0);

    public virtual bool CanBeUsed()
    {
        return true;
    }

    public virtual bool CanBeShattered()
    {
        return false;
    }

    public virtual IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyItemEquipped(ICharacter character)
    {
        yield break;
    }
    
    public virtual IEnumerable ApplyItemUnequipped(ICharacter character)
    {
        yield break;
    }
    
    public virtual string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph;
    }

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}