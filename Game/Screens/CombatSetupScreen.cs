using Microsoft.Xna.Framework;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;
using System;
using System.Collections.Generic;
using System.Linq;
using Encounter = SINEATER.Game.Gameplay.Encounter;
using Reward = SINEATER.Game.Gameplay.Reward;

namespace SINEATER.Game.Screens
{
    public class CombatSetupScreen : Screen
    {
        private World _world => SineaterGame.Instance.World;
        private int _combatPositionX;
        private int _combatPositionY;
        private CoreUtils.Encounter _encounter;
        private WorldMapScreen _worldScreen;

        private int _selectedIndex = 0;

        private int _pageSize = 9;
        private int _pageIndex = 0;
        private int _pageCount => _game.Party.Inventory.Items.Count / _pageSize + 1;

        List<Item> AvailableItems = new();

        public CombatSetupScreen(SineaterGame game, int x, int y, WorldMapScreen worldScreen, CoreUtils.Encounter encounter) : base(game)
        {
            _combatPositionX = x;
            _combatPositionY = y;
            _encounter = encounter;
            _worldScreen = worldScreen;
        }

        string[] fieldsAffinity = ["POI", "CLA", "WIL", "VIG"];
        Color[] affinityColors = [Color.CornflowerBlue, Color.GreenYellow, Color.ForestGreen, Color.Lerp(Color.Pink, Color.Purple, 0.5f)];

        public override void LayerDraw(GameTime gameTime)
        {
            _game.Layers["ascii"].Clear();
            _game.Layers["mrmo"].Clear();

            var start = new Vector2(2, 1);
            var end = new Vector2(38, 15);

            SineaterGame.Instance.Layers["mrmo"].SetRect(start, end, ' ');

            _game.Layers["mrmo"].SetBox(start, end, Sides.Mrmo, Corners.Mrmo);

            int i = 0;
            foreach (var p in SineaterGame.Instance.Party.Characters)
            {
                var (u, v) = p.Job.GetImage();
                p.X = i * 2 - 4;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, Color.White));
                _game.Layers["ascii"].Set(p.X + 11 + 2 * (i + 1), p.Y + 2, fieldsAffinity[i], affinityColors[i]);

