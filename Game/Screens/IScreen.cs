using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;

namespace SINEATER.Game.Screens;

public interface IScreen
{
    public MonoGame.Extended.OrthographicCamera? Camera { get; set; }
    public void Initialize(SineaterGame game);
    public void Update(GameTime gameTime);
    public void LayerDraw(GameTime gameTime);
    public void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState);
}

public abstract class Screen : IScreen
{
    protected SineaterGame _game;
    protected readonly int FullWidth = 20, FullHeight = 20;
    protected int Width, Height;
    protected int Time = 0;
    public MonoGame.Extended.OrthographicCamera? Camera { get; set; } = null;
    
    internal virtual (int X, int Y) DrawOffset { get; set; } = (8, 1);

    public Screen(SineaterGame game)
    {
        _game = game;
        Initialize(game);
    }

    internal (int, int)? GetUV(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetUV(x + ox, y + oy);
    }

    internal Color GetFg(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetFg(x + ox, y + oy);
    }

    internal void Draw(int x, int y, Glyph g)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, g);
    }

    internal void Draw(int x, int y, string s)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s);
    }

    internal void Draw(int x, int y, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c);
    }

    internal void Draw(int x, int y, string s, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c);
    }

    internal void Draw(int x, int y, string s, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c, b);
    }

    internal void Draw(int x, int y, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c, b);
    }

    public virtual void Initialize(SineaterGame game) {}

    public abstract void Update(GameTime gameTime);

    public virtual void LayerDraw(GameTime gameTime)
    {
    }

    public virtual void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
    }
}