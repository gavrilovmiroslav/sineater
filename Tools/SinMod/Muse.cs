using System;
using Microsoft.Xna.Framework;
using NotImplementedException = System.NotImplementedException;

namespace SINEATER.Tools.SinMod;

public enum EMusicState
{
    MainMenu = 0,
    World = 1,
    Rest = 2,
    Combat = 3,
}

public static class Muse
{
    private static float _pauseFader = 0.0f;
    private static float _pauseFaderTarget = 0.0f;
    private static bool _pauseTarget = false;
    
    public static void SetGameState(EMusicState state)
    {
        SinMod.System.GetLabelledInstance("bgm")?.SetParam("GameState", (int)state, true);
    }
    
    public static void SetPaused(bool paused)
    {
        if (paused != _pauseTarget)
        {
            _pauseTarget = paused;
            _pauseFaderTarget = paused ? 1 : 0;
        }
    }

    public static void Update(GameTime gameTime)
    {
        var bgm = SinMod.System.GetLabelledInstance("bgm");

        if (_pauseFaderTarget > 0)
        {
            _pauseFader = float.Lerp(_pauseFader, _pauseFaderTarget, 0.1f);
        }
        else
        {
            _pauseFader = float.Lerp(_pauseFader, _pauseFaderTarget, 0.25f);
        }

        bgm?.SetParam("Pause", _pauseFader, true);
        
        Tools.SinMod.System.Update(gameTime);
    }

    public static void Load()
    {
        Tools.SinMod.System.LoadBank(@"audio/Desktop/Master.bank");
        Tools.SinMod.System.CreateInstance("Music/Music", "bgm").Play();
    }
}