using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.SinMod;
using Wintellect.PowerCollections;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace SINEATER.Game.Screens;

public class TacticMapScreen : Screen
{
    private static readonly (int X, int Y)[] Directions = [(0, 1), (0, -1), (1, 0), (-1, 0)];
    
    private bool _rendered = false;
    private bool _detailedView = false;
    private Glyph[,] _groundGlyphs;
    internal FieldOfView<Cell> _fov;
    private MultiDictionary<(int, int), Color> _fgs = new(false);
    
    internal bool ShouldUpdateView = true;
    
    private List<Texture2D> _city = [];
    private Texture2D _pixel;

    private float[] _times = [ 21, 21, 21, 21, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20, Rnd.Instance.D20 ];
    private Enemy[] _enemies = [];
    private Queue<Character> _turn = [];
    private bool _timeFlow = true;
    private HashSet<Character> _selected = [];
    private float _levelTime = 60;
    private bool _paused = false;
    private ETimeOfDay _timeOfDay;
    
    public TacticMapScreen(SineaterGame game, (int X, int Y) xy, Encounter encounter, Reward reward, ETimeOfDay time) : base(game)
    {
        _timeOfDay = time;
        _xy = xy;
        _reward = reward.Rewards.ToArray();
        _enemies = encounter.Enemies.ToArray();
        foreach (var p in _game.Party.Characters)
        {
            p.Guard = 1;
        }
    }

    public override void Initialize(SineaterGame game)
    {
        Muse.SetGameState(EMusicState.Combat);
        _pixel = _game.Content.Load<Texture2D>("pixel");
        for (int i = 1; i < 7; i++)
        {
            _city.Add(_game.Content.Load<Texture2D>($"locations/Dusk City/City Dusk - {i}"));
        }
    }
    
    private void Next()
    {
        _turn.Dequeue();
    }
    
