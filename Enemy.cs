namespace SINEATER;

public record struct Enemy
{
    public Stats Stats = new();
    public Weapon? LeftWeapon = null;
    public Weapon? RightWeapon = null;

    public Enemy()
    {
        
    }
}