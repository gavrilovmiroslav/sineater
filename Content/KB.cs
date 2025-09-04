using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Key = Microsoft.Xna.Framework.Input.Keys;

namespace SINEATER.Content;

public class KB
{
    static KeyboardState currentKeyState;
    static KeyboardState previousKeyState;

    public static void Update()
    {
        previousKeyState = currentKeyState;
        currentKeyState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
    }

    public static bool IsPressed(Key key)
    {
        return currentKeyState.IsKeyDown(key);
    }

    public static bool HasBeenPressed(Key key)
    {
        return currentKeyState.IsKeyDown(key) && !previousKeyState.IsKeyDown(key);
    }
}