    public override void Update(GameTime gameTime)
    {
        if (!(_paused || !_timeFlow))
        {
            var ms = ((float)gameTime.ElapsedGameTime.TotalMilliseconds) / 1000.0f;
            _levelTime -= ms;
            if ((int)Math.Round(_levelTime) == 0)
            {
                CoroutineHandler.Run(new CoBlink(this));
            }
        }

        var f = 0;
        if (_focus != null)
        {
            //  0  1  2  3   4 5 6 7
            // -4 -3 -2 -1 0 1 2 3 4
            if (_focus.Value < 4)
            {
                f = -4 + _focus.Value;
            }
            else
            {
                f = _focus.Value - 3;
            }
        }
        
        _currentFocus = float.Lerp(_currentFocus, f, 0.1f);
        
        if (CoroutineHandler.IsActive())
        {
            DrawTop();
            CoroutineHandler.Update();
            return;
        }
        
        if (_enemies.All(e => e.Guard == 0))
        {
            foreach (var (time, rews) in _reward)
            {
                if (_levelTime > time)
                {
                    foreach (var rew in rews)
                    {
                        SineaterGame.Instance.Party.Inventory.Items.Add(rew);
                        SineaterGame.Instance.World.Encounters.Remove(_xy.X, _xy.Y);
                    }
                    break;
                }
            }
            CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
            Console.WriteLine("VICTORY!");
            return;
        }
        else if (_game.Party.Characters.All(e => e.Guard == 0))
        {
            CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
            Console.WriteLine("LOSS!");
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

    private void FindTargets(Item weapon, string selection, int fi, bool flip, out List<int> targets)
    {
        List<int> indices = [];
        targets = [];
        var all = false;

        if (selection == "self")
        {
            targets.Add(fi);
        }
        else
        {
            for (var e = 0; e < 4; e++)
            {
                var fe = flip ? 4 - e - 1 : e;
                if (selection[fe] == 'x' || selection[fe] == 'X')
                {
                    all |= selection[fe] == 'X';
                    indices.Add(fe);
                }
            }

            if (!all)
            {
                if (indices.Count > 0)
                {
                    targets = [indices[Rnd.Instance.Next(0, indices.Count)]];
                }
            }
            else
            {
                targets = indices;
            }
        }
    }
    
    private IEnumerable CoAttack(Character first)
    {
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
        _selected.Add(first);
        DrawCombat();
        yield return new CoBlinkCharacter(first, this);
        DrawCombat();
        yield return new WaitForSeconds(0.5f);
        
        for (var i = 0; i < 4; i++)
        {
            var fi = flip ? 4 - i - 1 : i;
            if (first == friends[i])
            {
                for (var w = 0; w < 4; w++)
                {
                    var item = first.Items[w];
                    if (item == null) continue;
                    var prim = item.PrimaryEffectModifier;
                    var friend = true;
                    switch (item.PrimaryEffect)
                    {
                        case EItemEffect.None:
                            _game.Layers["ascii"].Set(20, 18, $"{item.Name}: No effect.");
                            break;
                        case EItemEffect.Attack:
                            _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Attacking for {prim} damage.");
                            friend = false;
                            break;
                        case EItemEffect.Guard:
                            _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Increase guard by {prim}.");
                            break;
                        case EItemEffect.Resist:
                            if (prim > 0)
                                _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Increase chance to resist break by {prim}%");
                            else
                            {
                                friend = false;
                                _game.Layers["ascii"].Set(20, 18,
                                    $"{item.Name}: Decrease chance to resist break by {prim}%");
                            }

                            break;
                        case EItemEffect.Shield:
                            if (prim > 0)
                                _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Increase chance to shield from damage by {prim}%");
                            else
                            {
                                friend = false;
                                _game.Layers["ascii"].Set(20, 18,
                                    $"{item.Name}: Decrease chance to shield from damage by {prim}%");
                            }

                            break;
                        case EItemEffect.Speed:
                            if (prim > 0)
                                _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Increase speed this turn by {prim}%");
                            else
                            {
                                friend = false;
                                _game.Layers["ascii"].Set(20, 18,
                                    $"{item.Name}: Decrease speed this turn by {prim}%");
                            }

                            break;
                        case EItemEffect.Move:
                            friend = false;
                            if (prim > 0)
                                _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Knock back by {prim} positions.");
                            else
                                _game.Layers["ascii"].Set(20, 18, $"{item.Name}: Pull closer by {prim} positions.");

                            break;
                    }
                    
                    yield return new WaitForSeconds(2f);
                    _selected.Clear();
                    _selected.Add(first);
                    DrawCombat();
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
    
    public override void DrawWorld(bool noPlayer = false)
    {
        DrawParty();
        DrawCombat();
        DrawTop();
    }

    public void DrawTop()
    {
        if (_timeFlow)
        {
            _game.Layers["ascii"].SetRect(new Vector2(30, 0), new Vector2(43, 2), ' ');
            if (!_paused)
            {
                _game.Layers["input"].Set(18, 2, InputM.GetGlyph(EInputAction.Confirm));
                _game.Layers["ascii"].Set(36, 1, "PAUSE", _timeFlow ? Color.White : Color.Gray);
            }
            else
            {
                _game.Layers["input"].Set(18, 2, InputM.GetGlyph(EInputAction.Confirm));
                _game.Layers["ascii"].Set(36, 1, "FIGHT", Color.White);
            }

            _game.Layers["ascii"].SetRect(new Vector2(30, 2), new Vector2(43, 3), ' ');
            _game.Layers["mini"].Set(70, 7, " TIME LEFT: ", Color.White, Color.Black);
            _game.Layers["largenums"].Set(9, 2, ((int)MathF.Round(_levelTime)).ToString("00"), Color.White,
                Color.Transparent);

            foreach (var (time, rews) in _reward)
            {
                if (_levelTime > time)
                {
                    _game.Layers["ascii"].Set(0, 0, $"Reward (>{time}s): {string.Join(", ", rews.Select(r => r.Name))}");
                    break;
                }
            }
        }
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
        DrawTop();
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

        if (_paused)
            DrawControls();
    }
    
    public override void PreDraw(SpriteBatch batch, GameTime gameTime)
    {
        var mrmo = SineaterGame.Instance.Mrmo;
        
        var slowdown = 1.0f;
        var color = Color.White;
        if (_currentFocus < 0)
        {
            color = Color.Green;
        }
        else if (_currentFocus > 0)
        {
            color = Color.Red;
        }

        color = Color.Lerp(Color.White, color, MathF.Abs(MathF.Sign(_currentFocus)) / 3.0f);
        for (int ix = 0; ix < _city.Count; ix++)
        {
            batch.Draw(_city[ix], new Vector2(-100 + -30 * _currentFocus * (float)ix / _city.Count, -300), null, 
                Color.Lerp(color, Color.Black, (float)ix / 12), 0.0f, Vector2.Zero, new Vector2(4.5f, 4.5f),
                SpriteEffects.None, 0.0f); 
        }
        
        // CHARACTERS
        for (var n = 0; n < 4; n++)
        {
            batch.Draw(mrmo, 
                new Vector2(190 + 64 * n, 420 - 32), 
                new Rectangle(7 * 16, 65 * 16, 16, 16), 
                Color.White, 0.0f, Vector2.Zero, 
                new Vector2(2, 2), SpriteEffects.None, 0);
            
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), 
                null, Color.White, 0.0f, Vector2.Zero, 
                new Vector2(34.0f, 4.0f), SpriteEffects.None, 0);
            
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), 
                null, Color.Red, 0.0f, Vector2.Zero, 
                new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);

            if (!_timeFlow) continue;
            if (_paused) continue;
            
            var p = _game.Party.Characters[n];
            if (p.Broken)
            {
                _times[n] = 0;
                p.Broken = false;
            }
            var speed = 3.14f * p.Stats[n] + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;

            var t = _times[n];

            if (p.Guard <= 0)
            {
                slowdown = 1.5f;
            }
            else if (p.Guard >= 9)
            {
                slowdown = 1.25f;
            }
            
            _times[n] = Math.Clamp(
                _times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
        
            if (t <= 100 && _times[n] >= 100)
            {
                _times[n] = 0;
                if (p.Guard <= 0)
                {
                    p.Guard.Up(1);
                }
                else
                {
                    _focus = n;
                    _turn.Enqueue(p);
                }
            }
        }
        
        var i = 0;
        foreach (var p in SineaterGame.Instance.Party.Characters)
        {
            var (u, v) = p.Job.GetImage();
            p.X = 6 + i * 2 - 10;
            p.Y = 12 + (_selected.Contains(p) ? 1 : 0);
            Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, Color.White));
            Draw(6 + i * 2 - 10, 10, $"{p.Guard}", Color.White, Color.Transparent);

            if (_selectedIndex == i)
            {
                if (_paused)
                {
                    Draw(p.X, p.Y - 4, new Glyph(8, 74 - 16, Color.Transparent, Color.White));
                }
            }

            i++;
        }
        
        foreach (var p in _enemies)
        {
            var (u, v) = p.GetIcon();
            p.X = 5 + (4 - i) * 2 + 18;
            p.Y = 12 + (_selected.Contains(p) ? 1 : 0);
            Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, Color.White));
            Draw(5 + (4 - i) * 2 + 18, 10, $"{p.Guard}", Color.White, Color.Transparent);
            i++;
        }
        
