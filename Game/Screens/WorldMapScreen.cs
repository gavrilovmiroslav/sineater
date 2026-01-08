using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SadRex;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.ImGuiTools;
using SINEATER.Tools.SinMod;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER.Game.Screens;

public class CoBlink(Screen level): IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        for (var k = 0; k < 5; k++)
        {
            for (int i = 0; i < 24; i++)
            {
                for (int j = 0; j < 22; j++)
                {
                    level.Draw(i, j, " ", Color.Black);
                }
            }

            yield return new WaitForSeconds(0.01f * (6 - k));
            level.DrawWorld();
            yield return new WaitForSeconds(0.001f);
        }

        yield return new WaitForSeconds(0.15f);
    }
}

public class CoStartCombat(WorldMapScreen map, int x, int y, Encounter enc, Reward rew): IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new CoBlink(map);
        SineaterGame.Instance.ScreenStack.Push(new TacticMapScreen(SineaterGame.Instance, (x, y), enc, rew, map.TimeOfDay));
    }
}

public class CoShowInspectText(WorldMapScreen map, string text) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new ShowPopupAndWaitForKey(
            new Vector2(map.DrawOffset.X, 3 + 5),
            new Vector2(map.DrawOffset.X * 4 - 5, 10 + 5), (game, box) => box.Add(text));
    }
}

public class CoPassTimeAndMoveTo(WorldMapScreen map, int x, int y, SlowDown t) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        var chr = SineaterGame.Instance.Party.Characters[map.PlayerSelectedIndex];
        var (u, v) = chr.Job.GetImage();
        var (ox, oy) = map.CurrentPlayerPosition;
        var frame = 0;
        for (var i = 0; i < t.HoursSpent; i++)
        {
            map.HoursOfDay++;
            if (map.HoursOfDay > 5)
            {
                map.AtmosphereIndex = (map.AtmosphereIndex + 1) % 4;
                map.TimeOfDay = (ETimeOfDay)map.AtmosphereIndex;
                map.HoursOfDay = 0;
            }
            map.DrawWorld(true);
            
            //SineaterGame.Instance.Layers["mrmo"].Set(ox + 8, oy + 2,
            //    new Glyph(u, frame % 2 == 0 ? v : v - 4, Color.Black, chr.Tint));
            frame++;
            yield return new WaitForSeconds(0.02f);
            //SineaterGame.Instance.Layers["mrmo"].Set(ox + 8, oy + 2,
            //    new Glyph(u, frame % 2 == 0 ? v : v - 4, Color.Black, chr.Tint));
            frame++;
            yield return new WaitForSeconds(0.02f);
        }

        map.CurrentPlayerPosition.X = x;
        map.CurrentPlayerPosition.Y = y;
        
        if (t.FatigueGained > 0)
        {
            //SineaterGame.Instance.Party.Characters[0].AP.Add(EStatus.Fatigue, t.FatigueGained);
        }

        if (map.World.GeneralDescriptions.Has(x, y) && !map.World.GeneralDescriptions.IsVisited(x, y))
        {
            yield return new CoShowInspectText(map, map.World.GeneralDescriptions.Get(x, y)?.Text ?? $"<GENERAL DESCRIPTIONS MISSING AT {x}, {y}>");
            map.World.GeneralDescriptions.Visit(x, y);
        }
    }
}

