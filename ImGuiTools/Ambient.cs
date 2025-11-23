using ImGuiNET;
using Newtonsoft.Json;
using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace SINEATER.ImGuiTools
{
    public static class Ambient
    {
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

            if (ImGui.SliderFloat($"Grayscale##{name}-grayscale", ref gr, 0.0f, 1.0f))
            {
                atmo.Grayscale = gr;
                changed = true;
            }
            ImGui.Separator();

            return changed;
        }

        public static void ImguiEditor()
        {
            bool changed = ImGui.Button("Force Save");

            changed |= ImguiAtmo("Morning", ref Atmospheres.morning);
            changed |= ImguiAtmo("Afternoon", ref Atmospheres.afternoon);
            changed |= ImguiAtmo("Evening", ref Atmospheres.evening);
            changed |= ImguiAtmo("Night", ref Atmospheres.night);

            if (!changed) return;
            var colors =
                System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                    $"Content\\colors.json");
            using var sw = new StreamWriter(colors);
            using var writer = new JsonTextWriter(sw);
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, Atmospheres);
        }
    }
}