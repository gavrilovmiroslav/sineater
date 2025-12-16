using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using SadRex;
using SINEATER;
using SINEATER.ImGuiTools;
using SINEATER.Input;
using SINEATER.Serialization;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER;

public class CoBlink(WorldMapScreen level): IEnumerable
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
public class CoStartCombat(WorldMapScreen map, int x, int y, Encounter enc): IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new CoBlink(map);
        SineaterGame.Instance.ScreenStack.Push(new CombatMapScreen(
            SineaterGame.Instance, new CombatConfig()
            {
                Params = new CombatParameters(enc.ResourceCap, enc.MinEnemyLevel, enc.MaxEnemyLevel, enc.Reward),
                Terrain = enc.Biome
            }));
    }
}

public class CoShowInspectText(WorldMapScreen map, string text) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        yield return new ShowPopupAndWaitForKey(
            new Vector2(map.DrawOffset.X, 3),
            new Vector2(map.DrawOffset.X * 4 - 5, 10), (game, box) => box.Add(text));
    }
}

public class CoWakeUpEnemy(Enemy enemy, CombatMapScreen screen) : IEnumerable
{
    public IEnumerator GetEnumerator()
    {
        for (int i = 0; i < 5; i++)
        {
            var (gu, gv) = enemy.Icon;
            screen.Draw(enemy.X, enemy.Y, new Glyph(gu, gv, Color.Black, enemy.Tint));
            yield return new WaitForSeconds(0.02f);
            screen.Draw(enemy.X, enemy.Y, " ");
            yield return new WaitForSeconds(0.02f);
        }
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
            
            SineaterGame.Instance.Layers["mrmo"].Set(ox + 8, oy + 2,
                new Glyph(u, frame % 2 == 0 ? v : v - 4, Color.Black, chr.Tint));
            frame++;
            yield return new WaitForSeconds(0.02f);
            SineaterGame.Instance.Layers["mrmo"].Set(ox + 8, oy + 2,
                new Glyph(u, frame % 2 == 0 ? v : v - 4, Color.Black, chr.Tint));
            frame++;
            yield return new WaitForSeconds(0.02f);
        }

        map.CurrentPlayerPosition.X = x;
        map.CurrentPlayerPosition.Y = y;
        
        if (t.FatigueGained > 0)
        {
            SineaterGame.Instance.Party.Characters[0].AP.Add(EStatus.Fatigue, t.FatigueGained);
        }

        if (map.World.GeneralDescriptions.Has(x, y) && !map.World.GeneralDescriptions.IsVisited(x, y))
        {
            for (var i = 0; i < 8; i++) 
            {
                map.DrawWorld(i % 2 == 0);
                yield return new WaitForSeconds(0.04f);
            }
            yield return new CoShowInspectText(map, map.World.GeneralDescriptions.Get(x, y).Text);
            map.World.GeneralDescriptions.Visit(x, y);
        }
    }
}

public class WorldMapScreen : Screen
{
    private static readonly (int, int)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private readonly List<int> _offsets = [ 1, 1, 0, 0 ];
    private readonly List<int> _xoffsets = [ 0, 0, 0, 0 ];
    private readonly List<(int, int)> _positions = [ (0, 0), (3, 0), (0, 3), (3, 3) ];
    private readonly List<string> _positionStats = [ "WIL", "CLA", "POI", "VIG" ];

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
    
    private ETerrainKind _kind;
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
        
        var filePath = System.IO.Path.Combine(_game.Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        InitializeMapLayers();
        
        var colors = TitleContainer.OpenStream("Content\\colors.json");
        var c = string.Join("\n", colors.ReadLines(Encoding.Default));
        Ambient.Atmospheres = DataSerializer.Load<Atmospheres>(c);
    }

