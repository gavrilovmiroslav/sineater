using FMODMG;
using FMODMG.Studio;
using FmodForFoxesMG;
using FmodForFoxesMG.Studio;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Bank = FmodForFoxesMG.Studio.Bank;
using EventDescription = FmodForFoxesMG.Studio.EventDescription;
using EventInstance = FmodForFoxesMG.Studio.EventInstance;

namespace SINEATER.SinMod;

public static class System
{
    private static readonly Dictionary<string, SinEventInstance> _labels = [];
    private static readonly Dictionary<string, GUID> _guids = [];
    private static readonly List<SinEventInstance> _events = [];
    
    public static void Init(FmodForFoxesMG.Data.GuidsCollectionData collection)
    {
        FmodForFoxesMG.FmodManager.Init(FmodForFoxesMG.FmodInitMode.CoreAndStudio, enableLogging: true);

        foreach (var (key, value) in collection.GUIDS)
        {
            if (Util.parseID($"{{{value.ToString()}}}", out var guid) == RESULT.OK)
            {
                _guids.Add(key, guid);
            }
        }

    }

    public static void Update(GameTime gameTime)
    {
        FmodManager.Update();
        foreach (var ev in _events)
        {
            ev.Update(gameTime);
        }
    }
    
    //public static Bank LoadBank(string path)
    //{
    //    var buffer = FileLoader.LoadFileAsBuffer(path);
    //    unsafe
    //    {
    //        FMOD.Studio.Bank bank;
    //        RESULT num = StudioSystem.Native.loadBankMemory(buffer, LOAD_BANK_FLAGS.NORMAL, out bank);
    //        if (num != RESULT.OK)
    //        {
    //            throw new Exception($"FMOD LoadBank fail: {num}");
    //        }
    //        else
    //        {
    //            return new Bank(bank);
    //        }
    //    }
    //}

    public static FmodForFoxesMG.Studio.Bank LoadBank(FmodForFoxesMG.Data.SoundBankData fmodBank)
    {
        var buffer = fmodBank.Data;

        FMODMG.Studio.Bank bank;
        FMODMG.RESULT num = FmodForFoxesMG.Studio.StudioSystem.Native.loadBankMemory(
            buffer, FMODMG.Studio.LOAD_BANK_FLAGS.NORMAL, out bank);
        if (num != FMODMG.RESULT.OK)
        {
            throw new Exception($"FMOD LoadBank fail: {num}");
        }
        else
        {
            return new FmodForFoxesMG.Studio.Bank(bank);
        }
    }

    public static void GetEvents(Bank bank)
    {
        RESULT num = bank.Native.getEventList(out var evs);
        if (num != RESULT.OK)
        {
            throw new Exception($"FMOD GetEvents fail: {num}");
        }
        else
        {
            foreach (var ev in evs)
            {
                ev.getID(out GUID id);
                ev.getPath(out var path);
                Console.WriteLine($"{path}: {id.Data1}-{id.Data2}-{id.Data3}-{id.Data4}");
            }
        }
    }
    
    public static EventDescription GetEvent(string path)
    {
        if (_guids.TryGetValue(path, out var guid))
        {
            return GetEvent(guid);
        }
        
        throw new Exception($"FMOD GetEvent fail: {RESULT.ERR_EVENT_NOTFOUND}");
    }

    public static EventDescription GetEvent(GUID guid)
    {
        FMODMG.Studio.EventDescription _event;
        RESULT eventById = StudioSystem.Native.getEventByID(guid, out _event);
        if (eventById != RESULT.OK)
        {
            throw new Exception($"FMOD GetEvent fail: {eventById}");
        }
        
        return new EventDescription(_event);
    }

    public static SinEventInstance CreateInstance(string description)
    {
        var ev = SinMod.System.GetEvent($"event:/{description}");
        return CreateInstance(ev);
    }

    public static SinEventInstance CreateInstance(string description, string label)
    {
        var ev = SinMod.System.GetEvent($"event:/{description}");
        var inst = CreateInstance(ev);
        _labels[label] = inst;
        return inst;
    }

    public static SinEventInstance? GetLabelledInstance(string label)
    {
        return _labels[label];
    }
    
    public static SinEventInstance CreateInstance(EventDescription eventDescription)
    {
        RESULT result = eventDescription.Native.createInstance(out var instance);
        if (result != RESULT.OK)
        {
            throw new Exception($"FMOD CreateInstance fail: {result}");
        }
        var e = new SinEventInstance(new EventInstance(eventDescription, instance));
        _events.Add(e);
        
        return e;
    }

    public static void SetParam(SinEventInstance instance, string param, float value, bool ignoreSeekSpeed = false)
    {
        var desc = instance.Event.Description;
        var prm = desc.GetParameterDescription(param);
        var num = instance.Event.Native.setParameterByID(prm.id, value, ignoreSeekSpeed);
        if (num != RESULT.OK)
        {
            throw new Exception($"FMOD SetParam fail: {num}");
        }
    }
}