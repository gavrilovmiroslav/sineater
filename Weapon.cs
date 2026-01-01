using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER;

public interface IEquippable {}

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
public class Weapon(string name, string from, string toParty, string toEnemy, int weight, EStat stat,
    int attack, int guard, int quality,
    EStat? bonus, int drop,
    string effect, (int, int) inventoryPicture) : Item(name, inventoryPicture, stat, weight), ICloneable, IEquippable
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
    public string From { get; set; } = from;
    [JsonProperty]
    public string ToParty { get; set; } = toParty;
    [JsonProperty]
    public string ToEnemy { get; set; } = toEnemy;
    [JsonProperty]
    public int Weight { get; set; } = weight;
    [JsonProperty] 
    public EStat Stat { get; set; } = stat;
    [JsonProperty]
    public int Quality { get; set; } = quality;
    [JsonProperty]
    public int Attack { get; set; } = attack;
    [JsonProperty]
    public int Guard { get; set; } = guard;
    [JsonProperty] 
    public EStat? Bonus { get; set; } = bonus;
    [JsonProperty] 
    public int Drop { get; set; } = drop;
    [JsonProperty]
    public (int, int) Picture { get; set; } = inventoryPicture;
    #endregion // Serialization

    public Glyph Glyph => Glyph.Bw(14, 67);

    public string Profile
    {
        get
        {
            if (ToParty.All(c => c == '-'))
            {
                var from = string.Join("", From.Select(c => c == '-' ? '.' : 'o'));
                var into = string.Join("", ToEnemy.Select(c => c == '-' ? '.' : 'v'));
                return $"FROM [{from}] -ATK{Attack}-> [{into}]";
            }
            else
            {
                var from = string.Join("", From.Select(c => c == '-' ? '.' : 'o'));
                var into = string.Join("", ToParty.Select(c => c == '-' ? '.' : 'v'));
                return $"FROM [{from}] -GRD +{Guard}-> [{into}]";
            }
        }
    }

    public override string ToString()
    {
        return $"{Name}";
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
        this.Name = original.Name;
        this.Stat = original.Stat;
        this.Picture = original.Picture;
        this.Quality = original.Quality;
        this.Weight = original.Weight;
    }

    public static Weapon Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR WEAPON {name}");
        return new Weapon($"!{name}", "----", "----", "----", 3, EStat.Will,  1, 1, 0, null, 0, "", (0, 0));
    }
}