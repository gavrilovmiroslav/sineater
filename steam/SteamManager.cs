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
                    Console.WriteLine($"Failed to init steam API. Error: {result.ToString()}");
                }
            }
            catch (Exception e)
            {
                // We check this here as it will be the first instance of it
                Console.WriteLine(e);
            }

            Callback<GameOverlayActivated_t>.Create(OnGameOverlay);
            SteamUtils.SetOverlayNotificationPosition(ENotificationPosition.k_EPositionTopRight);
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

        private  void OnGameOverlay(GameOverlayActivated_t pCall)
        {
            int x = 0;
        }
    }
}
