namespace Dimmer.Data;

public class SongDetailNavArgs
{
    public required SongModelView Song { get; set; }
    public required BaseViewModel ViewModel { get; set; }
    public object? ExtraParam { get; set; }
}

public class ArtistDetailNavArgs
{
    public required ArtistModelView Artist { get; set; }
    public required BaseViewModel ViewModel { get; set; }
    public object? ExtraParam { get; set; }
}

public class AlbumDetailNavArgs
{
    public required SongModelView Album { get; set; }
    public required BaseViewModel ViewModel { get; set; }
    public object? ExtraParam { get; set; }
}

public class PlaylistDetailNavArgs
{
    public required PlaylistModelView Playlist { get; set; }
    public required BaseViewModel ViewModel { get; set; }
    public object? ExtraParam { get; set; }
}
