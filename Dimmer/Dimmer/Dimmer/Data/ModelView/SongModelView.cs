namespace Dimmer.Data.ModelView;
public partial class SongModelView : ObservableObject
{
    public SongModelView()
    {
        PrecomputeSearchableText();
    }
    [ObservableProperty]
    public partial ObjectId Id { get; set; }
    [ObservableProperty]
    public partial string? Title { get; set; }
    [ObservableProperty]
    public partial string? ArtistName { get; set; }
 
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView>? AllArtists { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<AlbumModelView>? AllAlbums { get; set; }

    [ObservableProperty]
    public partial string? AlbumName { get; set; }

    [ObservableProperty]
    public partial AlbumModelView? Album { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<ArtistModelView?> ArtistIds { get; set; }
    [ObservableProperty]
    public partial GenreModelView? Genre { get; set; }
    [ObservableProperty]
    public partial string? GenreName { get; set; }
    [ObservableProperty]
    public partial string FilePath { get; set; } = string.Empty;
    [ObservableProperty]
    public partial double DurationInSeconds { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial int? TrackNumber { get; set; }
    [ObservableProperty]
    public partial string FileFormat { get; set; } = string.Empty;
    [ObservableProperty]
    public partial long FileSize { get; set; }
    [ObservableProperty]
    public partial int? BitRate { get; set; }
    [ObservableProperty]
    public partial int Rating { get; set; } = 0;
    [ObservableProperty]
    public partial bool HasLyrics { get; set; }
    [ObservableProperty]
    public partial bool HasSyncedLyrics { get; set; }
    [ObservableProperty]
    public partial string SyncLyrics { get; set; } = string.Empty;
    
    [ObservableProperty]
    public partial byte[]? CoverImageBytes { get; set; }
    [ObservableProperty]
    public partial string CoverImagePath { get; set; } = "musicnoteslider.png";
    [ObservableProperty]
    public partial string UnSyncLyrics { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentPlayingHighlight { get; set; } = false;
    [ObservableProperty]
    public partial bool IsFavorite { get; set; }
    [ObservableProperty]
    public partial string Achievement { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsFileExists { get; set; } = true;
    [ObservableProperty]
    public partial DateTimeOffset? LastDateUpdated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial string? DeviceName { get; set; }
    [ObservableProperty]
    public partial string? DeviceFormFactor { get; set; }
    [ObservableProperty]
    public partial string? DeviceModel { get; set; }
    [ObservableProperty]
    public partial string? DeviceManufacturer { get; set; }
    [ObservableProperty]
    public partial string? DeviceVersion { get; set; }
    [ObservableProperty]
    public partial string OtherArtistsName { get; set; }


    [ObservableProperty]
    public partial string Lyricist { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Composer { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Conductor { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Description { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string Language { get; set; } = string.Empty;
    [ObservableProperty]
    public partial int? DiscNumber { get; set; }
    [ObservableProperty]
    public partial int? DiscTotal { get; set; }
    [ObservableProperty]
    public partial int? UserIDOnline { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<DimmerPlayEventView>? PlayEvents { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SyncLyricsView> EmbeddedSync { get; set; } = new();


    [ObservableProperty]
    public partial ObservableCollection<UserNoteModelView>? UserNote { get; set; }
    // Override Equals to compare based on string
    public override bool Equals(object? obj)
    {
        if (obj is SongModelView other)
        {
            return this.Id == other.Id;
        }

        return false;
    }

    [ObservableProperty]
    public partial string SearchableText { get; private set; }

    // This method is called after the object is created and its properties are set.
    public void PrecomputeSearchableText()
    {
        var allNotes=UserNote?.Select(note => note.UserMessageText ?? string.Empty).ToString()?? string.Empty;

        var sb = new StringBuilder();
        sb.Append(Title?.ToLowerInvariant()).Append(' ');
        sb.Append(OtherArtistsName?.ToLowerInvariant()).Append(' ');
        sb.Append(AlbumName?.ToLowerInvariant()).Append(' ');
        sb.Append(UnSyncLyrics?.ToLowerInvariant()).Append(' ');
        sb.Append(SyncLyrics?.ToLowerInvariant()).Append(' ');
        sb.Append(Genre?.Name?.ToLowerInvariant()).Append(' '); // Example of adding more
        sb.Append(allNotes?.ToLowerInvariant()).Append(' '); // Example of adding more

        SearchableText = sb.ToString();
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Id);
    }
}

public partial class UserNoteModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? UserMessageText { get; set; }
    [ObservableProperty]
    public partial string? UserMessageImagePath { get; set; }
    [ObservableProperty]
    public partial string? UserMessageAudioPath { get; set; }
    [ObservableProperty]
    public partial bool IsPinned { get; set; }
    [ObservableProperty]
    public partial int UserRating { get; set; }
    [ObservableProperty]
    public partial string? MessageColor { get; set; }
}
public class SyncLyricsView
{
    public int TimestampMs { get; set; }
    public string Text { get; set; }

    public SyncLyricsView(int timestampMs, string text)
    {
        TimestampMs = timestampMs;
        Text = text;
    }
    public SyncLyricsView(SyncLyrics syncLyricsDB)
    {
            Text= syncLyricsDB.Text;
            TimestampMs = syncLyricsDB.TimestampMs;

    }
}