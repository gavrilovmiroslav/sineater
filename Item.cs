using System.Collections;

namespace SINEATER;

public interface IItem
{
    public string Name { get; }
    public bool CanBeShattered();
    public IEnumerable ApplyItemUsed(ICharacter character);
    
    public IEnumerable ApplyItemShattered(int X, int Y);
}

public class Item(string name) : IAbilitySource, IItem
{
    public string Name => name;

    public virtual bool CanBeShattered()
    {
        return false;
    }

    public virtual IEnumerable ApplyItemUsed(ICharacter character)
    {
        yield break;
    }

    public virtual IEnumerable ApplyItemShattered(int X, int Y)
    {
        yield break;
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
}

public class PotionBloodReliquary() : Potion("Blood Reliquary")
{
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

    public override IEnumerable ApplyItemShattered(int X, int Y)
    {
        yield break;
    }
}