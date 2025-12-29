using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SINEATER.Input;

namespace SINEATER;

public class CharacterSheetScreen : Screen
{
    private SineaterGame _game;
    private int _time = 0;
    private CoroutineHandler _coroutineHandler = new();
    private int _charIndex = 0;
    
    public CharacterSheetScreen(SineaterGame game) : base(game)
    {
    }

    public override void Initialize(SineaterGame game)
    {
        _charIndex = _game.Party.Selected;
        if (_charIndex < 0) _charIndex = 0;
    }

    public override void Update(GameTime gameTime)
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
        
        if (InputM.IsActive(EInputAction.ChacterSheetExit))
        {
            _game.ScreenStack.Pop();
            _game.Layers["mrmo"].Clear();
            //_game.ScreenStack.Peek().Draw(gameTime);
        }

        if (InputM.IsActive(EInputAction.ChacterSheetCycle))
        {
            _charIndex = (_charIndex + 1) % 4;
        }
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {       
        var stack = _game.ScreenStack.ToArray();
        stack[1].Draw(batch, gameTime);
        
        var start = new Vector2(2, 3);
        var end = new Vector2(33, 16);
        _game.Layers["mrmo"].UnsetRect(start - Vector2.One, end + Vector2.One);
        _game.Layers["ascii"].UnsetRect(new Vector2(start.X * 2 - 1, start.Y - 1), new Vector2(end.X * 2 + 1, end.Y + 1));
        _game.Layers["mrmo"].SetBox(start, end, Sides.Mrmo, Corners.Mrmo);
        
        var chr = _game.Party.Characters[_charIndex];
        _game.Layers["ascii"].Set((int)start.X * 2 + 25, (int)start.Y + 1, $"CHARACTER PROFILE - {chr.Job}");
        var prt = chr.GetPortait();
        _game.Layers["portrait"].Set(1, 1, new Glyph(prt.Item1, prt.Item2, Color.Black, chr.Tint));

    }
}