using Microsoft.Xna.Framework.Input;
using Key = Microsoft.Xna.Framework.Input.Keys;

namespace SINEATER.Input;

public class KB
{
    static KeyboardState currentKeyState;
    static KeyboardState previousKeyState;

    public static void Update()
    {
        previousKeyState = currentKeyState;
        currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
    }

    public static bool IsDown(Key key)
    {
        return currentKeyState.IsKeyDown(key);
    }

    public static bool IsJustReleased(Key key)
    {
        return currentKeyState.IsKeyUp(key);
    }
    public static bool IsJustPressed(Key key)
    {
        return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
    }

    public static bool HasBeenPressed(Key key)
    {
        return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
    }
}