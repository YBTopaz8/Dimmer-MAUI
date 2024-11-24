﻿namespace Dimmer_MAUI.Utilities.Models;

public partial class AlbumModel : RealmObject
{
    public BaseEmbedded Instance { get; set; } // = new ();

    public string? Name { get; set; } = "Unknown Album";
    public int? ReleaseYear { get; set; }
    public string? Description { get; set; }
    public int NumberOfTracks { get; set; }
    public string? ImagePath { get; set; }
    public string? TotalDuration { get; set; }

    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumModel));

    public AlbumModel()
    {
        Instance = new BaseEmbedded();
        LocalDeviceId = (nameof(AlbumModel));
    }

    public AlbumModel(AlbumModelView model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        ReleaseYear = model.ReleaseYear;
        NumberOfTracks = model.NumberOfTracks;
        ImagePath = model.AlbumImagePath;
        
    }

}
public partial class AlbumModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumModelView));

    [ObservableProperty]
    BaseEmbeddedView instance = new();
    [ObservableProperty]
    string? name;
    [ObservableProperty]
    int? releaseYear;
    [ObservableProperty]
    int numberOfTracks;
    [ObservableProperty]
    string? totalDuration;
    [ObservableProperty]
    string? description;
    [ObservableProperty]
    string? albumImagePath;

    [ObservableProperty]
    bool isCurrentlySelected;
    public AlbumModelView(AlbumModel model)
    {

        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        NumberOfTracks = model.NumberOfTracks;
        AlbumImagePath = model.ImagePath;
        ReleaseYear = model.ReleaseYear;
        TotalDuration = model.TotalDuration;
    }

    public AlbumModelView()
    {
        
    }

 
}
