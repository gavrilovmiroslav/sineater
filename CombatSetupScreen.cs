using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SINEATER
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
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, p.Tint));
                _game.Layers["ascii"].Set(p.X + 11 + 2* (i + 1), p.Y + 2, fieldsAffinity[i], affinityColors[i]);

                if (_selectedIndex == i)
                {
                    Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Black, p.Tint));
                }

                i++;

            }

            int j = 0;
            foreach (var p in _encounter.Enemies)
            {
                var (u, v) = p.GetIcon();
                p.X = 5 + (4 - i) * 2 + 9;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
                _game.Layers["ascii"].Set(33 + j*4, p.Y + 2, fieldsAffinity[3-j], affinityColors[3-j]);
                i++;
                j++;
            }

            DrawParty();
            DrawControls();

            {
                DrawItems();
                DrawPreview();
            }
        }

        private void DrawPreview()
        {
            var inv = _game.Party.Inventory;
            var selectedItem = inv.Items[_submenuSelection];

            if (selectedItem != null)
            {
                var from = selectedItem.From;
                if (from.Any(x => x != '-') && from.Length == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var c = from[i];
                        if (c == 'x')
                        {
                            // ISPOD IGRACA
                            _game.Layers["mrmo"].Set((i*2 - 1 + 8 + 2 * (i + 1))/2, 6, new Glyph(12, 25, Color.Transparent, Color.Yellow));
                        }
                    }
                }

                var toEnemy = selectedItem.ToEnemy;
                if (toEnemy.Any(x => x != '-') && toEnemy.Length == 4)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var c = toEnemy[i];
                        if (c == 'x')
                        {
                            _game.Layers["mrmo"].Set(16 + i * 2, 2, new Glyph(12, 26, Color.Transparent, Color.Red));
                        }
                        else if(c == 'X')
                        {
                            _game.Layers["mrmo"].Set(16 + i*2, 2, new Glyph(12, 25, Color.Transparent, Color.Red));
                        }
                    }
                }

                var toParty = selectedItem.ToParty;
                if (toParty.Any(x => x != '-') && toParty.Length == 4)
                {
                    if (toParty == "self")
                    {
                        _game.Layers["mrmo"].Set((_selectedIndex * 2 - 1 + 8 + 2 * (_selectedIndex + 1)) / 2, 2, new Glyph(12, 25, Color.Transparent, Color.Green));
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        var c = toParty[i];
                        if (c == 'x')
                        {
                            _game.Layers["mrmo"].Set((i * 2 - 1 + 8 + 2 * (i + 1)) / 2, 2, new Glyph(12, 26, Color.Transparent, Color.Green));
                        }
                        else if (c == 'X')
                        {
                            _game.Layers["mrmo"].Set((i * 2 - 1 + 8 + 2 * (i + 1))/2, 2, new Glyph(12, 25, Color.Transparent, Color.Green));
                        }
                    }
                }
            }
        }

        private void SetupItems()
        {
            _submenuSelection = 0;
            _submenu.Clear();

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
        private void DrawItems()
        {
            if (_submenu.Count > 0)
            {
                var len = _submenu.Select(s => s.Length).Max() + 2 + 3;
                var (x, y) = (50, 2);
                _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
                _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                    Sides.Ascii, Corners.Ascii);

                for (var i = 0; i < _submenu.Count; i++)
                {
                    var item = _game.Party.Inventory.Items.Find(x => x.Name == _submenu[i]);

                    var stat = item.Attack > 0 ? -item.Attack : item.Guard;

                    _game.Layers["ascii"].Set(x + 3, y + 1 + i, " ", Color.White, GetColorForStat(item.Stat));
                    _game.Layers["ascii"].Set(x + 4, y + 1 + i, $" {_submenu[i]}");
                    _game.Layers["ascii"].Set(x + len, y + 1 + i, stat < 0 ? $" {stat}" : $" +{stat}", stat < 0 ? Color.Red : Color.Green);
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

            bool isSwap = false;

            int i = 0;
            for(; i < 3; i++)
            {
                var c = _game.Party.Characters[i];
                var equipped = c.Items[(int)(item.Stat - 1)];
                if (equipped != null)
                {
                    isSwap = equipped.Name != item.Name;
                    c.Equip(item.Stat, null);
                    break;
                }
            }

            if (i != _selectedIndex || isSwap)
            {
                _game.Party.Characters[_selectedIndex].Equip(_game.Party.Inventory.GetItem(action));
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
                _game.ScreenStack.Pop();
                var enc = _world.Encounters.Get(_combatPositionX, _combatPositionY);
                _worldScreen.CoroutineHandler.Run(new CoStartCombat(_worldScreen, _combatPositionX, _combatPositionY, enc));
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
        
        private void Swap(int leftIndex, int rightIndex)
        {
            (_game.Party.Characters[leftIndex], _game.Party.Characters[rightIndex]) = (_game.Party.Characters[rightIndex], _game.Party.Characters[leftIndex]);
        }

        private readonly List<(int, int)> _positions = [(0, 3), (1, 3), (2, 3), (3, 3)];

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

    }
}