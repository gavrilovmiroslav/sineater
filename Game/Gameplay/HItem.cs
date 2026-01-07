using System;
using Newtonsoft.Json;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Loadable;

namespace SINEATER.Game.Gameplay;

[JsonObject(MemberSerialization.OptIn)]
public class HItem(string name, (int U, int V) uv, EStat stat, int weight = 3) : ICloneable
{
    static int IDGen = 0;

    public static int NextId()
    {
        IDGen++;
        return IDGen;
    }

    public int ID = IDGen++;
    public void Copy(HItem original)
    {
        this.Name = original.Name;
        this.Picture = original.Picture;
    }
    
    public static HItem Dummy(string name)
    {
        Console.WriteLine($"DUMMY REQUIRED FOR ITEM {name}");
        return new HItem($"!{name}", (0, 0), EStat.Clarity);
    }
    
    ~HItem()
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