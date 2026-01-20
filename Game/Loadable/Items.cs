using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.Loadable;

public enum EItemEffect
{
    None = 0,
    Attack = 1,
    Shield = 2,
    Guard = 3,
    Speed = 4,
    Resist = 5,
    Move = 6,
}

public enum EBonusEffect
{
    None = 0,
    PlusMod = 1,
    Double = 2,
    TargetAll = 3,
}

public class ItemDefinition : ILoadableDefinition
{
    [JsonProperty] public string Name;
    [JsonProperty] public string Display;
    [JsonProperty] public (int, int) Icon;
    [JsonProperty] public string Description;
    
    [JsonProperty] public int Weight;
    
    [JsonProperty] public EItemEffect PrimaryEffect;
    [JsonProperty] public int PrimaryEffectModifier;
    [JsonProperty] public string PrimaryTargets;
    
    [JsonProperty] public EStat SecondaryStat;
    [JsonProperty] public int SecondaryStatRequirement;
    
    [JsonProperty] public EBonusEffect SecondaryEffect;
    [JsonProperty] public int SecondaryEffectModifier;
    [JsonProperty] public string SecondaryTargets;
    [JsonProperty] public int DropChance;
    
    [JsonProperty] public List<string> Tags;

    [JsonProperty] public float PoiseScale;
    [JsonProperty] public float ClarityScale;
    [JsonProperty] public float WillScale;
    [JsonProperty] public float VigorScale;
    
    public string Key => Name;
}

public class ItemParser : ILoadableRowParser<ItemDefinition>
{
    private const int NAME = 0;
    private const int DISPLAY = 1;
    private const int ICON = 2;
    private const int DESCRIPTION = 3;
    private const int WEIGHT = 4;
    private const int PRIMARY_EFFECT = 5;
    private const int PRIMARY_EFFECT_MOD = 6;
    private const int PRIMARY_EFFECT_TARGETS = 7;
    private const int SECONDARY_STAT = 8;
    private const int SECONDARY_STAT_REQ = 9;
    private const int SECONDARY_EFFECT = 10;
    private const int SECONDARY_EFFECT_MOD = 11;
    private const int SECONDARY_EFFECT_TARGETS = 12;
    private const int DROP_CHANCE = 13;
    private const int TAGS = 14;
    private const int POISE_SCALE = 15;
    private const int CLARITY_SCALE = 16;
    private const int WILL_SCALE = 17;
    private const int VIGOR_SCALE = 18;
    
    public ItemDefinition Parse(IList<object> row)
    {
        var name = row[NAME].ToString() ?? "";
        var display = row[DISPLAY].ToString() ?? "";
        var icon = (row[ICON].ToString() ?? "0, 0").Split(",");
        var desc = row[DESCRIPTION].ToString() ?? "";
        var weight = row[WEIGHT].ToString() ?? "0";
        var primEffect = row[PRIMARY_EFFECT].ToString() ?? "None";
        var primEffectMod = row[PRIMARY_EFFECT_MOD].ToString() ?? "0";
        var primEffectTargets = row[PRIMARY_EFFECT_TARGETS].ToString() ?? "----";
        var secnStat = row[SECONDARY_STAT].ToString() ?? "";
        var secStat = secnStat[0].ToString().ToUpper() + secnStat[1..].ToString().ToLower();
        var secReq = row[SECONDARY_STAT_REQ].ToString() ?? "0";
        var secEffect = row[SECONDARY_EFFECT].ToString() ?? "None";
        var secEffectMod = row[SECONDARY_EFFECT_MOD].ToString() ?? "0";
        var secEffectTargets = row[SECONDARY_EFFECT_TARGETS].ToString() ?? "----";
        var dropChance = row[DROP_CHANCE].ToString() ?? "0";
        var readTags = row.Count > 14 ? row[TAGS].ToString() : "";
        var tags = (readTags == null ? [] : readTags.Split(",")
            .Where(t => t.Trim().Length == 0).ToList());
        var scaPoise = row[POISE_SCALE].ToString() ?? "0.0";
        var scaClarity = row[CLARITY_SCALE].ToString() ?? "0.0";
        var scaWill = row[WILL_SCALE].ToString() ?? "0.0";
        var scaVigor = row[VIGOR_SCALE].ToString() ?? "0.0";
        
        var def = new ItemDefinition()
        {
            Name = name,
            Display = display,
            Icon = (int.Parse(icon[0].Trim()), int.Parse(icon[1].Trim())),
            Description = desc,
            Weight = int.Parse(weight),
            PrimaryEffect = Enum.Parse<EItemEffect>(primEffect),
            PrimaryEffectModifier = int.Parse(primEffectMod),
            PrimaryTargets = primEffectTargets,
            SecondaryStat = Enum.Parse<EStat>(secStat),
            SecondaryStatRequirement = int.Parse(secReq),
            SecondaryEffect = Enum.Parse<EBonusEffect>(secEffect),
            SecondaryEffectModifier = int.Parse(secEffectMod),
            SecondaryTargets = secEffectTargets,
            DropChance = int.Parse(dropChance),
            Tags = tags,
            PoiseScale = float.Parse(scaPoise),
            ClarityScale = float.Parse(scaClarity),
            WillScale = float.Parse(scaWill),
            VigorScale = float.Parse(scaVigor),
        };

        return def;
    }
}

public class ItemInterpreter : ILoadableInterpreter<ItemDefinition, Item>
{
    public Item MakeFrom(ItemDefinition? def)
    {
        var item = new Item
        {
            Name = def?.Name ?? "Dummy",
            Display = def?.Display ?? "Dummy",
            Icon = def?.Icon ?? (0, 0),
            Description = def?.Description ?? string.Empty,
            Weight = def?.Weight ?? 0,
            PrimaryEffect = def?.PrimaryEffect ?? EItemEffect.None,
            PrimaryEffectModifier = def?.PrimaryEffectModifier ?? 0,
            PrimaryTargets = def?.PrimaryTargets ?? "----",
            SecondaryStat = def?.SecondaryStat ?? EStat.None,
            SecondaryStatRequirement = def?.SecondaryStatRequirement ?? 0,
            SecondaryEffect = def?.SecondaryEffect ?? EBonusEffect.None,
            SecondaryEffectModifier = def?.SecondaryEffectModifier ?? 0,
            SecondarySources = def?.SecondaryTargets ?? "----",
            DropChance = def?.DropChance ?? 0,
            Tags = [..def?.Tags ?? []],
            PoiseScale = def?.PoiseScale ?? 1.0f,
            ClarityScale = def?.ClarityScale ?? 1.0f,
            WillScale = def?.WillScale ?? 1.0f,
            VigorScale = def?.VigorScale ?? 1.0f,
        };
        return item;
    }
}

public class Items : LoadableLibrary<ItemDefinition, ItemParser, ItemInterpreter, Item>
{
    private static readonly Lazy<Items> _Instance = new(() => new Items());
    public static Items Instance => _Instance.Value;
    
    protected override string Sheet => "Items";
    protected override string DataRange => "A1:S20";
    protected override string JsonPath => "items.json";
}

