using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.Screens;

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
    
    public void DrawPartyMember(Character character, int index)
    {
        var (u, v) = character.GetPortait();
        var (x, y) = (index, 3);
        
        _game.Layers["ascii"].Set(20 * x + 1, 5 * y + 11, $"Px Cx Vx Wx", Color.Gray);
        _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 11, $"{character.Poi}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 11, $"{character.Cla}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 8, 5 * y + 11, $"{character.Vig}", Color.White);
        _game.Layers["ascii"].Set(20 * x + 11, 5 * y + 11, $"{character.Wil}", Color.White);
        
        _game.Layers["ascii"].Set(20 * x + 6, 5 * y + 10, $"{character.Job}", Color.White);
        
        _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
        _game.Layers["portrait2"].Set(x * 4, y * 2 + 3, new Glyph(u, v, Color.Black, Color.White));
    }
    
    public void DrawParty()
    {
        for (var c = 0; c < 4; c++)
        {
            DrawPartyMember(_game.Party.Characters[c], c);
        }
    }
}