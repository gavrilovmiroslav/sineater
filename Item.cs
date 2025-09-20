using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;

public interface IItem : IAbilitySource
{
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
            
            var (isSuccess, _) = SineaterGame.Instance.Inventory.Put(thing);
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

public class Item(string name) : IItem
{
    public string Name => name;

    public Glyph Glyph => Glyph.Bw(0, 0);

    public bool CanBeUsed()
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
        var (isSuccess, _) = SineaterGame.Instance.Inventory.Put(this);
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

    public virtual IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        yield break;
    }

    public string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph;
    }
}

public class Potion(string name) : Item(name)
{
    public override bool CanBeShattered()
    {
        return true;
    }

    public override string ToString()
    {
        return $"{name} (Potion)";
    }

    public override Glyph GetIcon()
    {
        return Glyph.Bw(1, 0);
    }
}

public class PotionBloodReliquary() : Potion("Blood Reliquary")
{
    public override IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        var fields = new Dictionary<(int, int), ICharacter>();
        foreach (var ch in level.Party)
        {
            fields.Add((level.CombatStates[ch].X, level.CombatStates[ch].Y), ch);
        }

        foreach (var e in level.Enemies)
        {
            fields.Add((e.X, e.Y), e);
        }
        
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].Set(x, y, "o");
        yield return new WaitForSeconds(0.1f);
        for (int i = 1; i < 3; i++)
        {
            foreach (var cell in level.Map.GetCellsInCircle(x, y, i))
            {
                if (level.Map.IsTransparent(cell.X, cell.Y))
                {
                    if (level.IsInActivePartyFOV.Contains((cell.X, cell.Y)))
                    {
                        game.Layers["mrmo"].Set(cell.X, cell.Y + 2, "o", Color.Red);
                    }

                    if (fields.ContainsKey((cell.X, cell.Y)))
                    {
                        var ap = fields[(cell.X, cell.Y)].GetAP();

                        if (ap.Count<StatusWounds>() >= 1)
                        {
                            game.Layers["mrmo"].Set(cell.X, cell.Y + 2, "+", Color.OrangeRed);
                            ap.Reduce<StatusWounds>(1);
                        }
                        else
                        {
                            game.Layers["mrmo"].Set(cell.X, cell.Y + 2, "x", Color.DarkRed);
                            ap.Add<StatusInsanity>(1);
                        }
                    }
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);
    }

    public override IEnumerable ApplyItemUsed(ICharacter character)
    {
        var ap = character.GetAP();
        var wounds = ap.Count<StatusWounds>();
        var penalty = 5 - wounds;
        if (penalty < 0) penalty = 0;
        for (int i = 0; i < 5; i++)
        {
            ap.Reduce<StatusWounds>(1);
            yield return new WaitForSeconds(0.1f);
        }
        
        if (penalty > 0)
        {
            for (int i = 0; i < penalty; i++)
            {
                ap.Add<StatusInsanity>(i);
                yield return new WaitForSeconds(0.02f);
            }
        }
    }
    
    public override Glyph GetIcon()
    {
        return new Glyph(1, 0, Color.Black, Color.Red);
    }
}