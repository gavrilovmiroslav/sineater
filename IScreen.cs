using Microsoft.Xna.Framework;

namespace SINEATER;

public interface IScreen
{
    public void Initialize(SineaterGame game);
    public void Update(GameTime gameTime);
    public void Draw(GameTime gameTime);
}