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
    public void Initialize(SineaterGame game);
    public void Update(GameTime gameTime);
    public void LayerDraw(GameTime gameTime);
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
    {
    }

    public abstract void LayerDraw(GameTime gameTime);

    public virtual void Draw(SpriteBatch batch, GameTime gameTime)
    {
    }

    public virtual void PostDraw(SpriteBatch batch, GameTime gameTime)
    {}

    public virtual void SubmenuActivate(string action)
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
    Color[] affinityColors = [Color.CornflowerBlue, Color.GreenYellow, Color.ForestGreen, Color.Lerp(Color.Pink, Color.Purple, 0.5f)];
    public void DrawPartyMember(Character character, int index, bool isFocused)
    {
        if (!isFocused)
        {
            var (u, v) = character.GetPortait();
            var (x, y) = (index, 3);
            _game.Layers["ascii"].SetRect(new Vector2(20 * x + 1, 5 * y + 11), new Vector2(20 * x + 1, 5 * y - 3), ' ');
            _game.Layers["ascii"].Set(20 * x + 1, 5 * y + 11, $"Px Cx Vx Wx", Color.Gray);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 11, $"{character.Poi}", index == 0 ? affinityColors[0] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 11, $"{character.Cla}", index == 1 ? affinityColors[1] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 8, 5 * y + 11, $"{character.Vig}", index == 2 ? affinityColors[2] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 11, 5 * y + 11, $"{character.Wil}", index == 3 ? affinityColors[3] : Color.White);

            // _game.Layers["ascii"].Set(20 * x + 6, 5 * y + 8, $"SHLD{character.Shield}", Color.Gray);
            // _game.Layers["ascii"].Set(20 * x + 6, 5 * y + 9, $"RES{character.Resist}% ", Color.Gray);
            _game.Layers["ascii"].Set(20 * x + 6, 5 * y + 10, $"{character.Job}", Color.White);

            _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
            _game.Layers["portrait2"].Set(x * 4, y * 2 + 3, new Glyph(u, v, Color.Black, Color.White));

            int i = 0;
            foreach (var item in character.Items)
            {
                if (item is not null)
                {
                    _game.Layers["ascii"].Set(20 * x + 1, 5 * y + 7 - i, $"{item.Display}");
                    i++;
                }
            }
        }
        else
        {
            int[] p = [-1, 0, 1, 2];
            var (u, v) = character.GetPortait();
            var (x, y) = (index, 3);

            _game.Layers["ascii"].SetRect(new Vector2(20 * x + 1, 5 * y + 11), new Vector2(20 * x + 20, 5 * y - 3), ' ');
            _game.Layers["ascii"].Set(20 * x + 1, 5 * y + 11, $"Px Cx Vx Wx", Color.Gray);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 11, $"{character.Poi}", index == 0 ? affinityColors[0] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 11, $"{character.Cla}", index == 1 ? affinityColors[1] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 8, 5 * y + 11, $"{character.Vig}", index == 2 ? affinityColors[2] : Color.White);
            _game.Layers["ascii"].Set(20 * x + 11, 5 * y + 11, $"{character.Wil}", index == 3 ? affinityColors[3] : Color.White);

            _game.Layers["portrait"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
            _game.Layers["portrait"].Set(p[index] + x + 1, y + 2, new Glyph(u, v, Color.Black, Color.White));

            var items = character.Items.Where(s => s != null).ToList();
            var s = items.Count * 2;
            var toText = (char c) =>
            {
                if (c == 'x')
                {
                    return '^';
                }
                else if (c == 'X')
                {
                    return '$';
                }
                else
                {
                    return '_';
                }
            };
            
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item is not null)
                {
                    var prim = (item.PrimaryTargets == "self") 
                        ? "self" 
                        : string.Join("", item.PrimaryTargets.Select(toText));
                    
                    _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 6 - s, $"{item.Display}");
                    s--;
                    _game.Layers["ascii"].Set(20 * x + 4, 5 * y + 6 - s, $"{prim} {item.PrimaryEffectModifier}", 
                        item.PrimaryEffect is EItemEffect.Attack or EItemEffect.Move ? Color.Red : Color.GreenYellow);
                    s--;
                }
            }
        }
    }
    
    public void DrawParty(int focus = -1)
    {
        for (var c = 0; c < 4; c++)
        {
            DrawPartyMember(_game.Party.Characters[c], c, c == focus);
        }
    }
}