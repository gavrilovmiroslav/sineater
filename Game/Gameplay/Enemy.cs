namespace SINEATER.Game.Gameplay;

public class Enemy : Character
{
    public string Name;
    public string Display;
    public int NightSpeedUp = 0;
    public int DaySpeedUp = 0;
    public int NightGuardUp = 0;
    public int DayGuardUp = 0;
    
    public override string GetName()
    {
        return Name;
    }
    
    public string GetDisplayName()
    {
        return Display;
    }
}