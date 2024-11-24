namespace Dimmer_MAUI.Utilities.Models;
public partial class PlaylistModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(PlaylistModel));
    public BaseEmbedded? Instance { get; set; } // = new ();
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
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
    }
    public PlaylistModel()
    {
        Instance = new BaseEmbedded();
    }
}

public partial class PlaylistSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(PlaylistSongLink));
    public BaseEmbedded? Instance { get; set; } // = new ();

    public string? PlaylistId { get; set; }
    public string? SongId { get; set; }
    public PlaylistSongLink()
    {
        Instance = new BaseEmbedded();
    }
}


public partial class PlaylistModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(PlaylistModelView));

    [ObservableProperty]
    BaseEmbeddedView instance = new();
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
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        TotalDuration = model.TotalDuration;
        TotalSize = model.TotalSize;
        TotalSongsCount= model.TotalSongsCount;
    }
}