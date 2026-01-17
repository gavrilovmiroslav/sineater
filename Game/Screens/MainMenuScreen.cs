using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using SINEATER.Tools.SinMod;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER.Game.Screens;

enum EMainMenuState
{
    Waiting,
    Loading,
    Fading,
    Done
}

public class MainMenuScreen(SineaterGame game) : Screen(game)
{
    private Texture2D _logo;
    
    private Texture2D _fmod;
    private Texture2D _fmodCredits;
    private EMainMenuState _state = EMainMenuState.Waiting;
    private float _fadeTime = 0.0f;
    private Character _wizard = new PartyMember(ECharacterClass.Wizard);
    
    public override void Initialize(SineaterGame game)
    {
        _logo = _game.Content.Load<Texture2D>("sineater-logo");
        _fmod = _game.Content.Load<Texture2D>("fmod-logo");
        _fmodCredits = _game.Content.Load<Texture2D>("fmod-credits");

        _wizard.Items[0] = new Item
        {
            Name = "Misericorde", 
            PrimaryTargets = "xxx-",
            PrimaryEffect = EItemEffect.Attack,
            PrimaryEffectModifier = 2,
            SecondaryStat = EStat.Clarity, 
            SecondaryEffect = EBonusEffect.TargetAll,
            TimeGauge = 52
        };
        _wizard.Items[1] = new Item
        {
            Name = "Ash Branch", 
            PrimaryTargets = "XXXX", 
            PrimaryEffect = EItemEffect.Guard,
            PrimaryEffectModifier = 1,
            SecondaryStat = EStat.Vigor, 
            SecondaryEffect = EBonusEffect.PlusMod,
            TimeGauge = 40
        };
        _state = EMainMenuState.Waiting;
    }

    private void LoadItems()
    {
        Enemies.Instance.Load();
        Items.Instance.Load();
        SineaterGame.Instance.Party.MakeParty();
        _state = EMainMenuState.Fading;
    }
    
    private IEnumerable LoadGame()
    {
        _state = EMainMenuState.Loading;
        yield return new WaitForSeconds(0.5f);
        yield return Task.Run(LoadItems);
    }
    
    public override void Update(GameTime gameTime)
    {
        if (_state == EMainMenuState.Fading)
        {
            if (_fadeTime > 1.0f)
            {
                _state = EMainMenuState.Done;
                SineaterGame.Instance.PopAndPushScreen(new WorldMapScreen(SineaterGame.Instance));
            }
        }
        
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }
        
        switch (_state)
        {
            case EMainMenuState.Waiting:
                if (InputM.IsActive(EInputAction.Confirm))
                {
                    CoroutineHandler.Run(LoadGame());
                }
                break;
            case EMainMenuState.Loading:
                break;
            case EMainMenuState.Fading:
                Muse.SetGameState(EMusicState.World);
                break;
            case EMainMenuState.Done:
                break;
        }
    }
    
    public override void LayerDraw(GameTime gameTime)
    {
    }

    private float t = 3.0f;
    private float tt = 0.0f;
    private string[] dots = new[] { ".", "..", "..." };
    private float sinWave = 0.0f;
    private bool selected = false;
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        var mid = (int) (SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2.0f);
        if (_state == EMainMenuState.Waiting)
        {
        //    batch.DrawTextCenter(mid + Rnd.Instance.D4 - 2, 700 + Rnd.Instance.D4 - 2, SineaterGame.Instance.Font, "Press [SPACE] to start", rot: tt);
        //    tt += 0.01f;
        }
        else if (_state == EMainMenuState.Loading)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, $"Loading{dots[((int)t) % 3]}");
        }

        var rc = new Drawing.RenderContext(batch, gameTime);
        rc.SpeakerBox(400, 400, (2, 2), "Temple of Bones", ["Achievement unlocked!", "Defeat 50 skeletons."]);
    }

    public override void PreDraw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(_logo, new Vector2(game.Window.ClientBounds.Width / 2.0f, game.Window.ClientBounds.Height / 4.0f), null, 
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);
    }
    
    public override void PostDraw(SpriteBatch batch, GameTime gameTime)
    {
        var pixel = SineaterGame.Instance.Pixel; 
        if (_state == EMainMenuState.Fading)
        {
            _fadeTime += 0.01f;
            batch.Draw(pixel, new Vector2(0, 0), null,
                new Color(0, 0, 0, _fadeTime), 0.0f, new Vector2(0, 0),
                new Vector2(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height),
                SpriteEffects.None, 0);
        }
        else if (_state == EMainMenuState.Done)
        {
            batch.Draw(pixel, new Vector2(0, 0), null,
                new Color(0, 0, 0, 1), 0.0f, new Vector2(0, 0),
                new Vector2(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height),
                SpriteEffects.None, 0);
        }
    
        batch.Draw(_fmod, new Vector2(35, game.Window.ClientBounds.Height - 100), new Rectangle(0, 0, 640, 164), 
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One * 0.35f, SpriteEffects.None, 0);

        batch.Draw(_fmodCredits, new Vector2(35, game.Window.ClientBounds.Height - 40), new Rectangle(0, 0, 428, 22), 
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One, SpriteEffects.None, 0);
    }
}
