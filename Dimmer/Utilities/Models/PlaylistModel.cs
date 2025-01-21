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
    public string? LocalDeviceId { get; set; }

    [ObservableProperty]
    string? name;
    [ObservableProperty]
    double totalDuration;
    [ObservableProperty]
    double totalSize;
    [ObservableProperty]
    int totalSongsCount;

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