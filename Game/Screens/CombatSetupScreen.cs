using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.CoreUtils.Input;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.Screens
{
    public class CombatSetupScreen : Screen
    {

        private World _world => _worldScreen.World;
        private int _combatPositionX;
        private int _combatPositionY;
        private Encounter _encounter;
        private WorldMapScreen _worldScreen;

        private int _selectedIndex = 0;

        private int _pageSize = 9;
        private int _pageIndex = 0;
        private int _pageCount => _game.Party.Inventory.Items.Count / _pageSize + 1;

        public CombatSetupScreen(SineaterGame game, int x, int y, WorldMapScreen worldScreen, Encounter encounter) : base(game)
        {
            _combatPositionX = x;
            _combatPositionY = y;
            _encounter = encounter;
            _worldScreen = worldScreen;
        }

        string[] fieldsAffinity = ["POI", "CLA", "WIL", "VIG"];
        Color[] affinityColors = [Color.CornflowerBlue, Color.GreenYellow, Color.ForestGreen, Color.Lerp(Color.Pink, Color.Purple, 0.5f)];

        public override void Draw(SpriteBatch batch, GameTime gameTime)
        {
            _game.Layers["ascii"].Clear();
            _game.Layers["mrmo"].Clear();

            var start = new Vector2(2, 1);
            var end = new Vector2(30, 15);

            SineaterGame.Instance.Layers["mrmo"].SetRect(start, end, ' ');

            _game.Layers["mrmo"].SetBox(start, end, Sides.Mrmo, Corners.Mrmo);

            int i = 0;
            foreach (var p in SineaterGame.Instance.Party.Characters)
            {
                var (u, v) = p.Job.GetImage();
                p.X = i * 2 - 4;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, Color.White));
                _game.Layers["ascii"].Set(p.X + 11 + 2* (i + 1), p.Y + 2, fieldsAffinity[i], affinityColors[i]);

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
                _game.Layers["ascii"].Set(33 + j*4, p.Y + 2, fieldsAffinity[3-j], affinityColors[3-j]);
                i++;
                j++;
            }

            DrawParty();
            DrawControls();

            {
                DrawItems();
                //DrawPreview();
            }
        }

        // private void DrawPreview()
        // {
        //     var inv = _game.Party.Inventory;
        //     var aSelectedItem = inv.Items[_submenuSelection];
        //
        //     if (aSelectedItem is Weapon selectedItem)
        //     {
        //         var from = selectedItem.From;
        //         if (from.Any(x => x != '-') && from.Length == 4)
        //         {
        //             for (int i = 0; i < 4; i++)
        //             {
        //                 var c = from[i];
        //                 if (c == 'x')
        //                 {
        //                     // ISPOD IGRACA
        //                     _game.Layers["mrmo"].Set((i*2 - 1 + 8 + 2 * (i + 1))/2, 6, new Glyph(12, 25, Color.Transparent, Color.Yellow));
        //                 }
        //             }
        //         }
        //
        //         var toEnemy = selectedItem.ToEnemy;
        //         if (toEnemy.Any(x => x != '-') && toEnemy.Length == 4)
        //         {
        //             for (int i = 0; i < 4; i++)
        //             {
        //                 var c = toEnemy[i];
        //                 if (c == 'x')
        //                 {
        //                     _game.Layers["mrmo"].Set(16 + i * 2, 2, new Glyph(12, 26, Color.Transparent, Color.Red));
        //                 }
        //                 else if(c == 'X')
        //                 {
        //                     _game.Layers["mrmo"].Set(16 + i*2, 2, new Glyph(12, 25, Color.Transparent, Color.Red));
        //                 }
        //             }
        //         }
        //
        //         var toParty = selectedItem.ToParty;
        //         if (toParty.Any(x => x != '-') && toParty.Length == 4)
        //         {
        //             if (toParty == "self")
        //             {
        //                 _game.Layers["mrmo"].Set((_selectedIndex * 2 - 1 + 8 + 2 * (_selectedIndex + 1)) / 2, 2, new Glyph(12, 25, Color.Transparent, Color.Green));
        //             }
        //
        //             for (int i = 0; i < 4; i++)
        //             {
        //                 var c = toParty[i];
        //                 if (c == 'x')
        //                 {
        //                     _game.Layers["mrmo"].Set((i * 2 - 1 + 8 + 2 * (i + 1)) / 2, 2, new Glyph(12, 26, Color.Transparent, Color.Green));
        //                 }
        //                 else if (c == 'X')
        //                 {
        //                     _game.Layers["mrmo"].Set((i * 2 - 1 + 8 + 2 * (i + 1))/2, 2, new Glyph(12, 25, Color.Transparent, Color.Green));
        //                 }
        //             }
        //         }
        //     }
        // }

        private void SetupItems()
        {
            _submenuSelection = 0;
            _submenu.Clear();

            _game.Party.Inventory.Items.Reverse();

            var items = _game.Party.Inventory.Items;

            for (int i = _pageIndex * _pageSize; i < _pageIndex * _pageSize + _pageSize; i++)
            {
                if (items.Count <= i)
                    break;

                _submenu.Add(items[i].Name);
            }
        }

        private Color GetColorForStat(EStat s)
        {
            switch(s)
            {
                case EStat.Poise:
                    return affinityColors[0];
                case EStat.Will:
                    return affinityColors[1];
                case EStat.Clarity:
                    return affinityColors[2];
                case EStat.Vigor:
                    return affinityColors[3];
                default:
                    return Color.White;
            }
        }

        private bool IsEquipped(Item? w)
        {
            if (w is null)
                return false;

            foreach(var c in _game.Party.Characters)
            {
                if (c.Items.Any(x => x is not null && x.Name == w.Name))
                    return true;
            }

            return false;
        }


        private void DrawItems()
        {
            if (_submenu.Count > 0)
            {
                var len = _game.Party.Inventory.Items.Select(s => s.Name.Length).Max() + 2 + 3;
                var (x, y) = (50, 2);
                _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
                _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                    Sides.Ascii, Corners.Ascii);

                for (var i = 0; i < _submenu.Count; i++)
                {
                    var item = _game.Party.Inventory.Items.Find(x => x.Name.ToString() == _submenu[i]);

                    _game.Layers["ascii"].Set(x + 3, y + 1 + i, IsEquipped(item) ? "#" : " ", Color.White, Color.White);
                    _game.Layers["ascii"].Set(x + 4, y + 1 + i, $" {item.Name}");
                }

                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }
        }


        private void DrawControls()
        {
            var left = 6;
            var right = 27;
            var top = 13;
            _game.Layers["input"].Set(left - 1, top-1, InputM.GetGlyph(EInputAction.SwapLeft));
            _game.Layers["input"].Set(left, top -1, InputM.GetGlyph(EInputAction.SwapRight));
            _game.Layers["ascii"].Set(left * 2, top - 2, "Swap Left/Right");

            _game.Layers["input"].Set(left - 1, top , InputM.GetGlyph(EInputAction.MoveLeft));
            _game.Layers["input"].Set(left, top, InputM.GetGlyph(EInputAction.MoveRight));
            _game.Layers["ascii"].Set(left * 2, top -1, "Select");

            _game.Layers["input"].Set(left, top + 1, InputM.GetGlyph(EInputAction.Equip));
            _game.Layers["ascii"].Set(left * 2, top, "Equip/Unequip");

            _game.Layers["input"].Set(left, top + 2, InputM.GetGlyph(EInputAction.ChangePage));
            _game.Layers["ascii"].Set(left * 2, top + 1, "Cycle Item List", _pageCount == 1 ? Color.Gray : Color.White);

            _game.Layers["input"].Set(right, top + 1, InputM.GetGlyph(EInputAction.StartFight));
            _game.Layers["ascii"].Set(right * 2, top , "Ready");

            _game.Layers["input"].Set(right, top + 2, InputM.GetGlyph(EInputAction.CancelFight));
            _game.Layers["ascii"].Set(right * 2, top +1, "Back");
        }

        public override void SubmenuActivate(string action)
        {
            var item = _game.Party.Inventory.GetItem(action);
            if (item == null)
                return;

            bool hasItem = _game.Party.Characters[_selectedIndex].Items.FirstOrDefault(x => x is not null && x.Name == item.Name) != null;

            if (hasItem)
            {
                _game.Party.Characters[_selectedIndex].Equip(item);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    var c = _game.Party.Characters[i];
                    var equipped = c.Items.FirstOrDefault(x => x is not null && x.Name == item.Name);
                    if (equipped != null)
                    {
                        c.Equip(item);
                    }
                }

                _game.Party.Characters[_selectedIndex].Equip(item);
            }
        }

        public override void SubmenuItemSelected(int index)
        {

        }

        public override void Initialize(SineaterGame game)
        {
            _game.Layers["portrait"].Clear();
            _game.Layers["portrait2"].Clear();
            _game.Layers["porsmol"].Clear();
            _game.Layers["statuses"].Clear();
            _game.Layers["map"].Clear();
            _game.Layers["ascii"].Clear();
            _game.Layers["mrmo"].Clear();
            _game.Layers["inputtext"].Clear();

            SetupItems();

        }
        static int delay = 0;
        public override void Update(GameTime gameTime)
        {
            if (CoroutineHandler.IsActive())
            {
                CoroutineHandler.Update();
                return;
            }
            
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
                
                var enc = _world.Encounters.Get(_combatPositionX, _combatPositionY);
                var rew = _world.Rewards.Get(_combatPositionX, _combatPositionY);
                if (enc is { } encounter && rew is { } reward)
                {
                    _game.ScreenStack.Pop();
                    _worldScreen.CoroutineHandler.Run(new CoStartCombat(_worldScreen, _combatPositionX,
                        _combatPositionY, encounter, reward));
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

            CheckSubmenuInputs(false);
        }
    }
}