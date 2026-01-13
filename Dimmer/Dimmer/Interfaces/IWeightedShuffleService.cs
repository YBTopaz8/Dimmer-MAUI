using Dimmer.Data.Models;

namespace Dimmer.Interfaces;

/// <summary>
/// Service for computing weighted shuffle based on user behavior and ratings
/// </summary>
public interface IWeightedShuffleService
{
    /// <summary>
    /// Calculates the weight for a given song based on user behavior and preferences.
    /// Hidden songs return weight 0.
    /// </summary>
    /// <param name="song">The song to calculate weight for</param>
    /// <returns>Weight value >= 0, where 0 means the song should never be selected</returns>
    int CalculateWeight(SongModel song);

    /// <summary>
    /// Performs a weighted shuffle on a list of songs.
    /// Songs with higher weights have higher probability of appearing earlier.
    /// </summary>
    /// <typeparam name="T">Type of song model (SongModel or SongModelView)</typeparam>
    /// <param name="songs">List of songs to shuffle</param>
    /// <param name="weightCalculator">Function to calculate weight for each song</param>
    /// <returns>New list with songs in weighted shuffle order</returns>
    List<T> WeightedShuffle<T>(IEnumerable<T> songs, Func<T, int> weightCalculator);
}
