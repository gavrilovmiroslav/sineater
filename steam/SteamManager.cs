using SINEATER.Serialization;
using Steamworks;
using System;
using System.Collections.Generic;

namespace SINEATER.steam
{
    internal class SteamManager
    {
        public static SteamManager Instance = new SteamManager();

        public bool IsConnectedToSteam = false;

        public List<ISteamStat> Stats = new List<ISteamStat>();
        public void Initialize(string statsJson)
        {
            try
            {
                var result = SteamAPI.InitEx(out var error);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Console.WriteLine($"Failed to init steam API. Error: {result.ToString()}\n - {error}");
                    return;
                }

                IsConnectedToSteam = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            m_UserStatsRecieved = CallResult<UserStatsReceived_t>.Create(OnUserStatsRecieved);
            m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
            m_GlobalStatsRecieved = CallResult<GlobalStatsReceived_t>.Create(OnGlobalStatsRecieved);

            SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);

            Stats = DataSerializer.Load<List<ISteamStat>>(statsJson);

            RequestStats();
        }

        public void Update()
        {
            if (IsConnectedToSteam)
            {
                SteamAPI.RunCallbacks();
            }
        }

        public void ShutDown()
        {
            SteamAPI.Shutdown();
        }

        public void SetAchievement(string achievementName, bool condition = true)
        {
            SteamUserStats.GetAchievement(achievementName, out bool completed);
            if (!completed && condition)
            {
                if (SteamUserStats.SetAchievement(achievementName))
                {
                    SteamUserStats.StoreStats();
                }
                else
                {
                    Console.WriteLine($"Failed to activate achievement: {achievementName}");
                }
            }
        }

        public bool UpdateIntStat(string name, int delta)
        {
            var stat = Stats.Find(x => x.Name == name);
            if (stat != null)
            {
                stat.UpdateValue(delta);
                SteamUserStats.StoreStats();
                return true;
            }
            return false;
        }

        public bool UpdateFloatStat(string name, float delta)
        {
            var stat = Stats.Find(x => x.Name == name);
            if (stat != null)
            {
                stat.UpdateValue(delta);
                SteamUserStats.StoreStats();
                return true;
            }
            return false;
        }

        public void GetStatValue(string name, out int value)
        {
            var stat = Stats.Find(x => x.Name == name);
            if (stat != null)
            {
                value = stat is IntStat s ? s.Value : 0;
            }
            else
            {
                value = -1;
            }
        }

        public void GetStatValue(string name, out float value)
        {
            var stat = Stats.Find(x => x.Name == name);
            if (stat != null && stat is FloatStat floatStat)
            {
                value = floatStat.Value;
            }
            else
            {
                value = -1;
            }
        }

        private void RequestStats()
        {
            var callbackHandle = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            m_UserStatsRecieved.Set(callbackHandle);

            // Not sure we want this in game, but it's good to have it as demo
            RequestGlobalStats();
        }

        private void RequestGlobalStats()
        {
            var callbackHandle = SteamUserStats.RequestGlobalStats(3);
            m_GlobalStatsRecieved.Set(callbackHandle);
        }

        #region Callbacks

        private CallResult<UserStatsReceived_t> m_UserStatsRecieved = new();
        private Callback<UserStatsStored_t> m_UserStatsStored;
        private CallResult<GlobalStatsReceived_t> m_GlobalStatsRecieved = new();

        private void OnUserStatsRecieved(UserStatsReceived_t pCall, bool bIOFailure)
        {
            foreach (var stat in Stats)
            {
                stat.RefreshValue();
            }
        }
        private void OnUserStatsStored(UserStatsStored_t param)
        {

        }
        private void OnGlobalStatsRecieved(GlobalStatsReceived_t pCall, bool bIOFailure)
        {
            SteamUserStats.GetGlobalStat("STAT_1", out long data);
        }
        #endregion
    }
}