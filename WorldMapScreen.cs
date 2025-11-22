using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Arch.Persistence;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using RogueSharp;
using SadRex;
using SINEATER.ImGuiTools;
using SINEATER.Input;
using Cell = RogueSharp.Cell;
using Color = Microsoft.Xna.Framework.Color;

namespace SINEATER;

public class WorldMapScreen : IScreen
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
    private readonly int _fullWidth = 20, _fullHeight = 20;
    private readonly Image _rex;
    
    private int _width, _height;
    private SineaterGame _game;
    private ETerrainKind _kind;
    private bool _detailedView = false;
    private int _time = 0;
    private Glyph[,] _groundGlyphs;
    private CoroutineHandler CoroutineHandler = new();
    private List<string> _submenu = [];
    private int _submenuSelection = 0;
    private (int X, int Y) _submenuDelta = (0, 0);

    private (int X, int Y) DrawOffset { get; set; } = (8, 1);
    private bool _shouldUpdateView = true;

    private int _atmosphereIndex;
    private Atmosphere? _atmosphereOverride;
    private int _playerSelectedIndex = 0;
    
    private int _currentMapLayer = 1;
    private (int X, int Y) _currentPlayerPosition = (2, 7);
    private ETimeOfDay _timeOfDay = ETimeOfDay.Morning;
    private int _hoursOfDay = 0;

    private Arch.Core.World? _world;
    
    public WorldMapScreen(SineaterGame game)
    {
        _game = game;
        
        var arch = new ArchBinarySerializer();
        
        // LOAD WORLD
        {
            using var worldData =
                TitleContainer.OpenStream(System.IO.Path.Combine(_game.Content.RootDirectory, $"world.dat"));
            if (worldData != null)
            {
                _world = arch.Deserialize(worldData);
            }
        }

        if (_world == null)
        {
            _world = Arch.Core.World.Create();
            
            // SAVE WORLD
            var worldPath =
                System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName,
                    $"Content\\world.dat");
            arch.Serialize(new StreamWriter(worldPath).BaseStream, _world);
        }

        _groundGlyphs = new Glyph[_fullWidth, _fullHeight];
        _atmosphereIndex = 0;
        _atmosphereOverride = null;
        
        var filePath = System.IO.Path.Combine(_game.Content.RootDirectory, $"map.xp");
        using var stream = TitleContainer.OpenStream(filePath);
        _rex = Image.Load(stream);
        InitializeMapLayers();
        
        Initialize(game);

        
        var colors = TitleContainer.OpenStream("Content\\colors.json");
        var c = string.Join("\n", colors.ReadLines(Encoding.Default));
        Ambient.Atmospheres = JsonConvert.DeserializeObject<Atmospheres>(c) ?? new Atmospheres();
    }

    public void Initialize(SineaterGame game)
    {
        _game = game;
    }

    private void InitializeMapLayers()
    {
        foreach (var layer in _rex.Layers)
        {
            var levelMap = new Map<Cell>(20, 20);
            var layerIndex = 1;
            
            for (var y = 0; y < 20; y++)
            {
                for (var x = 0; x < 20; x++)
                {
                    var bg = _rex.Layers[layerIndex][x, y].Background;
                    var transparent = _rex.Layers[layerIndex + 2][x, y].Character != 32;
                    var isAccessible = bg != SadRex.Color.Transparent && bg != new SadRex.Color(0, 0, 0);
                    levelMap.SetCellProperties(x, y, isAccessible || transparent, isAccessible);
                }
            }
            
            Maps[layerIndex] = (levelMap, new FieldOfView<Cell>(levelMap));
        }
    }
    
    public void Update(GameTime gameTime)
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

        CheckPlayerInputs();
    }
    
    internal (int, int)? GetUV(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetUV(x + ox, y + oy);
    }

    internal Color GetFg(int x, int y)
    {
        var (ox, oy) = DrawOffset;
        return SineaterGame.Instance.Layers["mrmo"].GetFg(x + ox, y + oy);
    }
    
    internal void Draw(int x, int y, Glyph g)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, g);
    }
    
    internal void Draw(int x, int y, string s)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s);
    }
    
    internal void Draw(int x, int y, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c);
    }
    
    internal void Draw(int x, int y, string s, Color c)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c);
    }
    
    internal void Draw(int x, int y, string s, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, s, c, b);
    }

    internal void Draw(int x, int y, Color c, Color b)
    {
        var (ox, oy) = DrawOffset;
        _game.Layers["mrmo"].Set(x + ox, y + oy, c, b);
    }

    void UpdateCombatView()
    {
    }
    
    private void SelectNextAvailablePartyMember()
    {
        for (int i = 1; i <= 4; i++)
        {
            _playerSelectedIndex = (_playerSelectedIndex + 1) % 4;
            if (!_game.Party.Characters[_playerSelectedIndex].IsDone)
            {
                _shouldUpdateView = true;
                break;
            }
        }
    }
    
    internal void DrawWorld(bool onlyNow = false)
    {
        if (_shouldUpdateView)
        {
            UpdateCombatView();
            _shouldUpdateView = false;
        }

        foreach (var layer in SineaterGame.LayerNames)
        {
            _game.Layers[layer].Clear();
        }

        var (map, fov) = Maps[_currentMapLayer];
        var (x, y) = _currentPlayerPosition;
        var h = ((int)_timeOfDay + 1) % 4 * 6 + _hoursOfDay;
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
        
        var p = Ambient.Atmospheres[(int)_timeOfDay];
        var n = Ambient.Atmospheres[((int)_timeOfDay + 1) % 4];
        
        var bg = Color.Lerp(p.Bg.Tint, n.Bg.Tint, _hoursOfDay / 6.0f);
        var bgStr = float.Lerp(p.Bg.Strength, n.Bg.Strength, _hoursOfDay / 6.0f);
        var fg = Color.Lerp(p.Fg.Tint, n.Fg.Tint, _hoursOfDay / 6.0f);
        var fgStr = float.Lerp(p.Fg.Strength, n.Fg.Strength, _hoursOfDay / 6.0f);
        var gr = float.Lerp(p.Grayscale, n.Grayscale, _hoursOfDay / 6.0f);
        _atmosphereOverride = new Atmosphere((bg, bgStr), (fg, fgStr), gr);
        
        var atmo = _atmosphereOverride ?? Ambient.Atmospheres[_atmosphereIndex];
        _game.Layers["map"].SetRexFg(8, 2, _rex, _currentMapLayer, dim: true, grayscale: gr, atmo: atmo);
        _game.Layers["map"].SetRex(8, 2, _rex, _currentMapLayer, selected: light.Select(Predicate.CellToPosition).ToList(), atmo: atmo);
        
        var tick = _time is < 400 or > 800 and < 1200;
        
        var chr = _game.Party.Characters[_playerSelectedIndex];
        var (u, v) = chr.Job.GetImage();
        _game.Layers["mrmo"].Set(x + 8, y + 2, new Glyph(u, tick ? v : v - 4, Color.Black, chr.Tint));
        
        _game.ActionPoints.Draw(DrawOffset.X * 2 + 1, 26);
        
        DrawParty();
        
        _game.Layers["ascii"].Set(20, 0, $"{HourNames[h]} ({_timeOfDay})");
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
            if (character != _game.Party.Characters[_playerSelectedIndex])
            {
                tint = Color.Lerp(tint, Color.Black, 0.75f);
            }
            
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y - 1 + yoff, $"{character.Job.GetShortName()}", tint);
            _game.Layers["ascii"].Set(20 * x + 12 + (x > 0 ? -14 : 0), 5 * y + yoff, $"{_positionStats[index]}", tint);
            var hp = $"HP{character.HP}";
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
    
    public void Draw(GameTime gameTime)
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
            _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count), Sides.Ascii, Corners.Ascii);

            for (var i = 0; i < _submenu.Count; i++)
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {_submenu[i]}");
            }
            _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
        }
    }
    
    private bool _inspectMode = false;
    
    private void CheckPlayerInputs()
    {
        if (_inspectMode)
        {
            if (InputM.IsActive(EInputAction.ExitInspect))
            {
                _inspectMode = false;
            }
            return;
        }
        
        var current = _game.Party.Characters[_playerSelectedIndex];
        if (InputM.IsActive(EInputAction.Ability))
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
        else if (_playerSelectedIndex > -1)
        {
            if (InputM.IsActive(EInputAction.SelectNextCharacter))
            {
                SelectNextAvailablePartyMember();
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
                    var x = _currentPlayerPosition.X + dx;
                    var y = _currentPlayerPosition.Y + dy;
                    if (x >= 0 && y >= 0 && x < 20 && y < 20 && Maps[_currentMapLayer].Map.IsWalkable(x, y))
                    {
                        _currentPlayerPosition.X = x;
                        _currentPlayerPosition.Y = y;

                        _hoursOfDay++;
                        if (_hoursOfDay > 5)
                        {
                            _atmosphereIndex = (_atmosphereIndex + 1) % 4;
                            _timeOfDay = (ETimeOfDay)_atmosphereIndex;
                            _hoursOfDay = 0;
                        }
                    }
                }
                
                UpdateCombatView();
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
