using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Arch.Bus;
using LDtk;
using LDtkTypes;
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
    public List<IWorldMapMarker> MapMarkers = [];
    public PartyAvatarContext PartyContext;
    public PartyAvatarDrawable PartyAvatar;
    
    public LDtkLevel CurrentLevel;
    private ReadOnlyCollection<Cell> _fov = new ReadOnlyCollection<Cell>([]);
    
    public WorldMapDrawable(WorldMapScreen screen)
    {
        _screen = screen;
        CurrentLevel = SineaterGame.Instance.LDTKWorld.Levels[0];
        var start = CurrentLevel.GetEntityInstances<Start>().First();
        screen.CurrentPlayerPosition = (start._Grid.X, start._Grid.Y);
        PartyContext = new PartyAvatarContext() { Camera = screen.Camera };
        PartyAvatar = new PartyAvatarDrawable(PartyContext, 
            WorldMapScreen.InWorld(screen.CurrentPlayerPosition.X, screen.CurrentPlayerPosition.Y));
    }

    public readonly HashSet<LDtkLevel> VisitedLevels = [];

    public void Update(int x, int y, Drawing.RenderContext renderContext)
    {
        var xy = new Vector2(x, y);
        var wm = SineaterGame.Instance.WorldMap;
        
        foreach (var lvl in VisitedLevels)
        {
            SineaterGame.Instance.LDtkRenderer.RenderPrerenderedLevel(xy, lvl, 0, Vector2.One * WorldMapScreen.RESIZE, color: new Color(25, 25, 25, 25));
        }
        SineaterGame.Instance.LDtkRenderer.RenderPrerenderedLevel(xy, CurrentLevel, 0, Vector2.One * WorldMapScreen.RESIZE);
        PartyAvatar.Update(x, y, renderContext);

        List<IWorldMapMarker> all = [ PartyAvatar, ..MapMarkers ];
        foreach (var marker in all.OrderBy(a => a.Y))
        {
            marker.Update(x, y, renderContext);
        }

        MapMarkers.RemoveAll(m => m.ShouldDelete);
    }
}