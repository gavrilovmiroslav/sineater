using System;
using System.Collections;
using System.Threading.Tasks;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Loadable;
using SINEATER.Tools.SinMod;
using Color = Microsoft.Xna.Framework.Color;
using World = Arch.Core.World;
using Arch.System;

namespace SINEATER.Game.Screens;

public enum EMainMenuState
{
    Starting,
    Waiting,
    Loading,
    LoadingFailed,
    Fading,
    Done
}

public class MainMenuStateContext
{
    public MainMenuScreen Screen;
    public EMainMenuState State;
    public float FadeTime;
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
            case EMainMenuState.Waiting:
                break;
            case EMainMenuState.Loading:
                Task.Run(() =>
                {
                    try
                    {
                        Enemies.Instance.Load();
                        Items.Instance.Load();
                        SineaterGame.Instance.Party.MakeParty();

                        var goToFadingEvent = new MainMenuChangeStateEvent(@event.Menu, EMainMenuState.Fading);
                        EventBus.Send(ref goToFadingEvent);
                    }
                    catch (Exception e)
                    {
                        var goToFailedEvent = new MainMenuChangeStateEvent(@event.Menu, EMainMenuState.LoadingFailed);
                        EventBus.Send(ref goToFailedEvent);
                    }
                });
                break;
            case EMainMenuState.Fading:
                Muse.SetGameState(EMusicState.World);
                break;
            case EMainMenuState.Done:
                SineaterGame.Instance.PopAndPushScreen(new WorldMapScreen(SineaterGame.Instance));
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
    private bool _worldLoaded = false;
    
    public override void Initialize(SineaterGame game)
    {
        _logo = _game.Content.Load<Texture2D>("sineater-logo");
        _fmod = _game.Content.Load<Texture2D>("fmod-logo");
        _fmodCredits = _game.Content.Load<Texture2D>("fmod-credits");
        _ctx = new MainMenuStateContext() { Screen = this, State = EMainMenuState.Starting, FadeTime = 0.0f };

        Task.Run(() =>
        {
            SineaterGame.Instance.World = CoreUtils.World.LoadOrCreate("Content\\world.json");
            _worldLoaded = true;
            var goToWaitingEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Waiting);
            EventBus.Send(ref goToWaitingEvent);
        });
    }
    
    public override void Update(GameTime gameTime)
    {
        if (_worldLoaded)
        {
            if (_ctx.State == EMainMenuState.Fading)
            {
                _ctx.FadeTime += SineaterGame.DeltaTime;
                if (_ctx.FadeTime >= 1)
                {
                    var goToDoneEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Done);
                    EventBus.Send(ref goToDoneEvent);
                }
            }
            else if (_ctx.State is EMainMenuState.Waiting or EMainMenuState.LoadingFailed && InputM.IsActive(EInputAction.Confirm))
            {
                var goToLoadingEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Loading);
                EventBus.Send(ref goToLoadingEvent);
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
        else if (_ctx.State == EMainMenuState.Waiting)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, "Press [SPACE] to start");
        }
        else if (_ctx.State == EMainMenuState.Loading)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, $"Loading...");
        }
        else if (_ctx.State == EMainMenuState.LoadingFailed)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, $"Loading failed?");
        }

        batch.Draw(_fmod, new Vector2(35, game.Window.ClientBounds.Height - 100), new Rectangle(0, 0, 640, 164),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One * 0.35f, SpriteEffects.None, 0);

        batch.Draw(_fmodCredits, new Vector2(35, game.Window.ClientBounds.Height - 40), new Rectangle(0, 0, 428, 22),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One, SpriteEffects.None, 0);

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
    }
}
