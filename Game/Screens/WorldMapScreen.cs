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
using SINEATER.Game.LookNFeel;
using SINEATER.Game.Save;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Reward = SINEATER.Game.Gameplay.Reward;

namespace SINEATER.Game.Screens;

public class WorldMapScreen() : Screen()
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    public (int X, int Y) CurrentPlayerPosition
    {
        get => SineaterGame.Instance.Party.CurrentPlayerPosition;
        set => SineaterGame.Instance.Party.CurrentPlayerPosition = value;
    }
    
    public WorldMapDrawable WorldMap;
    
    public override void Initialize()
    {
        WorldMap = new WorldMapDrawable(this);
        Camera = new OrthographicCamera(Game.GraphicsDevice);
    }

    public void UpdateCamera(GameTime gameTime)
    {
        var xy = WorldMap.CurrentLevel.Position;
        var s = WorldMap.CurrentLevel.Size;
        s.X /= 2;
        s.Y /= 2;
        var px = xy.X + s.X;
        var py = xy.Y + s.Y;
        px *= 4;
        py *= 4;
        px -= SineaterGame.Instance.GraphicsDevice.Viewport.Width / 2;
        py -= SineaterGame.Instance.GraphicsDevice.Viewport.Height / 4 + RESIZE * 16;
        px += 350;
        if (Camera != null)
        {
            Camera.Position = Vector2.Lerp(Camera.Position, new Vector2(px, py),
                (float)(gameTime.TotalGameTime.Milliseconds / 1000.0f * 0.5f).CubicEaseOut());
        }
    }
    
    public override void Update(EScreenFadeState fade, GameTime gameTime)
    {
        CheckPlayerInputs();
        UpdateCamera(gameTime);
    }

    public static readonly int RESIZE = 4;
    private int OFFSET_X = 24;
    private int OFFSET_Y = 96;
    public static Vector2 InWorld(int x, int y) => new((1 + x) * (RESIZE * 16), (1 + y) * (RESIZE * 16));
    public static Vector2 InWorld(Vector2 xy) => InWorld((int)xy.X, (int)xy.Y);
    
    public static Vector2 OutWorld(float x, float y) => new(x / (RESIZE * 16) - (RESIZE * 16), y / (RESIZE * 16) - (RESIZE * 16));
    public static Vector2 OutWorld(Vector2 xy) => OutWorld(xy.X, xy.Y);
    
    int dx = 16;
    int dy = -56;
    private float _mapSize = 0.4f;
    
    public override void Draw(EScreenFadeState fade, SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var rc = new Drawing.RenderContext(batch, gameTime);

        // In camera
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity, 
            effect: SineaterGame.Instance.Grayscale);

            WorldMap.DrawVisited(OFFSET_X + dx, OFFSET_Y + dy, rc);
        batch.End();
        
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);
        
            WorldMap.Update(OFFSET_X + dx, OFFSET_Y + dy, rc);
        batch.End();
        
        // GUI
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, 
            DepthStencilState.Default, rasterizerState);

            if (InputM.IsActive(EInputAction.MoveMapUp))
            {
                _mapSize += 0.1f;
            }
            else if (InputM.IsActive(EInputAction.MoveMapDown))
            {
                _mapSize -= 0.1f;
            } 
            
            // MAP
            var xy = new Vector2(950 + OFFSET_X, 200 + OFFSET_Y);
            xy -= WorldMap.CurrentLevel.Position.ToVector2() / 2 + WorldMap.CurrentLevel.Size.ToVector2() * 0.5f;
            foreach (var lvl in WorldMap.VisitedLevels)
            {
                SineaterGame.Instance.LDtkRenderer.RenderPrerenderedLevelRect(xy, lvl, 0, 
                    Vector2.One * _mapSize, color: new Color(29, 43, 83), fill: false);
            }
            SineaterGame.Instance.LDtkRenderer.RenderPrerenderedLevelRect(xy, WorldMap.CurrentLevel, 
                0, Vector2.One * _mapSize, color: new Color(131, 118, 156), fill: true);
            
            rc.Party(60, 800);
            
            batch.DrawText(100, 60, SineaterGame.RM.FontMono, $"Player position: {CurrentPlayerPosition}");
        batch.End();
    }
    
    private void CheckPlayerInputs()
    {
        if (InputM.IsActive(EInputAction.DebugStartCombat))
        {
            SineaterGame.Instance.ScreenStack.Push(new CombatScreen(this,CurrentPlayerPosition, new Encounter([]), new Reward([])));
        }
        
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
                    if (walk.GetValueAt(x - levelPos.X, y - levelPos.Y) != 1)
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
