using System.IO;
using SINEATER.Game.CoreUtils;
using SINEATER.Tools.SinMod;

namespace SINEATER.Game.Loadable;

public class Options
{
    public int MasterVolume = 5;
    public int SfxVolume = 5;
    public int MusicVolume = 5;

    public void UpdateOptions()
    {
        SINEATER.Tools.SinMod.System.GetLabelledInstance("bgm")?.SetVolume((float)MasterVolume / 10.0f, true);
    }
    
    public void Save()
    {
        UpdateOptions();
        
        DataSerializer.Serialize(this, out var json);
        const string writePath = "options.json";
        File.WriteAllText(writePath, json);
    }
}