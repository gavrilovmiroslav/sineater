using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;

namespace SINEATER;

public class InventoryScreen : IScreen
{
    private SineaterGame _game;
    private int _time = 0;
    private bool _showOutfitting = false;
    private int _selected = -1;
    private int _toBeEquipped = -1;
    private bool _chooseToEquip = false;
    private Type? _chooseToEquipType = null;
    private int _chooseToEquipIndex = -1;
    private (int, int) _chooseCharSlot = (-1, -1);
    private CoroutineHandler _coroutineHandler = new();
    
    public InventoryScreen(SineaterGame game, bool showOutfitting = false)
    {
        _game = game;
        _showOutfitting = showOutfitting;
    }
    
    public void Initialize(SineaterGame game)
    {}

    public void ChooseToEquipPrevious()
    {
        var inventory = _game.Party.Characters[_game.Party.Selected].Inventory;
        if (inventory.Items.Length == 0) return;
        
        _chooseToEquipIndex--;
        if (_chooseToEquipIndex < 0)
        {
            _chooseToEquipIndex = inventory.Items.Length - 1;
            ChooseToEquipPrevious();
        }
        else
        {
            if (inventory.Items[_chooseToEquipIndex] != null)
            {
                var item = inventory.Items[_chooseToEquipIndex];
                if (!(item.GetType() == _chooseToEquipType || item.GetType().IsSubclassOf(_chooseToEquipType)))
                {
                    ChooseToEquipPrevious();
                }
            }
            else
            {
                ChooseToEquipPrevious();
            }
        }
    }

    public void ChooseToEquipNext()
    {
        var inventory = _game.Party.Characters[_game.Party.Selected].Inventory;
        if (inventory.Items.Length == 0) return;
        
        _chooseToEquipIndex++;
        if (_chooseToEquipIndex < 0)
        {
            ChooseToEquipNext();
        }
        else if (_chooseToEquipIndex >= inventory.Items.Length)
        {
            _chooseToEquipIndex = -1;
            ChooseToEquipNext();
        }
        else
        {
            if (inventory.Items[_chooseToEquipIndex] != null)
            {
                var item = inventory.Items[_chooseToEquipIndex];
                if (!(item.GetType() == _chooseToEquipType || item.GetType().IsSubclassOf(_chooseToEquipType)))
                {
                    ChooseToEquipNext();
                }
            }
            else
            {
                ChooseToEquipNext();
            }
        }
    }
    
