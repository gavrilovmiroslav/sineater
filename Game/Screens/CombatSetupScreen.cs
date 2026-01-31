using Microsoft.Xna.Framework;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using System;
using System.Collections.Generic;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Reward = SINEATER.Game.Gameplay.Reward;

namespace SINEATER.Game.Screens
{
    public class CombatSetupScreen : Screen
    {
        private World _world => SineaterGame.Instance.World;
        private int _combatPositionX;
        private int _combatPositionY;
        private Encounter _encounter;
        private WorldMapScreen _worldScreen;

        private int _selectedIndex = 0;

        private int _pageSize = 9;
        private int _pageIndex = 0;
        private int _pageCount => Game.Party.Inventory.Items.Count / _pageSize + 1;

        List<Item> AvailableItems = new();

        public CombatSetupScreen(int x, int y, WorldMapScreen worldScreen, Encounter encounter) : base()
        {
            _combatPositionX = x;
            _combatPositionY = y;
            _encounter = encounter;
            _worldScreen = worldScreen;
        }
        
        static int delay = 0;
        public override void Update(EScreenFadeState fade, GameTime gameTime)
        {
            if (delay < 10)
            {
                delay++;
                return;
            }

            if (InputM.IsActive(EInputAction.CancelFight))
            {
                Game.ScreenStack.Pop();
            }
            else if (InputM.IsActive(EInputAction.StartFight))
            {
                var tile = _world.Get(_combatPositionX, _combatPositionY);
                var enc = _world.ECS.Get<Encounter>(tile);
                var rew = _world.ECS.Get<Reward>(tile);
                
                if (enc is { } encounter && rew is { } reward)
                {
                    Game.ScreenStack.Pop();
                    // _worldScreen.CoroutineHandler.Run(new CoStartCombat(_worldScreen, _combatPositionX,
                    //     _combatPositionY, encounter, reward));
                }
                else
                {
                    Console.WriteLine($"??? WEIRD FIGHT BEHAVIOR AT {_combatPositionX}, {_combatPositionY}!!!");
                }
            }

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
                SineaterGame.Instance.Party.Characters.Swap(_selectedIndex, _selectedIndex - 1 < 0 ? 3 : _selectedIndex - 1);
                _selectedIndex -= 1;
                if (_selectedIndex < 0) _selectedIndex = 3;

            }
            else if (InputM.IsActive(EInputAction.SwapRight))
            {
                SineaterGame.Instance.Party.Characters.Swap(_selectedIndex, _selectedIndex + 1 > 3 ? 0 : _selectedIndex + 1);
                _selectedIndex += 1;
                if (_selectedIndex > 3) _selectedIndex = 0;
            }
            else if (InputM.IsActive(EInputAction.ChangePage))
            {
                if (_pageCount != 1)
                {
                    _pageIndex = _pageIndex + 1 < _pageCount ? _pageIndex + 1 : 0;
                }
            }
        }
    }
}