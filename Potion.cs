using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace SINEATER;
    
public class Potion(string name) : Item(name)
{
    public override bool CanBeShattered()
    {
        return true;
    }

    public override string ToString()
    {
        return $"{Name} (Potion)";
    }

    public override Glyph GetIcon()
    {
        return new Glyph(11, 68, Color.Black, Color.White);
    }

    public virtual IEnumerable ApplyOnSplat(CombatMapScreen level, Dictionary<(int, int), ICharacter> fields, int x, int y)
    {
        yield break;
    }
    
    public override IEnumerable ApplyItemShattered(CombatMapScreen level, int x, int y)
    {
        var fields = new Dictionary<(int, int), ICharacter>();
        foreach (var ch in level.Party)
        {
            fields.Add((level.CombatStates[ch].X, level.CombatStates[ch].Y), ch);
        }

        foreach (var e in level.Enemies)
        {
            if (!fields.ContainsKey((e.X, e.Y)))
                fields.Add((e.X, e.Y), e);
        }
        
        var game = SineaterGame.Instance;
        game.Layers["mrmo"].Set(x, y + 2, "!", Color.White);
        yield return new WaitForSeconds(0.1f);
        yield return ApplyOnSplat(level, fields, x, y);
        yield return new WaitForSeconds(0.1f);
        for (int i = 1; i < 3; i++)
        {
            foreach (var cell in level.Map.GetCellsInCircle(x, y, i))
            {
                if (level.Map.IsTransparent(cell.X, cell.Y))
                {
                    yield return ApplyOnSplat(level, fields, cell.X, cell.Y);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.5f);
    }
}

public class PotionBloodReliquary() : Potion("Blood Reliquary")
{
    public override IEnumerable ApplyOnSplat(CombatMapScreen level, Dictionary<(int, int), ICharacter> fields, int x, int y)
    {
        if (level.IsInActivePartyMemberFOV.Contains((x, y)))
        {
            SineaterGame.Instance.Layers["mrmo"].Set(x, y + 2, new Glyph(13, 68, Color.Black, Color.Red));
        }

        if (fields.ContainsKey((x, y)))
        {
            var ap = fields[(x, y)].GetAP();

            if (ap.Count<StatusWounds>() >= 1)
            {
                SineaterGame.Instance.Layers["mrmo"].Set(x, y + 2, new Glyph(14, 68, Color.Black, Color.Green));
                ap.Reduce<StatusWounds>(1);
            }
            else
            {
                SineaterGame.Instance.Layers["mrmo"].Set(x, y + 2, "x", Color.DarkRed);
                ap.Add<StatusInsanity>(1);
            }
        }

        yield break;
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
        return new Glyph(11, 68, Color.Black, Color.OrangeRed);
    }
}

public class GhylagsTear() : Potion("Ghylag's Tear")
{
    public override IEnumerable ApplyOnSplat(CombatMapScreen level, Dictionary<(int, int), ICharacter> fields, int x, int y)
    {
        if (level.Map.IsTransparent(x, y))
        {
            if (level.IsInActivePartyMemberFOV.Contains((x, y)))
            {
                SineaterGame.Instance.Layers["mrmo"].Set(x, y + 2, new Glyph(13, 68, Color.Black, Color.White));
            }

            if (fields.ContainsKey((x, y)))
            {
                var blind = new TraitBlind(2);
                yield return fields[(x, y)].AddTrait(blind);
                SineaterGame.Instance.Layers["mrmo"].Set(x, y + 2, new Glyph(10, 67, Color.Black, Color.White));
            }
        }
    }

    public override IEnumerable ApplyItemUsed(ICharacter character)
    {
        var eye = new TraitEagleEyed(5);
        character.GetTraits().Add(eye);
        yield return eye.ApplyOnReceived(character);
    }
    
    public override Glyph GetIcon()
    {
        return new Glyph(11, 68, Color.Black, Color.LightBlue);
    }
}