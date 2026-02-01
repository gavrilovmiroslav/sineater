namespace LDtkTypes;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class NPC : ILDtkEntity
{
    public static NPC Default() => new()
    {
        Identifier = "NPC",
        Uid = 34,
        Size = new Vector2(16f, 16f),
        Pivot = new Vector2(0f, 0f),
        Tile = new TilesetRectangle()
        {
            X = 176,
            Y = 16,
            W = 16,
            H = 16
        },
        SmartColor = new Color(46, 166, 43, 255),

    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public string[]? Name { get; set; }
}
#pragma warning restore
