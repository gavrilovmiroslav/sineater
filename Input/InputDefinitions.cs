using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SINEATER.Input;

internal enum EInputTrigger
{
    JustPressed,
    Down,
    JustReleased
}

internal class InputAction
{
    [JsonConverter(typeof(StringEnumConverter))]
    public EInputAction InputActionType { get; set; }

    [JsonIgnore]
    public bool IsActive = false;

    [JsonConverter(typeof(StringEnumConverter))]
    public Keys Keyboard = Keys.None;

    [JsonConverter(typeof(StringEnumConverter))]
    public Buttons Gamepad = Buttons.None;

    public bool IsHold = false;
    public int HoldTime = 0;
    private int _currentHoldTime = 0;

    public EInputTrigger InputType = EInputTrigger.JustPressed;

    public void Update(KeyboardState previous, KeyboardState current, int gametime) 
    {
        IsActive = false;
        if (IsHold)
        {
            if (current.IsKeyDown(Keyboard))
            {
                _currentHoldTime += gametime;
                if (_currentHoldTime > HoldTime)
                {
                    IsActive = true;
                    _currentHoldTime = 0;
                }
            }
            if (current.IsKeyUp(Keyboard))
            {
                _currentHoldTime = 0;
            }
        }
        else
        {
            IsActive = (!previous.IsKeyDown(Keyboard) || InputType == EInputTrigger.Down) && current.IsKeyDown(Keyboard);
        }
    }
    public void Update(GamePadState previous, GamePadState current, int gametime)
    {
        IsActive = false;
        if (IsHold)
        {
            if (current.IsButtonDown(Gamepad))
            {
                _currentHoldTime += gametime;
                if (_currentHoldTime > HoldTime)
                {
                    IsActive = true;
                    _currentHoldTime = 0;
                }
            }

            if(current.IsButtonUp(Gamepad))
            {
                _currentHoldTime = 0;
            }
        }
        else
        {
            IsActive = (!previous.IsButtonDown(Gamepad) || InputType == EInputTrigger.Down) && current.IsButtonDown(Gamepad);
        }
    }
}