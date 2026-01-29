namespace Dimmer.DimmerLive.Models;

/// <summary>
/// Parse-backed song comment model supporting public/private comments,
/// reactions, and timestamped notes
/// </summary>
[ParseClassName("SongComment")]
public class SongComment : ParseObject
{
    /// <summary>
    /// Pointer to the song (using original song ObjectId as string)
    /// </summary>
    [ParseFieldName("songId")]
    public string SongId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Song title for display/search purposes
    /// </summary>
    [ParseFieldName("songTitle")]
    public string SongTitle
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Artist name for display/search purposes
    /// </summary>
    [ParseFieldName("artistName")]
    public string ArtistName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// The author/creator of this comment
    /// </summary>
    [ParseFieldName("author")]
    public ParseUser Author
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Comment text content
    /// </summary>
    [ParseFieldName("text")]
    public string Text
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Optional timestamp in milliseconds for timestamped notes
    /// Null means it's a general comment
    /// </summary>
    [ParseFieldName("timestampMs")]
    public int? TimestampMs
    {
        get => GetProperty<int?>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Whether this comment is publicly visible
    /// </summary>
    [ParseFieldName("isPublic")]
    public bool IsPublic
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Reactions dictionary: key = emoji/reaction type, value = count
    /// Example: { "like": 12, "fire": 3, "sad": 1 }
    /// </summary>
    [ParseFieldName("reactions")]
    public IDictionary<string, int> Reactions
    {
        get => GetProperty<IDictionary<string, int>>() ?? new Dictionary<string, int>();
        set => SetProperty(value);
    }

    /// <summary>
    /// List of user IDs who have reacted with each reaction type
    /// Format: { "like": ["userId1", "userId2"], "fire": ["userId3"] }
    /// Used to prevent duplicate reactions from same user
    /// </summary>
    [ParseFieldName("reactionUsers")]
    public IDictionary<string, IList<string>> ReactionUsers
    {
        get => GetProperty<IDictionary<string, IList<string>>>() ?? new Dictionary<string, IList<string>>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Whether the comment is pinned (important/featured)
    /// </summary>
    [ParseFieldName("isPinned")]
    public bool IsPinned
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Optional image attachment path/URL
    /// </summary>
    [ParseFieldName("imagePath")]
    public string? ImagePath
    {
        get => GetProperty<string?>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Optional audio attachment path/URL
    /// </summary>
    [ParseFieldName("audioPath")]
    public string? AudioPath
    {
        get => GetProperty<string?>();
        set => SetProperty(value);
    }

    /// <summary>
    /// User rating (if applicable, 0-5 stars)
    /// </summary>
    [ParseFieldName("userRating")]
    public int UserRating
    {
        get => GetProperty<int>();
        set => SetProperty(value);
    }

    /// <summary>
    /// Optional color for the message/note
    /// </summary>
    [ParseFieldName("messageColor")]
    public string? MessageColor
    {
        get => GetProperty<string?>();
        set => SetProperty(value);
    }
}
