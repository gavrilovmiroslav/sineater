using System;
using System.Collections.Generic;
using System.Text;
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
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using World = SINEATER.Game.CoreUtils.World;

namespace SINEATER.Game.Screens;

public class WorldMapScreen(SineaterGame game) : Screen(game)
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    public (int X, int Y) CurrentPlayerPosition = (4, 8);

    public override void Initialize(SineaterGame game)
    {
        Camera = new OrthographicCamera(game.GraphicsDevice);
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
    }
    
    private Vector2 InWorld(int x, int y) => new Vector2(x * 48 + 50, y * 48 + 50);
    private float _time = 0;
    public override void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        _time += gameTime.ElapsedGameTime.Milliseconds;
        // In camera
        var (x, y) = CurrentPlayerPosition;
        var playerPos = InWorld(x, y);
        
        if (Camera != null) Camera.LookAt(playerPos);

        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);
        var wm = SineaterGame.Instance.WorldMap;
        batch.Draw(wm, new Vector2(0, 0), null, Color.White, 0, Vector2.Zero, Vector2.One * 3, SpriteEffects.None, 0);
        var pxy = InWorld(x, y);

        var sh = SineaterGame.Instance.SpriteShadow;
        var ps = SineaterGame.Instance.PartySprites;
        var chosen = SineaterGame.Instance.Party.Characters[0];
        var job = (int)chosen.Job;
        batch.Draw(sh, playerPos - new Vector2(24, 20), new Rectangle(0, 0, 64, 64), 
            Color.White, 0, new Vector2(32, 64), Vector2.One * 3, SpriteEffects.None, 0);
        batch.Draw(ps, playerPos - new Vector2(24, 24), new Rectangle(job * 64, 0, 64, 64), 
            Color.White, 0, new Vector2(32, 64), new Vector2(3, 2.9f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f), SpriteEffects.None, 0);
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

        var rc = new Drawing.RenderContext(batch, gameTime);
        rc.Party(60, 800);
        
        batch.End();
    }
    
    public override void LayerDraw(GameTime gameTime)
    {
    }
    
    private void CheckPlayerInputs()
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
                if (CurrentPlayerPosition.X + dx < 0 || CurrentPlayerPosition.Y + dy < 0 
                    || CurrentPlayerPosition.X + dx > 19 || CurrentPlayerPosition.Y + dy > 19)
                    return;

                var x = CurrentPlayerPosition.X + dx;
                var y = CurrentPlayerPosition.Y + dy;
                
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
