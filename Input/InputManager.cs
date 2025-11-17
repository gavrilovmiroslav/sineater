using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using SINEATER.Serialization;
using System.Collections.Generic;

namespace SINEATER.Input;

internal class InputManager
{
    public static InputManager Instance = new();

    private enum EInputSource
    {
        Keyboard = 0,
        GamePad = 1,
    }

    private List<InputContext> _loadedContexts = new();

    private Stack<InputContext> InputStacks = new();

    private KeyboardState currentKeyState;
    private KeyboardState previousKeyState;
    private GamePadState currentGamepadState;
    private GamePadState previousGamepadState;

    // default input source to Keyboard
    private EInputSource inputSource = EInputSource.Keyboard;

    public void Initialize(string json)
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = new InputDefinitionsSerializationBinder()
        };

        _loadedContexts = DataSerializer.Load<List<InputContext>>(json, settings);
    }

    public void Save()
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            SerializationBinder = new InputDefinitionsSerializationBinder()
        };

        DataSerializer.Serialize(_loadedContexts, settings);
    }

    public void Update(int gameTime)
    {
        if (InputStacks.Count == 0)
            return;

        var newKeyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
        var newGamePadState = Microsoft.Xna.Framework.Input.GamePad.GetState(0);

        // Update input source based on state (thanks Microsoft :) )
        if (inputSource != EInputSource.GamePad && newGamePadState != currentGamepadState)
        {
            inputSource = EInputSource.GamePad;
        }
        else
        {
            inputSource = EInputSource.Keyboard;
        }

        foreach (var input in InputStacks.Peek().Inputs)
        {
            if (input.IsHold)
            {
                input.UpdateHold(gameTime);
            }
        }

        previousKeyState = currentKeyState;
        previousGamepadState = currentGamepadState;
        currentKeyState = newKeyboardState;
        currentGamepadState = newGamePadState;
    }

    public bool IsActionActive(EInputActions action)
    {
        var context = InputStacks.Peek();
        if (context != null)
        {
            var definition = context.Inputs.Find(x => x.InputAction == action);
            if (definition != null)
            {
                if (inputSource == EInputSource.Keyboard)
                {
                    return definition != null ? definition.IsActive(previousKeyState, currentKeyState) : false;
                }
                else
                {
                    return definition != null ? definition.IsActive(previousGamepadState, currentGamepadState) : false;
                }
            }
        }
        return false;
    }

    public void PushContext(string contextName)
    {
        var context = _loadedContexts.Find(x => x.Name == contextName);
        if (context != null)
        {
            InputStacks.Push(context);
        }
    }

    public void PopContext()
    {
        InputStacks.Pop();
    }

    private void InitTest()
    {
        _loadedContexts = new();
        _loadedContexts.Add(new InputContext
        {
            Name = "Move",
            Inputs = new List<IInputDefinition>
            {
                new PressInputDefinition
                {
                InputAction = EInputActions.Move,
                Keyboard = Keys.E,
                Gamepad = Buttons.B
                },
                new HoldInputDefinition
                {
                    InputAction = EInputActions.Attack,
                    Keyboard = Keys.Space,
                    Gamepad = Buttons.B
                },
                new ComboInputDefinition
                {
                    InputAction = EInputActions.Dancee,
                    inputDefinitions = new List<IInputDefinition>
                    {
                        new PressInputDefinition
                        {
                            Keyboard = Keys.E,
                            Gamepad = Buttons.B
                        },
                        new HoldInputDefinition
                        {
                            Keyboard = Keys.Space,
                            Gamepad = Buttons.RightShoulder
                        }
                    }
                }
        }
        }
        );
    }
}
