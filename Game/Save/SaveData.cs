using SINEATER.Game.CoreUtils;
using System.IO;

namespace SINEATER.Game.Save
{
    public static class SaveSystem
    {
        public static void Save()
        {
            DataSerializer.Serialize(SineaterGame.Instance.Party, out string json);

            if (!Directory.Exists("Save"))
                Directory.CreateDirectory("Save");

            File.WriteAllText("save//save.sav", json);
        }

        public static void Load(string path)
        {

        }
    }

    public class SaveData
    {

    }
}
