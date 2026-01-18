using System;
using SINEATER.Game.Graphics;

namespace SINEATER.Game.CoreUtils;

public interface Drawable
{
    public virtual void Update(Drawing.RenderContext renderContext)
    {}
}

public abstract class Animation(Drawable[] overrides, Action? onEnd = null)
{
    public Drawable[] Overrides { get; private set; } = overrides;

    public void Finished()
    {
        onEnd?.Invoke();
    }

    public abstract void Update(Drawing.RenderContext renderContext);
}