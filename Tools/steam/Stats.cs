using System.Collections.Generic;
using Newtonsoft.Json;
using SINEATER.Game.CoreUtils;
using Steamworks;

namespace SINEATER.steam
{
    internal abstract class ISteamStat
    {
        public ISteamStat(int id, string name)
        {
            RefreshValue();
            Name = name;
            ID = id;
        }
        public string Name { get; set; } = "UNKNOWN";
        public int ID { get; set; } = -1;

        abstract public void UpdateValue(int value = 1);
        abstract public void UpdateValue(float value = 1.0f);

        abstract public void RefreshValue();
    }

    internal class IntStat : ISteamStat
    {
        public IntStat(string name, int id, int value) 
            :base(id, name) 
        {
            Value = value;
        }

        [JsonIgnore]
        public int Value { get; set; } = 0;

        public override void RefreshValue()
        {
            SteamUserStats.GetStat(Name, out int value);
            Value = value;
        }
        public override void UpdateValue(int value = 1)
        {
            Value = value;
            SteamUserStats.SetStat(Name, value);
        }

        public override void UpdateValue(float value = 1.0f)
        {
            throw new System.NotImplementedException();
        }
    }

    internal class FloatStat : ISteamStat
    {
        public FloatStat(string name, int id, float value)
            : base(id, name)
        {
            Value = value;
        }
        [JsonIgnore]
        public float Value { get; set; } = 0;

        public override void RefreshValue()
        {
            SteamUserStats.GetStat(Name, out float value);
            Value = value;
        }

        public override void UpdateValue(int value)
        {
            throw new System.NotImplementedException();

        }

        public override void UpdateValue(float value)
        {
            Value = value;
            SteamUserStats.SetStat(Name, value);
        }
    }

    internal class AvgrateStat : FloatStat
    {
        public AvgrateStat(string name, int id, float value) : base(name, id, value)
        {
        }
    }

    // let's leave it there for testing purpose
    internal static class Stats
    {
        private static List<ISteamStat> _stats = new List<ISteamStat>
        {
            new IntStat("STAT_1", 1,0),
            new FloatStat("STAT_2", 2, 4),
            new AvgrateStat("STAT_3", 3, 5),
        };

        public static void Save()
        {
            DataSerializer.Serialize(_stats);
        }
    }
}
