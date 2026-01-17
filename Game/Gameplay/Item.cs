using System.Collections.Generic;
using SINEATER.Game.Loadable;

namespace SINEATER.Game.Gameplay;

public class Item
{
    public string Name;
    public string Display;
    public (int, int) Icon;
    public string Description;
    
    public int Weight;
    
    public EItemEffect PrimaryEffect;
    public int PrimaryEffectModifier;
    public string PrimaryTargets = "----";
    
    public EStat SecondaryStat;
    public int SecondaryStatRequirement;
    
    public EBonusEffect SecondaryEffect;
    public int SecondaryEffectModifier;
    public string SecondarySources = "----";
    public int DropChance;
    
    public List<string> Tags = [];
    public int TimeGauge = 0;

    public bool BonusActivates(Character character, int index)
    {
        var ok = false;
        var req = SecondaryStatRequirement;
        var stat = SecondaryStat;
        switch (stat)
        {
            case EStat.Vigor:
                ok = character.Vig >= req;
                break;
            case EStat.Will:
                ok = character.Wil >= req;
                break;
            case EStat.Clarity:
                ok = character.Cla >= req;
                break;
            case EStat.Poise:
                ok = character.Poi >= req;
                break;
        }

        ok &= SecondarySources[index] == 'x';
        return ok;
    }
}