using System;

namespace SINEATER;

internal static class Extensions
{
    public static void Shuffle<T> (this T[] array)
    {
        var n = array.Length;
        while (n > 1) 
        {
            var k = Rnd.Instance.Next(0, n--);
            (array[n], array[k]) = (array[k], array[n]);
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
}