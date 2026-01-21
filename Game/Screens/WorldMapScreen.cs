using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Particles.Modifiers.Interpolators;
using RogueSharp;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.ImGuiTools;
using Cell = RogueSharp.Cell;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using World = SINEATER.Game.CoreUtils.World;

namespace SINEATER.Game.Screens;

public class PartyAvatarContext
{
    public OrthographicCamera Camera;
    public int Index { get; set; } = 0;
    public Vector2? Destination { get; set; } = null;
}

public class PartyAvatarDrawable(PartyAvatarContext ctx, Vector2 pos) : IDrawable
{
    private float _time = 0.0f;
    private float _moveDelta = 0.0f;
    private Vector2 _position = pos;
    private Vector2 _destinationInWorld;
    
    public void Update(int x, int y, Drawing.RenderContext rc)
    {
        var screenOffset = new Vector2(x, y);
        var sh = SineaterGame.Instance.SpriteShadow;
        var ps = SineaterGame.Instance.PartySprites;
        var chosen = SineaterGame.Instance.Party.Characters[ctx.Index];
        var job = (int)chosen.Job;
        
        _time += rc.Time.ElapsedGameTime.Milliseconds;
        if (ctx.Destination == null)
        {
            _moveDelta = 0.0f;

            var xy = _position;
            rc.Batch.Draw(sh, screenOffset + xy - new Vector2(24, 16), new Rectangle(0, 0, 64, 64),
                Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
            rc.Batch.Draw(ps, screenOffset + xy - new Vector2(24, 20), new Rectangle(job * 64, 0, 64, 64),
                Color.White, 0, new Vector2(32, 64),
                new Vector2(3, 2.8f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f), SpriteEffects.None, 0);
        }
        else if (ctx.Destination is var (dx, dy))
        {
            _destinationInWorld = WorldMapScreen.InWorld((int)dx, (int)dy);
            
            _moveDelta += 5 * rc.Time.ElapsedGameTime.Milliseconds / 1000.0f;
            var xy = Vector2.Lerp(_position, _destinationInWorld, _moveDelta);

            rc.Batch.Draw(sh, screenOffset + xy - new Vector2(22, 16), new Rectangle(0, 0, 64, 64),
                Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
            rc.Batch.Draw(ps, screenOffset + xy - new Vector2(22, 20), new Rectangle(job * 64, 0, 64, 64),
                Color.White, 0, new Vector2(32, 64),
                new Vector2(3, 3), SpriteEffects.None, 0);
            if (_moveDelta >= 1.0f)
            {
                _position = _destinationInWorld;
                ctx.Destination = null;
                _moveDelta = 0.0f;
                _time = 0;
            }
        }
    }
}

public class WorldMapScreen(SineaterGame game) : Screen(game)
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    public (int X, int Y) CurrentPlayerPosition = (4, 8);

    public PartyAvatarContext _partyContext;
    public PartyAvatarDrawable _partyAvatar;

    public override void Initialize(SineaterGame game)
    {
        Camera = new OrthographicCamera(game.GraphicsDevice);
        _partyContext = new PartyAvatarContext() { Camera = Camera };
        _partyAvatar = new PartyAvatarDrawable(_partyContext, InWorld(4, 8));
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
    }
    
    public static Vector2 InWorld(int x, int y) => new Vector2(x * 48 + 48, y * 48 + 48);
    
    public override void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var xy = new Vector2(48, 96);
        // In camera
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);
        var wm = SineaterGame.Instance.WorldMap;
        batch.Draw(wm, xy, null, Color.White, 0, Vector2.Zero, Vector2.One * 3, SpriteEffects.None, 0);
        
        // for test
        for (var i = 0; i < 20; i++)
        {
            for (var j = 0; j < 20; j++)
            {
                if ((i + j) % 2 == 0)
                {
                    batch.Draw(SineaterGame.Instance.Semi, xy + InWorld(i, j), new Rectangle(0, 0, 24, 24),
                        new Color(0.8f, 0.25f, 0.45f, 0.3f), 0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
                }
            }
        }
        
        var rc = new Drawing.RenderContext(batch, gameTime);
        _partyAvatar.Update((int)xy.X, (int)xy.Y, rc);
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

        rc.Party(60, 800);
        
        batch.End();
    }
    
    private void CheckPlayerInputs()
    {
        if (!_partyContext.Destination.HasValue)
        {
            var up = InputM.IsActive(EInputAction.MoveUp);
            var down = InputM.IsActive(EInputAction.MoveDown);
            var left = InputM.IsActive(EInputAction.MoveLeft);
            var right = InputM.IsActive(EInputAction.MoveRight);
            
            if (up || down || left || right)
            {
                var dx = (left ? -1 : 0) + (right ? 1 : 0);
                var dy = (up ? -1 : 0) + (down ? 1 : 0);
                if ((dx == 0 || dy == 0) && (dx != 0 || dy != 0))
                {
                    if (CurrentPlayerPosition.X + dx < 0 
                        || CurrentPlayerPosition.Y + dy < 0 
                        || CurrentPlayerPosition.X + dx > 19 
                        || CurrentPlayerPosition.Y + dy > 19)
                        return;

                    var x = CurrentPlayerPosition.X + dx;
                    var y = CurrentPlayerPosition.Y + dy;

                    var playerPos = InWorld(x, y);
                    _partyContext.Destination = new Vector2(x, y);
                    CurrentPlayerPosition = (x, y);

                    // if (Maps[1].Map.IsWalkable(x, y))
                    // {
                    //     CurrentPlayerPosition.X = x;
                    //     CurrentPlayerPosition.Y = y;
                    // }
                    // else
                    // {
                    //     var tile = SineaterGame.Instance.World.Get(x, y);
                    //     if (SineaterGame.Instance.World.ECS.Has<Dialogue>(tile))
                    //     {
                    //         //CoroutineHandler.Run(new CoShowInspectText(this, World.GeneralDescriptions.Get(x, y)?.Text ?? $"<GENERAL DESCRIPTIONS MISSING AT {x}, {y}>"));
                    //     }
                    // }
                }
            }
        }
    }
}
