using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Dimmer.Utilities.Extensions;

public static class EnumerableExtensions
{
    // Thread-safe Random per thread
    private static readonly ThreadLocal<Random> _rng = new(() => new Random());

    /// <summary>
    /// Shuffles the list in place using Fisher–Yates.
    /// </summary>
    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        var rng = _rng.Value;
        if (list == null || list.Count <= 1)
            return; // No need to shuffle if the list is null or has one or no elements
        if (rng is null)
            return; // Ensure rng is not null
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    /// <summary>
    /// Returns a new list containing the elements of source in shuffled order.
    /// </summary>
    public static List<T> Shuffled<T>(this IEnumerable<T> source)
    {
        var buffer = source.ToList();
        buffer.ShuffleInPlace();
        return buffer;
    }
}
