using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SINEATER;

public class EnemyLibraryDefinition
{
    [JsonProperty] public List<(string, EnemyDefinition)> Enemies = [];
}

public class EnemyDefinition : ILoadableDefinition
{
    [JsonProperty] public string Name;
    [JsonProperty] public string Display;
    [JsonProperty] public (int, int) Icon;
    [JsonProperty] public (int, int) Portrait;
    [JsonProperty] public Stats Stats;
    [JsonProperty] public int Guard;
    [JsonProperty] public int NightSpeedUp;
    [JsonProperty] public int DaySpeedUp;
    [JsonProperty] public int NightGuardUp;
    [JsonProperty] public int DayGuardUp;
    [JsonProperty] public List<string> Tags;
    
    public string Key => Name;
}

public class EnemyParser : ILoadableRowParser<EnemyDefinition>
{
    private const int NAME = 0;
    private const int DISPLAY = 1;
    private const int ICON = 2;
    private const int PORTRAIT = 3;
    private const int GUARD = 4;
    private const int POISE = 5;
    private const int CLARITY = 6;
    private const int WILL = 7;
    private const int VIGOR = 8;
    private const int NIGHTSPEEDUP = 9;
    private const int DAYSPEEDUP = 10;
    private const int NIGHTGUARDUP = 11;
    private const int DAYGUARDUP = 12;
    private const int TAGS = 13;
    
    public EnemyDefinition Parse(IList<object> row)
    {
        var name = row[NAME].ToString() ?? "";
        var display = row[DISPLAY].ToString() ?? "";
        var icon = (row[ICON].ToString() ?? "0, 0").Split(",");
        var portrait = (row[PORTRAIT].ToString() ?? "0, 0").Split(",");
        var guard = row[GUARD].ToString() ?? "0";
        var poise = row[POISE].ToString() ?? "0";
        var clarity = row[CLARITY].ToString() ?? "0";
        var will = row[WILL].ToString() ?? "0";
        var vigor = row[VIGOR].ToString() ?? "0";
        var nightSpeedUp = row[NIGHTSPEEDUP].ToString() ?? "0";
        var daySpeedUp = row[DAYSPEEDUP].ToString() ?? "0";
        var nightGuardUp = row[NIGHTGUARDUP].ToString() ?? "0";
        var dayGuardUp = row[DAYGUARDUP].ToString() ?? "0";
        var readTags = row.Count > 13 ? row[TAGS].ToString() : "";
        var tags = (readTags == null ? [] : readTags.Split(",").ToList());

        var def = new EnemyDefinition()
        {
            Tags = tags,
            Guard = int.Parse(guard),
            Stats = new Stats(int.Parse(will), int.Parse(clarity), int.Parse(poise), int.Parse(vigor)),
            Name = name,
            Display = display,
            Icon = (int.Parse(icon[0].Trim()), int.Parse(icon[1].Trim())),
            Portrait = (int.Parse(portrait[0].Trim()), int.Parse(portrait[1].Trim())),
            NightSpeedUp = int.Parse(nightSpeedUp),
            DaySpeedUp = int.Parse(daySpeedUp),
            NightGuardUp = int.Parse(nightGuardUp),
            DayGuardUp = int.Parse(dayGuardUp),
        };

        return def;
    }
}

public class EnemyInterpreter : ILoadableInterpreter<EnemyDefinition, Enemy>
{
    public Enemy MakeFrom(EnemyDefinition? def)
    {
        var enemy = new Enemy
        {
            X = 0,
            Y = 0,
            Stats = def?.Stats ?? new Stats(0, 0, 0, 0),
            Icon = def?.Icon ?? (0, 0),
            Portrait = def?.Portrait ?? (0, 0),
            DayGuardUp = def?.DayGuardUp ?? 0,
            DaySpeedUp = def?.DaySpeedUp ?? 0,
            NightGuardUp = def?.NightGuardUp ?? 0,
            NightSpeedUp = def?.NightSpeedUp ?? 0, 
            Name = def?.Display ?? "Dummy",
            Guard = def?.Guard ?? 0,
            Tags = def?.Tags ?? []
        };
        return enemy;
    }
}

public class Enemies : LoadableLibrary<EnemyDefinition, EnemyParser, EnemyInterpreter, Enemy>
{
    private static readonly Lazy<Enemies> _Instance = new Lazy<Enemies>(() => new Enemies());
    public static Enemies Instance => _Instance.Value;
    
    protected override string Sheet => "Enemies";
    protected override string DataRange => "A1:N20";
    protected override string JsonPath => "enemies.json";
}