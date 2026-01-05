using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace SINEATER;

public static class Enemies
{
    public static readonly Dictionary<string, Func<Enemy>> Library = [];

    private static string GetLocalBestiary()
    {
        return string.Join("\n", TitleContainer.OpenStream("Content/items/items.json").ReadLines(Encoding.Default));
    }

    private const int NAME = 0;
    private const int DISPLAY = 1;
    private const int ICON = 2;
    private const int PORTRAIT = 3;
    private const int GUARD = 4;
    private const int POISE = 5;
    private const int CLARITY = 6;
    private const int WILL = 7;
    private const int VIGOR = 8;
    private const int NIGHTSPEEDUP = 9;
    private const int DAYSPEEDUP = 10;
    private const int NIGHTGUARDUP = 11;
    private const int DAYGUARDUP = 12;
    private const int TAGS = 13;
    
    public static void LoadBestiary(ContentManager content)
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
        var enemies = res.Get(APPS_ID, $"Enemies!A1:T20").Execute();

        for (var i = 1; i < enemies.Values.Count; i++)
        {
            var name = enemies.Values[i][NAME].ToString() ?? "";
            var display = enemies.Values[i][DISPLAY].ToString() ?? "";
            var icon = (enemies.Values[i][ICON].ToString() ?? "0, 0").Split(",");
            var portrait = (enemies.Values[i][PORTRAIT].ToString() ?? "0, 0").Split(",");
            var guard = enemies.Values[i][GUARD].ToString() ?? "0";
            var poise = enemies.Values[i][POISE].ToString() ?? "0";
            var clarity = enemies.Values[i][CLARITY].ToString() ?? "0";
            var will = enemies.Values[i][WILL].ToString() ?? "0";
            var vigor = enemies.Values[i][VIGOR].ToString() ?? "0";
            var nightSpeedUp = enemies.Values[i][NIGHTSPEEDUP].ToString() ?? "0";
            var daySpeedUp = enemies.Values[i][DAYSPEEDUP].ToString() ?? "0";
            var nightGuardUp = enemies.Values[i][NIGHTGUARDUP].ToString() ?? "0";
            var dayGuardUp = enemies.Values[i][DAYGUARDUP].ToString() ?? "0";
            var readTags = enemies.Values[i].Count > 13 ? enemies.Values[i][TAGS].ToString() : "";
            var tags = (readTags == null ? [] : readTags.Split(",").ToList());
            
            Library.Remove(name);
            Library.Add(name, () => new Enemy
            {
                Tags = tags,
                Guard = int.Parse(guard),
                Stats = new Stats(int.Parse(will), int.Parse(clarity), int.Parse(poise), int.Parse(vigor)),
                Name = display,
                Icon = (int.Parse(icon[0].Trim()), int.Parse(icon[1].Trim())),
                Portrait = (int.Parse(portrait[0].Trim()), int.Parse(portrait[1].Trim())),
                NightSpeedUp = int.Parse(nightSpeedUp),
                DaySpeedUp = int.Parse(daySpeedUp),
                NightGuardUp = int.Parse(nightGuardUp),
                DayGuardUp = int.Parse(dayGuardUp),
            });
        }
    }
}
