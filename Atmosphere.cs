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

public static class Ambient
{
    private static bool _editorOpened = false;
    
    public static Atmospheres Atmospheres;
    
    public static bool ImguiAtmo(string name, ref Atmosphere atmo)
    {
        var changed = false;
        var f = atmo.Fg.Tint;
        var fstr = atmo.Fg.Strength;
        var b = atmo.Bg.Tint;
        var bstr = atmo.Bg.Strength;
        var gr = atmo.Grayscale;
        
        var fg = new System.Numerics.Vector3((float)f.R / 255, (float)f.G / 255, (float)f.B / 255);
        var bg = new System.Numerics.Vector3((float)b.R / 255, (float)b.G / 255, (float)b.B / 255);
        
        ImGui.BeginGroup();
        if (ImGui.ColorEdit3($"[{name}] Bg##{name}-color-bg", ref bg))
        {
            atmo.Bg = (new Color(bg.X, bg.Y, bg.Z), atmo.Bg.Strength);
            changed = true;
        }
        if (ImGui.SliderFloat($"%##{name}-str-bg", ref bstr, 0.0f, 1.0f))
        {
            atmo.Bg = (atmo.Bg.Tint, bstr);
            changed = true;
        }
        ImGui.EndGroup();
        
        ImGui.BeginGroup();
        if (ImGui.ColorEdit3($"[{name}] Fg##{name}-color-fg", ref fg))
        {
            atmo.Fg = (new Color(fg.X, fg.Y, fg.Z), atmo.Fg.Strength);
            changed = true;
        }
        if (ImGui.SliderFloat($"%##{name}-str-fg", ref fstr, 0.0f, 1.0f))
        {
            atmo.Fg = (atmo.Fg.Tint, fstr);
            changed = true;
        }
        ImGui.EndGroup();

        if (ImGui.SliderFloat($"Grayscale##{name}-grayscale", ref gr, 0, 1))
        {
            atmo.Grayscale = gr;
            changed = true;
        }
        ImGui.Separator();

        return changed;
    }
    
    public static void ImguiEditor()
    {
        var changed = false;
        ImGui.Begin("Atmosphere", ref _editorOpened);
        if (ImGui.Button("Force Save"))
        {
            changed = true;
        }
        
        changed |= ImguiAtmo("Morning", ref Atmospheres.morning);
        changed |= ImguiAtmo("Afternoon", ref Atmospheres.afternoon);
        changed |= ImguiAtmo("Evening", ref Atmospheres.evening);
        changed |= ImguiAtmo("Night", ref Atmospheres.night);
        ImGui.End();

        if (changed)
        {
            var colors =
                System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                    $"Content\\colors.json");
            using StreamWriter sw = new StreamWriter(colors);
            using JsonWriter writer = new JsonTextWriter(sw);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(writer, Atmospheres);
        }
    }
}
