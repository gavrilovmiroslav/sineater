using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using RogueSharp;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.ImGuiTools;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using World = SINEATER.Game.CoreUtils.World;

namespace SINEATER.Game.Screens;

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

public class PartyAvatarDrawable(PartyAvatarContext ctx, Vector2 pos) : IDrawable
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

                rc.Batch.Draw(sh, screenOffset + _position - new Vector2(28, 16), new Rectangle(0, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                rc.Batch.Draw(ps, screenOffset + _position - new Vector2(28, 20), new Rectangle(job * 64, 0, 64, 64),
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

                    rc.Batch.Draw(sh, screenOffset + xy - new Vector2(28, 16), new Rectangle(0, 0, 64, 64),
                        Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                    rc.Batch.Draw(ps, screenOffset + xy - new Vector2(28, 20), new Rectangle(job * 64, 0, 64, 64),
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
                rc.Batch.Draw(sh, screenOffset + _position - new Vector2(28 + _facing < 0 ? 10 : 0, 16), new Rectangle(0, 0, 64, 64),
                    Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
                rc.Batch.Draw(ps, screenOffset + _position - new Vector2(28 + _facing < 0 ? 10 : 0, 16), new Rectangle(job * 64, 0, 64, 64),
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

public partial class PartyAvatarStateEventReceiver
{
    public PartyAvatarStateEventReceiver() { Hook(); }
    [Event] public void OnPartyAvatarStateChanged(ref PartyAvatarStateChanged ev) {}
}

public static class PartyAvatarEventHandler
{
    [Event(order: 1)]
    public static void OnPartyAvatarStateChanged(ref PartyAvatarStateChanged ev)
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

public class WorldMapScreen(SineaterGame game) : Screen(game)
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    public (int X, int Y) CurrentPlayerPosition = (4, 8);

    public Map OverworldMap;
    public PartyAvatarContext PartyContext;
    public PartyAvatarDrawable PartyAvatar;

    private void InitializeMapLayers()
    {
        var filePath = System.IO.Path.Combine(_game.Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        var rex = Image.Load(stream);
        
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var layer = rex.Layers[layerIndex];
            var visibilityMask = rex.Layers[layerIndex + 2];
            var levelMap = new Map<Cell>(20, 20);
            
            for (var y = 0; y < 20; y++)
            {
                for (var x = 0; x < 20; x++)
                {
                    var bg = layer[x, y].Background;
                    var transparent = visibilityMask[x, y].Character != 32;
                    var isAccessible = bg != SadRex.Color.Transparent && bg != new SadRex.Color(0, 0, 0);
                    levelMap.SetCellProperties(x, y, isAccessible || transparent, isAccessible);
                }
            }
            
            Maps[layerIndex] = (levelMap, new FieldOfView<Cell>(levelMap));
        }
    }

    public Dictionary<int, (RogueSharp.Map<Cell> Map, FieldOfView<Cell> Fov)> Maps = [];

    public override void Initialize(SineaterGame game)
    {
        InitializeMapLayers();
        Camera = new OrthographicCamera(game.GraphicsDevice);
        PartyContext = new PartyAvatarContext() { Camera = Camera };
        PartyAvatar = new PartyAvatarDrawable(PartyContext, InWorld(4, 8));
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
    }

    public const int OffsetX = 40;
    public const int OffsetY = 96;
    public static Vector2 InWorld(int x, int y) => new(x * 48 + 48, y * 48 + 48);
    public static Vector2 InWorld(Vector2 xy) => InWorld((int)xy.X, (int)xy.Y);
    
    public static Vector2 OutWorld(float x, float y) => new((x - 48) / 48, (y - 48) / 48);
    public static Vector2 OutWorld(Vector2 xy) => OutWorld(xy.X, xy.Y);
    
    public override void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var xy = new Vector2(OffsetX, OffsetY);
        // In camera
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);
        var wm = SineaterGame.Instance.WorldMap;
        batch.Draw(wm, xy, null, Color.White, 0, Vector2.Zero, Vector2.One * 3, SpriteEffects.None, 0);
        
        // for test
        for (var i = 0; i < 20; i++)
        {
            for (var j = 0; j < 12; j++)
            {
                if (!Maps[1].Map.IsWalkable(i, j))
                {
                    batch.Draw(SineaterGame.Instance.Pixel, xy + InWorld(i, j), new Rectangle(0, 0, 24, 24),
                        new Color(1.0f, 0.0f, 0.0f, 0.5f), 0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
                }
                else if ((i + j) % 2 == 0)
                {
                    batch.Draw(SineaterGame.Instance.Semi, xy + InWorld(i, j), new Rectangle(0, 0, 24, 24),
                        new Color(0.8f, 0.25f, 0.45f, 0.3f), 0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
                }
            }
        }
        
        var rc = new Drawing.RenderContext(batch, gameTime);
        PartyAvatar.Update((int)xy.X, (int)xy.Y, rc);
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

        rc.Party(60, 800);
        
        batch.End();
    }
    
    private void CheckPlayerInputs()
    {
        if (PartyContext.State == EPartyAvatarState.Idle)
        {
            var up = InputM.IsActive(EInputAction.MoveUp);
            var down = InputM.IsActive(EInputAction.MoveDown);
            var left = InputM.IsActive(EInputAction.MoveLeft);
            var right = InputM.IsActive(EInputAction.MoveRight);
            var change = InputM.IsActive(EInputAction.Confirm);
            if (up || down || left || right)
            {
                var dx = (left ? -1 : 0) + (right ? 1 : 0);
                var dy = (up ? -1 : 0) + (down ? 1 : 0);
                if ((dx == 0 || dy == 0) && (dx != 0 || dy != 0))
                {
                    if (CurrentPlayerPosition.X + dx < 0 
                        || CurrentPlayerPosition.Y + dy < 0 
                        || CurrentPlayerPosition.X + dx > 24 
                        || CurrentPlayerPosition.Y + dy > 11)
                        return;

                    var x = CurrentPlayerPosition.X + dx;
                    var y = CurrentPlayerPosition.Y + dy;

                    if (Maps[1].Map.IsWalkable(x, y))
                    {
                        var changeEvent = new PartyAvatarStateChanged(this, EPartyAvatarState.Moving, (x, y), (dx, dy)); 
                        EventBus.Send(ref changeEvent);
                    }
                }
            }
            else if (change)
            {
                var changeEvent = new PartyAvatarStateChanged(this, EPartyAvatarState.Change); 
                EventBus.Send(ref changeEvent);
            }
        }
    }
}
