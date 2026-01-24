using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
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

    public PartyAvatarContext PartyContext;
    public PartyAvatarDrawable PartyAvatar;
    public WorldMapDrawable WorldMap;
    
    public override void Initialize(SineaterGame game)
    {
        WorldMap = new WorldMapDrawable(this);
        Camera = new OrthographicCamera(game.GraphicsDevice);
        PartyContext = new PartyAvatarContext() { Camera = Camera };
        PartyAvatar = new PartyAvatarDrawable(PartyContext, InWorld(CurrentPlayerPosition.X, CurrentPlayerPosition.Y));
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
    }

    private const int OFFSET_X = 40;
    private const int OFFSET_Y = 96;
    public static Vector2 InWorld(int x, int y) => new(x * 48 + 48, y * 48 + 48);
    public static Vector2 InWorld(Vector2 xy) => InWorld((int)xy.X, (int)xy.Y);
    
    public static Vector2 OutWorld(float x, float y) => new((x - 48) / 48, (y - 48) / 48);
    public static Vector2 OutWorld(Vector2 xy) => OutWorld(xy.X, xy.Y);
    
    public override void Draw(SpriteBatch batch, GameTime gameTime, RasterizerState rasterizerState)
    {
        var xy = new Vector2(OFFSET_X, OFFSET_Y);
        // In camera
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.Default, rasterizerState, transformMatrix: Camera?.GetViewMatrix() ?? Matrix.Identity);
        
        var rc = new Drawing.RenderContext(batch, gameTime);
        
        WorldMap.Update(OFFSET_X, OFFSET_Y, rc);
        PartyAvatar.Update(OFFSET_X, OFFSET_Y, rc);
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

                    if (WorldMap.Maps[1].Map.IsWalkable(x, y))
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
