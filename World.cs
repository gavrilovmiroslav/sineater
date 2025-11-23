using System.Collections.Generic;

namespace SINEATER;

public class ComponentStorage<T>
{
    private int _magicNumber = 20;

    public Dictionary<int, T> InternalStorage = [];

    public void Add((int X, int Y) key, T value)
    {
        InternalStorage[key.Y * _magicNumber + key.X] = value;
    }
}

public class World
{
    public ComponentStorage<Introduction> Introduction = new();
    public ComponentStorage<Encounter> Encounters = new();
}