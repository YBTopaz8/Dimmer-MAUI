namespace Dimmer_MAUI.Utilities.Models;
public partial class GenreModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(GenreModel));
    public BaseEmbedded? Instance { get; set; } // = new ();
    public string? Name { get; set; } = "Unknown Genre";
    public GenreModel()
    {
        Instance = new BaseEmbedded();
    }

    public GenreModel(GenreModelView modelView)
    {
        
        Name = modelView.Name;
        Instance = new BaseEmbedded();

    }


}

public partial class AlbumArtistGenreSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumArtistGenreSongLink));
    public BaseEmbedded? Instance { get; set; } // = new ();
    public string? SongId { get; set; }
    public string? AlbumId { get; set; }
    public string? ArtistId { get; set; }
    public string? GenreId { get; set; }
    
    public AlbumArtistGenreSongLink(AlbumArtistGenreSongLinkView model)
    {
        SongId = model.SongId;
        AlbumId = model.AlbumId;
        ArtistId = model.ArtistId;
        GenreId = model.GenreId;
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
    }

    public AlbumArtistGenreSongLink()
    {
        Instance = new BaseEmbedded();

    }
}


public partial class AlbumArtistGenreSongLinkView: ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumArtistGenreSongLinkView));

    [ObservableProperty]
    BaseEmbeddedView? instance = new();
    public string? SongId { get; set; }
    public string? AlbumId { get; set; }
    public string? ArtistId { get; set; }
    public string? GenreId { get; set; }
    
    public AlbumArtistGenreSongLinkView()
    {
        
    }
    public AlbumArtistGenreSongLinkView(AlbumArtistGenreSongLink model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        SongId = model.SongId;
        AlbumId = model.AlbumId;
        ArtistId = model.ArtistId;
        GenreId = model.GenreId;
     
    }
}

public partial class GenreModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(GenreModelView));
    [ObservableProperty]
    BaseEmbeddedView? instance = new();
    [ObservableProperty]
    string? name;
    [ObservableProperty]
    bool isCurrentlySelected;
    public GenreModelView(GenreModel model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        name = model.Name;
    }
    public GenreModelView()
    {

    }
}

