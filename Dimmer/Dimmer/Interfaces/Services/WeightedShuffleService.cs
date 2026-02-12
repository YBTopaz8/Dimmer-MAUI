using Dimmer.Data.Models;
using Dimmer.Interfaces;

namespace Dimmer.Interfaces.Services;

/// <summary>
/// Service for computing weighted shuffle based on user behavior and ratings
/// </summary>
public class WeightedShuffleService : IWeightedShuffleService
{
    private readonly Random _random = new();

    /// <summary>
    /// Calculates the weight for a given song based on user behavior and preferences.
    /// Weight factors:
    /// - Rating (0-5): multiply by 20 (0-100 points)
    /// - IsFavorite: +50 points
    /// - ManualFavoriteCount: +10 points per fave
    /// - PlayCompletedCount: +2 points per completion
    /// - SkipCount: -5 points per skip
    /// - LastPlayed recency: bonus for songs not played recently
    /// - Hidden songs: weight = 0
    /// </summary>
    public int CalculateWeight(SongModel song)
    {
        // Hidden songs should never be picked
        if (song.IsHidden)
        {
            return 0;
        }

        int weight = 100; // Base weight

        // Rating contribution (0-100)
        weight += song.Rating * 20;

        // Favorite status
        if (song.IsFavorite)
        {
            weight += 50;
        }

        // Manual favorite count
        weight += song.ManualFavoriteCount * 10;

        // Play completion bonus
        weight += song.PlayCompletedCount * 2;

        // Skip penalty
        weight -= song.SkipCount * 5;

        // Recency bonus - songs not played recently get a boost
        if (song.LastPlayed != default)
        {
            var daysSinceLastPlayed = (DateTimeOffset.UtcNow - song.LastPlayed).TotalDays;
            
            // Boost songs that haven't been played in a while (up to 30 days)
            if (daysSinceLastPlayed > 1)
            {
                var recencyBonus = Math.Min(daysSinceLastPlayed * 2, 60);
                weight += (int)recencyBonus;
            }
        }
        else
        {
            // Never played songs get a moderate boost
            weight += 30;
        }

        // Ensure weight is never negative
        return Math.Max(weight, 1); // Minimum weight of 1 for non-hidden songs
    }

    /// <summary>
    /// Performs a weighted shuffle using the weighted random sampling algorithm.
    /// Each song is selected with probability proportional to its weight.
    /// </summary>
    public List<T> WeightedShuffle<T>(IEnumerable<T> songs, Func<T, int> weightCalculator)
    {
        var songsList = songs.ToList();
        if (songsList.Count <= 1)
        {
            return songsList;
        }

        var result = new List<T>(songsList.Count);
        var remaining = new List<(T song, int weight)>(songsList.Count);

        // Calculate weights for all songs
        foreach (var song in songsList)
        {
            var weight = weightCalculator(song);
            if (weight > 0) // Only include songs with positive weight
            {
                remaining.Add((song, weight));
            }
        }

        // Weighted random selection
        while (remaining.Count > 0)
        {
            // Calculate total weight
            long totalWeight = 0;
            foreach (var (_, weight) in remaining)
            {
                totalWeight += weight;
            }

            if (totalWeight == 0)
            {
                // All remaining songs have 0 weight (shouldn't happen but handle it)
                break;
            }

            // Pick a random number in [0, totalWeight)
            var randomValue = (long)(_random.NextDouble() * totalWeight);

            // Find which song this maps to
            long cumulativeWeight = 0;
            int selectedIndex = -1;
            for (int i = 0; i < remaining.Count; i++)
            {
                cumulativeWeight += remaining[i].weight;
                if (randomValue < cumulativeWeight)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // Add selected song to result and remove from remaining
            if (selectedIndex >= 0)
            {
                result.Add(remaining[selectedIndex].song);
                remaining.RemoveAt(selectedIndex);
            }
            else
            {
                // Fallback: shouldn't happen but take last if calculation error
                result.Add(remaining[^1].song);
                remaining.RemoveAt(remaining.Count - 1);
            }
        }

        return result;
    }
}
