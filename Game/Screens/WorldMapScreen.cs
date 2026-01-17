using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SadRex;
using SINEATER.Game.Components;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.ImGuiTools;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using World = SINEATER.Game.CoreUtils.World;

namespace SINEATER.Game.Screens;

public class WorldMapScreen(SineaterGame game) : Screen(game)
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private readonly Dictionary<int, (Map<Cell> Map, FieldOfView<Cell> Fov)> Maps = [];
    private Image _rex;
    HashSet<(int,int)> _visited = [];

    public (int X, int Y) CurrentPlayerPosition = (2, 7);
    public ETimeOfDay TimeOfDay = ETimeOfDay.Morning;
    public int HoursOfDay = 0;

    private World _world = null;
    public World World => _world;

    public override void Initialize(SineaterGame game)
    {
        _world = World.LoadOrCreate("Content\\world.json");
        SineaterGame.Instance.World = _world;
        
        var filePath = System.IO.Path.Combine(_game.Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        
        var colors = TitleContainer.OpenStream("Content\\colors.json");
        var c = string.Join("\n", colors.ReadLines(Encoding.Default));
        Ambient.Atmospheres = DataSerializer.Load<Atmospheres>(c);
    }
    
    public override void Update(GameTime gameTime)
    {
        CheckPlayerInputs();
    }
    
    
    public void DrawCharacter(Character c, int x, int y, SpriteBatch batch)
    {
    }
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        var rc = new Drawing.RenderContext(batch, gameTime);
        for (var i = 0; i < 4; i++)
        {
            rc.CharacterProfile(60 + 300 * i, 800, SineaterGame.Instance.Party.Characters[i], i, false);
        }
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
                
                if (Maps[1].Map.IsWalkable(x, y))
                {
                    CurrentPlayerPosition.X = x;
                    CurrentPlayerPosition.Y = y;
                }
                else
                {
                    var tile = World.Get(x, y);
                    if (World.ECS.Has<CompDialogue>(tile))
                    {
                        //CoroutineHandler.Run(new CoShowInspectText(this, World.GeneralDescriptions.Get(x, y)?.Text ?? $"<GENERAL DESCRIPTIONS MISSING AT {x}, {y}>"));
                    }
                }
            }
        }
    }
}
