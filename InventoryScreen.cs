using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SINEATER.Content;

namespace SINEATER;

public class InventoryScreen : IScreen
{
    private SineaterGame _game;
    private bool _showEquipped = false;
    private int _selected = -1;

    public InventoryScreen(SineaterGame game)
    {
        _game = game;
    }
    
    public void Initialize(SineaterGame game)
    {}

    public void Update(GameTime gameTime)
    {
        if (KB.HasBeenPressed(Keys.Escape))
        {
            _game.ScreenStack.Pop();
        };

        if (KB.HasBeenPressed(Keys.Tab))
        {
            _showEquipped = !_showEquipped;
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
        if (newSelected != -1)
        {
            _selected = newSelected;
            if (_selected == oldSelected)
            {
                _selected = -1;
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

        if (_showEquipped)
        {
            _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "INVENTORY - EQUIPPED");
            int i = 0;
            foreach (var character in _game.Party.Characters)
            {
                var (u, v) = character.Job.GetImage();
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.LeftWeapon is { } lh)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   LH {lh.Name} (Attack: {lh.Attack}, Weight: {lh.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   LH --");
                }
                i++;
                
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.RightWeapon is { } rh)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   RH {rh.Name} (Attack: {rh.Attack}, Weight: {rh.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   RH --");
                }
                i++;
                
                _game.Layers["mrmo"].Set((int)start.X + 3, (int)start.Y + 3 + i, new Glyph(u, v, Color.Black, character.Tint));
                if (character.Armor is { } armor)
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   DF {armor.Name} (Guard: {armor.Guard}, Weight: {armor.Weight.ToString()})");
                }
                else
                {
                    _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}.   DF --");
                }
                i++;
            }

            if (_selected > -1)
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 2, (int)start.Y + 3 + _selected, $">");
            }
        }
        else
        {
            _game.Layers["ascii"].Set((int)start.X * 2 + 3, (int)start.Y + 1, "INVENTORY - BAG");
            for (int i = 0; i < 12; i++)
            {
                _game.Layers["ascii"].Set((int)start.X * 2 + 4, (int)start.Y + 3 + i, $"{Nums[i]}. ");
            }
        }
    }
}