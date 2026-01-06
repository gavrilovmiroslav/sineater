using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.ImGuiTools;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER.Game.Screens;

public class MainMenuScreen(SineaterGame game) : Screen(game)
{
    private Texture2D _logo;
    private bool _started = false;
    
    public override void Initialize(SineaterGame game)
    {
        _logo = _game.Content.Load<Texture2D>("sineater-logo");
        _started = false;
    }

    private IEnumerable LoadGame()
    {
        yield return new WaitForSeconds(0.5f);
        //       ItemLibrary.LoadItems(game.Content);
        Enemies.Instance.Load();
        SineaterGame.Instance.Party.MakeParty();
        yield return new FadeOutAndLoadScreen(1.0f, new WorldMapScreen(_game));
    }
    
    public override void Update(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }
        
        if (!_started && InputM.IsActive(EInputAction.Confirm))
        {
            _started = true;

            CoroutineHandler.Run(LoadGame());
        }
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        _game.Layers["mini"].Set(104, 20, $"v1.0.2");

        _game.Layers["ascii"].Clear();
        if (!_started)
        {
            _game.Layers["ascii"].Set(24, 20, "PRESS ANY KEY TO ABSOLVE...");
        }
        else
        {
            _game.Layers["ascii"].Set(29, 20, "LOADING...");
        }
    }

    public override void PreDraw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(_logo, new Vector2(game.Window.ClientBounds.Width / 2.0f, game.Window.ClientBounds.Height / 4.0f), null, 
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);
    }
}
