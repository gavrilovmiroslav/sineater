using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using SINEATER.Game.Gameplay;

namespace SINEATER.Game.CoreUtils;

internal static class Extensions
{
    public static void Swap<T>(this T[] arr, int leftIndex, int rightIndex)
        where T: Character
    {
        (arr[leftIndex], arr[rightIndex]) = (arr[rightIndex], arr[leftIndex]);
    }
    
    public static void SwapBy<T>(this T[] arr, int index, int ds)
        where T: Character
    {
        var s = Math.Sign(ds);
        for (int i = 0; i < Math.Abs(ds); i++)
        {
            if (index + s > 3 || index + s < 0)
            {
                break;
            }

            (arr[index], arr[index + s]) = (arr[index + s], arr[index]);
            index += s;
        }
    }

    public static void Consume(this IEnumerable<IEnumerable> ee)
    {
        foreach (var p in ee) { foreach (var _ in p) {} }
    }
    
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
    
    public static IEnumerable<string> ReadLines(this Stream stream, Encoding encoding)
    {
        using var reader = new StreamReader(stream, encoding);
        string line = "";
        while ((line = reader.ReadLine()) != null)
        {
            yield return line;
        }
    }
    
    public static Color Darken(this Color color, float factor)
    {
        color.ToHSV(out var h, out var s, out var l);
        return color.FromHSV(h, s, Math.Max(0.0f, Math.Min(1.0f, l - factor * 100)));
    }
    
    public static Color Lighten(this Color color, float factor)
    {
        color.ToHSV(out var h, out var s, out var l);
        return color.FromHSV(h, s, Math.Max(0.0f, Math.Min(1.0f, l + factor * 100)));
    }
    
    public static void Shuffle<T> (this T[] array)
    {
        var n = array.Length;
        while (n > 1) 
        {
            var k = Rnd.Instance.Next(0, n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }
    
    public static void Shuffle<T> (this List<T> list)
    {
        var n = list.Count;
        while (n > 1) 
        {
            var k = Rnd.Instance.Next(0, n--);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
    
    public class Enum<T> where T : struct, IConvertible
    {
        public static int Count()
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");

            return Enum.GetNames(typeof(T)).Length;
        }
        
        public static T Random() 
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enumerated type");
            else
            {
                var values = Enum.GetValuesAsUnderlyingType(typeof(T)) as T[];
                return values[Rnd.Instance.Next(0, values.Length - 1)];
            }
        }
    }

    public static Color Mix(this Color a, Color b)
    {
        return new Color(Math.Max(a.R / 2, b.R / 2), Math.Max(a.G / 2, b.G / 2), Math.Max(a.B / 2, b.B / 2));
    } 
}