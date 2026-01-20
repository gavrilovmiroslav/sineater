
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
        var portraits = SineaterGame.Instance.Portraits;
        ctx.Frame(x, y, 96, 112, 3, 7, false, frameColor ?? Color.White);
        ctx.Batch.Draw(portraits, new Rectangle(x + 16, y + 16, 80, 80),
            new Rectangle(portrait.U * 80, portrait.V * 80, 80, 80), portraitColor ?? Color.White, 0, Vector2.Zero, SpriteEffects.FlipHorizontally, 0);
    }

    public static void FrameEdge(this RenderContext ctx, int x, int y, int w, int bx, int by, Color color)
    {
        var frames = SineaterGame.Instance.Frames;
        var n = 0;
        ctx.Batch.Draw(frames, new Rectangle(x, y, 16, 16), new Rectangle(bx * 32, by * 32, 8, 8), color);
        for (var i = 0; i < (w - 16) / 16; i++)
        {
            ctx.Batch.Draw(frames, new Rectangle(x + 16 + i * 16, y, 16, 16),
                new Rectangle(bx * 32 + 8 * (n + 1), by * 32, 8, 8), color);
            n = (n + 1) % 2;
        }

        ctx.Batch.Draw(frames, new Rectangle(x + w, y, 16, 16), new Rectangle(bx * 32 + 24, by * 32, 8, 8), color);
    }
    
    public static void EmptyFrame(this RenderContext ctx, int x, int y, int w, int h, int bx, int by,
        Color color)
    {
        var frames = SineaterGame.Instance.Frames;
        var n = 0;
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

    public static void SpeakerBox(this RenderContext ctx, int x, int y, (int U, int V) portrait, 
        string speaker, string[] text)
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
    
    public static void CharacterProfile(this RenderContext ctx, int x, int y, Character chr, int index, bool selected)
    {
        var (u, v) = chr.Job.GetPortrait();
        ctx.Portrait(x, y, (u, v), frameColor: Color.Gray);
        ctx.Batch.DrawTextCenter(x + 56, y - 14, SineaterGame.Instance.FontBold, chr.GetName(), Color.White);
        if (selected)
        {
            ctx.EmptyFrame(x, y, 96, 112, 4, 7, Color.White);
        }

        ctx.Batch.DrawText(x + 0, y + 108, SineaterGame.Instance.FontMono, $"P{chr.Stats.Poise}",
            index != 0 ? Color.Gray : Color.MediumPurple);
        ctx.Batch.DrawText(x + 30, y + 108, SineaterGame.Instance.FontMono, $"C{chr.Stats.Clarity}",
            index != 1 ? Color.Gray : Color.MediumPurple);
        ctx.Batch.DrawText(x + 60, y + 108, SineaterGame.Instance.FontMono, $"W{chr.Stats.Will}",
            index != 2 ? Color.Gray : Color.MediumPurple);
        ctx.Batch.DrawText(x + 90, y + 108, SineaterGame.Instance.FontMono, $"V{chr.Stats.Vigor}",
            index != 3 ? Color.Gray : Color.MediumPurple);
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
            ctx.WeaponProfile(x + 125, y + 58 * i, item, index);
            i++;
        }
    }

    public static void WeaponProfile(this RenderContext ctx, int x, int y, Item? item, int index)
    {
        var gray = Color.Lerp(Color.DarkGray, Color.Black, 0.75f);
        var i = 0;
        ctx.Batch.Draw(SineaterGame.Instance.Pixel, new Rectangle(x - 9, y, 2, 55), item == null ? gray : Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pixel, new Rectangle(x - 6, y, 2, 55), item == null ? gray : Color.Lerp(Color.CornflowerBlue, Color.Gray, 0.5f));
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x - 8, y + 49, 16, 16), new Rectangle(15 * 16, 16, 16, 16), item == null ? gray : Color.Gray);
        
        ctx.Batch.DrawText(x, y + i * 20 + 3, SineaterGame.Instance.FontMono, item?.Display ?? "Empty", item == null ? gray : Color.White);
        i++;

        var ny = y + 8 + i * 20;
        
        ctx.Batch.Draw(SineaterGame.Instance.Pixel, new Rectangle(x + 32, ny - 28, 100, 4),
            new Rectangle(0, 0, 1, 1), item == null ? gray : Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pixel, new Rectangle(x + 32, ny - 28, item?.TimeGauge ?? 0, 4),
            new Rectangle(0, 0, 1, 1), item == null ? gray : Color.CornflowerBlue);
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x, ny - 29, 32, 16),
            new Rectangle(14 * 16, 0, 32, 16), item == null ? gray : Color.White);
        ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x + 32, ny - 29, 16, 16),
            new Rectangle(15 * 16, 16, 16, 16), item == null ? gray : Color.CornflowerBlue);
        
        if (item != null)
        {
            for (var dx = 0; dx < 4; dx++)
            {
                ctx.Batch.Draw(SineaterGame.Instance.Pins, new Rectangle(x + 32 + 25 * dx, ny - 29, 16, 16), new Rectangle(14 * 16, 16, 16, 16), Color.Lerp(Color.Blue, Color.CornflowerBlue, 0.75f));   
            }
        }
        
        var effectIndex = 0;
        var effectColor = Color.Gray;
        if (item?.PrimaryEffect is EItemEffect.Attack or EItemEffect.Move)
        {
            effectIndex = 10;
            effectColor = Color.OrangeRed;
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 10, ny), new Rectangle(effectIndex * 16, 0, 16, 32), effectColor);
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 10, ny), new Rectangle(4 * 16, 0, 16, 32),
                Color.White);
        }
        else if (item is not null)
        {
            effectIndex = 11;
            effectColor = Color.ForestGreen;
            
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 10, ny), new Rectangle(effectIndex * 16, 0, 16, 32), effectColor);
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 10, ny), new Rectangle(4 * 16, 0, 16, 32),
                Color.White);
        }
        
        for (var p = 0; p < 4; p++)
        {
            var pn = Pin(item?.PrimaryTargets[p] ?? '-');
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + p * 16, ny),
                new Rectangle(pn * 16, 0, 16, 32), item == null ? gray : Color.White);
            if (p == ((int)(item?.SecondaryStat ?? EStat.None) - 1))
            {
                var s = 9;
                switch (item?.SecondaryEffect ?? EBonusEffect.None)
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

                if (s != 9 && index == p)
                {
                    ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + p * 16, ny),
                        new Rectangle(pn * 16, 0, 16, 32), Color.GreenYellow);
                    ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + p * 16, ny),
                        new Rectangle(s * 16, 0, 16, 32), Color.GreenYellow);
                }
                else
                {
                    ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + p * 16, ny),
                        new Rectangle(s * 16, 0, 16, 32), Color.Gray);
                }
            }
        }
        
        if (item != null)
        {
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + 4 * 16, ny), new Rectangle(5 * 16, 0, 16, 32),
                Color.White);
            ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 26 + 4 * 16, ny), new Rectangle(5 * 16, 0, 16, 32),
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
                ctx.Batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 32 + 4 * 16, ny + 9),
                    new Rectangle((12 + px) * 16, py * 16, 16, 16), effectColor);
                ctx.Batch.DrawText(x + 16 + 6 * 16, ny, SineaterGame.Instance.FontMono,
                    $"{item.PrimaryEffectModifier}", effectColor);
            }
        }

        i += 2;
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