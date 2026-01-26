using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arch.Bus;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        var x = ev.NewPosition.X;
        var y = ev.NewPosition.Y;
        ev.Screen.WorldMap.UpdateFov(x, y);
        if (ev.Screen.WorldMap.MapMarkers.Any(m => m.X == x && m.Y == y))
        {
            var tile = SineaterGame.Instance.World.Get(x, y);
            var encounter = SineaterGame.Instance.World.ECS.Get<Encounter>(tile);
            SineaterGame.Instance.ScreenStack.Push(new CombatScreen(SineaterGame.Instance, ev.Screen, (x, y), encounter, new Reward([])));
        }
    }

    [Event(order: 1)]
    public static void OnUncoverWorld(ref UncoverWorld ev)
    {
        var tile = SineaterGame.Instance.World.Get(ev.X, ev.Y);
        var encounter = SineaterGame.Instance.World.ECS.Get<Encounter>(tile);
        var name = encounter.Enemies[0].Name;
        if (!SineaterGame.Instance.AllSpritesMap.ContainsKey(name))
        {
            name = "";
        }
        ev.Screen.WorldMap?.MapMarkers.Add(new PointOfInterestMarker(ev.X, ev.Y, name));
        ev.Screen.WorldMap?.MapMarkers.Add(new SurpriseMarker(ev.X, ev.Y));
    }
}

public interface IWorldMapMarker : IDrawable
{
    public bool ShouldDelete { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

public class SurpriseMarker(int X, int Y) : IWorldMapMarker
{
    public bool ShouldDelete { get; set; } = false;
    public int X { get; set; } = X;
    public int Y { get; set; } = Y;

    private float _time = 0.0f;

    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        _time += renderContext.Time.ElapsedGameTime.Milliseconds / 1000.0f;
        var alpha = float.Lerp(0f, 1.0f, _time.Low(0.4f, Easing.CubicEaseOut));
        var t = float.Lerp(0f, -20.0f, _time.Low(0.2f, Easing.CubicEaseIn));
        var xy = new Vector2(x, y) + WorldMapScreen.InWorld(X, Y);
        renderContext.Batch.DrawTextCenter((int)xy.X - 20, (int)(xy.Y - 70 + t), SineaterGame.Instance.FontBold, 
            "!", new Color(1.0f, 1.0f, 1.0f, 1.0f - alpha));
        
        if (alpha >= 1.0f)
        {
            ShouldDelete = true;
        }
    }
}

public class PointOfInterestMarker : IWorldMapMarker
{
    public bool ShouldDelete { get; set; } = false;
    
    private float _time = 0.0f;
    private float _moveDelta = 0.0f;

    public int X { get; set; }
    public int Y { get; set; }
    public string Text { get; set; }
    
    public PointOfInterestMarker(int x, int y, string text)
    {
        X = x;
        Y = y;
        Text = text;
        _time += Rnd.Instance.Next(0, 1000);
    }
    
    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        if (Text == "") return;
        var (u, v) = SineaterGame.Instance.AllSpritesMap[Text];
        _time += renderContext.Time.ElapsedGameTime.Milliseconds; 
        _moveDelta += 5 * renderContext.Time.ElapsedGameTime.Milliseconds / 1000.0f;
        var c = _moveDelta.Low(0.3f, Easing.CubicEaseOut);
        
        var xy = new Vector2(x - 40, y - 20) + WorldMapScreen.InWorld(X, Y);
        renderContext.Batch.Draw(SineaterGame.Instance.AllSprites, xy, new Rectangle(u * 64, v * 64, 64, 64),
            Color.White, 0, new Vector2(32, 64),
            new Vector2(3, 2.8f - MathF.Sign(MathF.Cos(0.005f * _time)) * 0.1f),
            SpriteEffects.None, 0);
        
        if (SineaterGame.Instance.ShowHelp)
        {
            renderContext.Batch.Draw(SineaterGame.Instance.AllSpriteOutlines, xy,
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
    public PartyAvatarContext PartyContext;
    public PartyAvatarDrawable PartyAvatar;
    
    private ReadOnlyCollection<Cell> _fov = new ReadOnlyCollection<Cell>([]);
    
    public WorldMapDrawable(WorldMapScreen screen)
    {
        _screen = screen;
        InitializeMapLayers();
        PartyContext = new PartyAvatarContext() { Camera = screen.Camera };
        PartyAvatar = new PartyAvatarDrawable(PartyContext, 
            WorldMapScreen.InWorld(screen.CurrentPlayerPosition.X, screen.CurrentPlayerPosition.Y));
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
        
        _fov = Maps[1].Fov.ComputeFov(x, y, 2, true);
        List<Cell> uncovered = [];
        foreach (var cell in _fov)
        {
            var d = Vector2.Distance(new Vector2(x, y), new Vector2(cell.X, cell.Y));
            if (FogOfWarMemory[cell.X, cell.Y] == 0)
            {
                FogOfWarMemory[cell.X, cell.Y] += 5;

                if (d < 2)
                {
                    var tile = SineaterGame.Instance.World.Get(cell.X, cell.Y);
                    var encounter = SineaterGame.Instance.World.ECS.Has<Encounter>(tile);

                    if (cell.IsWalkable && encounter)
                    {
                        uncovered.Add(cell);
                    }
                }
            }
            
            if (d == 0.0f)
            {
                FogOfWarTarget[cell.X, cell.Y] = 1;
            }
            else
            {
                FogOfWarTarget[cell.X, cell.Y] = 1 / d;
            }
        }

        foreach (var c in uncovered)
        {
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
        
        renderContext.Batch.Draw(wm, new Rectangle(x, y, wm.Width * 5, wm.Height * 5),
            new Rectangle(0, 0, wm.Width, wm.Height),
            new Color(0.8f, 0.7f, 0.7f));
        
        // for test
        for (var j = 0; j < 20; j++)
        {
            for (var i = 0; i < 20; i++)
            {
                if ((i + j) % 2 == 0)
                {
                    renderContext.Batch.Draw(SineaterGame.Instance.Semi, WorldMapScreen.InWorld(i, j), 
                        new Rectangle(0, 0, 80, 80), new Color(0.8f, 0.25f, 0.45f, 0.3f), 
                        0, new Vector2(40, 40), Vector2.One, SpriteEffects.None, 0);
                }
            }
        }
        
        for (var j = 0; j < 20; j++)
        {
            for (var i = 0; i < 20; i++)
            {
                FogOfWar[i, j] = float.Lerp(FogOfWar[i, j], FogOfWarTarget[i, j], 0.1f);

                var r = WorldMapScreen.InWorld(i, j);
                renderContext.Batch.Draw(SineaterGame.Instance.Pixel, 
                    new Rectangle((int)r.X, (int)r.Y, 80, 80), 
                    new Rectangle(0, 0, 80, 80), new Color(0.0f, 0.0f, 0.0f, 1 - FogOfWar[i, j]), 
                    0.0f, new Vector2(40, 40), SpriteEffects.None, 0);
            }
        }
        
        PartyAvatar.Update(x, y, renderContext);

        List<IWorldMapMarker> all = [ PartyAvatar, ..MapMarkers ];
        foreach (var marker in all.OrderBy(a => a.Y))
        {
            marker.Update(x, y, renderContext);
        }

        MapMarkers.RemoveAll(m => m.ShouldDelete);
    }
}