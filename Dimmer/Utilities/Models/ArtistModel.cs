namespace Dimmer_MAUI.Utilities.Models;

public partial class ArtistModel : RealmObject
{


    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(ArtistModel));
    public string? Name { get; set; } = "Unknown Artist";
    public string? Bio { get; set; }
    public string? ImagePath { get; set; }


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
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(ArtistModelView));
    
    [ObservableProperty]
    string? name;
    [ObservableProperty]
    string? imagePath;
    [ObservableProperty]
    string? bio;
    [ObservableProperty]
    bool isCurrentlySelected;


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