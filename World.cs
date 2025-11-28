using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Xna.Framework;
using SINEATER.Serialization;

namespace SINEATER;

public interface IComponentStorage
{
    public bool Has(int x, int y);
}

public class ComponentStorage<T> : IComponentStorage where T: struct, IWorldComponent 
{
    public readonly Dictionary<int, T> InternalStorage = [];

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

    public bool IsOkay(int x, int y)
    {
        if (!Has(x, y)) return false;
        return Get(x, y).IsOkay();
    }
    
    public void Set(int x, int y, T t)
    {
        var index = y * 20 + x;
        InternalStorage[index] = t;
    }

    public void Remove(int x, int y)
    {
        var index = y * 20 + x;
        InternalStorage.Remove(index);
    }
}

public class World(string path)
{
    public string Path => path;
    public readonly ComponentStorage<GeneralDescription> GeneralDescriptions = new();
    public readonly ComponentStorage<SpecificDescription> SpecificDescriptions = new();
    public readonly ComponentStorage<Encounter> Encounters = new();
    public readonly ComponentStorage<SlowDown> SlowDowns = new();
    
    public bool AnythingOn(int x, int y)
    {
        if (GeneralDescriptions.Has(x, y)) return true;
        if (SpecificDescriptions.Has(x, y)) return true;
        if (Encounters.Has(x, y)) return true;
        if (SlowDowns.Has(x, y)) return true;
        return false;
    }

    public bool AnythingChanged(int x, int y)
    {
        return !(GeneralDescriptions.IsOkay(x, y) || 
            SpecificDescriptions.IsOkay(x, y) || 
            Encounters.IsOkay(x, y) ||
            SlowDowns.IsOkay(x, y));
    }
    
    public void Save()
    {
        DataSerializer.Serialize(this, out var json);
        var writePath =
            System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, Path);
            
        File.WriteAllText(writePath, json);
    }
    
    public static World LoadOrCreate(string path)
    {
        var se = string.Concat(string.Join("\n", TitleContainer.OpenStream("Content/sheets.nosj.txt").ReadLines(Encoding.Default)).Reverse());
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GoogleCredential
                .FromJson(se)
                .CreateScoped(SheetsService.Scope.Spreadsheets)
        });
        
        var res = new SpreadsheetsResource.ValuesResource(service);
        var key = Environment.UserName.ToUpper()[0];
        
        const string APPS_ID = "19faV45LV7ZQ1KdA-R6JbdCg7gy8JIx_FsJgKhZ-Clr0";
        var inspect = res.Get(APPS_ID, $"Inspect!A1:T20").Execute();
        var world = new World(path);
        
        for (var i = 0; i < 20; i++)
        {
            for (var j = 0; j < 20; j++)
            {
                var text = inspect.Values[j][i].ToString() ?? "";
                if (text.Length > 0 && text.Contains(" "))
                {
                    world.GeneralDescriptions.Add((i, j), new GeneralDescription(text));
                }
            }
        }

        return world;

        // World? world = null;
        // try
        // {
        //     var wrld = SineaterGame.Instance.Content.Load<string>("world");
        //     world = DataSerializer.Load<World>(wrld);
        // }
        // catch(Exception e)
        // {
        //     Console.WriteLine(e);
        //     world = null;
        // }
        //
        // if (world == null)
        // {
        //     world = new World(path);
        //     world.GeneralDescriptions.Add((2, 2), new GeneralDescription("blabla"));
        //     DataSerializer.Serialize(world, out var json);
        //     var writePath =
        //         System.IO.Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, path);
        //     
        //     File.WriteAllText(writePath, json);
        // }
        // return world;
    }
}