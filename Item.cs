using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SINEATER;

public record struct UnlockableMove(EStat? RequiredMaxStat, string Move);
public record struct Upgrade(int Level, List<UnlockableMove> Moves);

[JsonObject(MemberSerialization.OptIn)]
public class Item(string name, (int U, int V) uv, EStat stat, int weight = 3) : ICloneable
{
    static int IDGen = 0;

    public static int NextId()
    {
        IDGen++;
        return IDGen;
    }

    public int ID = IDGen++;
    public void Copy(Item original)
    {
        this.Name = original.Name;
        this.Picture = original.Picture;
    }
    
    public static Item Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR ITEM {name}");
        return new Item($"!{name}", (0, 0), EStat.Clarity);
    }
    
    ~Item()
    {
        if (ItemLibrary.InstancedItems.ContainsKey(Name))
        {
            ItemLibrary.InstancedItems.Remove(Name, this);
        }
    }

#region Serialization
    [JsonProperty]
    public string Name { get; set; } = name;
    [JsonProperty]
    public virtual (int, int) Picture { get; set; } = (uv.U, uv.V);
    [JsonProperty]
    public virtual int Weight { get; set; } = weight;
    [JsonProperty]
    public virtual EStat Stat { get; set; } = stat;
#endregion // Serialization

    public virtual Glyph Glyph => Glyph.Bw(0, 0);
    
    public virtual string GetName()
    {
        return Name;
    }

    public virtual Glyph GetIcon()
    {
        return Glyph;
    }
    
    public object Clone()
    {
        return this.MemberwiseClone();
    }
}