using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.Graphics;

public static class Drawing
{
    public static void TextBox(int x, int y, SpriteBatch batch, (int U, int V)? uv = null)
    {
        if (uv is {} w)
        {
            Drawing.Frame(x, y, 400, 112, w.U, w.V, batch, true, Color.White);    
        }
        else
        {
            Drawing.Frame(x, y, 400, 112, 0, 0, batch, true, Color.White);
        }
        
    }
    
    public static void Portrait(int x, int y, (int U, int V) portrait, SpriteBatch batch)
    {
        var frames = SineaterGame.Instance.Portraits;
        Drawing.Frame(x, y, 96, 112, 3, 7, batch, false, Color.White);
        batch.Draw(frames, new Rectangle(x + 16, y + 16, 80, 80), new Rectangle(portrait.U * 80, portrait.V * 80, 80, 80), Color.White);
    }
    
    public static void Frame(int x, int y, int w, int h, int bx, int by, SpriteBatch batch, bool white, Color color)
    {
        var frames = SineaterGame.Instance.Frames;
        var n = 0;
        var wh = white ? 1 : 0;
        batch.Draw(frames, new Rectangle(x + 16, y + 16, w - 16, h - 16), new Rectangle(wh * 32, 0, 32, 32), color);
        batch.Draw(frames, new Rectangle(x, y, 16, 16), new Rectangle(bx * 32, by * 32, 8, 8), color);
        for (var i = 0; i < (w - 16) / 16; i++)
        {
            batch.Draw(frames, new Rectangle(x + 16 + i * 16, y, 16, 16), new Rectangle(bx * 32 + 8 * (n + 1), by * 32, 8, 8), color);
            batch.Draw(frames, new Rectangle(x + 16 + i * 16, y + h - 16, 16, 16), new Rectangle(bx * 32 + 8 * (n + 1), by * 32 + 24, 8, 8), color);
            n = (n + 1) % 2;
        }
        batch.Draw(frames, new Rectangle(x + w, y, 16, 16), new Rectangle(bx * 32 + 24, by * 32, 8, 8), color);
        
        for (var i = 1; i < (h - 16) / 16; i++)
        {
            batch.Draw(frames, new Rectangle(x, y + i * 16, 16, 16), new Rectangle(bx * 32, by * 32 + 8 * (n + 1), 8, 8), color);
            batch.Draw(frames, new Rectangle(x + w, y + i * 16, 16, 16), new Rectangle(bx * 32 + 24, by * 32 + 8 * (n + 1), 8, 8), color);
            n = (n + 1) % 2;
        }
        
        batch.Draw(frames, new Rectangle(x, y + h - 16, 16, 16), new Rectangle(bx * 32, by * 32 + 24, 8, 8), color);
        batch.Draw(frames, new Rectangle(x + w, y + h - 16, 16, 16), new Rectangle(bx * 32 + 24, by * 32 + 24, 8, 8), color);
    }

    public static void SpeakerBox(int x, int y, (int U, int V) portrait, string speaker, string[] text, SpriteBatch batch)
    {
        Drawing.TextBox(x + 110, y, batch, (3, 3));
        Drawing.Portrait(x, y, portrait, batch);
        batch.DrawText(x + 120, y + 10, SineaterGame.Instance.FontBold, speaker, Color.Black);
        var offset = 0;
        foreach (var line in text)
        {
            batch.DrawText(x + 140, y + 40 + offset, SineaterGame.Instance.FontMono, line, Color.Black);
            offset += 28;
        }
    }
    
    public static void CharacterProfile(int x, int y, Character chr, int index, SpriteBatch batch)
    {
        var (u, v) = chr.Job.GetPortrait();
        Drawing.Portrait(x, y, (u, v), batch);
        batch.DrawTextCenter(x + 56, y - 10, SineaterGame.Instance.FontBold, chr.GetName(), Color.White);
        batch.DrawText(x + 0, y + 108, SineaterGame.Instance.FontMono, $"P{chr.Stats.Poise}", index != 0 ? Color.Gray : Color.GreenYellow);
        batch.DrawText(x + 30, y + 108, SineaterGame.Instance.FontMono, $"C{chr.Stats.Clarity}", index != 1 ? Color.Gray : Color.OrangeRed);
        batch.DrawText(x + 60, y + 108, SineaterGame.Instance.FontMono, $"W{chr.Stats.Will}", index != 2 ? Color.Gray : Color.Purple);
        batch.DrawText(x + 90, y + 108, SineaterGame.Instance.FontMono, $"V{chr.Stats.Vigor}", index != 3 ? Color.Gray : Color.CornflowerBlue);
        batch.DrawText(x + 0, y + 108, SineaterGame.Instance.FontMono, $"P", index == 0 ? Color.White : Color.Gray);
        batch.DrawText(x + 30, y + 108, SineaterGame.Instance.FontMono, $"C", index == 1 ? Color.White : Color.Gray);
        batch.DrawText(x + 60, y + 108, SineaterGame.Instance.FontMono, $"W", index == 2 ? Color.White : Color.Gray);
        batch.DrawText(x + 90, y + 108, SineaterGame.Instance.FontMono, $"V", index == 3 ? Color.White : Color.Gray);

        var i = 0;
        var pin = (char c) =>
        {
            switch (c)
            {
                case 'x': return 0;
                case 'X': return 1;
                default:
                case '-': return 2;
            }
        };
        
        foreach (var item in chr.Items)
        {
            if (item == null) continue;
            batch.DrawText(x + 120, y + i * 20, SineaterGame.Instance.FontMono, item.Name, Color.White);
            i++;
            for (int p = 0; p < 4; p++)
            {
                var pn = pin(item.PrimaryTargets[p]);
                batch.Draw(SineaterGame.Instance.Pins, new Vector2(x + 130 + p * 16, y + 10 + i * 20), new Rectangle(pn * 16, 0, 16, 16), Color.White);
            }

            i++;
            i++;
        }
        
    }
}