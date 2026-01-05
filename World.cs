using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    public readonly HashSet<int> Visited = [];
    public readonly Dictionary<int, T> InternalStorage = [];
    
    public void Visit(int x, int y)
    {
        Visited.Add(y * 20 + x);
    }
    
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

    public bool IsVisited(int x, int y)
    {
        return Visited.Contains(y * 20 + x);
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
    public readonly ComponentStorage<Reward> Rewards = new();
    public readonly ComponentStorage<SlowDown> SlowDowns = new();
    
    public bool AnythingOn(int x, int y)
    {
        if (GeneralDescriptions.Has(x, y)) return true;
        if (SpecificDescriptions.Has(x, y)) return true;
        if (Encounters.Has(x, y)) return true;
        if (Rewards.Has(x, y)) return true;
        if (SlowDowns.Has(x, y)) return true;
        return false;
    }

    public bool AnythingChanged(int x, int y)
    {
        return !(GeneralDescriptions.IsOkay(x, y) || 
            SpecificDescriptions.IsOkay(x, y) || 
            Encounters.IsOkay(x, y) ||
            Rewards.IsOkay(x, y) ||
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
        var slowdown = res.Get(APPS_ID, $"Time!A1:T20").Execute();
        var combats = res.Get(APPS_ID, $"Combat!A1:T20").Execute();
        var rewards = res.Get(APPS_ID, $"Rewards!A1:T20").Execute();
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

                if (int.TryParse(slowdown.Values[j][i].ToString() ?? "/", out var time))
                {
                    world.SlowDowns.Add((i, j), new SlowDown(time, time > 3 ? time - 3 : 0));
                }

                // COMBAT
                {
                    var matches = Regex.Matches(combats.Values[j][i].ToString() ?? "/",
                        @"((\w+)\[([a-zA-Z, ]*)\]\s*)+");

                    if (matches.Count > 0)
                    {
                        List<Enemy> enemies = new List<Enemy>();
                        foreach (Match match in matches)
                        {
                            var enemyType = match.Groups[2].ToString();
                            var enemy = Enemies.Library[enemyType]();

                            foreach (var weapon in match.Groups[3].ToString().Split(','))
                            {
                                if (weapon.Trim() == "")
                                    continue;

                                var w = ItemLibrary.GetWeapon(weapon.Trim());
                                if (w != null)
                                {
                                    enemy.Equip(w);
                                }
                            }

                            enemies.Add(enemy);
                        }

                        if (enemies.Count > 0)
                        {
                            world.Encounters.Add((i, j), new Encounter(enemies));
                        }
                    }
                }

                // REWARDS
                {
                    var matches = Regex.Matches(rewards.Values[j][i].ToString() ?? "/",
                        @"((\w+)\[([a-zA-Z,]*)\]\s*)+");

                    if (matches.Count > 0)
                    {
                        var rewardList = new List<(int, List<Item>)>();
                        foreach (Match match in matches)
                        {
                            var timeLimit = int.Parse(match.Groups[2].ToString());

                            var items = new List<Item>();
                            foreach (var weapon in match.Groups[3].ToString().Split(','))
                            {
                                if (weapon.Trim() == "")
                                    continue;

                                var w = ItemLibrary.GetWeapon(weapon.Trim());
                                if (w != null)
                                {
                                    items.Add(w);
                                }
                            }

                            rewardList.Add((timeLimit, items));
                        }

                        if (rewardList.Count > 0)
                        {
                            world.Rewards.Add((i, j), new Reward(rewardList));
                        }
                    }
                }
            }
        }

        return world;
    }
}