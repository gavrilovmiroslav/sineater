namespace SINEATER.Game.Gameplay;

public class Enemy : Character
{
    public string Name;
    public (int, int) Icon;
    public (int, int) Portrait;
    public int NightSpeedUp = 0;
    public int DaySpeedUp = 0;
    public int NightGuardUp = 0;
    public int DayGuardUp = 0;
    
    public (int, int) GetIcon(bool selected = false)
    {
        var (x, y) = Icon;
        return (x, y + (selected ? -4 : 0));
    }
    
    public override string GetName()
    {
        return Name;
    }

    public override (int, int) GetPortait()
    {
        return Portrait;
    }
}