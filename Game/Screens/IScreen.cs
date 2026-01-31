using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;

namespace SINEATER.Game.Screens;

public interface IScreen
{
    public EScreenFadeState FadeState { get; set; }
    public float FadeSpeed { get; set; }
    public IScreen? NextScreen { get; set; }
    
    public MonoGame.Extended.OrthographicCamera? Camera { get; set; }
    public void Initialize();
    public void Update(GameTime gameTime);
    public void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState);
}

public enum EScreenFadeState
{
    FadingIn,
    Stable,
    FadingOut,
}

public abstract class Screen : IScreen
{
    public EScreenFadeState FadeState { get; set; } = EScreenFadeState.FadingIn;
    public float FadeSpeed { get; set; } = 1.0f;
    public IScreen? NextScreen { get; set; } = null;
    private float _fadeStrength = 1.0f;
    
    protected readonly int FullWidth = 20, FullHeight = 20;
    protected int Width, Height;
    protected int Time = 0;
    public MonoGame.Extended.OrthographicCamera? Camera { get; set; } = null;
    
    public SineaterGame Game => SineaterGame.Instance;
    
    internal virtual (int X, int Y) DrawOffset { get; set; } = (8, 1);

    public Screen()
    {
        Initialize();
    }
    
    public virtual void Initialize() {}

    public virtual void OnFadeInComplete() {}

    public virtual void OnFadeOutComplete()
    {
        if (NextScreen != null)
        {
            Game.PopAndPushScreen(NextScreen);
            NextScreen = null;
        }
    }

    public virtual void Update(GameTime gameTime)
    {
        switch (FadeState)
        {
            case EScreenFadeState.FadingIn:
                _fadeStrength -= (gameTime.ElapsedGameTime.Milliseconds / 1000.0f) * FadeSpeed;
                if (_fadeStrength <= 0.0f)
                {
                    FadeState = EScreenFadeState.Stable;
                    OnFadeInComplete();
                }
                break;
            
            case EScreenFadeState.FadingOut:
                _fadeStrength += (gameTime.ElapsedGameTime.Milliseconds / 1000.0f) * FadeSpeed;
                if (_fadeStrength >= 0.0f)
                {
                    OnFadeOutComplete();
                }
                break;
            
            case EScreenFadeState.Stable when NextScreen != null:
                FadeState = EScreenFadeState.FadingOut;
                break;
            
            case EScreenFadeState.Stable:
                Update(FadeState, gameTime);
                break;
        }
    }

    public virtual void Update(EScreenFadeState fade, GameTime gameTime) {}

    public virtual void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        Draw(FadeState, batch, gameTime, rasterizerState);
        if (FadeState != EScreenFadeState.Stable)
        {
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, rasterizerState);
            batch.FillRectangle(new RectangleF(0, 0, Game.GraphicsDevice.Viewport.Width, Game.GraphicsDevice.Viewport.Height), new Color(0, 0, 0, _fadeStrength), 0);
            batch.End();
        }
    }
    
    public virtual void Draw(EScreenFadeState fade, SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState) {}
}