using System.Collections.Generic;

namespace SINEATER;

public class ComponentStorage<T>
{
    public Dictionary<(int X, int Y), T> InternalStorage = [];

    public void Add((int X, int Y) key, T value)
    {
        InternalStorage[key] = value;
    }
}

public class World
{
    public ComponentStorage<Introduction> Introduction = new();
    public ComponentStorage<Encounter> Encounters = new();
}