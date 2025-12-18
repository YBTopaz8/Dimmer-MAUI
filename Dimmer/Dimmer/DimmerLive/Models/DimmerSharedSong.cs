namespace Dimmer.DimmerLive.Models;
[ParseClassName("DimmerSharedSong")]
public class DimmerSharedSong : ParseObject
{
    [ParseFieldName("Title")]
    public string Title
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("ArtistName")]
    public string ArtistName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("AlbumName")]
    public string AlbumName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("GenreName")]
    public string GenreName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("DurationSeconds")]
    public double? DurationSeconds
    {
        get => GetProperty<double?>();
        set => SetProperty(value);
    }

    [ParseFieldName("IsFavorite")]
    public bool IsFavorite
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }

    [ParseFieldName("IsPlaying")]
    public bool IsPlaying
    {
        get => GetProperty<bool>();
        set => SetProperty(value);
    }
    
    [ParseFieldName("sharedPositionInSeconds")]
    public double? SharedPositionInSeconds
    {
        get => GetProperty<double?>();
        set => SetProperty(value);
    }

    [ParseFieldName("audioFile")]
    public ParseFile AudioFile
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("AudioFileUrl")]
    public Uri AudioFileUrl
    {
        get => GetProperty<Uri>();
        set => SetProperty(value);
    }

    [ParseFieldName("AudioFileName")]
    public string AudioFileName
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("AudioFileMimeType")]
    public string AudioFileMimeType
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    [ParseFieldName("coverArtFile")]
    public ParseFile CoverArtFile
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("uploader")]
    public ParseUser Uploader
    {
        get => GetProperty<ParseUser>();
        set => SetProperty(value);
    }

    [ParseFieldName("unreadCounts")]
    public IDictionary<string, int> UnreadCounts
    {
        get => GetProperty<IDictionary<string, int>>();
        set => SetProperty(value);
    }

    // NEW: For group chat avatar (optional)
    [ParseFieldName("groupAvatar")]
    public ParseFile GroupAvatar
    {
        get => GetProperty<ParseFile>();
        set => SetProperty(value);
    }

    [ParseFieldName("audioMimeType")]
    public string AudioMimeType
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    // NEW: External source URL if the song is from a streaming service (optional)
    [ParseFieldName("ExternalUrl")]
    public string ExternalUrl
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }

    // NEW: Lyrics (if you want to store them with the shared song)
    [ParseFieldName("LyricsText")]
    public string LyricsText
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
    [ParseFieldName("OriginalSongId")]
    public string OriginalSongId
    {
        get => GetProperty<string>();
        set => SetProperty(value);
    }
}