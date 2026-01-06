namespace SINEATER.Tools.SinMod;

public static class Muse
{
    public static void SetCombatMood()
    {
        SinMod.System.GetLabelledInstance("bgm")?.SetParam("BGMusicMood", 1);
    }
    
    public static void SetTravelMood()
    {
        SinMod.System.GetLabelledInstance("bgm")?.SetParam("BGMusicMood", 0);
    }
}