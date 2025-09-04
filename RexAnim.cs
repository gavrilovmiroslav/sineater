using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SadRex;

namespace SINEATER;

public class RexAnim
{
    private readonly Image _rex;
    private readonly int _speed = 0;
    
    private int _frame = 0;
    private int _wait = 0;
    
    public RexAnim(ContentManager content, string filename, int speed)
    {
        var filePath = Path.Combine(content.RootDirectory, $"{filename}.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        if (_rex.LayerCount > 1)
        {
            _frame = 1;
        }

        _speed = speed;
        _wait = _speed;
    }

    public void Update(GameTime gameTime)
    {
        _wait -= gameTime.ElapsedGameTime.Milliseconds;
        if (_wait <= 0)
        {
            _frame++;
            if (_frame >= _rex.LayerCount)
            {
                _frame = 0;
            }
            _wait = _speed;
        }
    }

    public void Draw(int x, int y, TextLayer layer)
    {
        layer.SetRect(new Vector2(x, y), new Vector2(x + 10, y + 13), ' ');
        layer.SetRex(x, y, _rex, _frame);
    }
}