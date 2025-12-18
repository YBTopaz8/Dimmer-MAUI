namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public partial class PlaylistModelView : ObservableObject
{

    [ObservableProperty]
    public partial string PlaylistName { get; set; } = "Unknown Playlist";
    /// <summary>
    /// Gets or sets the date created.
    /// </summary>
    /// <value>
    /// The date created.
    /// </value>

    public bool IsSmartPlaylist { get; set; }
    [ObservableProperty]
    public partial string DateCreated { get; set; } = DateTime.UtcNow.ToString("o");

    public string QueryText { get; set; } = string.Empty;
    [ObservableProperty]
    public partial bool IsNew { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModelView?>? SongInPlaylist { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<ObjectId>? SongsIdsInPlaylist { get; set; }

    [ObservableProperty]
    public partial SongModelView? CurrentSong { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial string? CoverImagePath { get; set; }

    [ObservableProperty]
    public partial string? Color { get; set; }

    [ObservableProperty]
    public partial string? PlaylistType { get; set; } = "General";


    [ObservableProperty]
    public partial ObservableCollection<PlaylistEventView?>? PlaylistEvents { get; set; }

    [ObservableProperty]
    public partial string? DeviceName { get; set; }


    [ObservableProperty]
    public partial UserModelView? User { get; set; }

    [ObservableProperty]
    public partial ObjectId Id { get; set; }
}

public partial class PlaylistEventView : ObservableObject
{
    [ObservableProperty]
    public partial PlayType PlayType { get; set; }
    [ObservableProperty]
    public partial DateTimeOffset? DateCreated { get; set; } = DateTimeOffset.UtcNow;
    [ObservableProperty]
    public partial SongModelView? EventSong { get; set; }
}