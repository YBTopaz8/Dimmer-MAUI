namespace Dimmer.Data.ModelView;

/// <summary>
/// Data model for song story/card sharing
/// Contains all necessary information to generate a shareable card
/// </summary>
public class SongStoryData
{
    /// <summary>
    /// Song title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Artist name
    /// </summary>
    public string ArtistName { get; set; } = string.Empty;

    /// <summary>
    /// Album name
    /// </summary>
    public string AlbumName { get; set; } = string.Empty;

    /// <summary>
    /// Path to cover image file
    /// </summary>
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// Selected lyrics lines (max 5)
    /// </summary>
    public List<string> SelectedLyrics { get; set; } = new();

    /// <summary>
    /// Dominant color extracted from cover art
    /// Falls back to darkslateblue (#483D8B) if no cover
    /// </summary>
    public Color BackgroundColor { get; set; } = Color.FromArgb("#483D8B");

    /// <summary>
    /// Contrasting text color for readability
    /// </summary>
    public Color TextColor { get; set; } = Colors.White;

    /// <summary>
    /// Whether the song has lyrics
    /// </summary>
    public bool HasLyrics { get; set; }

    /// <summary>
    /// Original song model ID for reference
    /// </summary>
    public string? SongId { get; set; }
}
