using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;

namespace SINEATER.Game.Graphics;

public static class Drawing
{
    public record struct RenderContext(SpriteBatch Batch, GameTime Time);

    public static void TextBox(this RenderContext ctx, int x, int y, (int U, int V)? uv = null)
    {
        if (uv is { } w)
        {
            ctx.Frame(x, y, 400, 112, w.U, w.V, true, Color.White);
        }
        else
        {
            ctx.Frame(x, y, 400, 112, 0, 0, true, Color.White);
        }

    }

    public static void Portrait(this RenderContext ctx, int x, int y, (int U, int V) portrait, Color? frameColor = null,
        Color? portraitColor = null)
    {
        var frames = SineaterGame.Instance.Portraits;
        ctx.Frame(x, y, 96, 112, 3, 7, false, frameColor ?? Color.White);
        ctx.Batch.Draw(frames, new Rectangle(x + 16, y + 16, 80, 80),
            new Rectangle(portrait.U * 80, portrait.V * 80, 80, 80), portraitColor ?? Color.White);
    }

    public static void Frame(this RenderContext ctx, int x, int y, int w, int h, int bx, int by, bool white,
        Color color)
    {
        var frames = SineaterGame.Instance.Frames;
        var n = 0;
        var wh = white ? 1 : 0;
        ctx.Batch.Draw(frames, new Rectangle(x + 16, y + 16, w - 16, h - 16), new Rectangle(wh * 32, 0, 32, 32), color);
        ctx.Batch.Draw(frames, new Rectangle(x, y, 16, 16), new Rectangle(bx * 32, by * 32, 8, 8), color);
        for (var i = 0; i < (w - 16) / 16; i++)
        {
            ctx.Batch.Draw(frames, new Rectangle(x + 16 + i * 16, y, 16, 16),
                new Rectangle(bx * 32 + 8 * (n + 1), by * 32, 8, 8), color);
            ctx.Batch.Draw(frames, new Rectangle(x + 16 + i * 16, y + h - 16, 16, 16),
                new Rectangle(bx * 32 + 8 * (n + 1), by * 32 + 24, 8, 8), color);
            n = (n + 1) % 2;
        }

        ctx.Batch.Draw(frames, new Rectangle(x + w, y, 16, 16), new Rectangle(bx * 32 + 24, by * 32, 8, 8), color);

        for (var i = 1; i < (h - 16) / 16; i++)
        {
            ctx.Batch.Draw(frames, new Rectangle(x, y + i * 16, 16, 16),
                new Rectangle(bx * 32, by * 32 + 8 * (n + 1), 8, 8), color);
            ctx.Batch.Draw(frames, new Rectangle(x + w, y + i * 16, 16, 16),
                new Rectangle(bx * 32 + 24, by * 32 + 8 * (n + 1), 8, 8), color);
            n = (n + 1) % 2;
        }

        ctx.Batch.Draw(frames, new Rectangle(x, y + h - 16, 16, 16), new Rectangle(bx * 32, by * 32 + 24, 8, 8), color);
        ctx.Batch.Draw(frames, new Rectangle(x + w, y + h - 16, 16, 16),
            new Rectangle(bx * 32 + 24, by * 32 + 24, 8, 8), color);
    }

    public static void SpeakerBox(this RenderContext ctx, int x, int y, (int U, int V) portrait, string speaker,
        string[] text)
    {
        ctx.TextBox(x + 110, y, (3, 3));
        ctx.Portrait(x, y, portrait);
        ctx.Batch.DrawText(x + 120, y + 10, SineaterGame.Instance.FontBold, speaker, Color.Black);
        var offset = 0;
        foreach (var line in text)
        {
            ctx.Batch.DrawText(x + 140, y + 40 + offset, SineaterGame.Instance.FontMono, line, Color.Black);
            offset += 28;
        }
    }

