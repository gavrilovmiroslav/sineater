using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SINEATER.Input;
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
        private EScreenStage _stage = EScreenStage.Main;

        private int _selectedIndex = 0;
        private int _swappingIndex = -1;
        public CombatSetupScreen(SineaterGame game, int x, int y, WorldMapScreen worldScreen, Encounter encounter) : base(game)
        {
            _combatPositionX = x;
            _combatPositionY = y;
            _encounter = encounter;
            _worldScreen = worldScreen;
            _stage = EScreenStage.Main;
        }

        private void SetupSubmenu()
        {
            _submenu.Clear();
            _submenuSelection = 0;

            if (_stage == EScreenStage.Main)
            {
                _submenu.Add("FIGHT");
                _submenu.Add("PREPARE");
                _submenu.Add("CANCEL");
            }
            else if (_stage == EScreenStage.Preparing)
            {
                _submenu.Add("EQUIPMENT");
                _submenu.Add("SWAP");
                _submenu.Add("CANCEL");
            }
            else if (_stage == EScreenStage.Swapping)
            {
                _submenu.Add("SELECT");
                _submenu.Add("CANCEL");
            }
        }

        public override void Draw(SpriteBatch batch, GameTime gameTime)
        {
            _game.Layers["portrait"].Clear();
            _game.Layers["portrait2"].Clear();
            _game.Layers["ascii"].Clear();

            var start = new Vector2(6, 2);
            var end = new Vector2(30, 20);

            SineaterGame.Instance.Layers["mrmo"].SetRect(start, end, ' ');

            _game.Layers["mrmo"].SetBox(start, end, Sides.Mrmo, Corners.Mrmo);

            int i = 0;
            foreach (var p in SineaterGame.Instance.Party.Characters)
            {
                var (u, v) = p.Job.GetImage();
                p.X = i * 2;
                p.Y = 10;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Black, p.Tint));

                if (_selectedIndex == i && _stage == EScreenStage.Preparing)
                {
                    Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Black, p.Tint));
                }

                if (_stage == EScreenStage.Swapping)
                {
                    if (_selectedIndex == i)
                    {
                        Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Gray, p.Tint));
                    }
                }

                if (_swappingIndex == i && _stage == EScreenStage.Swapping)
                {
                    Draw(p.X, p.Y - 1, new Glyph(8, 74 - 16, Color.Black, p.Tint));
                }

                i++;
            }

            foreach (var p in _encounter.Enemies)
            {
                var (u, v) = p.GetIcon();
                p.X = 5 + (4 - i) * 2 + 15;
                p.Y = 10;
                Draw(p.X, p.Y, new Glyph(u, v, Color.Transparent, p.Tint));
                i++;
            }

            DrawSubmenu();
        }

        private void DrawSubmenu()
        {
            if (_submenu.Count > 0)
            {
                var len = _submenu.Select(s => s.Length).Max() + 2;
                var (x, y) = (15, 19);
                _game.Layers["ascii"].SetRect(new Vector2(x, y), new Vector2(x + 5 + len, y + 1 + _submenu.Count), ' ');
                _game.Layers["ascii"].SetBox(new Vector2(x, y), new Vector2(x + 4 + len, y + 1 + _submenu.Count),
                    Sides.Ascii, Corners.Ascii);

                for (var i = 0; i < _submenu.Count; i++)
                {
                    _game.Layers["ascii"].Set(x + 2, y + 1 + i, $"  {_submenu[i]}");
                }

                _game.Layers["ascii"].Set(x + 2, y + 1 + _submenuSelection, ">");
            }
        }

        public override void Initialize(SineaterGame game)
        {
            SetupSubmenu();
        }

        public override void Update(GameTime gameTime)
        {
            if (_stage == EScreenStage.Preparing)
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
            }
            else if (_stage == EScreenStage.Swapping)
            {
                if (InputM.IsActive(EInputAction.MoveRight))
                {
                    _swappingIndex += 1;
                    if (_swappingIndex == _selectedIndex) _swappingIndex += 1;

                    if (_swappingIndex > 3) _swappingIndex = _selectedIndex != 0 ? 0 : 1;
                }
                else if (InputM.IsActive(EInputAction.MoveLeft))
                {
                    _swappingIndex -= 1;
                    if (_swappingIndex == _selectedIndex) _swappingIndex -= 1;
                    if (_swappingIndex < 0) _swappingIndex = 3;
                }
            }

             CheckSubmenuInputs();
        }

        public override void SubmenuActivate(string action)
        {
            if (_stage == EScreenStage.Main)
            {
                HandleMainStage(action);
            }
            else if (_stage == EScreenStage.Preparing)
            {
                HandlePrepStage(action);
            }
            else if (_stage == EScreenStage.Swapping)
            {
                HandleSwapStage(action);
            }
        }
        private void HandleMainStage(string action)
        {
            if (action == "PREPARE")
            {
                _stage = EScreenStage.Preparing;
                SetupSubmenu();
            }
            else if (action == "FIGHT")
            {
                _game.ScreenStack.Pop();
                var enc = _world.Encounters.Get(_combatPositionX, _combatPositionY);
                CoroutineHandler.Run(new CoStartCombat(_worldScreen, _combatPositionX, _combatPositionY, enc));
            }
            else if (action == "CANCEL")
            {
                _game.ScreenStack.Pop();
            }
        }
        private void HandlePrepStage(string action)
        {
            if (action == "EQUIPMENT")
            {
                _stage = EScreenStage.Preparing;
                SetupSubmenu();
            }
            else if (action == "SWAP")
            {
                _stage = EScreenStage.Swapping;
                _swappingIndex = _selectedIndex + 1;
                if (_swappingIndex > 3) _swappingIndex = 0;
                SetupSubmenu();
            }
            else if (action == "CANCEL")
            {
                _stage = EScreenStage.Main;
                SetupSubmenu();
            }
        }

        private void HandleSwapStage(string action)
        {
            if (action == "SELECT")
            {
                var tmp = _game.Party.Characters[_selectedIndex];
                _game.Party.Characters[_selectedIndex] = _game.Party.Characters[_swappingIndex];
                _game.Party.Characters[_swappingIndex] = tmp;
                _swappingIndex = -1;

                _stage = EScreenStage.Preparing;
                SetupSubmenu();
            }
            else if (action == "CANCEL")
            {
                _stage = EScreenStage.Preparing;
                SetupSubmenu();
            }
        }
    }
}