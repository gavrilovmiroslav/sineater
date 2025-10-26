using System.Collections.Generic;

namespace SINEATER.steam
{
    // enum names should match to those setup on achievement page
    internal enum EAchievement : uint
    {
        ACH_TEST
    }
    internal class Achievement
    {
        public EAchievement AchievementID { get; set; }
        public bool IsDone { get; set; } = false;
    }

    internal class AchievementsLibrary
    {
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
        public bool IsInitialized { get; set; } = false;
    }
}
