using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SINEATER;

public class InventoryScreen : Screen
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
    
    public InventoryScreen(SineaterGame game, bool showOutfitting = false) : base(game)
    {
        _game = game;
        _showOutfitting = showOutfitting;
    }
    
    public string[] Nums = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "A", "B", "C", "D", "E"];

    public override void Initialize(SineaterGame game)
    {
        
    }

    public override void Update(GameTime gameTime)
    {
        
    }

    public override void Draw(SpriteBatch batch, GameTime gameTime)
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