namespace Dimmer.Data.ModelView;
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

    [ObservableProperty]
    public partial string DateCreated { get; set; } = DateTime.UtcNow.ToString("o");

    [ObservableProperty]
    public partial bool IsNew { get; set; }

    [ObservableProperty]
    public partial ObservableCollection<SongModel>? SongInPlaylist { get; set; }

    [ObservableProperty]
    public partial SongModel? CurrentSong { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial string? CoverImagePath { get; set; }

    [ObservableProperty]
    public partial string? Color { get; set; }

    [ObservableProperty]
    public partial string? PlaylistType { get; set; } = "General";


    [ObservableProperty]
    public partial ObservableCollection<PlaylistEventView>? PlaylistEvents { get; set; }

    [ObservableProperty]
    public partial string? DeviceName { get; set; }


    [ObservableProperty]
    public partial UserModel? User { get; set; }

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
    public partial SongModel? EventSong { get; set; }
}