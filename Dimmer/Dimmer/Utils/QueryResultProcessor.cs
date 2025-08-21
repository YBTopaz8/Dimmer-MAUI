using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Utils;

/// <summary>
/// A static helper class to perform final in-memory processing on a list of query results.
/// This includes applying limiters (First, Last, Random).
/// </summary>
public static class QueryResultProcessor
{
    // Use a single static Random instance for better performance and randomness.
    private static readonly Random _random = new();

    /// <summary>
    /// Applies the specified LimiterClause to a list of songs.
    /// </summary>
    /// <param name="songs">The materialized list of songs from the database query.</param>
    /// <param name="limiter">The limiter clause parsed from the TQL query.</param>
    /// <returns>A new list of songs with the limiter applied.</returns>
    public static List<SongModelView> ApplyLimiter(IList<SongModelView> songs, LimiterClause? limiter)
    {
        // If there's no limiter, return the original list.
        if (limiter == null)
        {
            return songs.ToList();
        }

        switch (limiter.Type)
        {
            case LimiterType.First:
                return songs.Take(limiter.Count).ToList();

            case LimiterType.Last:
                // We use Math.Max to prevent a negative number if the list is smaller than the count.
                int skipCount = Math.Max(0, songs.Count - limiter.Count);
                return songs.Skip(skipCount).ToList();

            case LimiterType.Random:
                // An efficient shuffle on the small, materialized list.
                return songs.OrderBy(s => _random.Next()).Take(limiter.Count).ToList();

            default:
                return songs.ToList();
        }
    }
}