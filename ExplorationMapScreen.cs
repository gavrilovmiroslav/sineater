using Microsoft.Xna.Framework;

namespace SINEATER;

public class ExplorationMapScreen : IScreen
{
    private readonly int _fullWidth = 25, _fullHeight = 15;
    private readonly int _offsetX = 4, _offsetY = 2;
    private SineaterGame _game;

    public ExplorationMapScreen(SineaterGame game)
    {
        _game = game;
        _game.World = new (_fullWidth, _fullHeight);
    }
    
    public void Initialize(SineaterGame game)
    {
    }

    public void Update(GameTime gameTime)
    {
        
    }

    public void Draw(GameTime gameTime)
    {
        _game.Layers["ascii"].Clear();
        _game.Layers["mrmo"].Clear();
        
        _game.World.Draw(_game, _offsetX, _offsetY);
    }
}