                if (_selectedIndex == i)
                {
                    Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Black, Color.White));
                }

                i++;
            }

            int j = 0;
            foreach (var p in _encounter.Enemies)
            {
                var (u, v) = p.GetIcon();
                p.X = 5 + (4 - i) * 2 + 9;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, Color.White));
                _game.Layers["ascii"].Set(33 + j * 4, p.Y + 2, fieldsAffinity[3 - j], affinityColors[3 - j]);
                i++;
                j++;
            }
            
            DrawControls();
            DrawItems();
        }

        private void GatherItems()
        {
            foreach (var item in _game.Party.Inventory.Items)
            {
                AvailableItems.Add(item);
            }

            foreach (var character in _game.Party.Characters)
            {
                foreach (var item in character.Items)
                {
                    if (item != null)
                        AvailableItems.Add(item);
                }
            }
        }

        private void SetupItems()
        {
            // _submenuSelection = 0;
            // _submenu.Clear();
            //
            // for (int i = _pageIndex * _pageSize; i < _pageIndex * _pageSize + _pageSize; i++)
            // {
            //     if (AvailableItems.Count <= i)
            //         break;
            //
            //     _submenu.Add(AvailableItems[i].Name);
            // }
        }

        private readonly List<string> _positionStats = ["NON", "VIG", "WIL", "CLA", "POI"];
        private void DrawItems()
        {
            // if (_submenu.Count > 0)
            // {
            //
            //     var len = AvailableItems.Select(s => s.Name.Length).Max() + 2 + 3;
            //     var (x, y) = (9, 7);
            //
            //     int NameStart = 11;
            //     var WeightStart = NameStart + len + 1;
            //     var primStart = WeightStart + 5 + 1;
            //     var separatorStart = primStart + 15;
            //     var secStart = separatorStart + 2;
            //     var requirmentStart = secStart + 15;
            //
            //     _game.Layers["ascii"].Set(NameStart, 7, "NAME");
            //     _game.Layers["ascii"].Set(WeightStart, 7, "WT");
            //     _game.Layers["ascii"].Set(primStart, 7, "EFFECT");
            //     _game.Layers["ascii"].Set(secStart, 7, "BONUS");
            //     _game.Layers["ascii"].Set(requirmentStart, 7, "REQ");
            //     _game.Layers["ascii"].Set(separatorStart, 7, "|");
            //
            //     var toText = (char c) =>
            //     {
            //         if (c == 'x')
            //         {
            //             return '^';
            //         }
            //         else if (c == 'X')
            //         {
            //             return '$';
            //         }
            //         else
            //         {
            //             return '_';
            //         }
            //     };

                //_game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
                //_game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                //    Sides.Ascii, Corners.Ascii);

                // for (var i = 0; i < _submenu.Count; i++)
                // {
                //     var item = AvailableItems[i];
                //
                //     var holder = _game.Party.Characters.FirstOrDefault(x => x.Items.Contains(item));
                //
                //     if (holder != null)
                //     {
                //         var (u, v) = holder.Job.GetImage();
                //         _game.Layers["mrmo"].Set(NameStart / 2 - 1, (y + 2 * i + 1) / 2 + 4, new Glyph(u, v, Color.Black, Color.White));
                //
                //     }
                //
                //     _game.Layers["ascii"].Set(NameStart, y + 1 + i, $"{item.Display}");
                //     _game.Layers["ascii"].Set(WeightStart - 1, y + 1 + i, $" {item.Weight}");
                //     
                //     var prim = (item.PrimaryTargets == "self")
                //         ? "self"
                //             : string.Join("", item.PrimaryTargets.Select(toText));
                //
                //     var align = (string s, int m) =>
                //     {
                //         var l = s.Length;
                //         for (int i = 0; i < m - l; i++)
                //         {
                //             s += " ";
                //         }
                //         return s;
                //     };
                //
                //     _game.Layers["ascii"].Set(primStart - 1, y + 1 + i, $" {align(item.PrimaryEffect.ToString(), 6)} " +
                //         $"{align(prim, 4)} {item.PrimaryEffectModifier}", item.PrimaryEffect is EItemEffect.Attack or EItemEffect.Move ? Color.Red : Color.GreenYellow);
                //
                //     _game.Layers["ascii"].Set(separatorStart, y + 1 + i, "|");
                //
                //     var sec = (item.SecondarySources == "self")
                //         ? "self"
                //         : string.Join("", item.SecondarySources.Select(toText));
                //
                //     var secondaryText = $" {align(item.SecondaryEffect.ToString(), 6)} " +
                //         $"{align(sec, 4)} {item.SecondaryEffectModifier}";
                //     var secondaryColor = item.SecondaryEffect switch
                //     {
                //         EBonusEffect.None => Color.Gray,
                //         EBonusEffect.PlusMod => Color.CadetBlue,
                //         EBonusEffect.Double => Color.Red,
                //         EBonusEffect.TargetAll => Color.Green,
                //     };
                //     
                //     if (item.SecondaryEffect == EBonusEffect.None)
                //     {
                //         secondaryText = $" {align(item.SecondaryEffect.ToString(), 6)} ";
                //         secondaryColor = Color.Gray;
                //     }
                //     _game.Layers["ascii"].Set(secStart - 1, y + 1 + i, secondaryText, secondaryColor);
                //
                //     _game.Layers["ascii"].Set(requirmentStart - 1, y + 1 + i, $" {_positionStats[(int)item.SecondaryStat]} {item.SecondaryStatRequirement}");
                // }
            //}
        }


        private void DrawControls()
        {
            var left = 6;
            var right = 27;
            var top = 19;
            _game.Layers["input"].Set(left - 1, top - 1, InputM.GetGlyph(EInputAction.SwapLeft));
            _game.Layers["input"].Set(left, top - 1, InputM.GetGlyph(EInputAction.SwapRight));
            _game.Layers["ascii"].Set(left * 2, top - 2, "Swap Left/Right");

            _game.Layers["input"].Set(left - 1, top, InputM.GetGlyph(EInputAction.MoveLeft));
            _game.Layers["input"].Set(left, top, InputM.GetGlyph(EInputAction.MoveRight));
            _game.Layers["ascii"].Set(left * 2, top - 1, "Select");

            _game.Layers["input"].Set(left, top + 1, InputM.GetGlyph(EInputAction.Equip));
            _game.Layers["ascii"].Set(left * 2, top, "Equip/Unequip");

            _game.Layers["input"].Set(left, top + 2, InputM.GetGlyph(EInputAction.ChangePage));
            _game.Layers["ascii"].Set(left * 2, top + 1, "Cycle Item List", _pageCount == 1 ? Color.Gray : Color.White);

            _game.Layers["input"].Set(right, top + 1, InputM.GetGlyph(EInputAction.StartFight));
            _game.Layers["ascii"].Set(right * 2, top, "Ready");

            _game.Layers["input"].Set(right, top + 2, InputM.GetGlyph(EInputAction.CancelFight));
            _game.Layers["ascii"].Set(right * 2, top + 1, "Back");
        }
        
        public override void Initialize(SineaterGame game)
        {
            _game.Layers["portrait"].Clear();
            _game.Layers["portrait2"].Clear();
            _game.Layers["porsmol"].Clear();
            _game.Layers["map"].Clear();
            _game.Layers["ascii"].Clear();
            _game.Layers["mrmo"].Clear();
            _game.Layers["inputtext"].Clear();

            GatherItems();
            SetupItems();
        }
        
        static int delay = 0;
        public override void Update(GameTime gameTime)
        {
            if (delay < 10)
            {
                delay++;
                return;
            }

            if (InputM.IsActive(EInputAction.CancelFight))
            {
                _game.ScreenStack.Pop();
            }
            else if (InputM.IsActive(EInputAction.StartFight))
            {
                var tile = _world.Get(_combatPositionX, _combatPositionY);
                var enc = _world.ECS.Get<Encounter>(tile);
                var rew = _world.ECS.Get<Reward>(tile);
                
                if (enc is { } encounter && rew is { } reward)
                {
                    _game.ScreenStack.Pop();
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
                    SetupItems();
                }
            }
        }
    }
}