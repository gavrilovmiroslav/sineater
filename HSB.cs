﻿using System;
using Microsoft.Xna.Framework;

namespace SINEATER;

public static class HSB
{
    /// <summary>
    /// Creates a Color from alpha, hue, saturation and brightness.
    /// </summary>
    /// <param name="alpha">The alpha channel value.</param>
    /// <param name="hue">The hue value.</param>
    /// <param name="saturation">The saturation value.</param>
    /// <param name="brightness">The brightness value.</param>
    /// <returns>A Color with the given values.</returns>
    public static Color New(int alpha, float hue, float saturation, float brightness)
    {
        if (0 > alpha
            || 255 < alpha)
        {
            throw new ArgumentOutOfRangeException(
                "alpha",
                alpha,
                "Value must be within a range of 0 - 255.");
        }

        if (0f > hue
            || 360f < hue)
        {
            throw new ArgumentOutOfRangeException(
                "hue",
                hue,
                "Value must be within a range of 0 - 360.");
        }

        if (0f > saturation
            || 1f < saturation)
        {
            throw new ArgumentOutOfRangeException(
                "saturation",
                saturation,
                "Value must be within a range of 0 - 1.");
        }

        if (0f > brightness
            || 1f < brightness)
        {
            throw new ArgumentOutOfRangeException(
                "brightness",
                brightness,
                "Value must be within a range of 0 - 1.");
        }

        if (0 == saturation)
        {
            return new Color(
                Convert.ToInt32(brightness * 255),
                Convert.ToInt32(brightness * 255),
                Convert.ToInt32(brightness * 255),
                alpha);
        }

        float fMax, fMid, fMin;
        int iSextant, iMax, iMid, iMin;

        if (0.5 < brightness)
        {
            fMax = brightness - (brightness * saturation) + saturation;
            fMin = brightness + (brightness * saturation) - saturation;
        }
        else
        {
            fMax = brightness + (brightness * saturation);
            fMin = brightness - (brightness * saturation);
        }

        iSextant = (int)Math.Floor(hue / 60f);
        if (300f <= hue)
        {
            hue -= 360f;
        }

        hue /= 60f;
        hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
        if (0 == iSextant % 2)
        {
            fMid = (hue * (fMax - fMin)) + fMin;
        }
        else
        {
            fMid = fMin - (hue * (fMax - fMin));
        }

        iMax = Convert.ToInt32(fMax * 255);
        iMid = Convert.ToInt32(fMid * 255);
        iMin = Convert.ToInt32(fMin * 255);

        switch (iSextant)
        {
            case 1:
                return new Color(iMid, iMax, iMin, alpha);
            case 2:
                return new Color(iMin, iMax, iMid, alpha);
            case 3:
                return new Color(iMin, iMid, iMax, alpha);
            case 4:
                return new Color(iMid, iMin, iMax, alpha);
            case 5:
                return new Color(iMax, iMin, iMid, alpha);
            default:
                return new Color(iMax, iMid, iMin, alpha);
        }
    }
}