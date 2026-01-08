namespace Dimmer.ViewsAndPages.NativeViews.SectionHeader;

/// <summary>
/// Represents a section header in the songs list.
/// Headers are computed based on the current sort mode.
/// </summary>
public class SectionHeaderModel
{
    /// <summary>
    /// Display text for the header (e.g., "A", "2024-01", "Shuffle")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Position in the flattened adapter (header + songs)
    /// </summary>
    public int AdapterPosition { get; set; }

    /// <summary>
    /// Starting index in the songs collection that this header represents
    /// </summary>
    public int SongStartIndex { get; set; }

    /// <summary>
    /// Number of songs in this section
    /// </summary>
    public int SongCount { get; set; }

    /// <summary>
    /// Type of section grouping used
    /// </summary>
    public SectionType Type { get; set; }
}

/// <summary>
/// Defines how songs are grouped into sections
/// </summary>
public enum SectionType
{
    /// <summary>
    /// Alphabetical sections (A, B, C...)
    /// </summary>
    Alphabetical,

    /// <summary>
    /// Date-based sections (YYYY-MM or specific dates)
    /// </summary>
    DateBased,

    /// <summary>
    /// Single shuffle header
    /// </summary>
    Shuffle,

    /// <summary>
    /// No sections (fallback)
    /// </summary>
    None
}
