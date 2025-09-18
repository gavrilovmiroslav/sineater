using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;

namespace SINEATER;

public class InventoryScreen : IScreen
{
    private SineaterGame _game;
    private bool _showOutfitting = false;
    private int _selected = -1;
    private int _toBeEquipped = -1;
    private CoroutineHandler _coroutineHandler = new();
    
    public InventoryScreen(SineaterGame game, bool showOutfitting = false)
    {
        _game = game;
        _showOutfitting = showOutfitting;
    }
    
    public void Initialize(SineaterGame game)
    {}

    public void Update(GameTime gameTime)
    {
        if (_coroutineHandler.IsActive())
        {
            _coroutineHandler.Update();
            return;
        }
        
        if (KB.HasBeenPressed(Keys.Escape))
        {
            if (_toBeEquipped >= 0)
            {
                _toBeEquipped = -1;
            }
            else
            {
                _game.ScreenStack.Pop();
                _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(36, 28), ' ');
                _game.ScreenStack.Peek().Draw(gameTime);
            }
        };

        if (_toBeEquipped >= 0)
        {
            var newSelected = -1;
            if (KB.HasBeenPressed(Keys.D1)) newSelected = 0;
            if (KB.HasBeenPressed(Keys.D2)) newSelected = 1;
            if (KB.HasBeenPressed(Keys.D3)) newSelected = 2;
            if (KB.HasBeenPressed(Keys.D4)) newSelected = 3;
            if (KB.HasBeenPressed(Keys.D5)) newSelected = 4;
            if (KB.HasBeenPressed(Keys.D6)) newSelected = 5;
            if (KB.HasBeenPressed(Keys.D7)) newSelected = 6;
            if (KB.HasBeenPressed(Keys.D8)) newSelected = 7;
            if (KB.HasBeenPressed(Keys.D9)) newSelected = 8;
            if (KB.HasBeenPressed(Keys.A)) newSelected = 9;
            if (KB.HasBeenPressed(Keys.B)) newSelected = 10;
            if (KB.HasBeenPressed(Keys.C)) newSelected = 11;
            if (newSelected != -1)
            {
                var charIndex = newSelected / 3;
                var slotIndex = newSelected % 3;
                Console.WriteLine($"char = {charIndex}, slot = {slotIndex}");
                switch (slotIndex)
                {
                    case 0:
                        if (_game.Party.Characters[charIndex].LeftWeapon == null && _game.Inventory.Items[_toBeEquipped] is Weapon lh)
                        {
                            _game.Party.Characters[charIndex].LeftWeapon = lh;
                            _game.Inventory.Drop(_toBeEquipped);
                            _toBeEquipped = -1;
                        }
                        break;
                    case 1:
                        if (_game.Party.Characters[charIndex].RightWeapon == null && _game.Inventory.Items[_toBeEquipped] is Weapon rh)
                        {
                            _game.Party.Characters[charIndex].RightWeapon = rh;
                            _game.Inventory.Drop(_toBeEquipped);
                            _toBeEquipped = -1;
                        }
                        break;
                    case 2:
                        if (_game.Party.Characters[charIndex].Armor == null && _game.Inventory.Items[_toBeEquipped] is Armor arm)
                        {
                            _game.Party.Characters[charIndex].Armor = arm;
                            _game.Inventory.Drop(_toBeEquipped);
                            _toBeEquipped = -1;
                        }
                        break;
                }
            }
        }
        else
        {
            if (KB.HasBeenPressed(Keys.Tab))
            {
                _showOutfitting = !_showOutfitting;
            }

            if (KB.HasBeenPressed(Keys.I))
            {
                _showOutfitting = false;
            }

            if (KB.HasBeenPressed(Keys.O))
            {
                _showOutfitting = true;
            }

            if (_selected >= 0 && KB.HasBeenPressed(Keys.D))
            {
                if (_showOutfitting)
                {
                    var charIndex = _selected / 3;
                    var slotIndex = _selected % 3;

                    switch (slotIndex)
                    {
                        case 0:
                            _game.Party.Characters[charIndex].LeftWeapon = null;
                            break;
                        case 1:
                            _game.Party.Characters[charIndex].RightWeapon = null;
                            break;
                        case 2:
                            _game.Party.Characters[charIndex].Armor = null;
                            break;
                    }
                }
                else
                {
                    _game.Inventory.Drop(_selected);
                }
            }

            if (_selected >= 0 && !_showOutfitting && _game.Inventory.Items[_selected] != null &&
                KB.HasBeenPressed(Keys.P))
            {
                _toBeEquipped = _selected;
                Console.WriteLine($"to be equipped = {_toBeEquipped}");
                _showOutfitting = true;
                return;
            }

            if (_selected >= 0 && _showOutfitting && KB.HasBeenPressed(Keys.U))
            {
                var charIndex = _selected / 3;
                var slotIndex = _selected % 3;

                IAbilitySource? saved = null;

                switch (slotIndex)
                {
                    case 0:
                        saved = _game.Party.Characters[charIndex].LeftWeapon;
                        break;
                    case 1:
                        saved = _game.Party.Characters[charIndex].RightWeapon;
                        break;
                    case 2:
                        saved = _game.Party.Characters[charIndex].Armor;
                        break;
                }

                if (saved != null)
                {
                    var (okay, _) = _game.Inventory.Put(saved);
                    if (okay)
                    {
                        switch (slotIndex)
                        {
                            case 0:
                                _game.Party.Characters[charIndex].LeftWeapon = null;
                                break;
                            case 1:
                                _game.Party.Characters[charIndex].RightWeapon = null;
                                break;
                            case 2:
                                _game.Party.Characters[charIndex].Armor = null;
                                break;
                        }
                    }
                }
            }
            else if (_selected >= 0 && !_showOutfitting && KB.HasBeenPressed(Keys.U))
            {
                if (_game.Inventory.Items[_selected] is IItem item)
                {
                    if (_game.Party.Selected == -1)
                    {
                        _coroutineHandler.Run(item.ApplyItemUsed(_game.Party.Characters[0]));
                    }
                    else
                    {
                        _coroutineHandler.Run(item.ApplyItemUsed(_game.Party.Characters[_game.Party.Selected]));
                    }
                    _game.Inventory.Drop(_selected);
                }
            }
            
            var oldSelected = _selected;
            var newSelected = -1;
            if (KB.HasBeenPressed(Keys.D1)) newSelected = 0;
            if (KB.HasBeenPressed(Keys.D2)) newSelected = 1;
            if (KB.HasBeenPressed(Keys.D3)) newSelected = 2;
            if (KB.HasBeenPressed(Keys.D4)) newSelected = 3;
            if (KB.HasBeenPressed(Keys.D5)) newSelected = 4;
            if (KB.HasBeenPressed(Keys.D6)) newSelected = 5;
            if (KB.HasBeenPressed(Keys.D7)) newSelected = 6;
            if (KB.HasBeenPressed(Keys.D8)) newSelected = 7;
            if (KB.HasBeenPressed(Keys.D9)) newSelected = 8;
            if (KB.HasBeenPressed(Keys.A)) newSelected = 9;
            if (KB.HasBeenPressed(Keys.B)) newSelected = 10;
            if (KB.HasBeenPressed(Keys.C)) newSelected = 11;
            if (KB.HasBeenPressed(Keys.Up))
            {
                newSelected = oldSelected - 1;
                if (newSelected < 0)
                    newSelected = 11;
            }

            if (KB.HasBeenPressed(Keys.Down)) newSelected = (oldSelected + 1) % 12;
            if (newSelected != -1)
            {
                _selected = newSelected;
                if (_selected == oldSelected)
                {
                    _selected = -1;
                }
            }
        }
    }

    public string[] Nums = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E"];
    
    public void Draw(GameTime gameTime)
    {
        var stack = _game.ScreenStack.ToArray();
        stack[1].Draw(gameTime);
        
        var start = new Vector2(10, 0);
        var end = new Vector2(35, 17);
        _game.Layers["mrmo"].SetRect(start - Vector2.One, end + Vector2.One, ' ');
        _game.Layers["ascii"].SetRect(new Vector2(start.X * 2 - 1, start.Y - 1), new Vector2(end.X * 2 + 1, end.Y + 1), ' ');
        _game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        {
            Top = Glyph.Bw(10, 27),
            Bottom = Glyph.Bw(10, 29),
            Left = Glyph.Bw(9, 28),
            Right = Glyph.Bw(11, 28),
        }, new Corners<Glyph>()
        {
            BottomLeft = Glyph.Bw(11 - 2, 31 - 4 + 2), 
            BottomRight = Glyph.Bw(10, 30), 
            TopLeft = Glyph.Bw(11 - 2, 31 - 4), 
            TopRight = Glyph.Bw(10, 31),
        });

        if (_showOutfitting)
        {
            if (_toBeEquipped >= 0)
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, $"EQUIPPING {_game.Inventory.Items[_toBeEquipped].ToString().ToUpper()}");
            }
            else
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "OUTFITTING");
            }

            int i = 0;
            foreach (var character in _game.Party.Characters)
            {
                var (u, v) = character.Job.GetImage();
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.LeftWeapon is { } lh)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   LH {lh.Name} (Attack: {lh.Attack}, Weight: {lh.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   LH --");
                }
                i++;
                
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.RightWeapon is { } rh)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   RH {rh.Name} (Attack: {rh.Attack}, Weight: {rh.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   RH --");
                }
                i++;
                
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.Armor is { } armor)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   DF {armor.Name} (Guard: {armor.Guard}, Weight: {armor.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   DF --");
                }
                i++;
            }

            if (_selected > -1)
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 3 + _selected, $">");
                _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)end.Y - 1, "[U]NEQUIP [D]ROP [T]HROW");
                _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)end.Y - 1, "U", Color.Gold);
                _game.Layers["ascii"].Set((int)start.X * 2 + 14, (int)end.Y - 1, "D", Color.Gold);
                _game.Layers["ascii"].Set((int)start.X * 2 + 21, (int)end.Y - 1, "T", Color.Gold);
            }
        }
        else
        {
            _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "INVENTORY");
            for (int i = 0; i < 12; i++)
            {
                var item = _game.Inventory.Items[i];
                if (item is Weapon weapon)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}. WPN {weapon.Name} (Attack: {weapon.Attack}, Weight: {weapon.Weight.ToString()})");
                }
                else if (item is Armor armor)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}. ARM {armor.Name} (Guard: {armor.Guard}, Weight: {armor.Weight.ToString()})");
                }
                else if (item is Item other)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     {other}");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     --");
                }
            }
            
            if (_selected > -1)
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 3 + _selected, $">");
                if (_game.Inventory.Items[_selected] is IEquippable eq)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)end.Y - 1, "EQUI[P]");
                    _game.Layers["ascii"].Set((int)start.X * 2 + 8, (int)end.Y - 1, "P", Color.Gold);
                }

                _game.Layers["ascii"].Set((int)start.X * 2 + 11, (int)end.Y - 1, "[D]ROP");
                _game.Layers["ascii"].Set((int)start.X * 2 + 12, (int)end.Y - 1, "D", Color.Gold);

                if (_game.Inventory.Items[_selected] is IItem item)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 18, (int)end.Y - 1, "[T]HROW");
                    _game.Layers["ascii"].Set((int)start.X * 2 + 19, (int)end.Y - 1, "T", Color.Gold);
                    
                    _game.Layers["ascii"].Set((int)start.X * 2 + 26, (int)end.Y - 1, "[U]SE");
                    _game.Layers["ascii"].Set((int)start.X * 2 + 27, (int)end.Y - 1, "U", Color.Gold);
                }
            }
        }
    }
}