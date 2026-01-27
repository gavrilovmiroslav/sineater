using System;
using System.Linq;
using Arch.Bus;
using LDtkTypes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay.WorldMap;
using SINEATER.Game.Graphics;
using SINEATER.Game.Save;
namespace SINEATER.Game.Screens;

public class WorldMapScreen(SineaterGame game) : Screen(game)
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    public (int X, int Y) CurrentPlayerPosition
    {
        get => SineaterGame.Instance.Party.CurrentPlayerPosition;
        set => SineaterGame.Instance.Party.CurrentPlayerPosition = value;
    }
    
    public WorldMapDrawable WorldMap;
    
    public override void Initialize(SineaterGame game)
    {
        WorldMap = new WorldMapDrawable(this);
        Camera = new OrthographicCamera(game.GraphicsDevice);
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
        var xy = WorldMap.CurrentLevel.Position;
        var s = WorldMap.CurrentLevel.Size;
        s.X /= 2;
        s.Y /= 2;
        var px = xy.X + s.X;
        var py = xy.Y + s.Y;
        px *= 5;
        py *= 5;
        px -= SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2;
        py -= SineaterGame.Instance.GraphicsDevice.Viewport.Height / 4 + 80;
        if (Camera != null)
        {
            Camera.Position = Vector2.Lerp(Camera.Position, new Vector2(px, py),
                gameTime.TotalGameTime.Milliseconds / 1000.0f);
            Console.WriteLine($"Going to {px}, {py}");
        }
    }

    private int OFFSET_X = 24;
    private int OFFSET_Y = 96;
    public static Vector2 InWorld(int x, int y) => new((1 + x) * 80, (1 + y) * 80);
    public static Vector2 InWorld(Vector2 xy) => InWorld((int)xy.X, (int)xy.Y);
    
    public static Vector2 OutWorld(float x, float y) => new(x / 80 - 80, y / 80 - 80);
    public static Vector2 OutWorld(Vector2 xy) => OutWorld(xy.X, xy.Y);
    
    int dx = 16;
    int dy = -56;
    
    public override void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var rc = new Drawing.RenderContext(batch, gameTime);
        // In camera
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);

            if (InputM.IsActive(EInputAction.ShowHelp))
            {
                dx += (InputM.IsActive(EInputAction.MoveMapLeft) ? -1 : 0);
                dx += (InputM.IsActive(EInputAction.MoveMapRight) ? 1 : 0);
                dy += (InputM.IsActive(EInputAction.MoveMapUp) ? -1 : 0);
                dy += (InputM.IsActive(EInputAction.MoveMapDown) ? 1 : 0);
                batch.DrawText(500, 100, SineaterGame.Instance.FontMono, $"{dx} {dy}", Color.White);
            }

            WorldMap.Update(OFFSET_X + dx, OFFSET_Y + dy, rc);
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

            rc.Party(60, 800);
            
            batch.DrawText(100, 60, SineaterGame.Instance.FontMono, $"Player position: {CurrentPlayerPosition}");
        batch.End();
    }
    
    private void CheckPlayerInputs()
    {
        if (WorldMap.PartyContext.State == EPartyAvatarState.Idle)
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
                    var x = CurrentPlayerPosition.X + dx;
                    var y = CurrentPlayerPosition.Y + dy;

                    var ok = true;
                    if (WorldMap.CurrentLevel.GetLDTKEntity<Exit>(CurrentPlayerPosition.X, CurrentPlayerPosition.Y) is { } exit)
                    {
                        if (exit.Component is { Endpoint: not null })
                        {
                            switch ((exit.Component.Direction, dx, dy))
                            {
                                case (Direction.DOWN, 0, 1):
                                case (Direction.UP, 0, -1):
                                case (Direction.LEFT, -1, 0):
                                case (Direction.RIGHT, 1, 0):
                                    ok = true;
                                    break;
                                default:
                                    ok = false;
                                    break;
                            }

                            if (ok)
                            {
                                var endpoint = exit.Component.Endpoint;
                                var level =
                                    SineaterGame.Instance.LDTKWorld.Levels.First(l => l.Iid == endpoint.LevelIid);
                                var ex = level
                                    .GetLDKTEntities<Exit>()
                                    .First(e => e.Instance.Iid == endpoint.EntityIid);

                                var changeLevelEvent = new PartyAvatarLevelChanged(this,
                                    (ex.Instance._Grid.X, ex.Instance._Grid.Y), (dx, dy), level);
                                EventBus.Send(ref changeLevelEvent);
                                return;
                            }
                        }
                    }

                    var wlevel = WorldMap.CurrentLevel;
                    var levelPos = wlevel.Position;
                    levelPos.X /= 16;
                    levelPos.Y /= 16;
                    var levelSize = wlevel.Size;
                    levelSize.X /= 16;
                    levelSize.Y /= 16;
                    
                    if (x < levelPos.X || y < levelPos.Y || x >= levelPos.X + levelSize.X || y >= levelPos.Y + levelSize.Y)
                    {
                        return;
                    }
                    
                    var walk = WorldMap.CurrentLevel.GetIntGrid("Walkable");
                    if (walk.GetValueAt(x, y) != 1)
                    {
                        var changeEvent = new PartyAvatarStateChanged(this, EPartyAvatarState.Moving, (x, y), (dx, dy)); 
                        EventBus.Send(ref changeEvent);
                        
                        var onMoved = new PartyAvatarMoved(this, (x, y));
                        EventBus.Send(ref onMoved);
                    }
                }
            }
            else if (change)
            {
                var changeEvent = new PartyAvatarStateChanged(this, EPartyAvatarState.Change); 
                EventBus.Send(ref changeEvent);
            }
        }

        if (InputM.IsActive(EInputAction.Save))
        {
            SaveSystem.Save();
        }
    }
}
