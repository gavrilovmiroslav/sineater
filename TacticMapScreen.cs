using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RogueSharp;
using SINEATER.Input;
using Wintellect.PowerCollections;

namespace SINEATER;

public class TacticMapScreen : Screen
{
    private static readonly (int X, int Y)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];

    private static readonly (EOrder Side, EAction Action)[] TurnOrder = [
        (EOrder.Player, EAction.Move),
        (EOrder.Player, EAction.Special),
        (EOrder.Player, EAction.Attack),
        (EOrder.Player, EAction.Rest),
        (EOrder.CPU, EAction.Move),
        (EOrder.CPU, EAction.Special),
        (EOrder.CPU, EAction.Attack),
        (EOrder.CPU, EAction.Rest),
    ];

    public int TurnOrderIndex = 0;
    
    private ETerrainKind _kind;
    public LevelStructure Structure;
    
    private bool _rendered = false;
    private bool _detailedView = false;
    private Glyph[,] _groundGlyphs;
    internal FieldOfView<Cell> _fov;
    private readonly CombatConfig? _config;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    
    internal bool ShouldUpdateView = true;

    public Domains Domains;
    public IMap? Map => Structure.Map;
    //public List<Character> InitiativeOrder = [];
    //public Character? InitiativeCurrent => InitiativeOrder.First();

    private List<Texture2D> _city = [];
    private Texture2D _pixel;

    private float[] _times = [ 50, 50, 50, 50, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20 ];
    private Enemy[] _enemies = [Bestiary.Bat(), Bestiary.Bat(), Bestiary.Bat(), Bestiary.Bat()];
    private Queue<Character> _turn = [];
    private Character? _currentTurn = null;

    private void Regenerate(bool resize) {
        if (resize)
        {
            this._width = _fullWidth - 2;
            this._height = _fullHeight - 2;
        }
    }

    public TacticMapScreen(SineaterGame game) : base(game)
    {
    }

    public override void Initialize(SineaterGame game)
    {
        _pixel = _game.Content.Load<Texture2D>("pixel");
        for (int i = 1; i < 5; i++)
        {
            _city.Add(_game.Content.Load<Texture2D>($"locations/Spikey Lands/Spikey Lands - {i}"));
        }
    }
    
    private void Next()
    {
        _turn.Dequeue();
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

        if (!CheckSubmenuInputs())
        {
            CheckPlayerInputs();
        }
        
        if (_turn.Count > 0)
        {
            var first = _turn.Peek();
            if (first is PartyMember pm && _currentTurn == null)
            {
                _currentTurn = first;
                StartSubmenu([ "DEFEND" ]);
            }
            else if (first is Enemy enm)
            {
                Next();
            }
        }
    }
    
    private IEnumerable RunUpkeep()
    {
        foreach (var ch in SineaterGame.Instance.Party.Characters)
        {
            ch.ForceRestart(this);
        }
        
        // foreach (var dom in Domains._domains)
        // {
        //     dom.Update(this);
        // }
        yield break;
    }

    private IEnumerable ResetPartyMembers()
    {
        foreach (var pm in _game.Party.Characters)
        {
            pm.SetOrigin();
            pm.IsDone = false;
            CalculateZone(pm);
        }
        yield break;
    }
    
    public void CalculateZone(PartyMember w)
    {
        w.Zone.Clear();
        var dis = new DistanceMap(Structure, false, [w.Origin], Predicate.Walkable);
        var walkRadius = w.MovementLeft;
        w.Zone = dis.GetAllBeneath(walkRadius + 1).ToHashSet();
        w.Zone.IntersectWith(Structure.Map.
            GetCellsInCircle(w.Origin.X, w.Origin.Y, walkRadius).
            Select(Predicate.CellToPosition).ToHashSet());
    }

    public bool CheckSubmenuInputs()
    {
        var isOpen = _submenu.Count > 0;
        if (isOpen)
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
            else if (InputM.IsActive(EInputAction.SubmenuConfirm))
            {
                var opt = _submenu[_submenuSelection];
                _submenu.Clear();
                SubmenuActivate(opt);
            }
        }

        return isOpen;
    }
    
    internal void DrawCombat(bool onlyNow = false)
    {
        if (ShouldUpdateView)
        {
            ShouldUpdateView = false;
        }

        foreach (var layer in SineaterGame.LayerNames)
        {
            _game.Layers[layer].Clear();
        }
        
        DrawParty();

        int i = 0;
        foreach (var p in SineaterGame.Instance.Party.Characters)
        {
            var (u, v) = p.Job.GetImage();
            Draw(6 - i * 2 - 4, 12, new Glyph(u, v, Color.Transparent, p.Tint)); ;
            i++;
        }
        
        foreach (var p in _enemies)
        {
            var (u, v) = p.GetIcon();
            Draw(5 + i * 2 + 4, 12, new Glyph(u, v, Color.Transparent, p.Tint));
            i++;
        }
    }

    private void DrawAP()
    {
        _game.Party.Characters[0].AP.Draw(DrawOffset.X + 1, 27);

        MultiDictionary<int, PartyMember> xs = new(false);
        foreach (var w in _game.Party.Characters)
        {
            var g = w.Job.GetImage();
            xs.Add(w.X, w);
        }

        foreach (var x in xs.Keys.Order())
        {
            var y = 25 - xs[x].Count;
            int n = 0;
            foreach (var pm in xs[x].OrderBy(p => p.Y))
            {
                var (u, v) = pm.Job.GetImage();
                Draw(x, y + n, new Glyph(u, v, Color.Black, pm.Tint));
                n++;
            }
        }
    }
    
    private readonly List<(int, int)> _positions = [
        (1, 3), (2, 3), (0, 3), (3, 3)
    ];
    
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
                var name = _submenu[i];
                if (name.EndsWith("*"))
                {
                    _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {name[..^1]}", Color.Gray);    
                }
                else
                {
                    _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {name}");
                }
            }

            if (_submenu[_submenuSelection].EndsWith("*"))
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, "x", Color.Gray);
            }
            else
            {
                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }
        }
    }
    
    public void DrawParty((PartyMember?, int?, int?, int?, int?)? change = null, IEnumerable<PartyMember>? toDraw = null, Color? colorOverride = null)
    {
        var drawSet = (toDraw ?? _game.Party.Characters).ToHashSet();
        var (cha, cwil, ccla, cvig, cpoi) = change ?? (null, null, null, null, null);
        var h = 19;
        var index = 0;
        
        foreach (var character in _game.Party.Characters)
        {
            if (drawSet.Contains(character))
            {
                var (m, r) = character.Job.GetImage();
                var (u, v) = character.GetPortait();
                var (x, y) = _positions[index];
                var tint = character.Tint;

                if (colorOverride is { } color)
                {
                    tint = color;
                }

                _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 11, $"WIL  CLA  ", tint);
                _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 12, $"VIG  POI  ", tint);

                if (character == cha)
                {
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 11, $"{cwil ?? character.Wil}",
                        cwil == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 11, $"{ccla ?? character.Cla}",
                        ccla == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 12, $"{cvig ?? character.Vig}",
                        cvig == null ? Color.White : Color.Yellow);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 12, $"{cpoi ?? character.Poi}",
                        cpoi == null ? Color.White : Color.Yellow);
                }
                else
                {
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 11, $"{character.Wil}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 11, $"{character.Cla}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 5, 5 * y + 12, $"{character.Vig}", Color.White);
                    _game.Layers["ascii"].Set(20 * x + 10, 5 * y + 12, $"{character.Poi}", Color.White);
                }

                for (int ix = 0; ix < 4; ix++)
                {
                    if (character.GetItem((EStat)ix) is { } item)
                    {
                        _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 3 + ix, $"{item.Name}", tint);    
                    }
                    else
                    {
                        _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 3 + ix, $"[{((EStat)ix).ToString().ToUpper()}]", Color.Gray);
                    }
                }

                if (index < 2)
                {
                    _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                    _game.Layers["portrait2"].Set(x * 2, y + 1, new Glyph(u, v, Color.Black, tint));
                }
                else
                {
                    _game.Layers["portrait2"].SetFlip(u, v, SpriteEffects.FlipHorizontally);
                    _game.Layers["portrait2"].Set(x * 2, y + 1, new Glyph(u, v, Color.Black, tint));
                }
            }

            index++;
        }
    }
    
    public bool ShouldHardUpdate { get; set; } = true;
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        if (CoroutineHandler.IsActive())
        {
            return;
        }

        _game.Layers["portrait"].Clear();
        _game.Layers["porsmol"].Clear();

        if (ShouldHardUpdate)
        {
            ShouldHardUpdate = false;
        }
        
        DrawCombat();
        DrawSubmenu();
    }
    
    public override void PreDraw(SpriteBatch batch, GameTime gameTime)
    {
        var slowdown = _turn.Count > 0 ? 0.25f : 1.0f;
        var i = 0;
        foreach (var img in _city)
        {
            batch.Draw(img, new Vector2(20, -180), null, Color.Lerp(Color.White, Color.Black, (float)i / 12), 0.0f, Vector2.Zero, new Vector2(4.0f, 4.0f),
                SpriteEffects.None, 0.0f);
            i++;
        }

        for (var n = 0; n < 4; n++)
        {
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), null, Color.White, 0.0f, Vector2.Zero, new Vector2(34.0f, 4.0f),
                SpriteEffects.None, 0);
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), null, Color.Red, 0.0f, Vector2.Zero, new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);
            var p = _game.Party.Characters[n];
            var speed = p.Vig + p.Wil + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;
            var t = _times[n];
            _times[n] = Math.Clamp(_times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
            if (t <= 100 && _times[n] >= 100)
            {
                Console.WriteLine(p.Job.GetShortName());
                _turn.Enqueue(p);
                _times[n] = 0;
            }
        }
        
        for (var j = 0; j < 4; j++)
        {
            var n = 4 + j;
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.White, 0.0f, Vector2.Zero, new Vector2(34.0f, 4.0f),
                SpriteEffects.None, 0);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.Red, 0.0f, Vector2.Zero, new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);
            var p = _enemies[j];
            var speed = p.Vig + p.Wil + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;
            var t = _times[n];
            _times[n] = Math.Clamp(_times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
            if (t <= 100 && _times[n] >= 100)
            {
                Console.WriteLine(p.GetName());
                _turn.Enqueue(p);
                _times[n] = 0;
            }
        }
    }

    private IEnumerable Coroutine_EndTurn()
    {
        yield return RunUpkeep();
        yield return ResetPartyMembers();
    }

    private bool _inspectMode = false;
    public Character? AttackTarget = null;
    
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
    }

    private void StartSubmenu(string[] opts, bool cancel = true)
    {
        _submenuSelection = 0;
        foreach (var opt in opts)
        {
            _submenu.Add(opt);
        }
        
        if (cancel)
            _submenu.Add("CANCEL");
    }

    public override void SubmenuActivate(string opt)
    {
        if (opt == "DEFEND")
        {
            _currentTurn = null;
            Next();
        }
        DrawCombat();
    }
}
