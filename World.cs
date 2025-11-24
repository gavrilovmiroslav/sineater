using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using SINEATER.Serialization;

namespace SINEATER;

public class ComponentStorage<T> where T : struct
{
    private readonly Dictionary<int, T> InternalStorage = [];

    public void Add((int X, int Y) key, T value)
    {
        InternalStorage[key.Y * 20 + key.X] = value;
    }

    public T? Get((int X, int Y) key)
    {
        return Get(key.X, key.Y);
    }
    
    public T Get(int x, int y)
    {
        var index = y * 20 + x;
        if (InternalStorage.ContainsKey(index))
        {
            return InternalStorage[index];
        }
        else
        {
            throw new Exception("Cannot get value");
        }
    }

    public bool Has(int x, int y)
    {
        var index = y * 20 + x;
        return InternalStorage.ContainsKey(index);
    }

    public void Set(int x, int y, T t)
    {
        var index = y * 20 + x;
        InternalStorage[index] = t;
    }
}

public class World
{
    public ComponentStorage<Introduction> Introduction = new();
    public ComponentStorage<Encounter> Encounters = new();
    
    public static World LoadOrCreate(string path)
    {
        using var stream = TitleContainer.OpenStream(path);
        using var reader = new StreamReader(stream);

        World? world = null;
        try
        {
            world = DataSerializer.Load<World>(reader.ReadToEnd());
        }
        catch
        {
            world = null;
        }

        if (world == null)
        {
            world = new World();
            DataSerializer.Serialize(world, out var json);
            var writePath =
                System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, path);
            
            File.WriteAllText(writePath, json);
        }

        return world;
    }
}