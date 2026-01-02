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
        public enum EScreenStage
        {
            Main,
            Inventory
        }

        private World _world => _worldScreen.World;
        private int _combatPositionX;
        private int _combatPositionY;
        private Encounter _encounter;
        private WorldMapScreen _worldScreen;
        private EScreenStage _stage = EScreenStage.Main;

        private int _selectedIndex = 0;
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

            var start = new Vector2(2, 2);
            var end = new Vector2(30, 17);

            SineaterGame.Instance.Layers["mrmo"].SetRect(start, end, ' ');

            _game.Layers["mrmo"].SetBox(start, end, Sides.Mrmo, Corners.Mrmo);

            int i = 0;
            foreach (var p in SineaterGame.Instance.Party.Characters)
            {
                var (u, v) = p.Job.GetImage();
                p.X = i * 2 - 1;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, p.Tint));
                _game.Layers["ascii"].Set(p.X + 14 + 2* (i + 1), p.Y + 2, fieldsAffinity[i], affinityColors[i]);


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
                p.X = 5 + (4 - i) * 2 + 12;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
                _game.Layers["ascii"].Set(39 + j*4, p.Y + 2, fieldsAffinity[3-j], affinityColors[3-j]);
                i++;
                j++;
            }

            DrawParty();
            DrawControls();

            if (_stage == EScreenStage.Inventory)
            {
                DrawItems();
                CheckSubmenuInputs();
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
                            _game.Layers["ascii"].Set(i * 2 - 1 + 14 + 2 * (i + 1), 6, "!");
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
                            _game.Layers["ascii"].Set(39 + i * 4, 2 , "O");
                        }
                        else if(c == 'X')
                        {
                            _game.Layers["ascii"].Set(39 + i * 4, 2, "@");
                        }
                    }
                }

                var toParty = selectedItem.ToParty;
                if (toParty.Any(x => x != '-') && toParty.Length == 4)
                {
                    if (toParty == "self")
                    {
                        _game.Layers["ascii"].Set(_selectedIndex * 2 - 1 + 14 + 2 * (_selectedIndex + 1), 2, "@");
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        var c = toParty[i];
                        if (c == 'x')
                        {
                            _game.Layers["ascii"].Set(i * 2 - 1 + 14 + 2 * (i + 1), 2, "O");
                        }
                        else if (c == 'X')
                        {
                            _game.Layers["ascii"].Set(i * 2 - 1 + 14 + 2 * (i + 1), 2, "@");
                        }
                    }
                }


            }
        }

        private void SetupItems()
        {
            _submenuSelection = 0;
            _submenu.Clear();

            foreach( var w in _game.Party.Inventory.Items)
            {
                _submenu.Add(w.Name);
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
                var len = _submenu.Select(s => s.Length).Max() + 2;
                var (x, y) = (55, 2);
                _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
                _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                    Sides.Ascii, Corners.Ascii);

                for (var i = 0; i < _submenu.Count; i++)
                {
                    var item = _game.Party.Inventory.Items.Find(x => x.Name == _submenu[i]);

                    _game.Layers["ascii"].Set(x + 3, y + 1 + i, " ", Color.White, GetColorForStat(item.Stat));
                    _game.Layers["ascii"].Set(x + 4, y + 1 + i, $" {_submenu[i]}");
                }

                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }
        }


        private void DrawControls()
        {
            var left = 6;
            var top = 13;
            _game.Layers["input"].Set(left - 1, top, InputM.GetGlyph(EInputAction.SwapLeft));
            _game.Layers["input"].Set(left, top, InputM.GetGlyph(EInputAction.SwapRight));
            _game.Layers["ascii"].Set(left * 2, top - 1, "Swap Left/Right");

            _game.Layers["input"].Set(left - 1, top + 1, InputM.GetGlyph(EInputAction.MoveLeft));
            _game.Layers["input"].Set(left, top + 1, InputM.GetGlyph(EInputAction.MoveRight));
            _game.Layers["ascii"].Set(left * 2, top, "Select");

            _game.Layers["input"].Set(left, top + 2, InputM.GetGlyph(EInputAction.Equipment));
            _game.Layers["ascii"].Set(left * 2, top + 1, "Equipment");

            _game.Layers["input"].Set(left, top + 3, InputM.GetGlyph(EInputAction.StartFight));
            _game.Layers["ascii"].Set(left * 2, top + 2, "Ready");

            _game.Layers["input"].Set(left, top + 4, InputM.GetGlyph(EInputAction.CancelFight));
            _game.Layers["ascii"].Set(left * 2, top + 3, "Back");
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

            _stage = EScreenStage.Main;
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

        }

        public override void Update(GameTime gameTime)
        {
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
            else if (InputM.IsActive(EInputAction.Equipment))
            {
                _stage = EScreenStage.Inventory;
                SetupItems();
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
        }
        private void Swap(int leftIndex, int rightIndex)
        {
            var tmp = _game.Party.Characters[leftIndex];
            _game.Party.Characters[leftIndex] = _game.Party.Characters[rightIndex];
            _game.Party.Characters[rightIndex] = tmp;
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