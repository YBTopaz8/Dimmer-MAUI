using Dimmer.Data.ModelView;

namespace Dimmer.Interfaces;

/// <summary>
/// Platform-specific service for rendering and sharing song story cards
/// </summary>
public interface ISongStoryShareService
{
    /// <summary>
    /// Generates a shareable card image from story data
    /// </summary>
    /// <param name="storyData">Song story data</param>
    /// <returns>Path to generated card image file</returns>
    Task<string> GenerateStoryCardAsync(SongStoryData storyData);

    /// <summary>
    /// Shares the generated card using platform-specific sharing mechanism
    /// </summary>
    /// <param name="cardImagePath">Path to card image</param>
    /// <param name="shareText">Optional text to include with share</param>
    Task ShareStoryAsync(string cardImagePath, string? shareText = null);

    /// <summary>
    /// Shows lyrics selection UI and returns selected lines
    /// </summary>
    /// <param name="allLyrics">All available lyrics lines</param>
    /// <returns>Selected lyrics lines (max 5)</returns>
    Task<List<string>?> ShowLyricsSelectionAsync(List<string> allLyrics);
}
