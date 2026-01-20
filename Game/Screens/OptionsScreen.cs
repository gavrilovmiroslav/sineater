using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;

namespace SINEATER.Game.Screens;

public enum EOption
{
    MasterVolume = 0,
    SfxVolume = 1,
    MusicVolume = 2,
}

public interface IOptionDrawable : IDrawable;

public class BlankDrawable : IDrawable
{
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
    }
}

public class OptionHeaderDrawable(string header) : IDrawable
{
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        renderContext.Batch.DrawText(x - 150, y - 16, SineaterGame.Instance.FontBold, header, Color.White);
    }
}

// runtime
public record struct RangeOptionContext(int Value, bool Selected);

// initial
public class RangeOptionDrawable(int min, int max, string name, int value, Action<int> action, bool selected = false) : IOptionDrawable
{
    public int Min => min;
    public int Max => max;

    public Action<int> Action => action;
    public RangeOptionContext Context { get; set; } = new(value, selected);

    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        renderContext.Batch.DrawText(x - 100, y - 16, SineaterGame.Instance.Font, name, Context.Selected ? Color.Gold : Color.White);
        renderContext.FrameEdge(x + 200, y - 8, max * 16, 3, 0, Color.Gray);
        renderContext.FrameEdge(x + 200, y - 8, Context.Value * 16, 3, 0, Context.Selected ? Color.Gold : Color.White);
        renderContext.Batch.DrawText(x + 400, y - 16, SineaterGame.Instance.Font, $"{Context.Value}", Context.Selected ? Color.Gold : Color.White);
    }
}

public class OptionsStateContext
{
    public int MenuOption;
}

public interface IOptionChangedEvent
{
    int Value { get; set; }
};

public record struct MasterVolumeChanged(int Value) : IOptionChangedEvent;
public record struct SFXVolumeChanged(int Value) : IOptionChangedEvent;
public record struct MusicVolumeChanged(int Value) : IOptionChangedEvent;

public partial class OptionsStateEventReceiver
{
    public OptionsStateEventReceiver() { Hook(); }
    [Event] public void OnMasterVolumeChangedEvent(ref MasterVolumeChanged ev) {}
    [Event] public void OnSfxVolumeChangedEvent(ref SFXVolumeChanged ev) {}
    [Event] public void OnMusicVolumeChangedEvent(ref MusicVolumeChanged ev) {}
}

public static class OptionsEventHandler
{
    [Event(order: 1)]
    public static void OnMasterVolumeChangedEvent(ref MasterVolumeChanged ev)
    {
        SineaterGame.Instance.CurrentOptions.MasterVolume = ev.Value;
        SineaterGame.Instance.CurrentOptions.Save();
    }

    [Event]
    public static void OnSfxVolumeChangedEvent(ref SFXVolumeChanged ev)
    {
        SineaterGame.Instance.CurrentOptions.SfxVolume = ev.Value;
        SineaterGame.Instance.CurrentOptions.Save();
    }

    [Event]
    public static void OnMusicVolumeChangedEvent(ref MusicVolumeChanged ev)
    {
        SineaterGame.Instance.CurrentOptions.MusicVolume = ev.Value;
        SineaterGame.Instance.CurrentOptions.Save();
    }
}

public class OptionsScreen(SineaterGame game) : Screen(game)
{
    private OptionsStateContext _ctx;
    private readonly List<IDrawable> _drawables = [];
    private readonly List<WeakReference<IOptionDrawable>> _options = [];

    public override void Initialize(SineaterGame game)
    {
        _ctx = new OptionsStateContext() { MenuOption = 0};
        var opts = SineaterGame.Instance.CurrentOptions;
        
        _drawables.Add(new OptionHeaderDrawable("SOUND & MUSIC"));
        _drawables.Add(new RangeOptionDrawable(0, 10, "Master Volume", opts.MasterVolume, (int v) =>
        {
            MasterVolumeChanged ev = new(v); EventBus.Send(ref ev);
        }, true));
        _drawables.Add(new RangeOptionDrawable(0, 10, "SFX Volume", opts.SfxVolume, (int v) =>
        {
            SFXVolumeChanged ev = new(v); EventBus.Send(ref ev);
        }));
        _drawables.Add(new RangeOptionDrawable(0, 10, "Music Volume", opts.MusicVolume, (int v) =>
        {
            MusicVolumeChanged ev = new(v); EventBus.Send(ref ev);
        }));

        foreach (var drawable in _drawables.Where(d => d is IOptionDrawable))
        {
            _options.Add(new WeakReference<IOptionDrawable>(drawable as IOptionDrawable));
        }
    }
    
    public override void Update(GameTime gameTime)
    {
        void Unselect()
        {
            if (_options[_ctx.MenuOption].TryGetTarget(out var old))
            {
                if (old is RangeOptionDrawable range)
                {
                    range.Context = range.Context with { Selected = false };
                }
            }
        }
        
        void Select()
        {
            if (_options[_ctx.MenuOption].TryGetTarget(out var old))
            {
                if (old is RangeOptionDrawable range)
                {
                    range.Context = range.Context with { Selected = true };
                }
            }
        }

        var size = Enum.GetNames(typeof(EOption)).Length;
        if (InputM.IsActive(EInputAction.MoveDown))
        {
            Unselect();
            _ctx.MenuOption = (_ctx.MenuOption + 1) % size;
            Select();
        }
        else if (InputM.IsActive(EInputAction.MoveUp))
        {
            Unselect();
            _ctx.MenuOption -= 1;
            if (_ctx.MenuOption < 0)
            {
                _ctx.MenuOption = size - 1;
            }
            Select();
        }
        else if (InputM.IsActive(EInputAction.MoveLeft))
        {
            if (_options[_ctx.MenuOption].TryGetTarget(out var tgt))
            {
                if (tgt is RangeOptionDrawable range)
                {
                    range.Context = range.Context with { Value = Math.Max(range.Min, range.Context.Value - 1) };
                    range.Action.Invoke(range.Context.Value);
                }
            }
        }
        else if (InputM.IsActive(EInputAction.MoveRight))
        {
            if (_options[_ctx.MenuOption].TryGetTarget(out var tgt))
            {
                if (tgt is RangeOptionDrawable range)
                {
                    range.Context = range.Context with { Value = Math.Min(range.Max, range.Context.Value + 1) };
                    range.Action.Invoke(range.Context.Value);
                }
            }
        }
        else if (InputM.IsActive(EInputAction.Exit))
        {
            SineaterGame.Instance.ScreenStack.Pop();
        }
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        batch.Draw(SineaterGame.Instance.Logo, new Vector2(game.Window.ClientBounds.Width / 2.0f, game.Window.ClientBounds.Height / 4.0f),
            null,
            Color.White, 0.0f, new Vector2(266, 102), Vector2.One, SpriteEffects.None, 0);

        var ctx = new Drawing.RenderContext(batch, gameTime);
        for (int i = 0; i < _drawables.Count; i++)
        {
            _drawables[i].Update(480, 500 + i * 40, ctx);
        }
    }
}
