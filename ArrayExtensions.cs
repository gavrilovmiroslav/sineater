namespace SINEATER;

internal static class ArrayExtensions
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
}