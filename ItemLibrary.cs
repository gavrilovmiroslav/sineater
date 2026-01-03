using Microsoft.Xna.Framework.Content;
using SINEATER.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
}

public static class ItemLibrary
{
    public static readonly (int, int) EmptyUv = (0, 9);
    private static Library Library { get; set; } = new();
    public static readonly MultiDictionary<string, Weapon> InstancedWeapons = new(false);
    public static readonly MultiDictionary<string, Item> InstancedItems = new(false);

    private static string GetLocalItems()
    {
        return string.Join("\n", TitleContainer.OpenStream("Content/items/items.json").ReadLines(Encoding.Default));
    }

    private const string APPS_ID = "1kzTUrcQpxx3vMJMXeVwM_ElqcgJGzOqexxmldqwrszk";

    private const string APPS_SCRIPT =
        "https://script.google.com/macros/s/AKfycbyOeruc6huyHDUcsvNJf9YymClUOLZ6HspG0QOoaCs3c4NzakqnxIFHPaxcUdYdqx3chA/exec";

    public static void LoadItems(ContentManager content)
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
        var v = res.Get(APPS_ID, $"GENERIC!R3").Execute();
        if (v.Values != null) {
            Console.WriteLine("LOADING NEW CONTENT FROM ONLINE: " + $"GENERIC!R3");
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
            res.Clear(new ClearValuesRequest(), APPS_ID, $"GENERIC!R3").Execute();
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
        
        foreach (var (k, w) in InstancedItems)
        {
            var original = Library.Items.Find(w => w.Name == k);
            if (original == null) continue;
            foreach (var instance in w)
            {
                instance?.Copy(original);
            }
        }

        return;

        async Task<string> Get(string uri)
        {
            var handler = new HttpClientHandler 
            { 
                AutomaticDecompression = DecompressionMethods.All 
            };
        
            var client = new HttpClient(handler);
            using var response = await client.GetAsync(uri);
            return await response.Content.ReadAsStringAsync();
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

        var dummy = Weapon.Dummy(name);
        InstancedWeapons.Add(name, dummy);
        return dummy;
    }

    public static Weapon? GetItem(string name)
    {
        return GetWeapon(name);
    }

    internal static Inventory CreateDefaultInventory()
    {
        var inventory = new Inventory();
        foreach(var weapon in Library.Weapons)
        {
            //inventory.Items.Add(GetWeapon(weapon.Name));
        }

        inventory.Items.Sort((x, y) => x.Stat.CompareTo(y.Stat));

        return inventory;
    }
}