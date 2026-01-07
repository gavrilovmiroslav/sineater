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
    
    public EItemEffect SecondaryEffect;
    public int SecondaryEffectModifier;
    public string SecondaryTargets = "----";
    public int DropChance;
    
    public List<string> Tags = [];
}