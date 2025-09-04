using System;

namespace SINEATER;

public record struct Stats
{
    public int Will;
    public int Clarity;
    public int Poise;
    public int Vigor;
    
    public int Score => Will + Clarity + Poise + Vigor;

    public Stats()
    {
        var bag = Rnd.Instance.Bag((i => i >= 2), 4, 6, 6, 8);
        
        Will = bag[0];
        Clarity = bag[1];
        Poise = bag[2];
        Vigor = bag[3];
    }
}

public record struct Character
{
    public Stats Stats = new();

    public Character()
    {
        Console.WriteLine($"Created character with {Stats}");
    }
}

public record struct Party
{
    public Character[] Characters = new Character[4];

    public Party()
    {
        for (var i = 0; i < 4; i++) Characters[i] = new Character();
    }
}