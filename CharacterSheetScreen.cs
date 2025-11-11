using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;

namespace SINEATER;

public class CharacterSheetScreen : IScreen
{
    private SineaterGame _game;
    private int _time = 0;
    private CoroutineHandler _coroutineHandler = new();
    private int _charIndex = 0;
    
    public CharacterSheetScreen(SineaterGame game)
    {
        _game = game;
    }

    public void Initialize(SineaterGame game)
    {
        _charIndex = _game.Party.Selected;
        if (_charIndex < 0) _charIndex = 0;
    }

    public void Update(GameTime gameTime)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }
        
        if (_coroutineHandler.IsActive())
        {
            _coroutineHandler.Update();
            return;
        }
        
        if (KB.HasBeenPressed(Keys.Escape))
        {
            _game.ScreenStack.Pop();
            _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(36, 28), ' ');
            _game.ScreenStack.Peek().Draw(gameTime);
        }

        if (KB.HasBeenPressed(Keys.Space))
        {
            _charIndex = (_charIndex + 1) % 4;
        }
    }

    public void Draw(GameTime gameTime)
    {       
        var stack = _game.ScreenStack.ToArray();
        stack[1].Draw(gameTime);
        
        var start = new Vector2(2, 3);
        var end = new Vector2(33, 16);
        _game.Layers["mrmo"].SetRect(start - Vector2.One, end + Vector2.One, ' ');
        _game.Layers["ascii"].SetRect(new Vector2(start.X * 2 - 1, start.Y - 1), new Vector2(end.X * 2 + 1, end.Y + 1), ' ');
        _game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        {
            Top = Glyph.Bw(10, 27),
            Bottom = Glyph.Bw(10, 29),
            Left = Glyph.Bw(9, 28),
            Right = Glyph.Bw(11, 28),
        }, new Corners<Glyph>()
        {
            BottomLeft = Glyph.Bw(11 - 2, 31 - 4 + 2), 
            BottomRight = Glyph.Bw(10, 30), 
            TopLeft = Glyph.Bw(11 - 2, 31 - 4), 
            TopRight = Glyph.Bw(10, 31),
        });
        
        var chr = _game.Party.Characters[_charIndex];
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 1, $"CHARACTER PROFILE - {chr.Job}");
        var prt = chr.GetPortait();
        _game.Layers["portrait"].Set(1, 1, new Glyph(prt.Item1, prt.Item2, Color.Black, chr.Tint));

        var (u, v) = ItemLibrary.EmptyUv;
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 3, $"LEFT HAND: --");
        if (chr.LeftWeapon is { } lw)
        {
            (u, v) = lw.Picture;
            _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 3, $"LEFT HAND: {lw.GetName()}");
        }
        _game.Layers["porsmol"].Set(1, 2, new Glyph(u, v, Color.Black, chr.Tint));
        
        (u, v) = ItemLibrary.EmptyUv;
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 5, $"ARMOR: --");
        
        (u, v) = ItemLibrary.EmptyUv;
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 6, $"ITEM: --");
        if (chr.Item is { } item)
        {
            (u, v) = item.Picture;
            _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 6, $"ITEM: {item.Name}");
        }
        _game.Layers["porsmol"].Set(3, 4, new Glyph(u, v, Color.Black, chr.Tint));

        (u, v) = ItemLibrary.EmptyUv;
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 4, $"RIGHT HAND: --");
        if (chr.RightWeapon is { } rw)
        {
            (u, v) = rw.Picture;
            _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 4, $"RIGHT HAND: {rw.GetName()}");
        }
        _game.Layers["porsmol"].Set(4, 2, new Glyph(u, v, Color.Black, chr.Tint));
    }
}