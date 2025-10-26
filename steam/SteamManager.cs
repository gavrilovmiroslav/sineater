using Steamworks;
using System;

namespace SINEATER.steam
{
    internal class SteamManager
    {
        public void Initialize()
        {
            try
            {
                var result = SteamAPI.InitEx(out var error);
                if (result != ESteamAPIInitResult.k_ESteamAPIInitResult_OK)
                {
                    Console.WriteLine($"Failed to init steam API. Error: {result.ToString()}\n - {error}");
                    return;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }

            m_UserStatsRecieved = CallResult<UserStatsReceived_t>.Create(OnUserStatsRecieved);
            m_UserStatsStored = Callback<UserStatsStored_t>.Create(OnUserStatsStored);

            SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);

            RequestStats();
        }

        public void Update()
        {
            if (SteamAPI.IsSteamRunning())
            {
                SteamAPI.RunCallbacks();
            }
        }

        public void ShutDown()
        {
            SteamAPI.Shutdown();
        }

        private void RequestStats()
        {
            var callbackHandle = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            m_UserStatsRecieved.Set(callbackHandle);
        }

        #region Callbacks

        private CallResult<UserStatsReceived_t> m_UserStatsRecieved;
        private Callback<UserStatsStored_t> m_UserStatsStored;

        private void OnUserStatsRecieved(UserStatsReceived_t pCall, bool bIOFailure)
        {
            int x = 0;
        }

        private void OnUserStatsStored(UserStatsStored_t param)
        {

        }
        #endregion
    }
}


/*
 * 
 *                     //if(SteamUserStats.SetAchievement("ACH_TEST"))
                    {
                        int xs = 0;
                    }
                    init = true;

                    SteamUserStats.ClearAchievement("ACH_TEST");
 */