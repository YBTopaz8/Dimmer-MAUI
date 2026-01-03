using Dimmer.Data.ModelView;

namespace Dimmer.Interfaces;

/// <summary>
/// Service for creating song story/card data for sharing
/// </summary>
public interface ISongStoryService
{
    /// <summary>
    /// Prepares song story data from a song model
    /// Extracts colors, prepares metadata
    /// </summary>
    /// <param name="song">Song to create story from</param>
    /// <param name="selectedLyrics">Optional pre-selected lyrics lines (max 5)</param>
    /// <returns>Story data ready for rendering</returns>
    Task<SongStoryData> PrepareSongStoryAsync(SongModelView song, List<string>? selectedLyrics = null);

    /// <summary>
    /// Extracts dominant color from cover image
    /// </summary>
    /// <param name="imagePath">Path to cover image</param>
    /// <returns>Dominant color or darkslateblue fallback</returns>
    Task<Color> ExtractDominantColorAsync(string? imagePath);

    /// <summary>
    /// Calculates contrasting text color for given background
    /// </summary>
    /// <param name="backgroundColor">Background color</param>
    /// <returns>Black or white for optimal contrast</returns>
    Color GetContrastingTextColor(Color backgroundColor);
}
