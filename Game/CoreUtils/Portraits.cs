using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SINEATER.Game.CoreUtils;

public class Portraits
{
    private Texture2D _texture;
    private int _width;
    private int _height;

    public Portraits(Texture2D texture, int w, int h)
    {
        _texture = texture;
        _width = w;
        _height = h;
    }

    public void Draw((int, int) uv, SpriteBatch spriteBatch, int x, int y, Color color, float scale = 1.0f, bool flip = false)
    {
        var (u, v) = uv;
        spriteBatch.Draw(_texture, new Vector2(x, y), new Rectangle(u * _width, v * _height, _width, _height), color, 0f, Vector2.Zero, scale, flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}