public class WorldMapScreen : Screen
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];

    private readonly List<string> _positionStats = [ "WIL", "CLA", "POI", "VIG" ];
    private bool _drawEquips = true;
    
    private static string[] HourNames =
    [
        "Midnight", // 0
        "The Silent Hour",
        "Second of the Night",
        "The Witching Hour",
        "Dead of Night",
        "The Wolf Hour",
        "First Watch", // 6
        "Dawnrise", // 7
        "The Eighth of the Day",
        "The Ninth Hour",
        "Decadence",
        "Forenoon",
        "Midday", // 12
        "The Slow Hour",
        "Second Watch",
        "Third of the Day",
        "Hecatombs",
        "Fifth of the Day",
        "Gloaming", // 18
        "Nightfall",
        "Eventide", // 20
        "Third Watch", // 21
        "The Shakes of Ten",
        "Snake Eyes", // 11
    ];
    
    private readonly Dictionary<int, (Map<Cell> Map, FieldOfView<Cell> Fov)> Maps = [];
    private readonly Image _rex;
    
    private bool _detailedView = false;

    private (int X, int Y) DrawOffset { get; set; } = (8, 1);
    private bool _shouldUpdateView = true;

    public int AtmosphereIndex;
    public Atmosphere? AtmosphereOverride;
    public int PlayerSelectedIndex = 0;
    
    public int CurrentMapLayer = 1;
    public (int X, int Y) CurrentPlayerPosition = (2, 7);
    public ETimeOfDay TimeOfDay = ETimeOfDay.Morning;
    public int HoursOfDay = 0;

    private bool _debug = false;
    private (int X, int Y) _lastPosBeforeDebug = (0, 0);
    private World _world = null;
    public World World => _world;
    
    internal bool Debug => _debug;
    internal (int X, int Y) LastPosBeforeDebug => _lastPosBeforeDebug;
    
    public WorldMapScreen(SineaterGame game) : base(game)
    {
        AtmosphereIndex = 0;
        AtmosphereOverride = null;

        _world = World.LoadOrCreate("Content\\world.json");
        SineaterGame.Instance.World = _world;
        
        var filePath = System.IO.Path.Combine(_game.Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        InitializeMapLayers();
        
        var colors = TitleContainer.OpenStream("Content\\colors.json");
        var c = string.Join("\n", colors.ReadLines(Encoding.Default));
        Ambient.Atmospheres = DataSerializer.Load<Atmospheres>(c);
    }

    public override void Initialize(SineaterGame game)
    {


    
    }

    private void InitializeMapLayers()
    {
        for (var layerIndex = 0; layerIndex < 2; layerIndex++)
        {
            var layer = _rex.Layers[layerIndex];
            var visibilityMask = _rex.Layers[layerIndex + 2];
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
            
            Maps[layerIndex] = (levelMap, new FieldOfView<Cell>(levelMap));
        }
    }
    
    public override void Update(GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            CoroutineHandler.Update();
            return;
        }

        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }

        if (InputM.IsActive(EInputAction.Debug))
        {
            _debug = !_debug;
            _game.ShouldDrawImgui |= _debug;
            if (_debug)
            {
                _lastPosBeforeDebug = CurrentPlayerPosition;
                Tools.ImGuiTools.Tools.DebugScreen = this;
            }
            else
            {
                CurrentPlayerPosition = _lastPosBeforeDebug;
                Tools.ImGuiTools.Tools.DebugScreen = null;
            }
        }

        if (!CheckSubmenuInputs())
        {
            CheckPlayerInputs();
        }
    }

    public override void PostDraw(SpriteBatch batch, GameTime gameTime)
    {
    }

    public override void SubmenuActivate(string opt)
    {
        var (dx, dy) = _submenuDelta;
        var (x, y) = (CurrentPlayerPosition.X + dx, CurrentPlayerPosition.Y + dy);
        
        if (opt == "INSPECT")
        {
            CoroutineHandler.Run(new CoShowInspectText(this, _world.GeneralDescriptions.Get(x, y)?.Text ?? $"<GENERAL DESCRIPTIONS MISSING AT {x}, {y}>"));
        }
        else if (opt == "VISIT")
        {
            if (_world.SlowDowns.Get(x, y) is {} slowdown)
            {
                CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, slowdown));
            }
            else
            {
                CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, new SlowDown(1, 0)));
            }
        }
        else if (opt == "CAMP")
        {
            CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, new SlowDown(12, 0)));
        }
        else if (opt == "FIGHT")
        {
            if (_world.Encounters.Get(x, y) is {} encounter)
            {
                _game.ScreenStack.Push(new CombatSetupScreen(_game, x, y, this, encounter));
            }
        }
        
        _submenuDelta = (0, 0);
        _game.Layers["input"].Clear();
    }
    
    HashSet<(int,int)> visited = [];

    public override void DrawWorld(bool noPlayer = false)
    {
        if (_shouldUpdateView)
        {
            _shouldUpdateView = false;
        }

        foreach (var layer in SineaterGame.LayerNames)
        {
            if (layer == "input" || layer == "inputtext")
                continue;

            _game.Layers[layer].Clear();
        }

        var (map, fov) = Maps[CurrentMapLayer];
        var (x, y) = CurrentPlayerPosition;
        var h = ((int)TimeOfDay + 1) % 4 * 6 + HoursOfDay;
        var radius = h switch
        {
            < 6 => 3,
            < 10 => 4,
            < 16 => 5,
            < 19 => 4,
            < 22 => 3,
            _ => 2
        };
        
        var light = fov.ComputeFov(x, y, radius, true);
        
        var p = Ambient.Atmospheres[(int)TimeOfDay];
        var n = Ambient.Atmospheres[((int)TimeOfDay + 1) % 4];
        
        var bg = Color.Lerp(p.Bg.Tint, n.Bg.Tint, HoursOfDay / 6.0f);
        var bgStr = float.Lerp(p.Bg.Strength, n.Bg.Strength, HoursOfDay / 6.0f);
        var fg = Color.Lerp(p.Fg.Tint, n.Fg.Tint, HoursOfDay / 6.0f);
        var fgStr = float.Lerp(p.Fg.Strength, n.Fg.Strength, HoursOfDay / 6.0f);
        var gr = float.Lerp(p.Grayscale, n.Grayscale, HoursOfDay / 6.0f);
        AtmosphereOverride = new Atmosphere((bg, bgStr), (fg, fgStr),  gr);
        
        var atmo = AtmosphereOverride ?? Ambient.Atmospheres[AtmosphereIndex];
        for (var l = 2; l > 0; l--)
        {
            var dimLight = fov.ComputeFov(x, y, radius + l, true);
            _game.Layers["map"].SetRexFg(8, 1, _rex, CurrentMapLayer, dim: true, grayscale: gr * (0.12f * l * l + 1.0f), atmo: atmo,
                selected: dimLight.Select(c => (c.X, c.Y)));
        }

        _game.Layers["map"].SetRexFg(8, 1, _rex, CurrentMapLayer, dim: true, grayscale: gr, atmo: atmo, selected: visited);
        _game.Layers["map"].SetRex(8, 1, _rex, CurrentMapLayer, selected: light.Select(c => (c.X, c.Y)).ToList(), atmo: atmo);
        
        var tick = _time is < 400 or > 800 and < 1200;
        
        var chr = _game.Party.Characters[PlayerSelectedIndex];
        var (u, v) = chr.Job.GetImage();
        if (!noPlayer)
        {
            var bgc = _game.Layers["map"].GetBg(x + 8, y + 1);
            if (_submenuDelta == (0, 0))
            {
                _game.Layers["mrmo"].Set(x + 8, y + 1, new Glyph(u, tick ? v : v - 4, bgc, Color.White));
            }
            else
            {
                _game.Layers["mrmo"].Set(x + 8, y + 1, new Glyph(u, tick ? v : v - 4, bgc, Color.White));
                if (tick)
                {
                    _game.Layers["mrmo"].Set(x + _submenuDelta.X + 8, y + _submenuDelta.Y + 1,
                        new Glyph(8, 74 - 16, bgc, Color.White));
                }
            }
        }

        if (_debug)
        {
            if (_time % 400 < 200)
            {
                var (dx, dy) = _lastPosBeforeDebug;
                _game.Layers["mrmo"].Set(dx + 8, dy + 1, "@", Color.Gray, Color.Black);
            }

            for (var i = 0; i < 20; i++)
            {
                for (var j = 0; j < 20; j++)
                {
                    var exists = _world.AnythingOn(i, j);
                    var changed = _world.AnythingChanged(i, j);
                    if (!exists) continue;
                    _game.Layers["mrmo"].Set(i + 8, j + 1, "*", changed ? Color.Red : Color.Green, Color.Black);
                }
            }

            if (_time < 800)
            {
                _game.Layers["mrmo"].Set(x + 8, y + 1, new Glyph(u, tick ? v : v - 4, Color.Black, Color.White));
            }
        }
        
        //_game.PartyActionPoints.Draw(DrawOffset.X + 2, 26);

        DrawParty();
        
        _game.Layers["ascii"].Set(20, 0, $"{HourNames[h]} ({TimeOfDay})");
        
        var (nx, ny) = (CurrentPlayerPosition.X, CurrentPlayerPosition.Y);

        if (_world.Encounters.Get(nx, ny) is {} enc)
        {
           _game.Layers["ascii"].Set(35, 1, $"Encounter: ");
           
            for (var i = 0; i < 4; i++)
            {
                var en = enc.Enemies[4 - i - 1];
                var (uu, vv) = en.GetIcon();
                Draw(15 + i, 0, new Glyph(uu, vv, Color.Transparent, Color.White));
            }
        }
    }
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        _game.Layers["input"].Clear();
        DrawControls();

        if (CoroutineHandler.IsActive())
        {
            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();

        DrawWorld();
        DrawSubmenu();
    }

    private void DrawSubmenu()
    {
        if (_submenu.Count > 0)
        {
            var len = _submenu.Select(s => s.Length).Max() + 4;
            var (x, y) = (15, 13);
            _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
            _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                Sides.Ascii, Corners.Ascii);

            for (var i = 0; i < _submenu.Count; i++)
            {
                _game.Layers["ascii"].Set(x + 4, y + 1 + i, $"  {_submenu[i]}");
                _game.Layers["input"].Set((x + 3)/2 + 2, y + 2 + i, InputM.GetGlyph(EInputAction.Confirm));
            }

            _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");

            if (_submenu[_submenuSelection] == "VISIT")
            {
                var (dx, dy) = _submenuDelta;
                var (nx, ny) = (CurrentPlayerPosition.X + dx, CurrentPlayerPosition.Y + dy);
                
                if (_world.SlowDowns.Get(nx, ny) is {} slowdown)
                {
                    var plural = slowdown.HoursSpent > 1;
                    var hours = plural ? "HOURS" : "HOUR";
                    var text = $"+{slowdown.HoursSpent} {hours}";
                    if (slowdown.FatigueGained > 0)
                    {
                        text += $", +{slowdown.FatigueGained} FATIGUE";
                    }

                    _game.Layers["ascii"].Set(x + 4, y + _submenu.Count + 2, text);
                }
                else
                {
                    _game.Layers["ascii"].Set(x + 4, y + _submenu.Count + 2, $"+1 HOUR");
                }
            }
        }
    }

    private void DrawControls()
    {
        var left = 3;
        var top = 5;
        // _game.Layers["input"].Set(left - 1, top, InputM.GetGlyph(EInputAction.MoveLeft));
        // _game.Layers["input"].Set(left, top, InputM.GetGlyph(EInputAction.MoveRight));
        // _game.Layers["inputtext"].Set(left * 2, top - 1, "Left/Right");
        //
        // _game.Layers["input"].Set(left - 1, top +1, InputM.GetGlyph(EInputAction.MoveUp));
        // _game.Layers["input"].Set(left, top+1, InputM.GetGlyph(EInputAction.MoveDown));
        // _game.Layers["inputtext"].Set(left * 2, top, "Up/Down");
        //
        // _game.Layers["input"].Set(left, top + 2, InputM.GetGlyph(EInputAction.Confirm));
        // _game.Layers["inputtext"].Set(left * 2, top + 1, "Inspect");
    }

    private void CheckPlayerInputs()
    {
        var current = _game.Party.Characters[PlayerSelectedIndex];

        if (InputM.IsActive(EInputAction.OpenInventory))
        {
            _drawEquips = !_drawEquips;
        }
        if (_submenu.Count > 0)
        {
            if (InputM.IsActive(EInputAction.SubmenuUp))
            {
                if (_submenuSelection == 0)
                {
                    _submenuSelection = _submenu.Count - 1;
                }
                else
                {
                    _submenuSelection--;
                }
            }
            else if (InputM.IsActive(EInputAction.SubmenuDown))
            {
                if (_submenuSelection == _submenu.Count - 1)
                {
                    _submenuSelection = 0;
                }
                else
                {
                    _submenuSelection++;
                }
            }
        }
        // MOVE
        else if (PlayerSelectedIndex > -1)
        {
            if (InputM.IsActive(EInputAction.ActionsMenu))
            {
                _submenuDelta = (0, 0);
                List<string> submenuOptions = [];
                var x = CurrentPlayerPosition.X;
                var y = CurrentPlayerPosition.Y;

                if (_world.Encounters.Has(x, y))
                {
                    submenuOptions.Add("FIGHT");
                }

                if (_world.GeneralDescriptions.Has(x, y))
                {
                    if (Maps[CurrentMapLayer].Map.IsWalkable(x, y) &&
                        _world.GeneralDescriptions.IsVisited(x, y)
                        || !Maps[CurrentMapLayer].Map.IsWalkable(x, y))
                    {
                        submenuOptions.Add("INSPECT");
                    }
                }
                StartSubmenu(submenuOptions.ToArray());
            }
            
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
                    
                    if (_debug)
                    {
                        if (x >= 0 && y >= 0 && x < 20 && y < 20)
                        {
                            CurrentPlayerPosition.X = x;
                            CurrentPlayerPosition.Y = y;
                        }
                    }
                    else
                    {
                        if (Maps[CurrentMapLayer].Map.IsWalkable(x, y))
                        {
                            if (_world.SlowDowns.Get(x, y) is {} slowdown)
                            {
                                CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, slowdown));
                            }
                            else
                            {
                                CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, new SlowDown(1, 0)));
                            }   
                        }
                        else
                        {
                            if (World.GeneralDescriptions.Has(x, y))
                            {
                                CoroutineHandler.Run(new CoShowInspectText(this, World.GeneralDescriptions.Get(x, y)?.Text ?? $"<GENERAL DESCRIPTIONS MISSING AT {x}, {y}>"));
                            }
                        }
                    }
                }
            }
        }
    }

    private void StartSubmenu(string[] opts)
    {
        _submenuSelection = 0;
        foreach (var opt in opts)
        {
            _submenu.Add(opt);
        }
        _submenu.Add("CANCEL");
    }
}
