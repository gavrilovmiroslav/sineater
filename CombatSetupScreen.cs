using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Input;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER
{
    public class CombatSetupScreen : Screen
    {
        public enum EScreenStage
        {
            Main,
            Preparing,
            Swapping,
            Inventory
        }

        private World _world => _worldScreen.World;
        private int _combatPositionX;
        private int _combatPositionY;
        private Encounter _encounter;
        private WorldMapScreen _worldScreen;

        private int _selectedIndex = 0;
        public CombatSetupScreen(SineaterGame game, int x, int y, WorldMapScreen worldScreen, Encounter encounter) : base(game)
        {
            _combatPositionX = x;
            _combatPositionY = y;
            _encounter = encounter;
            _worldScreen = worldScreen;
        }

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
                p.X = i * 2;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, p.Tint));

                if (_selectedIndex == i)
                {
                    Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Black, p.Tint));
                }

                i++;
            }

            foreach (var p in _encounter.Enemies)
            {
                var (u, v) = p.GetIcon();
                p.X = 5 + (4 - i) * 2 + 15;
                p.Y = 3;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
                i++;
            }

            DrawParty();

            DrawControls();
        }

        private void DrawControls()
        {

            var left = 6;

            _game.Layers["input"].Set(left - 1, 13, InputM.GetGlyph(EInputAction.SwapLeft));
            _game.Layers["input"].Set(left, 13, InputM.GetGlyph(EInputAction.SwapRight));
            _game.Layers["ascii"].Set(left * 2, 12, "Swap Left/Right");

            _game.Layers["input"].Set(left -1, 14, InputM.GetGlyph(EInputAction.MoveLeft));
            _game.Layers["input"].Set(left, 14, InputM.GetGlyph(EInputAction.MoveRight));
            _game.Layers["ascii"].Set(left*2, 13, "Select");

            _game.Layers["input"].Set(left, 15, InputM.GetGlyph(EInputAction.Equipment));
            _game.Layers["ascii"].Set(left*2, 14, "Equipment");

            _game.Layers["input"].Set(left, 16, InputM.GetGlyph(EInputAction.StartFight));
            _game.Layers["ascii"].Set(left*2, 15, "Ready");

            _game.Layers["input"].Set(left, 17, InputM.GetGlyph(EInputAction.CancelFight));
            _game.Layers["ascii"].Set(left*2, 16, "Back");
        }

        public override void PostDraw(SpriteBatch batch, GameTime gameTime)
        {
            base.PostDraw(batch, gameTime);
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