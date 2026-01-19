using System;
using System.Collections;
using System.Threading.Tasks;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using SINEATER.Tools.SinMod;
using Color = Microsoft.Xna.Framework.Color;
using SINEATER.Game.LookNFeel;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Reward = SINEATER.Game.Gameplay.Reward;
using LDtk;
using LDtk.Renderer;

namespace SINEATER.Game.Screens;

public enum EMainMenuState
{
    Starting,
    Menu,
    Fading,
    Done,
    Options
}

public class MainMenuStateContext
{
    public Task? LoaderTask = null;
    public EMainMenuState State;
    public float FadeTime;
    public bool WorldLoaded;
    public int MenuOption;
}

public record struct MainMenuChangeStateEvent(MainMenuStateContext Menu, EMainMenuState Next);

public partial class MainMenuStateEventReceiver
{
    public MainMenuStateEventReceiver() { Hook(); }
    [Event] public void OnMainMenuStateChanged(ref MainMenuChangeStateEvent ev) {}
}

public static class MainMenuEventHandler
{
    [Event(order: 1)]
    public static void OnMainMenuStateChanged(ref MainMenuChangeStateEvent ev)
    {
        ev.Menu.State = ev.Next;
        var @event = ev;

        switch (ev.Next)
        {
            case EMainMenuState.Starting:
                break;
            case EMainMenuState.Menu:
                break;
            case EMainMenuState.Options:
                SineaterGame.Instance.PopAndPushScreen(new OptionsScreen(SineaterGame.Instance));
                break;
            case EMainMenuState.Fading:
                Muse.SetGameState(EMusicState.World);
                break;
            case EMainMenuState.Done:
                var encounterEntity = SineaterGame.Instance.World.Get(3, 7);
                var encounter = SineaterGame.Instance.World.ECS.Get<Encounter>(encounterEntity);
                SineaterGame.Instance.PopAndPushScreen(new TacticMapScreen(SineaterGame.Instance, (3, 7), encounter, new Reward([]), ETimeOfDay.Afternoon));
                break;
        }
    }
}

public class MainMenuScreen(SineaterGame game) : Screen(game)
{
    private Texture2D _logo;
    private Texture2D _fmod;
    private Texture2D _fmodCredits;
    private MainMenuStateContext _ctx;
    
    private Texture2D _vfx;
    private GridAnimation _vfxAnimation;
    private GridAnimationContext _vfxAnimationContext;
    
