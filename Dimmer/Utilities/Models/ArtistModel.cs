namespace Dimmer_MAUI.Utilities.Models;

public partial class ArtistModel : RealmObject
{


    [PrimaryKey]
    public string? LocalDeviceId { get; set; }
    public string? Name { get; set; } = "Unknown Artist";
    public string? Bio { get; set; }
    public string? ImagePath { get; set; } = "lyricist.png";


    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public ArtistModel()
    {
        

    }

    public ArtistModel(ArtistModelView model)
    {        
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        ImagePath = model.ImagePath;
        Bio = model.Bio;
    }

}

// ViewModel for ArtistModel
public partial class ArtistModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial string? ImagePath { get; set; }
    [ObservableProperty]
    public partial string? Bio { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }


    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public ArtistModelView()
    {
    }

    public ArtistModelView(ArtistModel model)
    {        
        LocalDeviceId = model.LocalDeviceId;
        Name = model.Name;
        ImagePath = model.ImagePath;     
        Bio = model.Bio;
        
    }


}

public partial class ArtistGroup : ObservableCollection<ArtistModelView>
{
    public required string FirstLetter { get; set; }
    
}
