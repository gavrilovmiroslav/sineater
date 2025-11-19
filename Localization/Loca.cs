using SINEATER.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

namespace SINEATER.Localization;

internal static class Loca
{
    private static Dictionary<LocaIDs, string> _localizedStrings = new();

    public static string GetString(LocaIDs locaIDs) => _localizedStrings[locaIDs];
    public static void Load(string json)
    {
        _localizedStrings.Add(LocaIDs.Weapon_Name_Dagger, "Dagger");
        DataSerializer.Serialize(_localizedStrings);
    }

    public static void GenerateLocaFile(string name)
    {
        Dictionary<LocaIDs, string> pairs = new Dictionary<LocaIDs, string>();
        foreach (var kvp in Enum.GetValues(typeof(LocaIDs)))
        {
            pairs.Add((LocaIDs)kvp, "");
        }

        bool ignoreTypes = true;
        DataSerializer.Serialize(pairs, ignoreTypes);
    }

    public static void RegenerateFiles()
    {
        var locaFiles = Directory.EnumerateFiles("Content\\Loca");
        foreach (var locaFile in locaFiles)
        {
            Dictionary<LocaIDs, string> pairs = new Dictionary<LocaIDs, string>();
            pairs = DataSerializer.Load<Dictionary<LocaIDs, string>>(File.ReadAllText(locaFile));
            foreach (var kvp in Enum.GetValues(typeof(LocaIDs)))
            {
                if (!_localizedStrings.ContainsKey((LocaIDs)kvp))
                {
                    _localizedStrings.Add((LocaIDs)kvp, "");
                }

            }
        }
    }

}
