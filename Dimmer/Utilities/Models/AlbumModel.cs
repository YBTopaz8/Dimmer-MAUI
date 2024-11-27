namespace Dimmer_MAUI.Utilities.Models;

public partial class AlbumModel : RealmObject
{


    public string? Name { get; set; } = "Unknown Album";
    public int? ReleaseYear { get; set; }
    public string? Description { get; set; }
    public int NumberOfTracks { get; set; }
    public string? ImagePath { get; set; }
    public string? TotalDuration { get; set; }

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(AlbumModel));

    public AlbumModel()
    {
        
        LocalDeviceId = (nameof(AlbumModel));
    }

    public AlbumModel(AlbumModelView model)
    {
        
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

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [ObservableProperty]
    bool isCurrentlySelected;
    public AlbumModelView(AlbumModel model)
    {

        
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
