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

    private List<Texture2D> _city = [];
    private Texture2D _pixel;

    private float[] _times = [ 50, 50, 50, 50, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20 ];
    private Enemy[] _enemies = [];
    private Queue<Character> _turn = [];
    private bool _timeFlow = true;
    private HashSet<Character> _selected = [];

    private bool _paused = false;
    
    public TacticMapScreen(SineaterGame game, Encounter encounter) : base(game)
    {
        _enemies = encounter.Enemies.ToArray();
        foreach (var p in _game.Party.Characters)
        {
            p.Guard = 1;
        }

        foreach (var e in _enemies)
        {
            e.Guard = 1;
        }
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
            CoroutineHandler.Run(CoAttack(_turn.Peek()));
        }
    }

    private IEnumerable CoAttack(Character c)
    {
        var first = c;
        var flip = false;
        Character[] friends = [];
        Character[] enemies = [];

        if (first is PartyMember pm)
        {
            friends = _game.Party.Characters;
            enemies = _enemies;
        }
        else if (first is Enemy enm)
        {
            friends = _enemies;
            enemies = _game.Party.Characters;
            flip = true;
        }

        _timeFlow = false;
        _selected.Add(c);
        yield return new CoBlinkCharacter(c, this);
        DrawCombat();
        yield return new WaitForSeconds(0.5f);
        
        for (var i = 0; i < 4; i++)
        {
            var fi = flip ? 4 - i - 1 : i;
            if (first == friends[i])
            {
                var stat = (EStat)(4 - i);
                for (var w = 0; w < 4; w++)
                {
                    if (first.GetItem((EStat)(w + 1)) is Weapon weapon)
                    {
                        if (weapon.From[fi] != '-')
                        {
                            _game.Layers["ascii"].Set(2, 0, $"[{stat}] {first.GetName()} uses {weapon.Name}.");
                            yield return new WaitForSeconds(1f);
                            var atk = weapon.Attack;
                            var grd = weapon.Guard;

                            if (i == w)
                            {
                                atk += (int)Math.Min(1, MathF.Ceiling(atk * (float)weapon.Quality / 10.0f));
                                grd += (int)Math.Min(1, MathF.Ceiling(grd * (float)weapon.Quality / 10.0f));
                            }
                            
                            var indices = new List<int>();
                            var all = false;

                            if (!weapon.ToParty.All(c => c == '-'))
                            {
                                if (weapon.ToParty == "self")
                                {
                                    indices.Add(fi);
                                }
                                else
                                {
                                    for (var e = 0; e < 4; e++)
                                    {
                                        var fe = flip ? 4 - e - 1 : e;
                                        if (weapon.ToParty[fe] == 'x' || weapon.ToParty[fe] == 'X')
                                        {
                                            all |= weapon.ToParty[fe] == 'X';
                                            indices.Add(fe);
                                        }
                                    }

                                    if (!all)
                                    {
                                        if (indices.Count > 0)
                                        {
                                            indices = [indices[Rnd.Instance.Next(0, indices.Count)]];
                                        }
                                    }

                                    foreach (var idx in indices)
                                    {
                                        if (friends[idx].Guard == 9)
                                        {
                                            foreach (var e in friends)
                                            {
                                                if (e.Guard < 9)
                                                {
                                                    _selected.Add(e);
                                                    yield return new WaitForSeconds(0.2f);
                                                    DrawCombat();
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _selected.Add(friends[idx]);
                                            DrawCombat();
                                        }
                                    }
                                }

                                yield return new WaitForSeconds(0.5f);
                                foreach (var idx in indices)
                                {
                                    if (friends[idx].Guard == 0)
                                    {
                                        foreach (var e in friends)
                                        {
                                            if (e.Guard < 9)
                                            {
                                                yield return new CoBlinkCharacter(e, this);
                                                e.Guard.Up(grd);
                                                DrawCombat();
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        yield return new CoBlinkCharacter(friends[idx], this);
                                        friends[idx].Guard.Up(grd);
                                        DrawCombat();
                                    }
                                }
                            }
                            else
                            {
                                if (weapon.ToEnemy == "self")
                                {
                                    indices.Add(fi);
                                }
                                else
                                {
                                    for (var e = 0; e < 4; e++)
                                    {
                                        var fe = flip ? 4 - e - 1 : e;
                                        if (weapon.ToEnemy[fe] == 'x' || weapon.ToEnemy[fe] == 'X')
                                        {
                                            all |= weapon.ToEnemy[fe] == 'X';
                                            indices.Add(fe);
                                        }
                                    }

                                    if (!all)
                                    {
                                        if (indices.Count > 0)
                                        {
                                            indices = [indices[Rnd.Instance.Next(0, indices.Count)]];
                                        }
                                    }

                                    foreach (var idx in indices)
                                    {
                                        if (enemies[idx].Guard == 0)
                                        {
                                            foreach (var e in enemies)
                                            {
                                                if (e.Guard > 0)
                                                {
                                                    _selected.Add(e);
                                                    yield return new WaitForSeconds(0.2f);
                                                    DrawCombat();
                                                    break;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            _selected.Add(enemies[idx]);
                                            DrawCombat();
                                        }
                                    }
                                }

                                yield return new WaitForSeconds(0.5f);
                                foreach (var idx in indices)
                                {
                                    if (enemies[idx].Guard == 0)
                                    {
                                        foreach (var e in enemies)
                                        {
                                            if (e.Guard > 0)
                                            {
                                                yield return new CoBlinkCharacter(e, this);
                                                e.Guard.Down(1);
                                                DrawCombat();
                                                break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        yield return new CoBlinkCharacter(enemies[idx], this);
                                        enemies[idx].Guard.Down(atk);
                                        DrawCombat();
                                    }
                                }
                            }
                        }
                        else
                        {
                            _game.Layers["ascii"].Set(2, 0, $"[{stat}] {first.GetName()} can't use {weapon.Name} from here.", Color.Gray);
                            yield return new WaitForSeconds(2f);
                            DrawCombat();
                        }
                        
                        _selected.Clear();
                        _selected.Add(first);
                        DrawCombat();
                    }
                }
            }
        }
        
        _selected.Clear();
        _timeFlow = true;

        Next();
        yield break;
    }
    
    private IEnumerable RunUpkeep()
    {
        foreach (var ch in SineaterGame.Instance.Party.Characters)
        {
            ch.ForceRestart(this);
        }
        
        yield break;
    }

    private IEnumerable ResetPartyMembers()
    {
        foreach (var pm in _game.Party.Characters)
        {
            pm.SetOrigin();
            pm.IsDone = false;
        }
        yield break;
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
            p.X = 6 + i * 2 - 10;
            p.Y = 12 + (_selected.Contains(p) ? 1 : 0);
            Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
            Draw(6 + i * 2 - 10, 10, $"{p.Guard}", Color.White, Color.Transparent);
            i++;
        }
        
        foreach (var p in _enemies)
        {
            var (u, v) = p.GetIcon();
            p.X = 5 + (4 - i) * 2 + 18;
            p.Y = 12 + (_selected.Contains(p) ? 1 : 0);
            Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
            Draw(5 + (4 - i) * 2 + 18, 10, $"{p.Guard}", Color.White, Color.Transparent);
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
        (0, 3), (1, 3), (2, 3), (3, 3)
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
        
        for (var c = 0; c < 4; c++)
        {
            if (_game.Party.Characters[c] is { } character)
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

                    for (int ix = 1; ix <= 4; ix++)
                    {
                        if (character.GetItem((EStat)ix) is { } item)
                        {
                            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 6 - ix, $"{item.Name}", tint);
                        }
                        else
                        {
                            _game.Layers["ascii"].Set(20 * x + 2, 5 * y + 6 - ix,
                                $"[{((EStat)ix).ToString().ToUpper()}]", Color.Gray);
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
        var slowdown = 1.0f;
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

            if (!_timeFlow) continue;
            var p = _game.Party.Characters[n];
            var speed = p.Vig + p.Wil + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;
            
            var t = _times[n];

            if (p.Guard <= 0)
            {
                slowdown = 0.25f;
            }
            
            _times[n] = Math.Clamp(
                _times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
        
            if (t <= 100 && _times[n] >= 100)
            {
                _turn.Enqueue(p);
                _times[n] = 0;
                if (p.Guard <= 0) p.Guard.Up(1);
            }
        }
        
        for (var j = 0; j < 4; j++)
        {
            var n = 4 + j; //(4 - j - 1);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.White, 0.0f, Vector2.Zero, new Vector2(34.0f, 4.0f),
                SpriteEffects.None, 0);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.Red, 0.0f, Vector2.Zero, new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);
            
            if (!_timeFlow) continue;
            var p = _enemies[j];
            var speed = p.Vig + p.Wil + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;
            var t = _times[n];
            _times[n] = Math.Clamp(_times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
            if (t <= 100 && _times[n] >= 100)
            {
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
}
