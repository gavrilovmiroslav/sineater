using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arch.Bus;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Graphics;
using SINEATER.Game.LookNFeel;
using SINEATER.Game.Screens;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = SINEATER.Game.CoreUtils.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SINEATER.Game.Gameplay.WorldMap;

public record struct UncoverWorld(WorldMapScreen Screen, int X, int Y);

public partial class WorldMapStateEventReceiver
{
    public WorldMapStateEventReceiver() { Hook(); }

    [Event]
    public void OnUncoverWorld(ref UncoverWorld ev) {}
}

public static class WorldMapEventHandler
{
    [Event(order: 1)]
    public static void OnPartyAvatarMovedEvent(ref PartyAvatarMoved ev)
    {
        ev.Screen.WorldMap.UpdateFov(ev.NewPosition.X, ev.NewPosition.Y);
    }

    [Event(order: 1)]
    public static void OnUncoverWorld(ref UncoverWorld ev)
    {
        var tile = SineaterGame.Instance.World.Get(ev.X, ev.Y);
        var encounter = SineaterGame.Instance.World.ECS.Get<Encounter>(tile);
        var name = encounter.Enemies[0].Name;
        Console.WriteLine($"{string.Join(", ", encounter.Enemies.Select(s => s.Name))}");
        Console.WriteLine($"{name}");
        if (!SineaterGame.Instance.AllSpritesMap.ContainsKey(name))
        {
            name = "";
        }
        ev.Screen.WorldMap?.MapMarkers.Add(new PointOfInterestMarker(ev.X, ev.Y, name));
        ev.Screen.WorldMap?.MapMarkers.Add(new SurpriseMarker(ev.X, ev.Y));
        if (ev.Screen.WorldMap != null)
        {
            Console.WriteLine(ev.Screen.WorldMap.MapMarkers.Count);
        }
    }
}

public enum EMarkerOrder
{
    Before,
    After
}
public interface IWorldMapMarker : IDrawable
{
    public bool ShouldDelete { get; set; }
    public EMarkerOrder GetOrder();
}

public class SurpriseMarker(int X, int Y) : IWorldMapMarker
{
    public EMarkerOrder GetOrder() => EMarkerOrder.After;
    public bool ShouldDelete { get; set; } = false;
    
    private float _time = 0.0f;

    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        _time += renderContext.Time.ElapsedGameTime.Milliseconds / 1000.0f;
        var alpha = float.Lerp(0f, 1.0f, _time.Low(0.4f, Easing.CubicEaseOut));
        var t = float.Lerp(0f, -20.0f, _time.Low(0.2f, Easing.CubicEaseIn));
        var xy = new Vector2(x, y) + WorldMapScreen.InWorld(X, Y);
        renderContext.Batch.DrawTextCenter((int)xy.X - 24, (int)(xy.Y - 40 + t), SineaterGame.Instance.FontBold, 
            "!!", new Color(1.0f, 1.0f, 1.0f, 1.0f - alpha));
        
        if (alpha >= 1.0f)
        {
            ShouldDelete = true;
        }
    }
}

public class PointOfInterestMarker(int X, int Y, string text) : IWorldMapMarker
{
    public EMarkerOrder GetOrder() => EMarkerOrder.Before;
    public bool ShouldDelete { get; set; } = false;
    
    private float _time = 0.0f;
    private float _moveDelta = 0.0f;

    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        if (text == "") return;
        var (u, v) = SineaterGame.Instance.AllSpritesMap[text];
        _time += renderContext.Time.ElapsedGameTime.Milliseconds; 
        _moveDelta += 5 * renderContext.Time.ElapsedGameTime.Milliseconds / 1000.0f;
        var c = _moveDelta.Low(0.3f, Easing.CubicEaseOut);
        
        var xy = new Vector2(x, y) + WorldMapScreen.InWorld(X, Y);
        renderContext.Batch.Draw(SineaterGame.Instance.AllSprites, xy - new Vector2(28, 20), new Rectangle(u * 64, v * 64, 64, 64),
            Color.White, 0, new Vector2(32, 64),
            new Vector2(3, 2.8f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f),
            SpriteEffects.None, 0);
        if (SineaterGame.Instance.ShowHelp)
        {
            renderContext.Batch.Draw(SineaterGame.Instance.AllSpriteOutlines, xy - new Vector2(28, 20),
                new Rectangle(u * 64, v * 64, 64, 64),
                Color.White, 0, new Vector2(32, 64),
                new Vector2(3, 2.8f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f),
                SpriteEffects.None, 0);
        }
    }
}

public class WorldMapDrawable : IDrawable
{
    private readonly WorldMapScreen _screen;
    public readonly Dictionary<int, (RogueSharp.Map<Cell> Map, FieldOfView<Cell> Fov)> Maps = [];
    public readonly float[,] FogOfWar = new float[20, 20];
    public readonly float[,] FogOfWarTarget = new float[20, 20];
    public readonly int[,] FogOfWarMemory = new int[20, 20];
    public List<IWorldMapMarker> MapMarkers = [];

    private ReadOnlyCollection<Cell> _fov = new ReadOnlyCollection<Cell>([]);
    
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
        
        _fov = Maps[1].Fov.ComputeFov(x, y, 5, true);
        List<Cell> uncovered = [];
        foreach (var cell in _fov)
        {
            if (FogOfWarMemory[cell.X, cell.Y] == 0)
            {
                FogOfWarMemory[cell.X, cell.Y] += 5;
                uncovered.Add(cell);
            }

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

        var sorted = uncovered.OrderByDescending(c =>
        {
            var tile = SineaterGame.Instance.World.Get(c.X, c.Y);
            var encounter = SineaterGame.Instance.World.ECS.Has<Encounter>(tile);
            
            return c.IsWalkable && encounter;
        });

        if (sorted.Any())
        {
            var c = sorted.First();
            var tile = SineaterGame.Instance.World.Get(c.X, c.Y);
            var encounter = SineaterGame.Instance.World.ECS.Has<Encounter>(tile);
            if (encounter)
            {
                var uncover = new UncoverWorld(_screen, c.X, c.Y);
                EventBus.Send(ref uncover);
            }
        }
    }
    
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        var xy = new Vector2(x, y);
        var wm = SineaterGame.Instance.WorldMap;
        
        renderContext.Batch.Draw(wm, xy, null, new Color(0.8f, 0.7f, 0.7f), 0, 
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
            }
        }
        
        foreach (var marker in MapMarkers.Where(m => m.GetOrder() == EMarkerOrder.Before))
        {
            marker.Update(x, y, renderContext);
        }
        
        for (var j = 0; j < 20; j++)
        {
            for (var i = 0; i < 20; i++)
            {
                FogOfWar[i, j] = float.Lerp(FogOfWar[i, j], FogOfWarTarget[i, j], 0.1f);

                renderContext.Batch.Draw(SineaterGame.Instance.Pixel, xy + WorldMapScreen.InWorld(i, j), 
                    new Rectangle(0, 0, 24, 24), new Color(0.0f, 0.0f, 0.0f, 1 - FogOfWar[i, j]), 
                    0, new Vector2(24, 24), Vector2.One * 2, SpriteEffects.None, 0);
            }
        }
        
        foreach (var marker in MapMarkers.Where(m => m.GetOrder() == EMarkerOrder.After))
        {
            marker.Update(x, y, renderContext);
        }

        MapMarkers.RemoveAll(m => m.ShouldDelete);
    }
}