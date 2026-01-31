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
    
    public TacticMapScreen((int X, int Y) xy, Encounter encounter, Reward reward, ETimeOfDay time) : base()
    {
        _timeOfDay = time;
        _xy = xy;
        _reward = reward.Rewards.ToArray();
        _enemies = encounter.Enemies.ToArray();
        foreach (var p in Game.Party.Characters)
        {
            p.Guard = 1;
        }
    }

    public override void Initialize()
    {
        Muse.SetGameState(EMusicState.Combat);
        _pixel = Game.Content.Load<Texture2D>("pixel");
        for (int i = 1; i < 7; i++)
        {
            _city.Add(Game.Content.Load<Texture2D>($"locations/Dusk City/City Dusk - {i}"));
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
    
    public override void Update(EScreenFadeState fade, GameTime gameTime)
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
        else if (Game.Party.Characters.All(e => e.Guard == 0))
        {
            //CoroutineHandler.Run(new FadeOutAndLeaveScreen(1.0f));
            Muse.SetGameState(EMusicState.World);
            Console.WriteLine("LOSS!");
            return;
        }

        Time += gameTime.ElapsedGameTime.Milliseconds;
        if (Time > 1600)
        {
            Time = 0;
        }
        
        if (_turn.Count > 0)
        {
            //CoroutineHandler.Run(CoAttack(_turn.Peek()));
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
        (Game.Party.Characters[leftIndex], Game.Party.Characters[rightIndex]) = (Game.Party.Characters[rightIndex], Game.Party.Characters[leftIndex]);
        (_times[leftIndex], _times[rightIndex]) = (_times[rightIndex], _times[leftIndex]);
    }
    
}
