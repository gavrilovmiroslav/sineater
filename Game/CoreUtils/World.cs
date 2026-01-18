using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Arch.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Xna.Framework;
using SINEATER.Game.Gameplay;
using SINEATER.Game.Loadable;

namespace SINEATER.Game.CoreUtils;

public class World
{
    public Arch.Core.World ECS { get; }
    private readonly Dictionary<(int X, int Y), Entity> _entitiesOnMaps = [];
    private readonly Dictionary<Entity, (int X, int Y)> _positionByEntity = [];

    public Entity Get(int x, int y) => _entitiesOnMaps[(x, y)];
    public (int X, int Y) Get(Entity e) => _positionByEntity[e];
    
    public string Path { get; }

    public World(string path)
    {
        Path = path;
        ECS = Arch.Core.World.Create();
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
        var rex = SineaterGame.Instance.Rex;
        
        var se = string.Concat(string.Join("\n", TitleContainer.OpenStream("Content/sheets.nosj.txt").ReadLines(Encoding.Default)).Reverse());
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GoogleCredential
                .FromJson(se)
                .CreateScoped(SheetsService.Scope.Spreadsheets)
        });
        
        var res = new SpreadsheetsResource.ValuesResource(service);
        
        const string APPS_ID = "19faV45LV7ZQ1KdA-R6JbdCg7gy8JIx_FsJgKhZ-Clr0";
        var inspect = res.Get(APPS_ID, $"Inspect!A1:T20").Execute();
        var combats = res.Get(APPS_ID, $"Combat!A1:T20").Execute();
        var rewards = res.Get(APPS_ID, $"Rewards!A1:T20").Execute();
        
        var world = new World(path);
        
        for (var i = 0; i < 20; i++)
        {
            for (var j = 0; j < 20; j++)
            {
                var tile = world.ECS.Create();
                world.ECS.Add(tile, new WorldMapTile(rex.Layers[1][i, j]));
                world.ECS.Add(tile, new Position(i, j));
                world._entitiesOnMaps.Add((i, j), tile);
                world._positionByEntity.Add(tile, (i, j));
                
                var text = inspect.Values[j][i].ToString() ?? "";
                if (text.Length > 0 && text.Contains(' '))
                {
                    world.ECS.Add(tile, new Dialogue([], text)); // TODO: tags
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
                            var enemy = Enemies.Instance.Make(enemyType);

                            foreach (var weapon in match.Groups[3].ToString().Split(','))
                            {
                                if (weapon.Trim() == "")
                                    continue;

                                var w = Items.Instance.Make(weapon.Trim());
                                enemy.Equip(w);
                            }

                            enemies.Add(enemy);
                        }

                        if (enemies.Count > 0)
                        {
                            world.ECS.Add(tile, new Gameplay.Encounter(enemies));
                        }
                    }
                }

                // REWARDS
                {
                    var matches = Regex.Matches(rewards.Values[j][i].ToString() ?? "/",
                        @"((\w+)\[([a-zA-Z, ]*)\]\s*)+");

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

                                var w = Items.Instance.Make(weapon.Trim());
                                items.Add(w);
                            }

                            rewardList.Add((timeLimit, items));
                        }

                        if (rewardList.Count > 0)
                        {
                            world.ECS.Add(tile, new Gameplay.Reward(rewardList));
                        }
                    }
                }
            }
        }

        return world;
    }
}