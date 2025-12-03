using System;
using Newtonsoft.Json;
using SINEATER.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SINEATER;

[JsonObject(MemberSerialization.OptIn)]
public class Item(string name, (int U, int V) uv, EWeightClass weight = EWeightClass.Medium, List<string>? tags = null) : ICloneable
{
    public void Copy(Item original)
    {
        this.Name = original.Name;
        this.Picture = original.Picture;
    }
    
    public static Item Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR ITEM {name}");
        return new Item($"!{name}", (0, 0));
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
    public virtual EWeightClass Weight { get; set; } = weight;

    [JsonProperty] public List<string>? Tags { get; set; } = tags; 

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