using Microsoft.Xna.Framework.Content;
using SINEATER.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Microsoft.Xna.Framework;
using Wintellect.PowerCollections;

namespace SINEATER;
public class ItemNotFoundException : Exception
{
    public ItemNotFoundException(string name) : base(String.Format("Item with name {0} not found in library", name)) { }
}

public class Library
{
    public List<Weapon> Weapons { get; set; } = new();
    public List<Item> Items { get; set; } = new();
    public List<Armor> Armors { get; set; } = new();
    public List<Shield> Shields { get; set; } = new();
}

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    public static Library Library { get; set; } = new();
    public static MultiDictionary<string, Weapon> InstancedWeapons = new(false);
    public static MultiDictionary<string, Armor> InstancedArmors = new(false);
    public static MultiDictionary<string, Shield> InstancedShields = new(false);
    public static MultiDictionary<string, Item> InstancedItems = new(false);

    private static string GetLocalItems()
    {
        return string.Join("\n", TitleContainer.OpenStream("Content/items/items.json").ReadLines(Encoding.Default));
    }

    internal const string APPS_ID = "1kzTUrcQpxx3vMJMXeVwM_ElqcgJGzOqexxmldqwrszk";
    internal const string APPS_SCRIPT =
        "https://script.googleusercontent.com/macros/echo?user_content_key=AehSKLhC0M9wjoH2fpeSBw5-IWnMl8iP6Ph155WKLio9f8u6P3Ma9g1K_TuHto4N9CGHLD8-FjO_3DoQ20xpgaigVAtwfePtBxismGK2S6XRwr-em3MnzvgeGj1fNfS3PWACrSlfL1mFJ_mvd2WY0HdIXrFoVzDPYIka1rnrINvpDfn1erU4k8WfkuHhovWEfMp-00-i7Cy9oqiKSWKt_rJi2GIwQncYl_qNjesLjDepCsFQmFKopnV23v0L_iTnHsayZU9lWeHeCpGVC2o7TjkoksJ557Z1EnSiSXM5NEXV&lib=M1tQVk_yTk_xVNLpFcyFKEoxl_Pbn3PYE";

    public static void LoadItems(ContentManager content)
    {
        async Task<string> Get(string uri)
        {
            HttpClientHandler handler = new HttpClientHandler 
            { 
                AutomaticDecompression = DecompressionMethods.All 
            };
        
            var client = new HttpClient(handler);
            using HttpResponseMessage response = await client.GetAsync(uri);
            return await response.Content.ReadAsStringAsync();
        }

        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GoogleCredential
                .FromStream(TitleContainer.OpenStream("Content/hellth-415523-d1e97f600491.json"))
                .CreateScoped(SheetsService.Scope.Spreadsheets)
        });
        
        var res = new SpreadsheetsResource.ValuesResource(service);
        var v = res.Get(APPS_ID, "GENERIC!C40").Execute();
        if (v.Values != null) {
            Console.WriteLine("LOADING NEW CONTENT FROM ONLINE");
            var t = Task.Run(async () =>
            {
                var json = await Get(APPS_SCRIPT);
                try
                {
                    DataSerializer.Load<Library>(json);
                }
                catch(Exception e)
                {
                    Console.WriteLine("COULDN'T LOAD NEW ONLINE CONTENT - FAILED TO BUILD LIBRARY");
                    Console.WriteLine(e.Message);
                    return;
                }
                
                var dir = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
                File.WriteAllLines($"{dir}/Content/items/items.json", [ json ]);
                File.WriteAllLines("Content/items/items.json", [ json ]);
                Library = DataSerializer.Load<Library>(json);
            });
            
            t.Wait();
            Console.WriteLine("NEW JSON LOADED FROM ONLINE");
            res.Clear(new ClearValuesRequest(), APPS_ID, "GENERIC!C40").Execute();
        }
        else
        {
            Library = DataSerializer.Load<Library>(GetLocalItems());
        }

        foreach (var (k, w) in InstancedWeapons)
        {
            var original = Library?.Weapons.Find(w => w.Name == k);
            if (original == null) continue;
            foreach (var instance in w)
            {
                instance?.Copy(original);
            }
        }
        
        foreach (var (k, w) in InstancedShields)
        {
            var original = Library?.Shields.Find(w => w.Name == k);
            if (original == null) continue;
            foreach (var instance in w)
            {
                instance?.Copy(original);
            }
        }
        
        foreach (var (k, w) in InstancedArmors)
        {
            var original = Library?.Armors.Find(w => w.Name == k);
            if (original == null) continue;
            foreach (var instance in w)
            {
                instance?.Copy(original);
            }
        }
        
        foreach (var (k, w) in InstancedItems)
        {
            var original = Library.Items.Find(w => w.Name == k);
            if (original == null) continue;
            foreach (var instance in w)
            {
                instance?.Copy(original);
            }
        }
    }

    public static Weapon? GetWeapon(string name)
    {
        if (Library.Weapons.Find(x => x.Name == name) is {} result)
        {
            var item = (Weapon)result.Clone();
            InstancedWeapons.Add(result.GetName(), item);
            return item;
        }
        throw new ItemNotFoundException(name);
    }

    public static Armor? GetArmor(string name)
    {
        if (Library.Armors.Find(x => x.Name == name) is {} result)
        {
            var item = (Armor)result.Clone();
            InstancedArmors.Add(result.GetName(), item);
            return item;
        }
        throw new ItemNotFoundException(name);
    }

    public static Shield? GetShield(string name)
    {
        if (Library.Shields.Find(x => x.Name == name) is {} result)
        {
            var item = (Shield)result.Clone();
            InstancedShields.Add(result.GetName(), item);
            return item;
        }
        throw new ItemNotFoundException(name);
    }

    public static Item? GetItem(string name)
    {
        if (Library.Items.Find(x => x.Name == name) is {} result)
        {
            var item = (Item)result.Clone();
            InstancedItems.Add(result.GetName(), item);
            return item;
        }
        throw new ItemNotFoundException(name);
    }
}