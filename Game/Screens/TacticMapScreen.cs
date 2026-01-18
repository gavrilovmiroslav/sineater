using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RogueSharp;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Graphics;
using SINEATER.Game.Loadable;
using SINEATER.Game.LookNFeel;
using SINEATER.Tools.SinMod;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Wintellect.PowerCollections;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Reward = SINEATER.Game.Gameplay.Reward;

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
    private HashSet<Character> _markedFull = [];
    private HashSet<Character> _markedEmpty = [];
    private float _levelTime = 60;
    private bool _paused = false;
    private bool _over = false;
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
        if (_turn.TryPeek(out var last))
        {
            if (last is PartyMember pm)
            {
                pm.Details = false;
            }
        }
        
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
                //CoroutineHandler.Run(new CoBlink(this));
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
        
        // if (CoroutineHandler.IsActive())
        // {
        //     DrawTop();
        //     CoroutineHandler.Update();
        //     return;
        // }
        
        if (_enemies.All(e => e.Guard == 0))
        {
            foreach (var (time, rews) in _reward)
            {
                if (_levelTime > time)
                {
                    _over = true;
                    _paused = true;
                    //CoroutineHandler.Run(Victory(rews));
                }
            }

            return;
        }
        else if (_game.Party.Characters.All(e => e.Guard == 0))
        {
            //CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
            Muse.SetGameState(EMusicState.World);
            Console.WriteLine("LOSS!");
            return;
        }

        _time += gameTime.ElapsedGameTime.Milliseconds;
        if (_time > 1600)
        {
            _time = 0;
        }
        
        if (_turn.Count > 0)
        {
            //CoroutineHandler.Run(CoAttack(_turn.Peek()));
        }
    }

    private IEnumerable Victory(List<Item> rewards)
    {
        var world = SineaterGame.Instance.World; 
        var tile = world.Get(_xy.X, _xy.Y);
        world.ECS.Remove<Encounter>(tile);

        // yield return new ShowPopupAndWaitForKey(new Vector2(2, 5), new Vector2(33, 14), (s, t) =>
        // {
        //     t.Add("VICTORY!", Color.Green);
        //     t.Newline();
        //     t.Newline();
        //     t.Add("You receive:");
        //     t.Newline();
        //     foreach (var rew in rewards)
        //     {
        //         SineaterGame.Instance.Party.Inventory.Items.Add(rew);
        //         t.Add($"  1x {rew.Display}");
        //     }
        // });

        Muse.SetGameState(EMusicState.World);
        //yield return new FadeOutAndLeaveScreen(1.0f);
        yield break;
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
        //yield return new CoBlinkCharacter(first, this);
        DrawCombat();
        //yield return new WaitForSeconds(0.5f);
        
        for (var i = 0; i < 4; i++)
        {
            var fi = flip ? 4 - i - 1 : i;
            if (first == friends[i])
            {
                for (var w = 0; w < first.Items.Length; w++)
                {
                    var item = first.Items[w];
                    if (item == null) continue;

                    yield return CoAttackWithItem(first, item);
                    
                    _selected.Clear();
                    _selected.Add(first);
                    DrawCombat();
                }
            }
        }
        
        _selected.Clear();
        _timeFlow = true;

        Next();
    }

    private IEnumerable CoAttackWithItem(Character character, Item item)
    {
        var prim = item.PrimaryEffectModifier;
        var friend = true;
        var skip = false;

        var mx = 0;
        var my = 9;
        _game.Layers["inputtext"].SetRect(new Vector2(0, my), new Vector2(80, my), ' ');
        
        switch (item.PrimaryEffect)
        {
            case EItemEffect.None:
                skip = true;
                yield break;
            case EItemEffect.Attack:
                _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Attacking for {prim} damage.");
                friend = false;
                break;
            case EItemEffect.Guard:
                _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Increase guard by {prim}.");
                break;
            case EItemEffect.Resist:
                if (prim > 0)
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Resist break% up by {prim}%");
                else
                {
                    friend = false;
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Resist break% down by {prim}%");
                }

                break;
            case EItemEffect.Shield:
                if (prim > 0)
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Shield up by {prim}%");
                else
                {
                    friend = false;
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Shield down by {prim}%");
                }

                break;
            case EItemEffect.Speed:
                if (prim > 0)
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Speed up by {prim}%");
                else
                {
                    friend = false;
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Speed down by {prim}%");
                }

                break;
            case EItemEffect.Move:
                friend = false;
                if (prim > 0)
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Knock back by {prim} positions.");
                else
                    _game.Layers["inputtext"].Set(mx, my, $" {item.Display}: Pull closer by {prim} positions.");

                break;
        }
        
        //yield return new WaitForSeconds(2.0f);

        var target = item.PrimaryTargets;
        if (!friend)
        {
            target = string.Join("", target.Reverse());
        }

        bool self = target == "self";
        
        Character[] ourFriends = SineaterGame.Instance.Party.Characters;
        Character[] ourEnemies = _enemies;
        if (character is Enemy)
        {
            (ourFriends, ourEnemies) = (ourEnemies, ourFriends);
        }

        var targets = new Character[] { null, null, null, null };
        for (int i = 0; i < 4; i++)
        {
            targets[i] = (friend ? ourFriends : ourEnemies)[i];
        }

        int index = 0;
        for (int i = 0; i < 4; i++)
        {
            if (ourFriends[i] == character)
            {
                index = i;
                break;
            }
        }

        var sec = item.BonusActivates(character, index);
        
        bool all = sec && item.SecondaryEffect == EBonusEffect.TargetAll;
        
        List<int> chances = [];
        for (var i = 0; i < 4; i++)
        {
            var tgt = targets[i];
            if (all || (self && tgt == character))
            {
                for (var t = 0; t < 5; t++)
                {
                    _markedFull.Add(tgt);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.02f);
                    _markedFull.Remove(tgt);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.02f);
                }
                
                yield return CoResolveItem(character, tgt, (friend ? ourFriends : ourEnemies), i, item, sec);
            }
            else
            {
                if (target[i] == 'x')
                {
                    chances.Add(i);
                    _markedEmpty.Add(targets[i]);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.1f);
                }
                else if (target[i] == 'X')
                {
                    chances.Add(i);
                    _markedFull.Add(targets[i]);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.1f);
                    all = true;
                }
            }
        }

        if (all)
        {
            for (var t = 0; t < 5; t++)
            {
                foreach (var c in chances) _markedFull.Add(targets[c]);
                DrawCombat();
                //yield return new WaitForSeconds(0.02f);
                foreach (var c in chances) _markedFull.Remove(targets[c]);
                DrawCombat();
                //yield return new WaitForSeconds(0.02f);
            }
            
            foreach (var i in chances)
            {
                var tgt = targets[i];
                yield return CoResolveItem(character, tgt, (friend ? ourFriends : ourEnemies), i, item, sec);
            }
        }
        else
        {
            if (chances.Count > 0)
            {
                for (var t = 0; t < 5; t++)
                {
                    foreach (var c in chances)
                    {
                        _markedEmpty.Clear();
                        _markedEmpty.Add(targets[c]);
                        DrawCombat();
                        //yield return new WaitForSeconds(0.02f);
                    }
                }

                var i = -1;
                if (Rnd.Instance.Next(100) < (friend ? 50 : 80)) // target higher guard
                {
                    var g = 0;
                    for (int ix = 0; ix < chances.Count; ix++)
                    {
                        if (targets[chances[ix]].Guard > g)
                        {
                            i = ix;
                            g = targets[chances[ix]].Guard;
                        }
                    }
                }
                
                if (i == -1)
                {
                    i = Rnd.Instance.Next(0, chances.Count);
                }

                var tgt = targets[chances[i]];

                _markedEmpty.Clear();
                DrawCombat();
                
                for (var t = 0; t < 5; t++)
                {
                    _markedFull.Add(tgt);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.02f);
                    _markedFull.Remove(tgt);
                    DrawCombat();
                    //yield return new WaitForSeconds(0.02f);
                }

                yield return CoResolveItem(character, tgt, (friend ? ourFriends : ourEnemies), chances[i], item, sec);
            }
        }
        
        _markedEmpty.Clear();
        _markedFull.Clear();
        DrawCombat();
    }

    private IEnumerable CoResolveItem(Character character, Character target, Character[] targets, int index, Item item, bool secondary)
    {
        var stat = (EStat)item.SecondaryStatRequirement;
        var ok = false;
        var str = item.PrimaryEffectModifier;
        
        if (secondary)
        {
            switch (item.SecondaryEffect)
            {
                case EBonusEffect.None:
                    break;
                case EBonusEffect.PlusMod:
                    str += character.Stats.Mod(stat) + item.SecondaryEffectModifier;
                    break;
                case EBonusEffect.Double:
                    str *= 2;
                    break;
            }
        }

        _selected.Add(target);
        DrawCombat();
        //yield return new WaitForSeconds(1.0f);
        switch (item.PrimaryEffect)
        {
            case EItemEffect.None: break;
            case EItemEffect.Attack:
                var roll = Rnd.Instance.D100;
                Console.WriteLine($"Target resist: {target.Resist} vs {roll}");
                if (roll < target.Resist)
                {
                    str = 0;
                    target.Resist--;
                }
                else if (target.Shield > 0 && Rnd.Instance.D100 < 50)
                {
                    str -= target.Shield;
                    if (str < 0) str = 0;
                }

                var old = (int)target.Guard;
                target.Guard.Down(str);
                if (old == 0)
                {
                    target.Broken = true;
                }
                break;
            case EItemEffect.Guard:
                target.Guard.Up(str);
                break;
            case EItemEffect.Speed:
                target.Speed += str;
                break;
            case EItemEffect.Resist:
                target.Resist += str;
                break;
            case EItemEffect.Shield:
                target.Shield += str;
                break;
            case EItemEffect.Move:
                targets.SwapBy(index, -str);
                break;
        }

        yield break;
    }
    
    public void DrawTop()
    {
        if (_timeFlow)
        {
            //_game.Layers["ascii"].SetRect(new Vector2(30, 0), new Vector2(43, 2), ' ');
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
            
            _game.Layers["mini"].Set(70, 7, " TIME LEFT: ", Color.White, Color.Transparent);
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

        var p = SineaterGame.Instance.Party.Characters;
        var focus = -1;
        for (int i = 0; i < 4; i++)
        {
            if (p[i].Details)
            {
                focus = i;
                break;
            }
        }

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
    
    public override void LayerDraw(GameTime gameTime)
    {
        // if (CoroutineHandler.IsActive())
        // {
        //     return;
        // }

        //_game.Layers["portrait"].Clear();
        //_game.Layers["porsmol"].Clear();

        //if (ShouldHardUpdate)
        //{
        //    ShouldHardUpdate = false;
        //}
        
        //DrawCombat();
        //DrawSubmenu();

        //if (_paused)
        //    DrawControls();
    }
    
    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        if (_over) return;
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
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), 
                null, Color.White, 0.0f, Vector2.Zero, 
                new Vector2(34.0f, 4.0f), SpriteEffects.None, 0);
            
            batch.Draw(_pixel, new Vector2(190 + 64 * n, 424), 
                null, Color.Blue, 0.0f, Vector2.Zero, 
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
            var speed = 3.14f * p.Stats[n] + 4 * ((13.0f - p.Weight) / 13.0f) + p.Speed;

            var t = _times[n];

            if (p.Guard <= 0)
            {
                slowdown = 1.5f;
            }
            else if (p.Guard >= 9)
            {
                slowdown = 1.25f;
            }
            
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
                    for (var ni = 0; ni < 4; ni++)
                    {
                        SineaterGame.Instance.Party.Characters[ni].Details = false;
                    }
                    p.Details = true;
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
            Draw(p.X, 10, $"{p.Guard}", Color.White, Color.Transparent);

            if (_markedEmpty.Contains(p))
            {
                Draw(p.X, 14, new Glyph(13, 26, Color.Transparent, Color.White));
            }

            if (_markedFull.Contains(p))
            {
                Draw(p.X, 14, new Glyph(13, 25, Color.Transparent, Color.White));
            }
            
            if (_selectedIndex == i)
            {
                if (_paused)
                {
                    for (var ni = 0; ni < 4; ni++)
                    {
                        SineaterGame.Instance.Party.Characters[ni].Details = false;
                    }
                    p.Details = true;
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
            Draw(p.X, 10, $"{p.Guard}", Color.White, Color.Transparent);
                        
            if (_markedEmpty.Contains(p))
            {
                Draw(p.X, 14, new Glyph(13, 26, Color.Transparent, Color.White));
            }

            if (_markedFull.Contains(p))
            {
                Draw(p.X, 14, new Glyph(13, 25, Color.Transparent, Color.White));
            }
            i++;
        }
        
        // ENEMIES
        for (var j = 0; j < 4; j++)
        {
            slowdown = 1.0f;
            var n = 4 + (4 - j - 1);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.White, 0.0f, Vector2.Zero, new Vector2(34.0f, 4.0f),
                SpriteEffects.None, 0);
            batch.Draw(_pixel, new Vector2(864 + 64 * j, 424), null, Color.Blue, 0.0f, Vector2.Zero, new Vector2(34.0f * ((float)_times[n] / 100.0f), 4.0f),
                SpriteEffects.None, 0);
            
            if (!_timeFlow) continue;
            if (_paused) continue;
            
            var p = _enemies[4 - j - 1];
            if (p.Broken)
            {
                _times[n] = 0;
                p.Broken = false;
            }
            
            var speed = 3 * p.Stats[j] + 4 * ((13.0f - p.Weight) / 13.0f) + p.Speed;
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

        var rc = new Drawing.RenderContext(batch, gameTime);
        for (var ii = 0; ii < 4; ii++)
        {
            rc.CharacterProfile(60 + 300 * ii, 800, SineaterGame.Instance.Party.Characters[ii], ii, false);
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

        if (InputM.IsActive(EInputAction.Confirm) && !_over)
        {
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
