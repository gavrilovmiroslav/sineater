using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using SINEATER.Serialization;
using System.Collections.Generic;

namespace SINEATER.Input
{


    public static class InputM
    {
        public static bool IsActive(EInputAction action) => InputManager.Instance.IsActionActive(action);
    }

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
        private GamePadState currentGamepadState;

        // default input source to Keyboard
        private EInputSource _inputSource = EInputSource.Keyboard;

        public void Initialize(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new InputDefinitionsSerializationBinder()
            };

            InitDefault();

            //_loadedContexts = DataSerializer.Load<List<InputContext>>(json, settings);
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
            if (_inputSource != EInputSource.GamePad && newGamePadState != currentGamepadState)
            {
                _inputSource = EInputSource.GamePad;
            }
            else if (_inputSource != EInputSource.Keyboard && newKeyboardState != currentKeyState)
            {
                _inputSource = EInputSource.Keyboard;
            }

            foreach (var input in InputStacks.Peek().Inputs)
            {
                if (_inputSource == EInputSource.Keyboard)
                {
                    input.Update(currentKeyState, newKeyboardState, gameTime);
                }
                else
                {
                    input.Update(currentGamepadState, newGamePadState, gameTime);
                }
            }
            currentKeyState = newKeyboardState;
            currentGamepadState = newGamePadState;
        }

        public bool IsActionActive(EInputAction action)
        {
            var context = InputStacks.Peek();
            if (context != null)
            {
                var definition = context.Inputs.Find(x => x.InputActionType == action);
                if (definition != null)
                {
                    return definition.IsActive;
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

        private void InitDefault()
        {
            _loadedContexts = new();
            _loadedContexts.Add(new InputContext
            {
                Name = "Default",
                Inputs = new List<InputAction>
            {
                MakeAction(EInputAction.Exit, Keys.Escape, Buttons.Back, true, 200),

                MakeAction(EInputAction.MoveUp, Keys.Up, Buttons.DPadUp),
                MakeAction(EInputAction.MoveDown, Keys.Down, Buttons.DPadDown),
                MakeAction(EInputAction.MoveLeft, Keys.Left, Buttons.DPadLeft),
                MakeAction(EInputAction.MoveRight, Keys.Right, Buttons.DPadRight),
                MakeAction(EInputAction.Confirm, Keys.Space, Buttons.A),

                MakeAction(EInputAction.SubmenuUp, Keys.Up, Buttons.DPadUp),
                MakeAction(EInputAction.SubmenuDown, Keys.Down, Buttons.DPadDown),
                MakeAction(EInputAction.SubmenuConfirm, Keys.Space, Buttons.A),

                MakeAction(EInputAction.VolumeDown, Keys.PageDown, Buttons.None),
                MakeAction(EInputAction.VolumeUp, Keys.PageUp, Buttons.None),
                MakeAction(EInputAction.Mute, Keys.End, Buttons.None),

                MakeAction(EInputAction.LoadItems, Keys.F5, Buttons.None),
                MakeAction(EInputAction.RestartExploration, Keys.F1, Buttons.None),
                MakeAction(EInputAction.ExplorationDebug, Keys.F10, Buttons.None),
                MakeAction(EInputAction.ShowImGui, Keys.F2, Buttons.None),

                MakeAction(EInputAction.ChacterSheetEnter, Keys.C, Buttons.Y),
                MakeAction(EInputAction.ChacterSheetCycle, Keys.Space, Buttons.A),
                MakeAction(EInputAction.ChacterSheetExit, Keys.Escape, Buttons.B),

                MakeAction(EInputAction.OpenInventory, Keys.I, Buttons.None),
                MakeAction(EInputAction.OpenInventoryOutfit, Keys.O, Buttons.None),

                MakeAction(EInputAction.MoveMapLeft, Keys.U, Buttons.None),
                MakeAction(EInputAction.MoveMapRight, Keys.I, Buttons.None),
                MakeAction(EInputAction.Regenerate, Keys.F1, Buttons.None),
                MakeAction(EInputAction.ShowMap, Keys.F10, Buttons.None),

                MakeAction(EInputAction.ExitInspect, Keys.F10, Buttons.B),
                MakeAction(EInputAction.Ability, Keys.A, Buttons.X),
                MakeAction(EInputAction.ActionsMenu, Keys.Space, Buttons.A),
                MakeAction(EInputAction.EndTurn, Keys.Enter, Buttons.B),
                MakeAction(EInputAction.SelectNextCharacter, Keys.Tab, Buttons.RightTrigger),
                MakeAction(EInputAction.SelectPreviousCharacter, Keys.None, Buttons.LeftTrigger),
                MakeAction(EInputAction.DetailedView, Keys.LeftAlt, Buttons.None, eInput: EInputTrigger.JustReleased),
            }
            }
            );
        }

        private InputAction MakeAction(EInputAction action, Keys key, Buttons button, bool isHold = false, int HoldTime = 0, EInputTrigger eInput = EInputTrigger.JustPressed)
        {
            return new InputAction
            {
                InputActionType = action,
                Gamepad = button,
                Keyboard = key,
                IsHold = isHold,
                HoldTime = HoldTime,
                InputType = eInput
            };
        }
    }
}