    public override void Initialize(SineaterGame game)
    {
        _vfx = _game.Content.Load<Texture2D>("vfx11");
        _vfxAnimationContext = new GridAnimationContext(_vfx, (4, 4), 0.01f, Color.White, 2.0f);
        _vfxAnimation = new GridAnimation(_vfxAnimationContext, () => { });
        
        _logo = _game.Content.Load<Texture2D>("sineater-logo");
        _fmod = _game.Content.Load<Texture2D>("fmod-logo");
        _fmodCredits = _game.Content.Load<Texture2D>("fmod-credits");
        _ctx = new MainMenuStateContext() { LoaderTask = null, State = EMainMenuState.Starting, FadeTime = 0.0f, MenuOption = 0 };

        Task.Run(() =>
        {
            SineaterGame.Instance.World = CoreUtils.World.LoadOrCreate("Content\\world.json");
            _ctx.WorldLoaded = true;
            _vfxAnimation.Start();
            
            var goToMenuEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Menu);
            EventBus.Send(ref goToMenuEvent);
        });
    }
    
    public override void Update(GameTime gameTime)
    {
        if (_ctx.WorldLoaded)
        {
            if (_ctx.State == EMainMenuState.Fading)
            {
                _ctx.FadeTime += SineaterGame.DeltaTime;
                if (_ctx.FadeTime >= 1)
                {
                    if (_ctx.LoaderTask?.IsCompleted ?? false)
                    {
                        var goToDoneEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Done);
                        EventBus.Send(ref goToDoneEvent);
                    }
                }
            }
            else if (_ctx.State is EMainMenuState.Menu)
            {
                if (InputM.IsActive(EInputAction.MoveDown))
                {
                    _ctx.MenuOption = (_ctx.MenuOption + 1) % 3;
                }
                else if (InputM.IsActive(EInputAction.MoveUp))
                {
                    _ctx.MenuOption = _ctx.MenuOption - 1;
                    if (_ctx.MenuOption < 0)
                    {
                        _ctx.MenuOption = 2;
                    }
                }
                
                else if (InputM.IsActive(EInputAction.Confirm))
                {
                    switch (_ctx.MenuOption)
                    {
                        case 0:
                            Task.Run(() =>
                            {
                                try
                                {
                                    _ctx.LoaderTask = Task.Run(() =>
                                    {
                                        Enemies.Instance.Load();
                                        Items.Instance.Load();
                                        SineaterGame.Instance.Party.MakeParty();
                                        Console.WriteLine("DONE!");
                                    });

                                    var goToFadingEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Fading);
                                    EventBus.Send(ref goToFadingEvent);
                                }
                                catch (Exception e)
                                {
                                    var goToMenuEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Menu);
                                    EventBus.Send(ref goToMenuEvent);
                                }
                            });
                            break;
                        case 1:
                            var goToOptionsEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Options);
                            EventBus.Send(ref goToOptionsEvent);
                            break;
                        case 2:
                            SineaterGame.Instance.Exit();
                            break;
                    }
                }
            }
        }
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(_logo, new Vector2(game.Window.ClientBounds.Width / 2.0f, game.Window.ClientBounds.Height / 4.0f),
            null,
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);

        var mid = (int)(SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2.0f);
        if (_ctx.State == EMainMenuState.Starting)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, "Checking updates...");
        }
        else if (_ctx.State is EMainMenuState.Menu)
        {
            batch.DrawTextCenter(mid, 640, SineaterGame.Instance.Font, "START", _ctx.MenuOption == 0 ? Color.Gold : Color.White);
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, "OPTIONS", _ctx.MenuOption == 1 ? Color.Gold : Color.White);
            batch.DrawTextCenter(mid, 760, SineaterGame.Instance.Font, "QUIT", _ctx.MenuOption == 2 ? Color.Gold : Color.White);
        }

        batch.Draw(_fmod, new Vector2(35, game.Window.ClientBounds.Height - 100), new Rectangle(0, 0, 640, 164),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One * 0.35f, SpriteEffects.None, 0);

        batch.Draw(_fmodCredits, new Vector2(35, game.Window.ClientBounds.Height - 40), new Rectangle(0, 0, 428, 22),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One, SpriteEffects.None, 0);

        _vfxAnimation?.Update(mid, 700, new Drawing.RenderContext(batch, gameTime));
        
        var pixel = SineaterGame.Instance.Pixel;
        if (_ctx.State == EMainMenuState.Fading)
        {
            batch.Draw(pixel, new Vector2(0, 0), null,
                new Color(0, 0, 0, _ctx.FadeTime), 0.0f, new Vector2(0, 0),
                new Vector2(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height),
                SpriteEffects.None, 0);
        }
        else if (_ctx.State == EMainMenuState.Done)
        {
            batch.Draw(pixel, new Vector2(0, 0), null,
                new Color(0, 0, 0, 1), 0.0f, new Vector2(0, 0),
                new Vector2(game.Window.ClientBounds.Width, game.Window.ClientBounds.Height),
                SpriteEffects.None, 0);
        }

        // batch.Draw(_pg, new Vector2(game.Window.ClientBounds.Width / 2.0f + 40, game.Window.ClientBounds.Height / 2.0f),
        //     null,
        //     Color.White, 0.0f, new Vector2(_pg.Width / 2, _pg.Height / 2), Vector2.One * 4, SpriteEffects.None, 0);
        //
        // batch.Draw(_wizard, new Vector2(game.Window.ClientBounds.Width / 2.0f - 240, game.Window.ClientBounds.Height / 2.0f - 14 - 80),
        //     null,
        //     Color.White, 0.0f, new Vector2(_wizard.Width / 2, _wizard.Height), Vector2.One * 3, SpriteEffects.None, 0);
    }
}
