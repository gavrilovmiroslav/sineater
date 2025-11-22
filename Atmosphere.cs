using System;
using System.IO;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace SINEATER;

[JsonObject(MemberSerialization.Fields)]
public class Atmosphere((Color Tint, float Strength) bg, (Color Tint, float Strength) fg, float grayscale)
{
    [JsonProperty] public (Color Tint, float Strength) Bg { get; set; } = bg;
    [JsonProperty] public (Color Tint, float Strength) Fg { get; set; } = fg;
    [JsonProperty] public float Grayscale { get; set; } = grayscale;

    public Atmosphere() : this((Color.White, 0.0f), (Color.White, 0.0f), 1.0f)
    {}
}

[JsonObject(MemberSerialization.Fields)]
public class Atmospheres
{
    public Atmospheres(Atmosphere morning, Atmosphere afternoon, Atmosphere evening, Atmosphere night)
    {
        this.morning = morning;
        this.afternoon = afternoon;
        this.evening = evening;
        this.night = night;
    }
    
    public Atmospheres()
    {
        morning = new();
        afternoon = new();
        evening = new();
        night = new();
    }
    
    internal Atmosphere morning;
    internal Atmosphere afternoon;
    internal Atmosphere evening;
    internal Atmosphere night;
    
    [JsonProperty]
    public Atmosphere Morning { get => morning; set => value = morning; }
    [JsonProperty]
    public Atmosphere Afternoon { get => afternoon; set => value = afternoon; }
    [JsonProperty]
    public Atmosphere Evening { get => evening; set => value = evening; }
    [JsonProperty]
    public Atmosphere Night { get => night; set => value = night; }

    public Atmosphere this[int n]
    {
        get
        {
            return (n % 4) switch
            {
                0 => Morning,
                1 => Afternoon,
                2 => Evening,
                _ => Night,
            };
        }
    }
};
