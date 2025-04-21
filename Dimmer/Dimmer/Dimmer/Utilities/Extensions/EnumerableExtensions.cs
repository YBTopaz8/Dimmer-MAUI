using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utilities.Extensions;
public static class EnumerableExtensions
{
    // Thread‑safe Random per thread
    private static readonly ThreadLocal<Random> _rng = new(() => new Random());

    public static void ShuffleInPlace<T>(this IList<T> list)
    {
        if (_rng.Value == null)
        {
            throw new InvalidOperationException("Random instance is null.");
        }

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = _rng.Value.Next(i + 1);
            // swap
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
