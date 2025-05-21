namespace Dimmer.Data.ModelView;

public partial class ArtistModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? Id { get; set; } = string.Empty;
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial string? ImagePath { get; set; }
    [ObservableProperty]
    public partial string? Bio { get; set; }
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
    [ObservableProperty]
    public partial bool IsVisible { get; set; }


    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
}