using SINEATER.Game.CoreUtils;
using System;
using System.IO;

namespace SINEATER.Game.Save
{
    public static class SaveSystem
    {
        public static EventHandler<SaveData>? OnSaveLoaded = delegate { };
        public static void Save()
        {
            var saveData = new SaveData();
            saveData.Save();

            DataSerializer.Serialize(saveData, out string json);

            if (!Directory.Exists("save"))
                Directory.CreateDirectory("save");

            File.WriteAllText("save//save.sav", json);
        }

        public static SaveData? Load()
        {
            string path = "save//save.sav";
            if (File.Exists("save//save.sav"))
            {
                string json = File.ReadAllText(path);
                var saveData = DataSerializer.Load<SaveData>(json);

                if (saveData != null)
                    OnSaveLoaded?.Invoke(null, saveData);

                return saveData;
            }

            return null;
        }

        public static bool HasSave()
        {
            return File.Exists("save//save.sav");
        }

        public static string[] TempSaveFiles = new string[] { };

        public static void GatherTempSaves()
        {
            if (Directory.Exists("temp_save"))
            {
                TempSaveFiles = Directory.GetFiles("temp_save");
            }
        }
    }
}
