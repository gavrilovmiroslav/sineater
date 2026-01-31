using System;
using System.Threading.Tasks;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using Color = Microsoft.Xna.Framework.Color;
using SINEATER.Game.Save;
using SINEATER.Tools;

namespace SINEATER.Game.Screens;

public enum EMainMenuState
{
    Starting,
    Menu,
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
                if (SineaterGame.Instance.ScreenStack.Peek() is { } peek)
                {
                    peek.NextScreen = new OptionsScreen();
                }
                
                ev.Menu.State = EMainMenuState.Menu;
                break;
            
            case EMainMenuState.Done:
                if (SineaterGame.Instance.ScreenStack.Peek() is { } world)
                {
                    world.NextScreen = new WorldMapScreen();
                }
                break;
        }
    }
}

public class MainMenuScreen() : Screen()
{
    private Texture2D _fmod;
    private Texture2D _fmodCredits;
    private MainMenuStateContext _ctx;
    
    private Texture2D _vfx;
    private GridAnimation _vfxAnimation;
    private GridAnimationContext _vfxAnimationContext;
    
    public override void Initialize()
    {
        _vfx = Game.Content.Load<Texture2D>("vfx11");
        _vfxAnimationContext = new GridAnimationContext(_vfx, (4, 4), 0.01f, Color.White, 2.0f);
        _vfxAnimation = new GridAnimation(_vfxAnimationContext, () => { });
        
        _fmod = Game.Content.Load<Texture2D>("fmod-logo");
        _fmodCredits = Game.Content.Load<Texture2D>("fmod-credits");
        _ctx = new MainMenuStateContext() { LoaderTask = null, State = EMainMenuState.Starting, FadeTime = 0.0f, MenuOption = 0 };

        Task.Run(() =>
        {
            Enemies.Instance.Load();
            Items.Instance.Load();

            SineaterGame.Instance.World = CoreUtils.World.LoadOrCreate("Content\\world.json");
            _ctx.WorldLoaded = true;
            _vfxAnimation.Start();
            
            var goToMenuEvent = new MainMenuChangeStateEvent(_ctx, EMainMenuState.Menu);
            EventBus.Send(ref goToMenuEvent);
        });
    }

    public int _menuItemsCount = SaveSystem.HasSave() ? 4 : 3;
    public override void Update(EScreenFadeState fade, GameTime gameTime)
    {
        if (_ctx.WorldLoaded)
        {
            if (_ctx.State is EMainMenuState.Menu)
            {
                if (InputM.IsActive(EInputAction.MoveDown))
                {
                    _ctx.MenuOption = (_ctx.MenuOption + 1) % _menuItemsCount;
                }
                else if (InputM.IsActive(EInputAction.MoveUp))
                {
                    _ctx.MenuOption = _ctx.MenuOption - 1;
                    if (_ctx.MenuOption < 0)
                    {
                        _ctx.MenuOption = _menuItemsCount - 1;
                    }
                }
                else if (InputM.IsActive(EInputAction.Confirm))
                {
                    var loadSave = SaveSystem.HasSave() && _ctx.MenuOption == 0;
                    switch (_ctx.MenuOption)
                    {
                        case 0:
                        case 1:
                            _ctx.LoaderTask = Task.Run(() =>
                            {
                                try
                                {
                                    if (loadSave)
                                    {
                                        SaveSystem.Load();
                                    }
                                    else
                                    {
                                        SineaterGame.Instance.Party.MakeParty();
                                        SaveSystem.Save();
                                    }
                                }
                                catch (Exception e)
                                {
                                    Crash.Report(e);
                                }
                            });
                            break;
                        case 2:
                            NextScreen = new OptionsScreen();
                            break;
                        case 3:
                            SineaterGame.Instance.Exit();
                            break;
                    }
                }
            }
        }
    }

    public override void Draw(EScreenFadeState fade, SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

        batch.Draw(SineaterGame.Instance.Logo, new Vector2(Game.Window.ClientBounds.Width / 2.0f, Game.Window.ClientBounds.Height / 4.0f),
            null,
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);

        var mid = (int)(SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2.0f);
        if (_ctx.State == EMainMenuState.Starting)
        {
            batch.DrawTextCenter(mid, 700, SineaterGame.Instance.Font, "Checking updates...");
        }
        else if (_ctx.State is EMainMenuState.Menu)
        {
            var top = 640;
            var height = 60;

            if (SaveSystem.HasSave())
            {
                batch.DrawTextCenter(mid, top, SineaterGame.Instance.Font, "CONTINUE", _ctx.MenuOption == (SaveSystem.HasSave() ? 0 : 1) ? Color.Gold : Color.White);
                top += height;
            }

            batch.DrawTextCenter(mid, top, SineaterGame.Instance.Font, "START", _ctx.MenuOption == (SaveSystem.HasSave() ? 1 : 0) ? Color.Gold : Color.White);
            batch.DrawTextCenter(mid, top + height, SineaterGame.Instance.Font, "OPTIONS", _ctx.MenuOption == (SaveSystem.HasSave() ? 1 : 0) + 1 ? Color.Gold : Color.White);
            batch.DrawTextCenter(mid, top + height + height, SineaterGame.Instance.Font, "QUIT", _ctx.MenuOption == (SaveSystem.HasSave() ? 1 : 0) + 2 ? Color.Gold : Color.White);
        }

        batch.Draw(_fmod, new Vector2(35, Game.Window.ClientBounds.Height - 100), new Rectangle(0, 0, 640, 164),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One * 0.35f, SpriteEffects.None, 0);

        batch.Draw(_fmodCredits, new Vector2(35, Game.Window.ClientBounds.Height - 40), new Rectangle(0, 0, 428, 22),
            Color.White, 0.0f, new Vector2(0, 0), Vector2.One, SpriteEffects.None, 0);

        _vfxAnimation?.Update(mid, 700, new Drawing.RenderContext(batch, gameTime));
        
        var pixel = SineaterGame.Instance.Pixel;
        if (_ctx.State == EMainMenuState.Done)
        {
            batch.Draw(pixel, new Vector2(0, 0), null,
                new Color(0, 0, 0, 1), 0.0f, new Vector2(0, 0),
                new Vector2(Game.Window.ClientBounds.Width, Game.Window.ClientBounds.Height),
                SpriteEffects.None, 0);
        }
        
        batch.End();
    }
}
