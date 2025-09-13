using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;

namespace SINEATER;

public class TextLayerBox(TextLayer layer, Vector2 xy, Vector2 br)
{
    private int _x = (int)xy.X;
    private int _y = (int)xy.Y;

    private string[] Split(string text)
    {
        return Regex.Split(text, @"(?<=[ .,;])");
    }
    
    private void AddWord(string s, Color fg)
    {
        if (_x + s.Length < br.X)
        {
            layer.Set(_x, _y, s, fg);
            _x += s.Length;
        }
        else
        {
            Newline();
            AddWord(s, fg);
        }
    }
    
    public void Add(string sentence)
    {
        foreach (var word in Split(sentence))
        {
            AddWord(word, Color.White);
        }
    }

    public void Add(string sentence, Color fg)
    {
        foreach (var word in Split(sentence))
        {
            AddWord(word, fg);
        }
    }

    public void Newline()
    {
        _x = (int)xy.X;
        _y++;
    }
}