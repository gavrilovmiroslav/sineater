using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace SINEATER.Game.CoreUtils;

public class LoadableLibraryDefinition<T> where T: ILoadableDefinition
{
    [JsonProperty] public string Hash = "";
    [JsonProperty] public List<(string, T)> Entries = [];
}

public interface ILoadableDefinition
{
    public string Key { get; }
}

public interface ILoadableRowParser<out T> where T : ILoadableDefinition
{
    public T Parse(IList<object> row);
}

public interface ILoadableInterpreter<TDefinition, TResult>
{
    public TResult MakeFrom(TDefinition? def);
}

public abstract class LoadableLibrary<TDefinition, TParser, TInterpreter, TResult> 
    where TDefinition : class, ILoadableDefinition
    where TParser: ILoadableRowParser<TDefinition>, new()
    where TInterpreter: ILoadableInterpreter<TDefinition, TResult>, new()
{
    private const string KEY_ID = "Content/sheets.nosj.txt";
    private const string APPS_ID = "19faV45LV7ZQ1KdA-R6JbdCg7gy8JIx_FsJgKhZ-Clr0";
    
    protected abstract string Sheet { get; }
    protected abstract string DataRange { get; }
    protected abstract string JsonPath { get; }
    
    private readonly Dictionary<string, TDefinition> _library = [];

    private TInterpreter interp = new();

    public List<string> EnumerateItems()
    {
        var result = new List<string>();
        foreach(var item in  _library.Keys)
        {
            result.Add(item);
        }
        return result;
    }

    public bool Has(string key)
    {
        return _library.ContainsKey(key);
    }

    public TResult Make(string key)
    {
        if (_library.ContainsKey(key))
            return interp.MakeFrom(_library[key]);
        else
            return interp.MakeFrom(null);
    }
    
    public TResult Make(TDefinition? def)
    {
        return interp.MakeFrom(def);
    }

    private string GetHash(IList<IList<object?>?>? omg)
    {
        var hash = "";
        if (omg is { } a && a[0] is { } b && b[0] is {} c)
        {
            hash = c.ToString();
        }

        return hash;
    }
    
    public void Load()
    {
        _library.Clear();

        // using parent parent with exception will bug ship build, since menu never changes states!
        // FIX ME
        var dir = Environment.CurrentDirectory;
        if (dir is null)
        {
            throw new Exception("CONTENT FOLDER MISSING!");
        }

        var local = JsonConvert.DeserializeObject<LoadableLibraryDefinition<TDefinition>>(
            File.ReadAllText($"{dir}/Content/{JsonPath}"));
        
        foreach (var e in local?.Entries ?? [])
        {
            _library.Add(e.Item1, e.Item2);
        }
        
        var se = string.Concat(string.Join("\n", TitleContainer.OpenStream(KEY_ID).ReadLines(Encoding.Default)).Reverse());
        var service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = GoogleCredential
                .FromJson(se)
                .CreateScoped(SheetsService.Scope.Spreadsheets)
        });
        
        var res = new SpreadsheetsResource.ValuesResource(service);
        var hash = GetHash(res.Get(APPS_ID, $"{Sheet}!Z1").Execute().Values);

        if (hash != (local?.Hash ?? ""))
        {
            Console.WriteLine("Hashes not matching, loading from net!");
            _library.Clear();
            var sheet = res.Get(APPS_ID, $"{Sheet}!{DataRange}").Execute();

            var parser = new TParser();
            for (var i = 1; i < sheet.Values.Count; i++)
            {
                var def = parser.Parse(sheet.Values[i]);
                _library.Remove(def.Key);
                _library.Add(def.Key, def);
            }

            var lib = new LoadableLibraryDefinition<TDefinition>();
            foreach (var entry in _library)
            {
                lib.Entries.Add((entry.Key, entry.Value));
            }

            lib.Hash = hash;

            if (Environment.CurrentDirectory is { } parent)
            {
                var json = JsonConvert.SerializeObject(lib);
                File.WriteAllLines($"{parent}/Content/{JsonPath}", [json]);
                File.WriteAllLines($"Content/{JsonPath}", [json]);
            }
        }
    }
}