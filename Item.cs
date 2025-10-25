using SINEATER.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SINEATER;

public interface IItem : IAbilitySource
{
    public (int, int) Picture { get; }
    public string Name { get; }
    public Glyph Glyph { get; }
    public bool CanBeUsed();
    public bool CanBeShattered();
    public IEnumerable ApplyItemUsed(ICharacter character);
    public IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character);
    public IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y);
    public IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y);
}

public class Pile : IAbilitySource, IItem
{
    public List<IItem> Things { get; private set; } = []; 
    
    public (int, int) Picture => ItemLibrary.EmptyUv;
    public string Name { get; } = "Pile";
    public Glyph Glyph { get; } = Glyph.Bw(1, 1);
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
        for (var i = Things.Count - 1; i >= 0; i--)
        {
            var thing = Things[i];
            
            var (isSuccess, _) = character.Inventory.Put(thing);
            if (isSuccess)
            {
                Things.RemoveAt(i);
            }
        }

        if (Things.Count == 0)
        {
            level.Floor.Remove((x, y));
        }

        yield break;
    }

    public virtual IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
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

    public IEnumerable ApplyItemShattered(int X, int Y)
    {
        yield break;
    }

    public string GetName()
    {
        return Name;
    }

    public Glyph GetIcon()
    {
        return Glyph.Bw(1, 1);
    }
}
[DataContract]
public class Item(string name, (int, int) uv) : IItem
{
#region Serialization
    [DataMember]
    public string Name { get; set; } = name;
    [DataMember]
    public virtual (int, int) Picture { get; set; } = (uv.Item1, uv.Item2);

    //// ISerializableObject
    //public override void OnCreate()
    //{
    //}
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

    public virtual IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        var (isSuccess, _) = character.Inventory.Put(this);
        if (isSuccess)
        {
            if (level.Floor.ContainsKey((x, y)))
            {
                var onFloor = level.Floor[(x, y)];
                if (onFloor == this)
                {
                    level.Floor.Remove((x, y));
                }
            }
        }

        yield break;
    }

    public virtual IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
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

    public virtual IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
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
}

public class ItemStack(Item item, int count) : Item(item.Name, item.Picture)
{
    public override (int, int) Picture => item.Picture;
    public override Glyph Glyph => item.Glyph;

    public override bool CanBeUsed()
    {
        if (count == 0) return false;
        return item.CanBeUsed();
    }

    public override bool CanBeShattered()
    {
        if (count == 0) return false;
        return item.CanBeShattered();
    }

    public override IEnumerable ApplyItemUsed(ICharacter character)
    {
        if (count == 0) yield break;
        foreach (var i in item.ApplyItemUsed(character))
        {
            yield return i;
        }
    }

    public override IEnumerable ApplyItemPickedUp(CombatMapScreen level, int x, int y, ICharacter character)
    {
        if (count == 0) yield break;
        foreach (var i in item.ApplyItemPickedUp(level, x, y, character))
        {
            yield return i;
        }
    }

    public override IEnumerable ApplyItemLanded(CombatMapScreen level, int x, int y)
    {
        if (count == 0) yield break;
        foreach (var i in item.ApplyItemLanded(level, x, y))
        {
            yield return i;
        }
    }

    public override IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        if (count == 0) yield break;
        foreach (var i in item.ApplyItemShattered(level, x, y))
        {
            yield return i;
        }
    }

    public override string GetName()
    {
        return $"{count}x {item.Name}";
    }

    public override Glyph GetIcon()
    {
        return item.Glyph;
    }
}