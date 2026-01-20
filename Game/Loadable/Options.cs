using System.IO;
using SINEATER.Game.CoreUtils;

namespace SINEATER.Game.Loadable;

public class Options
{
    public int MasterVolume = 5;
    public int SfxVolume = 5;
    public int MusicVolume = 5;

    public void Save()
    {
        DataSerializer.Serialize(this, out var json);
        const string writePath = "options.json";
        File.WriteAllText(writePath, json);
    }
}