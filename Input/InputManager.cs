using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using SINEATER.Serialization;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System;

namespace SINEATER.Input
{
    public static class InputM
    {
        public static bool IsActive(EInputAction action) => InputManager.Instance.IsActionActive(action);
        public static Glyph GetGlyph(EInputAction action) => InputManager.Instance.GetGlyph(action);
    }

    internal class InputManager
    {
        public static InputManager Instance = new();

        public enum EInputSource
        {
            Keyboard = 0,
            GamePad = 1,
        }

        public EventHandler? OnInputSourceChanged { get; set; } = delegate { };

        private List<InputContext> _loadedContexts = new();

        private Stack<InputContext> InputStacks = new();

        private KeyboardState currentKeyState;
        private GamePadState currentGamepadState;

        // default input source to Keyboard
        private EInputSource _inputSource = EInputSource.Keyboard;
        public EInputSource InputSource => _inputSource;

        public void Initialize(string json)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                SerializationBinder = new InputDefinitionsSerializationBinder()
            };

            InitDefault();
            InitializeGlyphs();

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
                OnInputSourceChanged?.Invoke(this, new EventArgs());
            }
            else if (_inputSource != EInputSource.Keyboard && newKeyboardState != currentKeyState)
            {
                _inputSource = EInputSource.Keyboard;
                OnInputSourceChanged?.Invoke(this, new EventArgs());
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
                    MakeAction(EInputAction.Debug, Keys.F3, Buttons.None, false, eInput: EInputTrigger.JustReleased),
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
                    MakeAction(EInputAction.DetailedView, Keys.LeftAlt, Buttons.Y, eInput: EInputTrigger.Down),
                    
                    MakeAction(EInputAction.StartFight, Keys.Enter, Buttons.X),
                    MakeAction(EInputAction.CancelFight, Keys.Escape, Buttons.B),
                    MakeAction(EInputAction.Equipment, Keys.I, Buttons.Y),
                    MakeAction(EInputAction.SwapLeft, Keys.Q, Buttons.LeftTrigger),
                    MakeAction(EInputAction.SwapRight, Keys.E, Buttons.RightTrigger),
                }
            });
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

        Dictionary<Keys, Glyph> _keyboardGlyphs = new();
        Dictionary<Buttons, Glyph> _gamepadGlyphs = new();

        public Glyph GetGlyph(EInputAction action)
        {
            var defaultG = new Glyph(0, 0, Color.Transparent, Color.Red);
            var context = InputStacks.Peek();
            if (context != null)
            {
                var definition = context.Inputs.Find(x => x.InputActionType == action);
                if (definition != null)
                {
                    if(InputSource == EInputSource.Keyboard)
                    {
                        return _keyboardGlyphs.ContainsKey(definition.Keyboard) ? _keyboardGlyphs[definition.Keyboard] : defaultG;
                    }
                    else
                    {
                        return _gamepadGlyphs.ContainsKey(definition.Gamepad) ? _gamepadGlyphs[definition.Gamepad] : defaultG;
                    }

                }
            }

            return defaultG;
        }

        private void InitializeGlyphs()
        {
            // Keyboard

            _keyboardGlyphs.Add(Keys.Q, new Glyph(0, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.W, new Glyph(1, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.E, new Glyph(2, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.R, new Glyph(3, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.T, new Glyph(4, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.Z, new Glyph(5, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.U, new Glyph(6, 1, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.I, new Glyph(7, 1, Color.Transparent, Color.Gray));

            _keyboardGlyphs.Add(Keys.Escape, new Glyph(6, 4, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.Enter, new Glyph(10, 4, Color.Transparent, Color.Gray));

            _keyboardGlyphs.Add(Keys.Left, new Glyph(6, 5, Color.Transparent, Color.Gray));
            _keyboardGlyphs.Add(Keys.Right, new Glyph(8, 5, Color.Transparent, Color.Gray));

            
            // GamePad

            _gamepadGlyphs.Add(Buttons.A, new Glyph(0, 0, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.B, new Glyph(1, 0, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.X, new Glyph(2, 0, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.Y, new Glyph(3, 0, Color.Transparent, Color.Gray));

            _gamepadGlyphs.Add(Buttons.DPadLeft, new Glyph(3, 1, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.DPadRight, new Glyph(1, 1, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.LeftShoulder, new Glyph(1, 3, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.RightShoulder, new Glyph(2, 3, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.LeftTrigger, new Glyph(1, 4, Color.Transparent, Color.Gray));
            _gamepadGlyphs.Add(Buttons.RightTrigger, new Glyph(2, 4, Color.Transparent, Color.Gray));
        }
    }
}