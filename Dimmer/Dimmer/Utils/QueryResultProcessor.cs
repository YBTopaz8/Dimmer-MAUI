namespace Dimmer.Utils;

/// <summary>
/// A static helper class to perform final in-memory processing on a list of query results.
/// This includes applying limiters (First, Last, Random).
/// </summary>
public static class QueryResultProcessor
{

    public static List<SongModelView> ApplyDaypart(IList<SongModelView> songs, DaypartNode? daypartNode)
    {
        if (daypartNode == null)
        {
            return songs.ToList();
        }

        // Find the real property name from the FieldRegistry
        if (!FieldRegistry.FieldsByAlias.TryGetValue(daypartNode.DateField, out var fieldDef))
        {
            return songs.ToList(); // Invalid field
        }

        return songs.Where(song => {
            var songDate = SemanticQueryHelpers.GetDateProp(song, fieldDef.PropertyName);
            if (songDate == null)
                return false;

            var timeOfDay = songDate.Value.TimeOfDay;

            // Handle overnight ranges (e.g., night is 22:00 to 06:00)
            if (daypartNode.StartTime > daypartNode.EndTime)
            {
                return timeOfDay >= daypartNode.StartTime || timeOfDay < daypartNode.EndTime;
            }

            return timeOfDay >= daypartNode.StartTime && timeOfDay < daypartNode.EndTime;
        }).ToList();
    }
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

    /// <summary>
    /// Applies a random chance filter to a list of songs.
    /// </summary>
    /// <param name="songs">The list of songs to filter.</param>
    /// <param name="chanceNode">The RandomChanceNode parsed from the TQL query.</param>
    /// <returns>A new, smaller list of songs that passed the random chance.</returns>
    public static List<SongModelView> ApplyChance(IList<SongModelView> songs, RandomChanceNode? chanceNode)
    {
        // If there's no chance node in the query, do nothing and return the original list.
        if (chanceNode == null)
        {
            return songs.ToList();
        }

        // For each song, generate a random number from 0 to 99.
        // If that number is less than the percentage specified by the user, the song is included.
        // This provides the desired statistical chance for each item.
        return songs.Where(_ => _random.Next(100) < chanceNode.Percentage).ToList();
    }
    public static List<SongModelView> ApplyShuffle(IList<SongModelView>? songs, ShuffleNode? shuffleNode)
    {
        if (songs == null) return Enumerable.Empty<SongModelView>().ToList();
        if (shuffleNode == null)
        {
            return songs.ToList(); // No shuffle directive was present
        }

        // Case 1: Simple, pure random shuffle
        if (!shuffleNode.IsBiased)
        {
            return songs.OrderBy(_ => _random.Next()).Take(shuffleNode.Count).ToList();
        }

        // Case 2: The Smart, Biased Shuffle
        var biasedSongs = songs.Select(song => {
            // Step 1: Calculate the base weight for the song
            double weight = CalculateWeight(song, shuffleNode.BiasField!, shuffleNode.BiasDirection);

            // Step 2: Generate a random number
            double randomFactor = _random.NextDouble();

            // Step 3: Calculate the final score
            double finalScore = weight * randomFactor;

            return new { Song = song, Score = finalScore };
        });

        // Step 4: Sort by the score and take the requested count
        return biasedSongs.OrderByDescending(x => x.Score)
                          .Select(x => x.Song)
                          .Take(shuffleNode.Count)
                          .ToList();
    }
    private static double CalculateWeight(SongModelView song, FieldDefinition field, SortDirection direction)
    {
        // Get the value of the property using our helper
        var propValue = SemanticQueryHelpers.GetComparableProp(song, field.PropertyName);

        double rawValue = 0;
        // Convert various types to a simple numeric value for weighting
        if (propValue is double d)
            rawValue = d;
        else if (propValue is int i)
            rawValue = i;
        else if (propValue is bool b)
            rawValue = b ? 1 : 0;
        else if (propValue is DateTimeOffset dto)
        {
            // Weight by how many days ago it was. A higher number is older.
            rawValue = (DateTimeOffset.UtcNow - dto).TotalDays;
        }

        // Handle direction. If descending (e.g., rating desc), a higher value is better.
        // If ascending (e.g., played asc), a higher value (more days ago) is better.
        // So, we need to invert for descending cases where a lower value is "better".
        if (field.PropertyName == nameof(SongModel.LastPlayed) && direction == SortDirection.Descending)
        {
            // This is a special case: "played desc" means we want RECENT songs.
            // A smaller number of days ago is better, so we invert the weight.
            // Add 1 to avoid division by zero.
            return 1.0 / (rawValue + 1.0);
        }

        // Add a small constant to avoid weights of 0, which would always result in a score of 0.
        return rawValue + 0.1;
    }
}
