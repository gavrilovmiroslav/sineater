using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.Graphics;

namespace SINEATER.Game.CoreUtils;

public interface IDrawable
{
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {}
}

public abstract class Animation(IDrawable[] overrides, Action? onEnd = null) : IDrawable
{
    public IDrawable[] Overrides { get; private set; } = overrides;

    public void Finished()
    {
        onEnd?.Invoke();
    }

    public abstract void Update(int x, int y, Drawing.RenderContext renderContext);
}

public record struct GridAnimationContext(
    Texture2D Source,
    (int X, int Y) Grid,
    float FrameLength,
    Color Tint,
    float Scale
);

public class GridAnimation(GridAnimationContext ctx, Action? onEnd = null) : Animation([], onEnd)
{
    private bool _started = false;
    private bool _finished = false;
    private float _time = 0.0f;
    private int _frame = 0;
    private readonly int _width = ctx.Source.Width / ctx.Grid.X;
    private readonly int _height = ctx.Source.Height / ctx.Grid.Y;
    
    private (int X, int Y) IndexToGrid(int index)
    {
        var x = index % ctx.Grid.X;
        var y = index / ctx.Grid.Y;
        return (x, y);
    }

    public void Start()
    {
        _started = true;
    }
    
    public override void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        if (!_started) return;
        if (_finished) return;
        
        var frameCount = ctx.Grid.X * ctx.Grid.Y;
        _time += (float)renderContext.Time.ElapsedGameTime.Milliseconds / 1000.0f;
        if (_time >= ctx.FrameLength)
        {
            _time = 0.0f;
            _frame++;
            if (_frame >= frameCount)
            {
                _finished = true;
                Finished();
                return;
            }
        }
        
        var (fx, fy) = IndexToGrid(_frame);
        renderContext.Batch.Draw(
            ctx.Source, new Vector2(x, y), 
            new Rectangle(_width * fx, _height * fy, _width, _height), 
            Color.White, 0.0f, 
            new Vector2(_width / 2, _height / 2), 
            new Vector2(ctx.Scale, ctx.Scale), SpriteEffects.None, 0);
    }
}