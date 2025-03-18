namespace Dimmer_MAUI.Utilities.Models;
public partial class PlaylistModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } 
    
    public string? Name { get; set; } = "Unknown Playlist";
    public double TotalDuration { get; set; }
    public double TotalSize { get; set; }
    public int TotalSongsCount { get; set; }
    public PlaylistModel(PlaylistModelView model)
    {
        Name = model.Name;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
        TotalSongsCount = model.TotalSongsCount;        
        LocalDeviceId = model.LocalDeviceId;
    }
    public PlaylistModel()
    {
        
    }
}

public partial class PlaylistSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; }
    public string? PlaylistId { get; set; }
    public string? SongId { get; set; }
    public PlaylistSongLink()
    {
        
    }
}


public partial class PlaylistModelView : ObservableObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateLocalDeviceID(nameof(PlaylistModelView));

    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial double TotalDuration { get; set; }
    [ObservableProperty]
    public partial double TotalSize { get; set; }
    [ObservableProperty]
    public partial int TotalSongsCount { get; set; }
    [ObservableProperty]
    public partial ObservableCollection<SongModelView>? DisplayedSongsFromPlaylist { get; set; }

    [ObservableProperty]
    public partial int? TotalPlayCount { get; set; } // Total number of times songs in this playlist have been played

    [ObservableProperty]
    public partial int? TotalCompletedPlays { get; set; } // Number of completed plays

    [ObservableProperty]
    public partial double? AveragePlayDuration { get; set; } // Average duration of completed plays (in seconds)

    [ObservableProperty]
    public partial SongModelView? MostPlayedSong { get; set; } // The most played song (can be null if no plays)

    [ObservableProperty]
    public partial SongModelView? LeastPlayedSong { get; set; } // The least played song

    [ObservableProperty]
    public partial PlayDataLink? TopArtist { get; set; } // Tuple for top artist and play count

    [ObservableProperty]
    public partial PlayDataLink? TopAlbum { get; set; } // Tuple for top album and play count

    [ObservableProperty]
    public partial Dictionary<string, int>? SongsCountByGenre { get; set; } // Dictionary of genre counts

    [ObservableProperty]
    public partial double? AverageSongRating { get; set; }  // Average rating of songs in the playlist

    [ObservableProperty]
    public partial List<SongModelView>? FavoriteSongs { get; set; } // List of favorite songs

    [ObservableProperty]
    public partial int? SongsWithLyricsCount { get; set; } // Count of songs with lyrics

    [ObservableProperty]
    public partial List<SongModelView>? SkippedSongs { get; set; }   // List of skipped songs

    [ObservableProperty]
    public partial Dictionary<int, int>? PlayTypeCounts { get; set; } // Counts for each play type (Play, Pause, Skip, etc.)

    [ObservableProperty]
    public partial int? MostActiveHour { get; set; } // Hour of the day with the most plays (-1 if none)

    [ObservableProperty]
    public partial int? AistinctArtistsCount { get; set; } // Number of distinct artists

    [ObservableProperty]
    public partial double? TotalPlaytimeFromEvents { get; set; } // Total playtime from play events (in seconds)

    [ObservableProperty]
    public partial double? AverageSkipPosition { get; set; }  // Average position (seconds) where songs are skipped.

    [ObservableProperty]
    public partial double? CompletedPercentage { get; set; }//Percentage of total plays completed

    [ObservableProperty]
    public partial double? NotCompletedPercentage { get; set; }// Percentage of total plays not completed

    public PlaylistModelView()
    {
        
    }

    public PlaylistModelView(PlaylistModel model)
    {        
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
        TotalSongsCount= model.TotalSongsCount;
    }
}