using System;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SINEATER.Game.Graphics;
using SINEATER.Game.LookNFeel;
using SINEATER.Game.Screens;

namespace SINEATER.Game.Gameplay.WorldMap;


public enum EPartyAvatarState
{
    Idle,
    Moving,
    Change,
}

public class PartyAvatarContext
{
    public EPartyAvatarState State = EPartyAvatarState.Idle;
    public OrthographicCamera Camera;
    public int Index { get; set; } = 0;
    public Vector2? Destination { get; set; } = null;
    public Vector2 Delta { get; set; } = Vector2.Zero;
}

public class PartyAvatarDrawable(PartyAvatarContext ctx, Vector2 pos) : CoreUtils.IDrawable
{
    private float _time = 0.0f;
    private float _moveDelta = 0.0f;
    private float _facing = 1.0f;
    private Vector2 _position = pos;
    public Vector2 Position => _position;
    private Vector2 _destinationInWorld;
    private bool _changed = false;
    
    public void Update(int x, int y, Drawing.RenderContext rc)
    {
        var screenOffset = new Vector2(x, y);
        var sh = SineaterGame.Instance.SpriteShadow;
        var ps = SineaterGame.Instance.PartySprites;
        var chosen = SineaterGame.Instance.Party.Characters[ctx.Index];
        var job = (int)chosen.Job;
        
        _time += rc.Time.ElapsedGameTime.Milliseconds;
        switch (ctx.State)
        {
            case EPartyAvatarState.Idle:
                _moveDelta = 0.0f;

                rc.Batch.Draw(sh, screenOffset + _position - new Vector2(28 - (_facing < 0 ? 8 : 0), 16), new Rectangle(0, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                rc.Batch.Draw(ps, screenOffset + _position - new Vector2(28 - (_facing < 0 ? 8 : 0), 20), new Rectangle(job * 64, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64),
                    new Vector2(3, 2.8f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f), 
                    _facing > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
                break;
            
            case EPartyAvatarState.Moving:
                if (ctx.Destination is var (dx, dy))
                {
                    _destinationInWorld = WorldMapScreen.InWorld((int)dx, (int)dy);
                    if (ctx.Delta.X != 0)
                    {
                        _facing = Math.Sign(ctx.Delta.X);
                    }

                    _moveDelta += 5 * rc.Time.ElapsedGameTime.Milliseconds / 1000.0f;
                    var c = _moveDelta.Low(0.3f, Easing.CubicEaseOut);
                    var xy = Vector2.Lerp(_position, _destinationInWorld, c);

                    rc.Batch.Draw(sh, screenOffset + xy - new Vector2(28 - (_facing < 0 ? 8 : 0), 16), new Rectangle(0, 0, 64, 64),
                        Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                    rc.Batch.Draw(ps, screenOffset + xy - new Vector2(28 - (_facing < 0 ? 8 : 0), 20), new Rectangle(job * 64, 0, 64, 64),
                        Color.White, 0, new Vector2(32, 64),
                        new Vector2(
                            3.0f + float.Lerp(0.0f, 0.5f, c), 
                            3.0f - float.Lerp(0.0f, 0.5f, c)), 
                        _facing > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
                    if (_moveDelta >= 1.0f)
                    {
                        _position = _destinationInWorld;
                        ctx.Destination = null;
                        _moveDelta = 0.0f;
                        _time = 0.5f;
                        ctx.State = EPartyAvatarState.Idle;
                    }
                }
                break;
            
            case EPartyAvatarState.Change:
                if (_moveDelta == 0.0f)
                {
                    _changed = false;
                }
                _moveDelta += 5 * rc.Time.ElapsedGameTime.Milliseconds / 1000.0f;

                if (_moveDelta > 0.5f && !_changed)
                {
                    ctx.Index = (ctx.Index + 1) % 4;
                    _changed = true;
                }
                
                var a = MathF.Sin(180.0f * _moveDelta * MathF.PI / 180.0f) * 0.5f;
                if (_moveDelta is < 0.2f or >= 0.8f)
                {
                    a = -0.05f;
                }
                rc.Batch.Draw(sh, screenOffset + _position - new Vector2(28 - (_facing < 0 ? 8 : 0), 16), new Rectangle(0, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                rc.Batch.Draw(ps, screenOffset + _position - new Vector2(28 - (_facing < 0 ? 8 : 0), 16), new Rectangle(job * 64, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64), new Vector2(3, 3 - a * 2.8f), 
                    _facing > 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
                if (_moveDelta >= 1.0f)
                {
                    _moveDelta = 0.0f;
                    _time = 0.5f;
                    ctx.State = EPartyAvatarState.Idle;
                }
                break;
        }
    }
}

public record struct PartyAvatarStateChanged(WorldMapScreen Screen, EPartyAvatarState NewState, (int X, int Y)? XY = null, (int DX, int DY)? Delta = null);
public record struct PartyAvatarMoved(WorldMapScreen Screen, (int X, int Y) NewPosition);

public partial class PartyAvatarStateEventReceiver
{
    public PartyAvatarStateEventReceiver() { Hook(); }
    [Event] public void OnPartyAvatarStateChangedEvent(ref PartyAvatarStateChanged ev) {}

    [Event]
    public void OnPartyAvatarMovedEvent(ref PartyAvatarMoved ev) {}
}

public static class PartyAvatarEventHandler
{
    [Event(order: 1)]
    public static void OnPartyAvatarStateChangedEvent(ref PartyAvatarStateChanged ev)
    {
        if (ev.XY is var (x, y))
        {
            ev.Screen.PartyContext.Delta = new Vector2(ev.Delta?.DX ?? 0, ev.Delta?.DY ?? 0);
            ev.Screen.PartyContext.Destination = new Vector2(x, y);
            ev.Screen.CurrentPlayerPosition = ev.XY!.Value;
        }
        
        ev.Screen.PartyContext.State = ev.NewState;
    }
}
