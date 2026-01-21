using System;

namespace SINEATER.Game.LookNFeel;

public static class Easing
{
    public static float SineEaseIn(this float x) 
        => 1.0f - MathF.Cos((x * MathF.PI) / 2.0f);
    
    public static float SineEaseOut(this float x)
        => MathF.Cos((x * MathF.PI) / 2.0f);

    public static float SineEaseInOut(this float x)
        => -(MathF.Cos(MathF.PI * x) - 1) / 2;

    public static float CubicEaseIn(this float x)
        => x * x * x;

    public static float CubicEaseOut(this float x)
        => 1.0f - MathF.Pow(1.0f - x, 3.0f);

    public static float CubicEaseInOut(this float x)
        => x < 0.5 ? 4 * x * x * x : 1 - MathF.Pow(-2 * x + 2, 3) / 2;

    public static float Low(this float x, float min, Func<float, float> easingFunction)
        => x < min ? 0.0f : easingFunction(x);

    public static float BackEaseOut(this float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        return 1 + c3 * MathF.Pow(x - 1, 3) + c1 * MathF.Pow(x - 1, 2);
    }
}