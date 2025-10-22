using System.Collections;
using System.Runtime.Serialization;

namespace SINEATER;

[DataContract]
public class Armor(string name, int guard, EWeightClass weight, int quality, (int, int) uv) : IEquippable, IItem
{
    [DataMember]
    public string Name { get; set; } = name;
    [DataMember]
    public EWeightClass Weight { get; set; } = weight;
    [DataMember]
    public int Quality { get; set; } = quality;
    [DataMember]
    public (int, int) Picture { get; set; } =  (uv.Item1, uv.Item2); 
    public Glyph Glyph => Glyph.Bw(8, 68);
    
    public bool CanBeUsed()
    {
        return false;
    }
    
    public bool CanBeShattered()
    {
        return false;
    }

    public IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        if (character is PartyMember chr)
        {
            if (chr.Armor == null || chr.Armor.Guard < this.Guard)
            {
                if (chr.Armor != null)
                {
                    character.Inventory.Put(chr.Armor);
                }
                chr.Armor = this;
            }
            else
            {
                character.Inventory.Put(this);
            }
        }
        
        yield break;
    }

    public IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
    {
        if (level.Floor.ContainsKey((x, y)))
        {
            var onFloor = level.Floor[(x, y)];
            if (onFloor is Pile pile)
            {
                pile.Things.Add(this);
            }
            else
            {
                var heap = new Pile();
                heap.Things.Add(onFloor);
                heap.Things.Add(this);
                level.Floor[(x, y)] = heap;
            }
        }
        else
        {
            level.Floor[(x, y)] = this;
        }

        yield break;
    }

    public IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        yield break;
    }

    public int Guard{ get; set; } = guard;
    public override string ToString()
    {
        return $"{Name} ({Guard}{Weight.Short()})";
    }

    public string ToLongString()
    {
        return $"{Name} (Guard: {Guard}, Weight: {Weight.ToString()})";
    }
    public Glyph GetIcon()
    {
        return Glyph;
    }

    public string GetName()
    {
        return Name;
    }
}