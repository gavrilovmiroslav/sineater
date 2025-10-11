using System;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using EventInstance = FmodForFoxes.Studio.EventInstance;

namespace SINEATER.SinMod;

public class SinEventInstanceDelta(Action<float> action, float target, float speed = 1, float min = 0, float max = 1, float current = 0)
{
    public float Current;
    private float _target = target;

    public float Target
    {
        get => _target;
        set
        {
            _target = Math.Clamp(value, min, max);
        }
    }
    
    public float Speed { get; set; } = speed;
    public float Min => min;
    public float Max => max;
    
    public void Update(GameTime gameTime)
    {
        if (!(Math.Abs(Current - Target) > 0.0000001f)) return;
        var dt = 0.01f;
        if (Current > _target) dt *= -1;
        Current += dt * speed;
        Current = Math.Clamp(Current, min, max);
        action(Current);
    }
}

public class SinEventInstance
{
    private static readonly PARAMETER_ID VOLUME = new() { data1 = 0, data2 = 0 };
    private Dictionary<string, PARAMETER_ID> _parameters = new();
    private Dictionary<PARAMETER_ID, SinEventInstanceDelta> _deltas = [];

    public EventInstance Event { get; private set; }
    
    public SinEventInstance(EventInstance instance, float volume = 0.0f)
    {
        Event = instance;
        for (int i = 0; i < instance.Description.ParameterCount; i++)
        {
            var desc = instance.Description.GetParameterDescription(i);
            _parameters[(string)desc.name] = desc.id;
            _deltas[desc.id] = new SinEventInstanceDelta(v => instance.SetParameterValue(desc.id, v),
                desc.defaultvalue, 1, desc.minimum, desc.maximum, desc.defaultvalue);
        }

        _deltas[VOLUME] = new SinEventInstanceDelta(v => { instance.Volume = v; }, 0, 0.1f, 0, 1, 0);
        instance.Volume = 0;
        _deltas[VOLUME].Target = 1;
    }
    
    public void Update(GameTime gameTime)
    {
        foreach (var (_, delta) in _deltas)
        {
            delta.Update(gameTime);
        }
    }

    public void SetVolume(float volume)
    {
        _deltas[VOLUME].Target = volume;
    }

    public void ModVolume(float volume)
    {
        _deltas[VOLUME].Target += volume;
    }

    public void SetParam(string name, float value)
    {
        _deltas[_parameters[name]].Target = value;
    }

    public void Play()
    {
        Event.Start();
    }

    public void Stop()
    {
        Event.Stop();
    }
}