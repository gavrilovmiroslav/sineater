using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Graphics;
using SINEATER.Game.Screens;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SINEATER.Game.Gameplay.WorldMap;

public class WorldMapContext
{
    
}

public static class WorldMapEventHandler
{
    [Event(order: 1)]
    public static void OnPartyAvatarMovedEvent(ref PartyAvatarMoved ev)
    {
        ev.Screen.WorldMap.UpdateFov(ev.NewPosition.X, ev.NewPosition.Y);
    }
}

public class WorldMapDrawable : IDrawable
{
    private WorldMapScreen _screen;
    public readonly Dictionary<int, (RogueSharp.Map<Cell> Map, FieldOfView<Cell> Fov)> Maps = [];
    public readonly float[,] FogOfWar = new float[20, 20];
    public readonly float[,] FogOfWarTarget = new float[20, 20];

    private ReadOnlyCollection<Cell> _fov;
    
    public WorldMapDrawable(WorldMapScreen screen)
    {
        _screen = screen;
        InitializeMapLayers();
    }
    
    private void InitializeMapLayers()
    {
        var filePath = System.IO.Path.Combine(SineaterGame.Instance.Content.RootDirectory, $"map.xp");
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

            var fov = new FieldOfView<Cell>(levelMap);
            
            Maps[layerIndex] = (levelMap, fov);
        }
        
        UpdateFov(_screen.CurrentPlayerPosition.X, _screen.CurrentPlayerPosition.Y);
    }

    public void UpdateFov(int x, int y)
    {
        for (var j = 0; j < 20; j++)
        {
            for (var i = 0; i < 20; i++)
            {
                FogOfWarTarget[i, j] = 0.0f;
            }
        }
        
        _fov = Maps[1].Fov.ComputeFov(x, y, 3, true);
        foreach (var cell in _fov)
        {
            var d = Vector2.Distance(new Vector2(x, y), new Vector2(cell.X, cell.Y));
            if (d == 0.0f)
            {
                FogOfWarTarget[cell.X, cell.Y] = 1;
            }
            else
            {
                FogOfWarTarget[cell.X, cell.Y] = 1 / d;
            }
        }
    }
    
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        var xy = new Vector2(x, y);
        var wm = SineaterGame.Instance.WorldMap;
        renderContext.Batch.Draw(wm, xy, null, Color.White, 0, 
            Vector2.Zero, Vector2.One * 3, SpriteEffects.None, 0);
        
        // for test
        for (var j = 0; j < 20; j++)
        {
            for (var i = 0; i < 20; i++)
            {
                if (!Maps[1].Map.IsWalkable(i, j))
                {
                    renderContext.Batch.Draw(SineaterGame.Instance.Pixel, xy + WorldMapScreen.InWorld(i, j), 
                        new Rectangle(0, 0, 24, 24), new Color(1.0f, 0.0f, 0.0f, 0.5f), 
                        0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
                }
                else if ((i + j) % 2 == 0)
                {
                    renderContext.Batch.Draw(SineaterGame.Instance.Semi, xy + WorldMapScreen.InWorld(i, j), 
                        new Rectangle(0, 0, 24, 24), new Color(0.8f, 0.25f, 0.45f, 0.3f), 
                        0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
                }

                FogOfWar[i, j] = float.Lerp(FogOfWar[i, j], FogOfWarTarget[i, j], 0.1f);

                renderContext.Batch.Draw(SineaterGame.Instance.Pixel, xy + WorldMapScreen.InWorld(i, j), 
                    new Rectangle(0, 0, 24, 24), new Color(0.0f, 0.0f, 0.0f, 1 - FogOfWar[i, j]), 
                    0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
            }
        }
    }
}