    public void Update(GameTime gameTime)
    {
        // var inventory = _game.Party.Characters[_game.Party.Selected].Inventory;
        // _time += gameTime.ElapsedGameTime.Milliseconds;
        // if (_time > 1600)
        // {
        //     _time = 0;
        // }
        //
        // if (_coroutineHandler.IsActive())
        // {
        //     _coroutineHandler.Update();
        //     return;
        // }
        //
        // if (KB.HasBeenPressed(Keys.Space))
        // {
        //     var p = _game.Party.Selected; 
        //     p = (p + 1) % 4;
        //     _game.Party.Selected = p;
        // }
        //
        // if (KB.HasBeenPressed(Keys.Escape))
        // {
        //     if (_toBeEquipped >= 0)
        //     {
        //         _toBeEquipped = -1;
        //     }
        //     else if (_chooseToEquip)
        //     {
        //         _chooseToEquip = false;
        //         _chooseToEquipType = null;
        //         _chooseToEquipIndex = -1;
        //         _chooseCharSlot = (-1, -1);
        //     }
        //     else
        //     {
        //         _game.ScreenStack.Pop();
        //         _game.Layers["mrmo"].SetRect(new Vector2(0, 0), new Vector2(36, 28), ' ');
        //         _game.ScreenStack.Peek().Draw(gameTime);
        //     }
        // };
        //
        // if (_toBeEquipped >= 0)
        // {
        //     var newSelected = -1;
        //     if (KB.HasBeenPressed(Keys.D1)) newSelected = 0;
        //     if (KB.HasBeenPressed(Keys.D2)) newSelected = 1;
        //     if (KB.HasBeenPressed(Keys.D3)) newSelected = 2;
        //     if (KB.HasBeenPressed(Keys.D4)) newSelected = 3;
        //     if (KB.HasBeenPressed(Keys.D5)) newSelected = 4;
        //     if (KB.HasBeenPressed(Keys.D6)) newSelected = 5;
        //     if (KB.HasBeenPressed(Keys.D7)) newSelected = 6;
        //     if (KB.HasBeenPressed(Keys.D8)) newSelected = 7;
        //     if (KB.HasBeenPressed(Keys.D9)) newSelected = 8;
        //     if (KB.HasBeenPressed(Keys.A)) newSelected = 9;
        //     if (KB.HasBeenPressed(Keys.B)) newSelected = 10;
        //     if (KB.HasBeenPressed(Keys.C)) newSelected = 11;
        //     if (newSelected != -1)
        //     {
        //         var charIndex = newSelected / 3;
        //         var slotIndex = newSelected % 3;
        //         Console.WriteLine($"char = {charIndex}, slot = {slotIndex}");
        //         switch (slotIndex)
        //         {
        //             case 0:
        //                 if (_game.Party.Characters[charIndex].LeftWeapon == null && inventory.Items[_toBeEquipped] is Weapon lh)
        //                 {
        //                     _game.Party.Characters[charIndex].EquipLeftWeapon(lh);
        //                     inventory.Drop(_toBeEquipped);
        //                     _toBeEquipped = -1;
        //                 }
        //                 break;
        //             case 1:
        //                 if (_game.Party.Characters[charIndex].RightWeapon == null && inventory.Items[_toBeEquipped] is Weapon rh)
        //                 {
        //                     _game.Party.Characters[charIndex].EquipRightWeapon(rh);
        //                     inventory.Drop(_toBeEquipped);
        //                     _toBeEquipped = -1;
        //                 }
        //                 break;
        //             case 2:
        //                 if (_game.Party.Characters[charIndex].Armor == null && inventory.Items[_toBeEquipped] is Armor arm)
        //                 {
        //                     _game.Party.Characters[charIndex].EquipArmor(arm);
        //                     inventory.Drop(_toBeEquipped);
        //                     _toBeEquipped = -1;
        //                 }
        //                 break;
        //         }
        //     }
        // }
        // else if (_chooseToEquip)
        // {
        //     if (KB.HasBeenPressed(Keys.Up))
        //     {
        //         ChooseToEquipPrevious();
        //     }
        //     else if (KB.HasBeenPressed(Keys.Down))
        //     {
        //         ChooseToEquipNext();
        //     }
        //     else if (KB.HasBeenPressed(Keys.Enter))
        //     {
        //         if (_chooseToEquipIndex > -1)
        //         {
        //             var item = inventory.Items[_chooseToEquipIndex];
        //             var (chr, slot) = _chooseCharSlot;
        //             switch (slot)
        //             {
        //                 case 0:
        //                     _game.Party.Characters[chr].EquipLeftWeapon(item as Weapon);
        //                     break;
        //                 case 1:
        //                     _game.Party.Characters[chr].EquipRightWeapon(item as Weapon);
        //                     break;
        //                 case 2:
        //                     _game.Party.Characters[chr].EquipArmor(item as Armor);
        //                     break;
        //             }
        //             inventory.Items[_chooseToEquipIndex] = null;
        //             _chooseToEquip = false;
        //             _chooseToEquipIndex = -1;
        //             _chooseCharSlot = (-1, -1);
        //             _chooseToEquipType = null;
        //         }
        //     }
        // }
        // else
        // {
        //     if (KB.HasBeenPressed(Keys.Tab))
        //     {
        //         _showOutfitting = !_showOutfitting;
        //     }
        //     
        //     if (KB.HasBeenPressed(Keys.I))
        //     {
        //         _showOutfitting = false;
        //     }
        //
        //     if (KB.HasBeenPressed(Keys.O))
        //     {
        //         _showOutfitting = true;
        //     }
        //
        //     if (_selected >= 0 && KB.HasBeenPressed(Keys.D))
        //     {
        //         if (_showOutfitting)
        //         {
        //             var charIndex = _selected / 3;
        //             var slotIndex = _selected % 3;
        //
        //             switch (slotIndex)
        //             {
        //                 case 0:
        //                     _game.Party.Characters[charIndex].EquipLeftWeapon(null);
        //                     break;
        //                 case 1:
        //                     _game.Party.Characters[charIndex].EquipRightWeapon(null);
        //                     break;
        //                 case 2:
        //                     _game.Party.Characters[charIndex].EquipArmor(null);
        //                     break;
        //             }
        //         }
        //         else
        //         {
        //             inventory.Drop(_selected);
        //         }
        //     }
        //
        //     if (_selected >= 0 && !_showOutfitting && inventory.Items[_selected] != null &&
        //         KB.HasBeenPressed(Keys.E))
        //     {
        //         _toBeEquipped = _selected;
        //         Console.WriteLine($"to be equipped = {_toBeEquipped}");
        //         _showOutfitting = true;
        //         return;
        //     }
        //
        //     if (_selected >= 0 && _showOutfitting && KB.HasBeenPressed(Keys.E))
        //     {
        //         var charIndex = _selected / 3;
        //         var slotIndex = _selected % 3;
        //
        //         IItem? item = null;
        //         switch (slotIndex)
        //         {
        //             case 0:
        //                 item = _game.Party.Characters[charIndex].LeftWeapon;
        //                 break;
        //             case 1:
        //                 item = _game.Party.Characters[charIndex].RightWeapon;
        //                 break;
        //             case 2:
        //                 item = _game.Party.Characters[charIndex].Armor;
        //                 break;
        //         }
        //
        //         if (item == null)
        //         {
        //             _chooseToEquip = true;
        //             _chooseCharSlot = (charIndex, slotIndex);
        //             if (slotIndex < 2) _chooseToEquipType = typeof(Weapon);
        //             else _chooseToEquipType = typeof(Armor);
        //
        //             for (var i = 0; i < inventory.Items.Length; i++)
        //             {
        //                 if (inventory.Items[i] == null) continue;
        //                 var t = inventory.Items[i].GetType();
        //                 if (t == _chooseToEquipType || t.IsSubclassOf(_chooseToEquipType))
        //                 {
        //                     _chooseToEquipIndex = i;
        //                     break;
        //                 }
        //             }
        //         }
        //     }
        //
        //     if (_selected >= 0
        //         && _game.ScreenStack.Any(i => i is CombatMapScreen) 
        //         && KB.HasBeenPressed(Keys.T))
        //     {
        //         if (!_showOutfitting && inventory.Items[_selected] != null)
        //         {
        //             _game.ScreenStack.Pop();
        //             var cmb = _game.ScreenStack.Peek() as CombatMapScreen;
        //             var owner = _game.Party.Characters[cmb.PlayerSelectedIndex];
        //             
        //             cmb.RangedActionConfig = new RangedTargetting
        //             {
        //                 Source = inventory.Items[_selected],
        //                 Owner = owner,
        //                 X = owner.X,
        //                 Y = owner.Y
        //             };
        //         }
        //         else if (_showOutfitting)
        //         {
        //             var charIndex = _selected / 3;
        //             var slotIndex = _selected % 3;
        //             IAbilitySource? item = null;
        //
        //             switch (slotIndex)
        //             {
        //                 case 0:
        //                     item = _game.Party.Characters[charIndex].LeftWeapon;
        //                     break;
        //                 case 1:
        //                     item = _game.Party.Characters[charIndex].RightWeapon;
        //                     break;
        //                 case 2:
        //                     item = _game.Party.Characters[charIndex].Armor;
        //                     break;
        //             }
        //
        //             if (item != null)
        //             {
        //                 switch (slotIndex)
        //                 {
        //                     case 0:
        //                         _game.Party.Characters[charIndex].EquipLeftWeapon(null);
        //                         break;
        //                     case 1:
        //                         _game.Party.Characters[charIndex].EquipRightWeapon(null);
        //                         break;
        //                     case 2:
        //                         _game.Party.Characters[charIndex].EquipArmor(null);
        //                         break;
        //                 }
        //                 _game.ScreenStack.Pop();
        //                 var cmb = _game.ScreenStack.Peek() as CombatMapScreen;
        //                 cmb.PlayerSelectedIndex = charIndex;
        //                 var owner = _game.Party.Characters[cmb.PlayerSelectedIndex];
        //                 cmb.RangedActionConfig = new RangedTargetting
        //                 {
        //                     Source = item,
        //                     Owner = owner,
        //                     X = owner.X,
        //                     Y = owner.Y
        //                 };
        //             }
        //         }
        //     }
        //     
        //     if (_selected >= 0 && _showOutfitting && KB.HasBeenPressed(Keys.U))
        //     {
        //         var charIndex = _selected / 3;
        //         var slotIndex = _selected % 3;
        //
        //         IItem? saved = null;
        //
        //         switch (slotIndex)
        //         {
        //             case 0:
        //                 saved = _game.Party.Characters[charIndex].LeftWeapon;
        //                 break;
        //             case 1:
        //                 saved = _game.Party.Characters[charIndex].RightWeapon;
        //                 break;
        //             case 2:
        //                 saved = _game.Party.Characters[charIndex].Armor;
        //                 break;
        //         }
        //
        //         if (saved != null)
        //         {
        //             var (okay, _) = inventory.Put(saved);
        //             if (okay)
        //             {
        //                 switch (slotIndex)
        //                 {
        //                     case 0:
        //                         _game.Party.Characters[charIndex].EquipLeftWeapon(null);
        //                         break;
        //                     case 1:
        //                         _game.Party.Characters[charIndex].EquipRightWeapon(null);
        //                         break;
        //                     case 2:
        //                         _game.Party.Characters[charIndex].EquipArmor(null);
        //                         break;
        //                 }
        //             }
        //         }
        //     }
        //     else if (_selected >= 0 && !_showOutfitting && KB.HasBeenPressed(Keys.U))
        //     {
        //         if (inventory.Items[_selected] is IItem item)
        //         {
        //             _coroutineHandler.Run(item.ApplyItemUsed(_game.Party.Characters[_game.Party.Selected]));
        //             inventory.Drop(_selected);
        //         }
        //     }
        //     
        //     var oldSelected = _selected;
        //     var newSelected = -1;
        //     if (KB.HasBeenPressed(Keys.D1)) newSelected = 0;
        //     if (KB.HasBeenPressed(Keys.D2)) newSelected = 1;
        //     if (KB.HasBeenPressed(Keys.D3)) newSelected = 2;
        //     if (KB.HasBeenPressed(Keys.D4)) newSelected = 3;
        //     if (KB.HasBeenPressed(Keys.D5)) newSelected = 4;
        //     if (KB.HasBeenPressed(Keys.D6)) newSelected = 5;
        //     if (KB.HasBeenPressed(Keys.D7)) newSelected = 6;
        //     if (KB.HasBeenPressed(Keys.D8)) newSelected = 7;
        //     if (KB.HasBeenPressed(Keys.D9)) newSelected = 8;
        //     if (KB.HasBeenPressed(Keys.A)) newSelected = 9;
        //     if (KB.HasBeenPressed(Keys.B)) newSelected = 10;
        //     if (KB.HasBeenPressed(Keys.C)) newSelected = 11;
        //     if (KB.HasBeenPressed(Keys.Up))
        //     {
        //         newSelected = oldSelected - 1;
        //         if (newSelected < 0)
        //             newSelected = 11;
        //     }
        //
        //     if (KB.HasBeenPressed(Keys.Down)) newSelected = (oldSelected + 1) % 12;
        //     if (newSelected != -1)
        //     {
        //         _selected = newSelected;
        //         if (_selected == oldSelected)
        //         {
        //             _selected = -1;
        //         }
        //     }
        // }
    }

