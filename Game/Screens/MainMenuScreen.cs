using System;
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
    Waiting,
    Loading,
    Fading,
    Done
}

public class MainMenuStateContext
{
    public MainMenuScreen Screen;
    public EMainMenuState State;
    public float FadeTime;
}

public record struct MainMenuChangeContext(MainMenuStateContext Menu, EMainMenuState Next);
public record struct MainMenuChangeStateEvent(MainMenuChangeContext Context);

public partial class MainMenuStateEventReceiver
{
    public MainMenuStateEventReceiver() { Hook(); }
    [Event] public void OnChangeState(ref MainMenuChangeStateEvent ev) {}
}

public static class MainMenuEventHandler
{
    [Event(order: 1)]
    public static void OnLoadEvent(ref MainMenuChangeStateEvent ev)
    {
        Console.WriteLine("MAIN MENU EVENT HANDLER");
        Console.WriteLine(ev);
        ev.Context.Menu.State = EMainMenuState.Loading;
        var @event = ev;
        Task.Run(() =>
        {
            try
            {
                Enemies.Instance.Load();
                Items.Instance.Load();
                SineaterGame.Instance.Party.MakeParty();

                var loadEvent = new MainMenuChangeStateEvent(new MainMenuChangeContext(@event.Context.Menu, EMainMenuState.Fading));
                EventBus.Send(ref loadEvent);
            }
            catch (Exception e)
            {
                Console.WriteLine("Loading failed: " + e.ToString());
            }
        });
    }
}

public partial class MainMenuStateUpdateSystem(World world) : BaseSystem<World, MainMenuStateContext>(world)
{
    [Query]
    public void UpdateMainMenuState([Data] in MainMenuStateContext ctx)
    {
        if (ctx is { State: EMainMenuState.Fading, FadeTime: > 1.0f })
        {
            ctx.State = EMainMenuState.Done;
            SineaterGame.Instance.PopAndPushScreen(new WorldMapScreen(SineaterGame.Instance));
        }
        
        switch (ctx.State)
        {
            case EMainMenuState.Waiting:
                if (InputM.IsActive(EInputAction.Confirm))
                {
                    var loadEvent = new MainMenuChangeStateEvent(new MainMenuChangeContext(ctx, EMainMenuState.Loading));
                    EventBus.Send(ref loadEvent);
                }
                break;
            case EMainMenuState.Fading:
                Muse.SetGameState(EMusicState.World);
                break;
            default:
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
    private Group<MainMenuStateContext>? _systems = null;
    private bool _systemsLoaded = false;
    
    public override void Initialize(SineaterGame game)
    {
        _logo = _game.Content.Load<Texture2D>("sineater-logo");
        _fmod = _game.Content.Load<Texture2D>("fmod-logo");
        _fmodCredits = _game.Content.Load<Texture2D>("fmod-credits");
        _ctx = new MainMenuStateContext() { Screen = this, State = EMainMenuState.Waiting, FadeTime = 0.0f };

        Task.Run(() =>
        {
            SineaterGame.Instance.World = CoreUtils.World.LoadOrCreate("Content\\world.json");
            _systems = new Group<MainMenuStateContext>("Main Menu", new MainMenuStateUpdateSystem(SineaterGame.Instance.World.ECS));
            _systems.Initialize();
            _systemsLoaded = true;
            Console.WriteLine("Systems loaded!");
        });

    }
    
    public override void Update(GameTime gameTime)
    {
        if (_systemsLoaded && _systems is not null)
        {
            _systems.BeforeUpdate(in _ctx);
            _systems.Update(in _ctx);
            _systems.AfterUpdate(in _ctx);
        }
    }
    
    public override void LayerDraw(GameTime gameTime)
    {
    }
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        var mid = (int) (SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2.0f);
        if (_ctx.State == EMainMenuState.Waiting)
        {
            batch.DrawTextCenter(mid + Rnd.Instance.D4 - 2, 700 + Rnd.Instance.D4 - 2, SineaterGame.Instance.Font, "Press [SPACE] to start");
        }
        else if (_ctx.State == EMainMenuState.Loading)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, $"Loading...");
        }
    }

    public override void PreDraw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(_logo, new Vector2(game.Window.ClientBounds.Width / 2.0f, game.Window.ClientBounds.Height / 4.0f), null, 
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);
    }
    
    public override void PostDraw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(_fmod, new Vector2(35, game.Window.ClientBounds.Height - 100), new Rectangle(0, 0, 640, 164), 
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One * 0.35f, SpriteEffects.None, 0);

        batch.Draw(_fmodCredits, new Vector2(35, game.Window.ClientBounds.Height - 40), new Rectangle(0, 0, 428, 22), 
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One, SpriteEffects.None, 0);

        var pixel = SineaterGame.Instance.Pixel; 
        if (_ctx.State == EMainMenuState.Fading)
        {
            _ctx.FadeTime += 0.01f;
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