    public override void Initialize(SineaterGame game)
    {}

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
                Tools.DebugScreen = this;
            }
            else
            {
                CurrentPlayerPosition = _lastPosBeforeDebug;
                Tools.DebugScreen = null;
            }
        }

        if (!CheckSubmenuInputs())
        {
            CheckPlayerInputs();
        }
    }

    public override void SubmenuActivate(string opt)
    {
        var (dx, dy) = _submenuDelta;
        var (x, y) = (CurrentPlayerPosition.X + dx, CurrentPlayerPosition.Y + dy);
        
        if (opt == "INSPECT")
        {
            CoroutineHandler.Run(new CoShowInspectText(this, _world.GeneralDescriptions.Get(x, y).Text));
        }
        else if (opt == "VISIT")
        {
            if (_world.SlowDowns.Has(x, y))
            {
                var slowdown = _world.SlowDowns.Get(x, y);
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
            if (_world.Encounters.Has(x, y))
            {
                var enc = _world.Encounters.Get(x, y);
                CoroutineHandler.Run(new CoStartCombat(this, x, y, enc));
            }
        }
        
        _submenuDelta = (0, 0);
    }

    void UpdateExplorationView()
    {
    }
    
    private void SelectNextAvailablePartyMember()
    {
        for (int i = 1; i <= 4; i++)
        {
            PlayerSelectedIndex = (PlayerSelectedIndex + 1) % 4;
            if (!_game.Party.Characters[PlayerSelectedIndex].IsDone)
            {
                _shouldUpdateView = true;
                break;
            }
        }
    }

    HashSet<(int,int)> visited = [];

    internal void DrawWorld(bool noPlayer = false)
    {
        if (_shouldUpdateView)
        {
            UpdateExplorationView();
            _shouldUpdateView = false;
        }

        foreach (var layer in SineaterGame.LayerNames)
        {
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

        foreach (var l in light)
        {
            visited.Add((l.X, l.Y));
        }
        
        var p = Ambient.Atmospheres[(int)TimeOfDay];
        var n = Ambient.Atmospheres[((int)TimeOfDay + 1) % 4];
        
        var bg = Color.Lerp(p.Bg.Tint, n.Bg.Tint, HoursOfDay / 6.0f);
        var bgStr = float.Lerp(p.Bg.Strength, n.Bg.Strength, HoursOfDay / 6.0f);
        var fg = Color.Lerp(p.Fg.Tint, n.Fg.Tint, HoursOfDay / 6.0f);
        var fgStr = float.Lerp(p.Fg.Strength, n.Fg.Strength, HoursOfDay / 6.0f);
        var gr = float.Lerp(p.Grayscale, n.Grayscale, HoursOfDay / 6.0f);
        AtmosphereOverride = new Atmosphere((bg, bgStr), (fg, fgStr), gr);
        
        var atmo = AtmosphereOverride ?? Ambient.Atmospheres[AtmosphereIndex];
        _game.Layers["map"].SetRexFg(8, 2, _rex, CurrentMapLayer, dim: true, grayscale: gr, atmo: atmo, selected: visited);
        _game.Layers["map"].SetRex(8, 2, _rex, CurrentMapLayer, selected: light.Select(Predicate.CellToPosition).ToList(), atmo: atmo);

        //for (var i = 0; i < 20; i++)
        //{
        //    for (var j = 0; j < 20; j++)
        //    {
        //        if (!fov.IsInFov(i, j) && !visited.Contains((i, j)))
        //        {
        //            _game.Layers["mrmo"].Set(i + 8, j + 2, " ", Color.Black, Color.Black);
        //        }
        //    }
        //}

        var tick = _time is < 400 or > 800 and < 1200;
        
        var chr = _game.Party.Characters[PlayerSelectedIndex];
        var (u, v) = chr.Job.GetImage();
        if (!noPlayer)
        {
            if (_submenuDelta == (0, 0))
            {
                _game.Layers["mrmo"].Set(x + 8, y + 2, new Glyph(u, tick ? v : v - 4, Color.Black, chr.Tint));
            }
            else
            {
                _game.Layers["mrmo"].Set(x + 8, y + 2, new Glyph(u, tick ? v : v - 4, Color.Black, chr.Tint));
                if (tick)
                {
                    _game.Layers["mrmo"].Set(x + _submenuDelta.X + 8, y + _submenuDelta.Y + 2,
                        new Glyph(8, 74 - 16, Color.Black, chr.Tint));
                }
            }
        }

        if (_debug)
        {
            if (_time % 400 < 200)
            {
                var (dx, dy) = _lastPosBeforeDebug;
                _game.Layers["mrmo"].Set(dx + 8, dy + 2, "@", Color.Gray, Color.Black);
            }

            for (var i = 0; i < 20; i++)
            {
                for (var j = 0; j < 20; j++)
                {
                    var exists = _world.AnythingOn(i, j);
                    var changed = _world.AnythingChanged(i, j);
                    if (!exists) continue;
                    _game.Layers["mrmo"].Set(i + 8, j + 2, "*", changed ? Color.Red : Color.Green, Color.Black);
                }
            }

            if (_time < 800)
            {
                _game.Layers["mrmo"].Set(x + 8, y + 2, new Glyph(u, tick ? v : v - 4, Color.Black, chr.Tint));
            }
        }
        
        _game.PartyActionPoints.Draw(DrawOffset.X + 2, 26);
        
        
        DrawParty();
        
        _game.Layers["ascii"].Set(20, 0, $"{HourNames[h]} ({TimeOfDay})");
        
        var (nx, ny) = (CurrentPlayerPosition.X, CurrentPlayerPosition.Y);

        if (_world.Encounters.Has(nx, ny))
        {
            var enc = _world.Encounters.Get(nx, ny);
            var hard = enc.ResourceCap switch
            {
                <= 100 => "Easy",
                <= 200 => "Average",
                <= 300 => "Hard",
                <= 400 => "Severe",
                <= 500 => "Insane",
                _ => "Average"
            };
            _game.Layers["ascii"].Set(35, 22, $"Environment: {enc.Biome}");
            _game.Layers["ascii"].Set(35, 23, $"Encounter: {hard}");
            _game.Layers["ascii"].Set(35, 24, $"Reward: {enc.Reward}");
        }
    }
    
    private void DrawParty((PartyMember?, int?, int?, int?, int?)? change = null)
    {
        var (cha, cwil, ccla, cvig, cpoi) = change ?? (null, null, null, null, null);
        var h = 19;
        var index = 0;
        foreach (var character in _game.Party.Characters)
        {
            var (m, r) = character.Job.GetImage();
            var (u, v) = character.GetPortait();
            var (x, y) = _positions[index];
            var (xoff, yoff) = (_xoffsets[index], _offsets[index]);
            var tint = character.Tint;
            if (character != _game.Party.Characters[PlayerSelectedIndex])
            {
                tint = Color.Lerp(tint, Color.Black, 0.75f);
            }
            
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y - 1 + yoff, $"{character.Job.GetShortName()}", tint);
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y + yoff, $"{_positionStats[index]}", tint);
            var hp = $"HP{character.Hits}";
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -11 - hp.Length : 0), 5 * y + yoff + 1, hp, Color.White);
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -11 - hp.Length : 0), 5 * y + yoff + 1, $"HP", tint);
            
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 4 + yoff, $"WIL  CLA  ", tint);
            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 5 + yoff, $"VIG  POI  ", tint);
            
            if (character == cha)
            {
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{cwil ?? character.Wil}", cwil == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{ccla ?? character.Cla}", ccla == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{cvig ?? character.Vig}", cvig == null ? Color.White : Color.Yellow);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{cpoi ?? character.Poi}", cpoi == null ? Color.White : Color.Yellow);
            }
            else
            {
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 4 + yoff, $"{character.Wil}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 4 + yoff, $"{character.Cla}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 5 + yoff, $"{character.Vig}", Color.White);
                _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 5 + yoff, $"{character.Poi}", Color.White);
            }

            if (character.GetLeftWeapon() is {} lw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, $"{lw.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 7 + yoff, "[LEFT ARM]", Color.Gray);
            
            if (character.GetRightWeapon() is {} rw)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, $"{rw.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 8 + yoff, "[RIGHT ARM]", Color.Gray);
            
            if (character.GetItem() is {} it)
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, $"{it.Name}", tint);
            else
                _game.Layers["ascii"].Set(20 * x + 2 + xoff, 5 * y + 9 + yoff, "[EQUIPMENT]", Color.Gray);
            
            if (index < 2)
            {
                _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait2"].Set(x * 2, y, new Glyph(u, v, Color.Black, tint));
            }
            else
            {
                _game.Layers["portrait"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                _game.Layers["portrait"].Set(x * 2, y, new Glyph(u, v, Color.Black, tint));
            }

            index++;
        }
    }
    
    public override void Draw(GameTime gameTime)
    {
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
            var len = _submenu.Select(s => s.Length).Max() + 2;
            var (x, y) = (15, 19);
            _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
            _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                Sides.Ascii, Corners.Ascii);

            for (var i = 0; i < _submenu.Count; i++)
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {_submenu[i]}");
            }

            _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");

            if (_submenu[_submenuSelection] == "VISIT")
            {
                var (dx, dy) = _submenuDelta;
                var (nx, ny) = (CurrentPlayerPosition.X + dx, CurrentPlayerPosition.Y + dy);
                
                if (_world.SlowDowns.Has(nx, ny))
                {
                    var slowdown = _world.SlowDowns.Get(nx, ny);
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

    private void CheckPlayerInputs()
    {
        var current = _game.Party.Characters[PlayerSelectedIndex];
        if (!_debug && InputM.IsActive(EInputAction.Ability))
        {
            var ability = current.Ability;
            if (ability != null)
            {
                if (ability.CanBeUsed(current, current.X, current.Y) && current.AP.Count(EStatus.Stamina) > 0)
                {
                    CoroutineHandler.Run(new ShowPopupWindowAndWaitForKey((game, layer) =>
                    {
                        layer.Add("The witch burns sin to open a domain!");
                    }, true));
                    CoroutineHandler.Run(ability.Use(this, current, current.X, current.Y));
                }
                else
                {
                    CoroutineHandler.Run(new ShowPopupWindowAndWaitForKey((game, layer) =>
                    {
                        layer.Add("Not enough sin to open this domain...");
                    }, true));
                }
            }
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
            if (!_debug && InputM.IsActive(EInputAction.SelectNextCharacter))
            {
                SelectNextAvailablePartyMember();
            }
            
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
                        if (_world.SlowDowns.Has(x, y))
                        {
                            var slowdown = _world.SlowDowns.Get(x, y);
                            CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, slowdown));
                        }
                        else
                        {
                            CoroutineHandler.Run(new CoPassTimeAndMoveTo(this, x, y, new SlowDown(1, 0)));
                        }
                    }
                }
                
                UpdateExplorationView();
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