        // ENEMIES
        for (var j = 0; j < 4; j++)
        {
            slowdown = 1.0f;
            var n = 4 + (4 - j - 1);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.White, 0.0f, Vector2.Zero, new Vector2(34.0f, 4.0f),
                SpriteEffects.None, 0);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.Red, 0.0f, Vector2.Zero, new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);
            
            if (!_timeFlow) continue;
            if (_paused) continue;
            
            var p = _enemies[4 - j - 1];
            if (p.Broken)
            {
                _times[n] = 0;
                p.Broken = false;
            }
            
            var speed = 3 * p.Stats[j] + 4 * ((13.0f - p.Weight) / 13.0f) + Rnd.Instance.D4;
            if (_timeOfDay == ETimeOfDay.Night) speed += p.NightSpeedUp;
            if (_timeOfDay == ETimeOfDay.Afternoon) speed += p.DaySpeedUp;
            if (p.Guard <= 0)
            {
                slowdown = 0.25f;
            }
            else if (p.Guard >= 9)
            {
                slowdown = 1.5f;
            }
            
            var t = _times[n];
            _times[n] = Math.Clamp(_times[n] + slowdown * speed * ((float)gameTime.ElapsedGameTime.Milliseconds / 1000.0f), 0, 100);
            if (t <= 100 && _times[n] >= 100)
            {
                _times[n] = 0;
                if (p.Guard <= 0)
                {
                    p.Guard.Up(1);
                }
                else
                {
                    _focus = n;
                    _turn.Enqueue(p);
                }
            }
        }
    }

    public Character? AttackTarget = null;
    private int _selectedIndex = 0;
    private int? _focus = null;
    private float _currentFocus = 0.0f;
    private readonly (int, List<Item>)[] _reward;
    private readonly (int X, int Y) _xy;

    private void CheckPlayerInputs()
    {
        if (_paused)
        {
            if (InputM.IsActive(EInputAction.MoveRight))
            {
                _selectedIndex += 1;
                if (_selectedIndex > 3) _selectedIndex = 0;
            }
            else if (InputM.IsActive(EInputAction.MoveLeft))
            {
                _selectedIndex -= 1;
                if (_selectedIndex < 0) _selectedIndex = 3;
            }
            else if (InputM.IsActive(EInputAction.SwapLeft))
            {
                Swap(_selectedIndex, _selectedIndex - 1 < 0 ? 3 : _selectedIndex - 1);
                _selectedIndex -= 1;
                if (_selectedIndex < 0) _selectedIndex = 3;

            }
            else if (InputM.IsActive(EInputAction.SwapRight))
            {
                Swap(_selectedIndex, _selectedIndex + 1 > 3 ? 0 : _selectedIndex + 1);
                _selectedIndex += 1;
                if (_selectedIndex > 3) _selectedIndex = 0;
            }
        }

        if (InputM.IsActive(EInputAction.Confirm))
        {
            _paused = !_paused;
            Muse.SetPaused(_paused);
        }
    }

    private void Swap(int leftIndex, int rightIndex)
    {
        (_game.Party.Characters[leftIndex], _game.Party.Characters[rightIndex]) = (_game.Party.Characters[rightIndex], _game.Party.Characters[leftIndex]);
        (_times[leftIndex], _times[rightIndex]) = (_times[rightIndex], _times[leftIndex]);
    }

    private void DrawControls()
    {
        var left = 6;
        var top = 8;
        _game.Layers["input"].Set(left - 1, top - 1, InputM.GetGlyph(EInputAction.SwapLeft));
        _game.Layers["input"].Set(left, top - 1, InputM.GetGlyph(EInputAction.SwapRight));
        _game.Layers["ascii"].Set(left * 2, top - 2, "Swap Left/Right");

        _game.Layers["input"].Set(left - 1, top, InputM.GetGlyph(EInputAction.MoveLeft));
        _game.Layers["input"].Set(left, top, InputM.GetGlyph(EInputAction.MoveRight));
        _game.Layers["ascii"].Set(left * 2, top - 1, "Select");
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
