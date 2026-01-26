namespace LDtkTypes;

// This file was automatically generated, any modifications will be lost!
#pragma warning disable

using LDtk;
using Microsoft.Xna.Framework;

public partial class Exit : ILDtkEntity
{
    public static Exit Default() => new()
    {
        Identifier = "Exit",
        Uid = 17,
        Size = new Vector2(16f, 16f),
        Pivot = new Vector2(0f, 0f),
        SmartColor = new Color(190, 74, 47, 255),

    };

    public string Identifier { get; set; }
    public System.Guid Iid { get; set; }
    public int Uid { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Pivot { get; set; }
    public Rectangle Tile { get; set; }

    public Color SmartColor { get; set; }

    public EntityReference? Endpoint { get; set; }
    public MoveBehavior? Kind { get; set; }
}
#pragma warning restore
