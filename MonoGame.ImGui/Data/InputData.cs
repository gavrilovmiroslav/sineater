using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Vector2 = System.Numerics.Vector2;

namespace MonoGame.ImGui.Data;

/// <summary>
///     Contains the GUIRenderer's input data elements.
/// </summary>
public class InputData
{
    public Dictionary<ImGuiKey, Keys> KeyMap;
    public int Scrollwheel;

    public InputData()
    {
        Scrollwheel = 0;
        KeyMap = new Dictionary<ImGuiKey, Keys>();
    }

    public void Update(Game game)
    {
        if (!game.IsActive)
            return;

        var io = ImGuiNET.ImGui.GetIO();
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();

        foreach(var (key, value) in KeyMap)
        {
            io.AddKeyEvent(key, keyboard.IsKeyDown(value));
        }

        io.KeyShift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        io.KeyCtrl = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        io.KeyAlt = keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt);
        io.KeySuper = keyboard.IsKeyDown(Keys.LeftWindows) || keyboard.IsKeyDown(Keys.RightWindows);

        io.DisplaySize = new Vector2(game.GraphicsDevice.PresentationParameters.BackBufferWidth, game.GraphicsDevice.PresentationParameters.BackBufferHeight);
        io.DisplayFramebufferScale = new Vector2(1f, 1f);

        io.MousePos = new Vector2(mouse.X, mouse.Y);

        io.MouseDown[0] = mouse.LeftButton == ButtonState.Pressed;
        io.MouseDown[1] = mouse.RightButton == ButtonState.Pressed;
        io.MouseDown[2] = mouse.MiddleButton == ButtonState.Pressed;

        var scrollDelta = mouse.ScrollWheelValue - Scrollwheel;
        io.MouseWheel = scrollDelta > 0 ? 1 : scrollDelta < 0 ? -1 : 0;
        Scrollwheel = mouse.ScrollWheelValue;
    }
    public InputData Initialize(Game game)
    {
        var io = ImGuiNET.ImGui.GetIO();

        KeyMap.Add(ImGuiKey.Tab, Keys.Tab);
        KeyMap.Add(ImGuiKey.LeftArrow, Keys.Left);
        KeyMap.Add(ImGuiKey.RightArrow, Keys.Right);
        KeyMap.Add(ImGuiKey.UpArrow, Keys.Up);
        KeyMap.Add(ImGuiKey.DownArrow, Keys.Down);
        KeyMap.Add(ImGuiKey.PageUp, Keys.PageUp);
        KeyMap.Add(ImGuiKey.PageDown, Keys.PageDown);
        KeyMap.Add(ImGuiKey.Home, Keys.Home);
        KeyMap.Add(ImGuiKey.End, Keys.End);
        KeyMap.Add(ImGuiKey.Delete, Keys.Delete);
        KeyMap.Add(ImGuiKey.Backspace, Keys.Back);
        KeyMap.Add(ImGuiKey.Enter, Keys.Enter);
        KeyMap.Add(ImGuiKey.Escape, Keys.Escape);

        game.Window.TextInput += (sender, args) =>
        {
            if (args.Character != '\t')
                io.AddInputCharacter(args.Character);
        };

        io.Fonts.AddFontDefault();
        return this;
    }
}