using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Linq;

namespace SINEATER.Input;

internal abstract class IInputDefinition
{
    [JsonConverter(typeof(StringEnumConverter))]
    public EInputActions InputAction { get; set; }

    public abstract bool IsActive(KeyboardState previous, KeyboardState current);
    public abstract bool IsActive(GamePadState previous, GamePadState current);
    [JsonIgnore]
    public bool IsHold = false;
    public abstract void UpdateHold(int gametime);
}

internal class PressInputDefinition : IInputDefinition
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Keys Keyboard = Keys.None;
    [JsonConverter(typeof(StringEnumConverter))]
    public Buttons Gamepad = Buttons.None;

    public override bool IsActive(KeyboardState previous, KeyboardState current)
    {
        return !previous.IsKeyDown(Keyboard) && current.IsKeyDown(Keyboard);
    }

    public override bool IsActive(GamePadState previous, GamePadState current)
    {
        return !previous.IsButtonDown(Gamepad) && current.IsButtonDown(Gamepad);
    }

    public override void UpdateHold(int gametime) { }
}
internal class HoldInputDefinition : IInputDefinition
{
    [JsonConverter(typeof(StringEnumConverter))]
    public Keys Keyboard = Keys.None;
    [JsonConverter(typeof(StringEnumConverter))]
    public Buttons Gamepad = Buttons.None;

    private int _holdTime = 0;
    private int _currentHoldTime = 0;

    public HoldInputDefinition()
    {
        IsHold = true;
    }

    public override bool IsActive(KeyboardState previous, KeyboardState current)
    {
        return _currentHoldTime > _holdTime && current.IsKeyDown(Keyboard);
    }
    public override bool IsActive(GamePadState previous, GamePadState current)
    {
        return _currentHoldTime > _holdTime && current.IsButtonDown(Gamepad);
    }
    public override void UpdateHold(int gametime)
    {
        _currentHoldTime += gametime;
    }
}

internal class ComboInputDefinition : IInputDefinition
{
    public ComboInputDefinition()
    {
        IsHold = inputDefinitions.Any(x => x.IsHold);
    }

    public List<IInputDefinition> inputDefinitions = new List<IInputDefinition>();
    public override bool IsActive(KeyboardState previous, KeyboardState current)
    {
        return inputDefinitions.All(x => x.IsActive(previous, current));
    }

    public override bool IsActive(GamePadState previous, GamePadState current)
    {
        return inputDefinitions.All(x => x.IsActive(previous, current));
    }

    public override void UpdateHold(int gametime)
    {
        foreach(var input in inputDefinitions)
        {
            input.UpdateHold(gametime);
        }
    }
}