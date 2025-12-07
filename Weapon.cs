using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER;

public interface IEquippable {}

public enum EWeightClass
{
    Tiny = 0,
    Light = 1,
    Medium = 2,
    Heavy = 3,
    Large = 4
}

public static class WeightClassExtensions
{
    public static string Short(this EWeightClass weightClass)
    {
        switch (weightClass)
        {
            case EWeightClass.Tiny:
                return "T";
            case EWeightClass.Light:
                return "S";
            case EWeightClass.Medium:
                return "M";
            case EWeightClass.Heavy:
                return "H";
            case EWeightClass.Large:
                return "L";
            default:
                return "-";
        }
    }
}

public enum EScalingFactor
{
    F = 0,
    D = 1,
    C = 2,
    B = 3,
    A = 5,
    S = 10,
}

public record struct Unlockable<T>(T Thing, int MinLevel);

public record struct StatsScaling(
    EScalingFactor vigorScaling = EScalingFactor.F,
    EScalingFactor willScaling = EScalingFactor.F,
    EScalingFactor poiseScaling = EScalingFactor.F,
    EScalingFactor clarityScaling = EScalingFactor.F);

[JsonObject(MemberSerialization.OptIn)]
public class Weapon(string name, EWeightClass weight, string mainStat,
    int attack, int guard,
    int quality, (int, int) inventoryPicture,
    // STAT SCALING
    EScalingFactor wilScaling = EScalingFactor.F, 
    EScalingFactor claScaling = EScalingFactor.F,
    EScalingFactor poiScaling = EScalingFactor.F, 
    EScalingFactor vigScaling = EScalingFactor.F,
    // SCALING CURVE VALUES
    float scalingBase = 14.0f, float scalingCurve = 1.5f, 
    List<string>? upgrades = null) : Item(name, inventoryPicture, weight), ICloneable, IEquippable
{
    ~Weapon()
    {
        if (ItemLibrary.InstancedWeapons.ContainsKey(Name))
        {
            ItemLibrary.InstancedWeapons.Remove(Name, this);
        }
    }
    
    #region Serialization
    [JsonProperty]
    public string Name { get; set; } = name;
    [JsonProperty]
    public EWeightClass Weight { get; set; } = weight;
    [JsonProperty] 
    public string MainStat { get; set; } = mainStat;
    [JsonProperty]
    public int Quality { get; set; } = quality;
    [JsonProperty]
    public int Attack { get; set; } = attack;
    [JsonProperty]
    public int Guard { get; set; } = guard;

    [JsonProperty]
    public (int, int) Picture { get; set; } = inventoryPicture;
    [JsonProperty]
    public EScalingFactor WilScaling { get; set; } = wilScaling;
    [JsonProperty]
    public EScalingFactor ClaScaling { get; set; } = claScaling;
    [JsonProperty]
    public EScalingFactor PoiScaling { get; set; } = poiScaling;
    [JsonProperty]
    public EScalingFactor VigScaling { get; set; } = vigScaling;
    [JsonProperty] 
    public float ScalingBase { get; set; } = scalingBase;
    [JsonProperty] 
    public float ScalingCurve { get; set; } = scalingCurve;
    private readonly Dictionary<int, Upgrade> _availableUpgrades = [];
    [JsonProperty] 
    public List<string>? Upgrades { get; set; } = upgrades; 
    [JsonProperty]
    public int Level { get; set; } = 1;
    #endregion // Serialization

    //            base   level scaling   quality^2            level
    // =Floor((Pow($B$24 * A3, $B$25 - $B$26 * $B$26 * 0.01 / A3)))
    public int ExperienceNeeded => (int)Math.Floor(Math.Pow(ScalingBase * Level, ScalingCurve - (11 - Quality) * (11 - Quality) * 0.01f / Level));
    public int ExperienceNow { get; set; } = 0;
    
    public Glyph Glyph => Glyph.Bw(14, 67);

    public float Base => Level * (int)Weight;
    
    public override string ToString()
    {
        return $"{Name}";
    }

    public object Clone()
    {
        var clone = this.MemberwiseClone();
        if (clone is Weapon w)
        {
            foreach (var upgrade in w.Upgrades ?? [])
            {
                var pts = upgrade.Split(":");
                var level = int.Parse(pts[0]);
                _availableUpgrades[level] = new Upgrade(level, []);
            
                var upgds = pts[1].Split("|");
                foreach (var up in upgds)
                {
                    EStat? stat = null;
                    var name = up.Trim();
                    if (up.Contains("]"))
                    {
                        var upg = up.Split("]");
                        name = upg[1].Trim();
                        stat = upg[0].Replace("[", "").Trim() switch
                        {
                            "W" => EStat.Will,
                            "C" => EStat.Clarity,
                            "V" => EStat.Vigor,
                            "P" => EStat.Poise,
                            _ => null
                        };
                    }

                    _availableUpgrades[level].Moves.Add(new UnlockableMove(stat, name));
                    if (level is 0 or 1)
                    {
                        AvailableMoves.Add(SineaterGame.Instance.Moves.Get(name));
                    }
                }
            }
        }

        return clone;
    }

    public virtual string ToLongString()
    {
        return $"{Name} (Quality: {Quality}, Weight: {Weight.ToString()})";
    }

    public string GetName()
    {
        return Name;
    }
    
    public virtual Glyph GetIcon()
    {
        return Glyph;
    }

    public void Copy(Weapon original)
    {
        this.WilScaling = original.WilScaling;
        this.ClaScaling = original.ClaScaling;
        this.PoiScaling = original.PoiScaling;
        this.VigScaling = original.VigScaling;

        this.ExperienceNow = original.ExperienceNow;
        this.Name = original.Name;
        this.Picture = original.Picture;
        this.Quality = original.Quality;
        this.Weight = original.Weight;
        this.ScalingBase = original.ScalingBase;
        this.ScalingCurve = original.ScalingCurve;
    }

    public static Weapon Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR WEAPON {name}");
        return new Weapon($"!{name}", EWeightClass.Medium, "WIL",  1, 1, 0, (0, 0));
    }

    public void UpdateMoves(Character character)
    {
        if (_availableUpgrades.TryGetValue(Level, out var upgrade))
        {
            var highestStat = character.Stats.Highest();
                
            foreach (var move in upgrade.Moves)
            {
                if (AvailableMoves.Any(m => m.Name == move.Move)) continue;
                
                if (move.RequiredMaxStat != null)
                {
                    if (highestStat == move.RequiredMaxStat)
                    {
                        AvailableMoves.Add(SineaterGame.Instance.Moves.Get(move.Move));
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    AvailableMoves.Add(SineaterGame.Instance.Moves.Get(move.Move));
                    break;
                }
            }
        }
    }
    
    public void GainExp(Character c, int exp)
    {
        ExperienceNow += exp;
        if (ExperienceNow >= ExperienceNeeded)
        {
            Level++;
            ExperienceNow = 0;
            UpdateMoves(c);
        }
    }
}