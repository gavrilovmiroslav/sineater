using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Input;

namespace SINEATER;

public interface IScreen
{
    public void Initialize(SineaterGame game);
    public void Update(GameTime gameTime);
    public void PreDraw(SpriteBatch batch, GameTime gameTime);
    public void Draw(SpriteBatch batch, GameTime gameTime);
    public void PostDraw(SpriteBatch batch, GameTime gameTime);
}

public abstract class Screen : IScreen
{
    protected SineaterGame _game;
    protected readonly int _fullWidth = 20, _fullHeight = 20;
    protected int _width, _height;
    protected int _time = 0;
    public CoroutineHandler CoroutineHandler = new();
    
    protected List<string> _submenu = [];
    protected int _submenuSelection = 0;
    protected (int X, int Y) _submenuDelta = (0, 0);

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

    public abstract void Initialize(SineaterGame game);
    public abstract void Update(GameTime gameTime);

    public virtual void PreDraw(SpriteBatch batch, GameTime gameTime)
    {}

    public abstract void Draw(SpriteBatch batch, GameTime gameTime);

    public virtual void PostDraw(SpriteBatch batch, GameTime gameTime)
    {}

    public virtual void SubmenuActivate(string action)
    {
    }

    public virtual void SubmenuItemSelected(int index)
    { 
    }

    public bool CheckSubmenuInputs(bool shouldClearOnConfirm = true)
    {
        var isOpen = _submenu.Count > 0;
        if (isOpen)
        {
            if (InputM.IsActive(EInputAction.SubmenuUp))
            {
                if (_submenuSelection == 0)
                {
                    _submenuSelection = _submenu.Count - 1;
                }
                else
                {
                    _submenuSelection--;
                }

                SubmenuItemSelected(_submenuSelection);
            }
            else if (InputM.IsActive(EInputAction.SubmenuDown))
            {
                if (_submenuSelection == _submenu.Count - 1)
                {
                    _submenuSelection = 0;
                }
                else
                {
                    _submenuSelection++;
                }
                SubmenuItemSelected(_submenuSelection);
            }
            else if (InputM.IsActive(EInputAction.SubmenuConfirm))
            {
                var opt = _submenu[_submenuSelection];
                if (shouldClearOnConfirm)
                {
                    _submenu.Clear();
                }

                SubmenuActivate(opt);
            }
        }

        return isOpen;
    }
    
    public virtual void DrawWorld(bool noPlayer = false) {}
}