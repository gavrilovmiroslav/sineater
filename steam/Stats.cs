using Steamworks;
using System.Collections.Generic;

namespace SINEATER.steam
{
    internal interface ISteamStat
    {
        public string Name { get; set; }
        public int ID { get; set; }

        public void RefreshValue();
    }

    internal class IntStat : ISteamStat
    {
        public string Name { get; set; } = "";
        public int ID { get; set; } = -1;
        public int Value { get; set; } = 0;

        public void RefreshValue()
        {
            SteamUserStats.GetStat(Name, out int value);
            Value = value;
        }
    }

    internal class FloatStat : ISteamStat
    {
        public string Name { get; set; } = "";
        public int ID { get; set; } = -1;
        public float Value { get; set; } = 0;

        public void RefreshValue()
        {
            SteamUserStats.GetStat(Name, out float value);
            Value = value;
        }
    }

    internal class AvgrateStat : FloatStat { }

    internal static class Stats
    {
        private static List<ISteamStat> _stats = new List<ISteamStat>
        {
            new IntStat
            {
                ID = 0,
                Name = "STAT_1"
            }
        };
    }
}
