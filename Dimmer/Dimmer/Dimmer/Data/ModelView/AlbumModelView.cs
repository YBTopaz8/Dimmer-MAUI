namespace Dimmer.Data.ModelView;
public partial class AlbumModelView : ObservableObject
{
    [ObservableProperty]
    public partial string? Name { get; set; }
    [ObservableProperty]
    public partial string? LocalDeviceId { get; set; }
    [ObservableProperty]
    public partial int? ReleaseYear { get; set; }
    [ObservableProperty]
    public partial int NumberOfTracks { get; set; }
    [ObservableProperty]
    public partial string? TotalDuration { get; set; }
    [ObservableProperty]
    public partial string? Description { get; set; }
    [ObservableProperty]
    public partial string? AlbumImagePath { get; set; }

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    [ObservableProperty]
    public partial bool IsCurrentlySelected { get; set; }
}
