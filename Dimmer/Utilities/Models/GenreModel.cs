namespace Dimmer_MAUI.Utilities.Models;
public partial class GenreModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(GenreModel));

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public string? Name { get; set; } = "Unknown Genre";
    public GenreModel()
    {
        
    }

    public GenreModel(GenreModelView modelView)
    {
        
        Name = modelView.Name;
        

    }


}

public partial class AlbumArtistGenreSongLink : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumArtistGenreSongLink));
    
    public string? SongId { get; set; }
    public string? AlbumId { get; set; }
    public string? ArtistId { get; set; }
    public string? GenreId { get; set; }

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public AlbumArtistGenreSongLink(AlbumArtistGenreSongLinkView model)
    {
        SongId = model.SongId;
        AlbumId = model.AlbumId;
        ArtistId = model.ArtistId;
        GenreId = model.GenreId;
        
        LocalDeviceId = model.LocalDeviceId;
    }

    public AlbumArtistGenreSongLink()
    {
        

    }
}


public partial class AlbumArtistGenreSongLinkView: ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumArtistGenreSongLinkView));
        
    public string? SongId { get; set; }
    public string? AlbumId { get; set; }
    public string? ArtistId { get; set; }
    public string? GenreId { get; set; }
    
    public AlbumArtistGenreSongLinkView()
    {
        
    }
    public AlbumArtistGenreSongLinkView(AlbumArtistGenreSongLink model)
    {
        
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
    string? name;
    [ObservableProperty]
    bool isCurrentlySelected;
    public GenreModelView(GenreModel model)
    {
        
        LocalDeviceId = model.LocalDeviceId;
        name = model.Name;
    }
    public GenreModelView()
    {

    }
}

