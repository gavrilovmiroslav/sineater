using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using SINEATER.Game.CoreUtils;
using SINEATER.Game.Gameplay;
using YamlDotNet.Serialization;

namespace SINEATER.Game.LookNFeel;

public class Barks
{
    public static Barks Instance;
    public string[] Wizard;
    public string[] Witch;
    public string[] Knight;
    public string[] Monk;
    public string[] Sage;
    public string[] Priest;
    public string[] Thief;

    public Barks()
    {
        Instance = this;
    }

    public static Barks Load(ContentManager content)
    {
        var filePath = Path.Combine(content.RootDirectory, $"barks.yaml");
        using var stream = TitleContainer.OpenStream(filePath);
        var yaml = string.Join("\n", stream.ReadLines(Encoding.Default).ToList());
        var deser = new DeserializerBuilder().Build();
        return deser.Deserialize<Barks>(yaml);
    }

    public string[] this[ECharacterClass job]
    {
        get
        {
            switch (job)
            {
                case ECharacterClass.Wizard:
                    return Wizard;
                case ECharacterClass.Witch:
                    return Witch;
                case ECharacterClass.Knight:
                    return Knight;
                case ECharacterClass.Monk:
                    return Monk;
                case ECharacterClass.Sage:
                    return Sage;
                case ECharacterClass.Priest:
                    return Priest;
                case ECharacterClass.Thief:
                    return Thief;
                default:
                    throw new ArgumentOutOfRangeException(nameof(job), job, null);
            }
        }
    }
}