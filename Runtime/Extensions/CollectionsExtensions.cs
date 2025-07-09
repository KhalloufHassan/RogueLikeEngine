using System;
using System.Collections.Generic;
using System.Linq;
using static UnityEngine.Random;

namespace RogueLikeEngine.Extensions
{
    public static class CollectionsExtensions
{
    private static readonly Random Rng = new();  

    public static void Clear<T>(this T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = default;
        }
    }
    
    public static int IndexOf<T>(this T[] array,T match)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (Equals(array[i], match))
                return i;
        }

        return -1;
    }

    public static T Random<T>(this IList<T> list) => list.Count == 0 ? default : list[Range(0, list.Count)];

    public static T Random<T>(this IEnumerable<T> enumerable) => enumerable.OrderBy(_ => value).FirstOrDefault();
    public static IEnumerable<T> Random<T>(this IEnumerable<T> enumerable,int count) => enumerable.OrderBy(_ => value).Take(count);
    public static IEnumerable<T> RandomOrder<T>(this IEnumerable<T> enumerable) => enumerable.OrderBy(_ => value);

    /// <summary>
    /// Wraps this object instance into an IEnumerable&lt;T&gt;
    /// consisting of a single item.
    /// </summary>
    /// <typeparam name="T"> Type of the object. </typeparam>
    /// <param name="item"> The instance that will be wrapped. </param>
    /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }

    public static T RemoveLast<T>(this IList<T> list)
    {
        if (list is not { Count: > 0 }) return default;
        T t = list[^1];
        list.RemoveAt(list.Count - 1);
        return t;
    }
    
    public static IList<T> Shuffle<T>(this IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = Rng.Next(n + 1);  
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable) => enumerable.OrderBy(_ => value);
    
    public static bool IsIndexInBounds<T>(this T[] array, int index)
    {
        if (array == null)
            throw new ArgumentNullException(nameof(array));

        return index >= 0 && index < array.Length;
    }
    
    public static T FirstOrDefaultStartingAtIndex<T>(this IList<T> source, int index, Func<T, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        
        if (source.Count == 0)
            return default;

        int currentIndex = index + 1;

        do
        {
            if (currentIndex >= source.Count)
                currentIndex = 0;
            if (predicate(source[currentIndex]))
                return source[currentIndex];

            currentIndex++;
        } while (currentIndex != index);

        return default;
    }
    
    public static T LastOrDefaultStartingAtIndex<T>(this IList<T> source, int index, Func<T, bool> predicate)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        
        if (source.Count == 0)
            return default;

        int currentIndex = index - 1;

        do
        {
            if (currentIndex < 0)
                currentIndex = source.Count - 1;
            if (predicate(source[currentIndex]))
                return source[currentIndex];

            currentIndex--;
        } while (currentIndex != index);

        return default;
    }
}
}