    public string[] Nums = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E"];

    public void Draw(GameTime gameTime)
    {
        // var stack = _game.ScreenStack.ToArray();
        // stack[1].Draw(gameTime);
        //
        // var start = new Vector2(10, 1);
        // var end = new Vector2(35, 17);
        // _game.Layers["mrmo"].SetRect(start - Vector2.One, end + Vector2.One, ' ');
        // _game.Layers["ascii"].SetRect(new Vector2(start.X * 2, start.Y), new Vector2(end.X * 2 + 1, end.Y + 1), ' ');
        // _game.Layers["mrmo"].SetBox(start, end, new Sides<Glyph>()
        // {
        //     Top = Glyph.Bw(10, 27),
        //     Bottom = Glyph.Bw(10, 29),
        //     Left = Glyph.Bw(9, 28),
        //     Right = Glyph.Bw(11, 28),
        // }, new Corners<Glyph>()
        // {
        //     BottomLeft = Glyph.Bw(11 - 2, 31 - 4 + 2), 
        //     BottomRight = Glyph.Bw(10, 30), 
        //     TopLeft = Glyph.Bw(11 - 2, 31 - 4), 
        //     TopRight = Glyph.Bw(10, 31),
        // });
        //
        // var inventory = _game.Party.Characters[_game.Party.Selected].Inventory;
        // if (_showOutfitting)
        // {
        //     if (_toBeEquipped >= 0)
        //     {
        //         _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, $"EQUIPPING {inventory.Items[_toBeEquipped].ToString().ToUpper()}");
        //     }
        //     else
        //     {
        //         _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "OUTFITTING");
        //     }
        //
        //     int i = 0;
        //     foreach (var character in _game.Party.Characters)
        //     {
        //         var (u, v) = character.Job.GetImage();
        //         _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
        //         if (character.LeftWeapon is Shield lshield)
        //         { 
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   LH {lshield.Name}");
        //         }
        //         else if (character.LeftWeapon is { } lh)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   LH {lh.Name}");
        //         }
        //         else
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   LH --");
        //         }
        //         i++;
        //         
        //         _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
        //         if (character.RightWeapon is Shield rshield)
        //         { 
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   RH {rshield.Name}");
        //         }
        //         else if (character.RightWeapon is { } rh)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   RH {rh.Name}");
        //         }
        //         else
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   RH --");
        //         }
        //         i++;
        //         
        //         _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
        //         if (character.Armor is { } armor)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   DF {armor.Name}");
        //         }
        //         else
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.   DF --");
        //         }
        //         i++;
        //     }
        //
        //     if (_selected > -1)
        //     {
        //         _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 3 + _selected, $">");
        //         
        //         var charIndex = _selected / 3;
        //         var slotIndex = _selected % 3;
        //
        //         IItem? piece = null;
        //
        //         switch (slotIndex)
        //         {
        //             case 0:
        //                 piece = _game.Party.Characters[charIndex].LeftWeapon;
        //                 break;
        //             case 1:
        //                 piece = _game.Party.Characters[charIndex].RightWeapon;
        //                 break;
        //             case 2:
        //                 piece = _game.Party.Characters[charIndex].Armor;
        //                 break;
        //         }
        //
        //         if (piece != null)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)end.Y - 1, "[U]NEQUIP [D]ROP [T]HROW");
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)end.Y - 1, "U", Color.Gold);
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 14, (int)end.Y - 1, "D", Color.Gold);
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 21, (int)end.Y - 1, "T", Color.Gold);
        //         }
        //         else
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)end.Y - 1, "[E]QUIP");
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)end.Y - 1, "E", Color.Gold);
        //             if (_chooseToEquip)
        //             {
        //                 if ((_time / 200) % 2 == 0)
        //                 {
        //                     if (_chooseToEquipIndex >= 0 && _chooseToEquipIndex < inventory.Items.Length)
        //                     {
        //                         var item = inventory.Items[_chooseToEquipIndex];
        //                         if (item != null)
        //                         {
        //                             if (item is Weapon w)
        //                             {
        //                                 _game.Layers["ascii"].Set((int)start.X * 2 + 13, (int)start.Y + 3 + _selected,
        //                                     $"[{w.ToLongString()}]", Color.Yellow);
        //                             } 
        //                             else if (item is Armor a)
        //                             {
        //                                 _game.Layers["ascii"].Set((int)start.X * 2 + 13, (int)start.Y + 3 + _selected,
        //                                     $"[{a.ToLongString()}]", Color.Yellow);
        //                             }
        //                             else
        //                             {
        //                                 _game.Layers["ascii"].Set((int)start.X * 2 + 13, (int)start.Y + 3 + _selected,
        //                                     $"[{item}]", Color.Yellow);
        //                             }
        //                         }
        //                     }
        //                 }
        //             }
        //         }
        //     }
        // }
        // else
        // {
        //     _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "INVENTORY");
        //     for (int i = 0; i < 12; i++)
        //     {
        //         var item = inventory.Items[i];
        //         if (item is Shield shield)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     {shield.Name}");
        //         }
        //         else if (item is Weapon weapon)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     {weapon.Name}");
        //         }
        //         else if (item is Armor armor)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     {armor.Name}");
        //         }
        //         else if (item is Item other)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     {other}");
        //         }
        //         else
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 5, (int)start.Y + 3 + i, $"{Nums[i]}.     --");
        //         }
        //     }
        //     
        //     if (_selected > -1)
        //     {
        //         _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 3 + _selected, $">");
        //         if (inventory.Items[_selected] is IEquippable _)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)end.Y - 1, "[E]QUIP");
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)end.Y - 1, "E", Color.Gold);
        //         }
        //
        //         if (inventory.Items[_selected] is not null)
        //         {
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 11, (int)end.Y - 1, "[D]ROP");
        //             _game.Layers["ascii"].Set((int)start.X * 2 + 12, (int)end.Y - 1, "D", Color.Gold);
        //         }
        //
        //         if (inventory.Items[_selected] is IItem item)
        //         {
        //             if (_game.ScreenStack.Any(i => i is CombatMapScreen))
        //             {
        //                 _game.Layers["ascii"].Set((int)start.X * 2 + 18, (int)end.Y - 1, "[T]HROW");
        //                 _game.Layers["ascii"].Set((int)start.X * 2 + 19, (int)end.Y - 1, "T", Color.Gold);
        //             }
        //
        //             if (item.CanBeUsed())
        //             {
        //                 _game.Layers["ascii"].Set((int)start.X * 2 + 26, (int)end.Y - 1, "[U]SE");
        //                 _game.Layers["ascii"].Set((int)start.X * 2 + 27, (int)end.Y - 1, "U", Color.Gold);
        //             }
        //         }
        //     }
        // }
    }
}