    public static void CharacterProfile(this RenderContext ctx, int x, int y, Character chr, int index)
    {
        var (u, v) = chr.Job.GetPortrait();
        var frameColor = Color.White;
        ctx.Portrait(x, y, (u, v), frameColor: frameColor);
        ctx.Batch.DrawTextCenter(x + 56, y - 10, SineaterGame.Instance.FontBold, chr.GetName(), frameColor);
        ctx.Batch.DrawText(x + 0, y + 108, SineaterGame.Instance.FontMono, $"P{chr.Stats.Poise}",
            index != 0 ? Color.Gray : Color.ForestGreen);
        ctx.Batch.DrawText(x + 30, y + 108, SineaterGame.Instance.FontMono, $"C{chr.Stats.Clarity}",
            index != 1 ? Color.Gray : Color.ForestGreen);
        ctx.Batch.DrawText(x + 60, y + 108, SineaterGame.Instance.FontMono, $"W{chr.Stats.Will}",
            index != 2 ? Color.Gray : Color.ForestGreen);
        ctx.Batch.DrawText(x + 90, y + 108, SineaterGame.Instance.FontMono, $"V{chr.Stats.Vigor}",
            index != 3 ? Color.Gray : Color.ForestGreen);
        ctx.Batch.DrawText(x + 0, y + 108, SineaterGame.Instance.FontMono, $"P", index == 0 ? Color.White : Color.Gray);
        ctx.Batch.DrawText(x + 30, y + 108, SineaterGame.Instance.FontMono, $"C",
            index == 1 ? Color.White : Color.Gray);
        ctx.Batch.DrawText(x + 60, y + 108, SineaterGame.Instance.FontMono, $"W",
            index == 2 ? Color.White : Color.Gray);
        ctx.Batch.DrawText(x + 90, y + 108, SineaterGame.Instance.FontMono, $"V",
            index == 3 ? Color.White : Color.Gray);

        var i = 0;
        foreach (var item in chr.Items)
        {
            if (item == null) continue;
            ctx.WeaponProfile(x + 5, y + 5 * i, item, index, ref i);
        }
    }

    public static void WeaponProfile(this RenderContext ctx, int x, int y, Item item, int index, ref int i)
    {
        ctx.Batch.DrawText(x + 120, y + i * 20 + 5, SineaterGame.Instance.FontMono, item.Name, Color.White);
        i++;

        var ny = y + 10 + i * 20;
        ctx.Batch.Draw(SineaterGame.Instance.Pixel, new Rectangle(x + 152, ny - 28, 100, 4), new Rectangle(0, 0, 1, 1), Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x + 120, ny - 29, 32, 16), new Rectangle(14 * 16, 0, 32, 16), Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x + 252, ny - 29, 16, 16), new Rectangle(15 * 16, 16, 16, 16), Color.White);
        for (var dx = 0; dx < 4; dx++)
        {
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x + 152 + 25 * dx, ny - 29, 16, 16), new Rectangle(14 * 16, 16, 16, 16), Color.White);   
        }
        var effectIndex = 0;
        var effectColor = Color.White;
        if (item.PrimaryEffect is EItemEffect.Attack or EItemEffect.Move)
        {
            effectIndex = 10;
            effectColor = Color.OrangeRed;
        }
        else
        {
            effectIndex = 11;
            effectColor = Color.CornflowerBlue;
        }

        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 - 16, ny), new Rectangle(effectIndex * 16, 0, 16, 32), effectColor);

        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 - 16, ny), new Rectangle(4 * 16, 0, 16, 32),
            Color.White);
        
        for (var p = 0; p < 4; p++)
        {
            var pn = Pin(item.PrimaryTargets[p]);
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 + p * 16, ny),
                new Rectangle(pn * 16, 0, 16, 32), Color.White);
            if (p == (int)item.SecondaryStat - 1)
            {
                var s = 9;
                switch (item.SecondaryEffect)
                {
                    case EBonusEffect.PlusMod:
                        s = 6;
                        break;
                    case EBonusEffect.Double:
                        s = 7;
                        break;
                    case EBonusEffect.TargetAll:
                        s = 8;
                        break;
                }

                ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 + p * 16, ny),
                    new Rectangle(s * 16, 0, 16, 32), Color.GreenYellow);
            }
        }

        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 + 4 * 16, ny), new Rectangle(5 * 16, 0, 16, 32),
            Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 146 + 4 * 16, ny), new Rectangle(5 * 16, 0, 16, 32),
            Color.White);
        var px = -1;
        var py = -1;
        switch (item.PrimaryEffect)
        {
            case EItemEffect.Attack:
                px = 0;
                py = 0;
                break;
            case EItemEffect.Guard:
            case EItemEffect.Shield:
                px = 0;
                py = 1;
                break;
            case EItemEffect.Speed:
                px = 1;
                py = 1;
                break;
            case EItemEffect.Resist:
                break;
            case EItemEffect.Move:
                px = 1;
                py = 0;
                break;
            default:
                break;
        }

        if (px > -1 && py > -1)
        {
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 152 + 4 * 16, ny + 9),
                new Rectangle((12 + px) * 16, py * 16, 16, 16), effectColor);
            ctx.Batch.DrawText(x + 138 + 6 * 16, ny, SineaterGame.Instance.FontMono,
                $"{item.PrimaryEffectModifier}", effectColor);
        }

        i++;
        i++;
        return;

        int Pin(char c)
        {
            switch (c)
            {
                case 'x':
                    return 0;
                case 'X':
                    return 1;
                default:
                case '-':
                    return 2;
            }
        }
    }
}