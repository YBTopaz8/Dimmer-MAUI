namespace Dimmer.Data.ModelView;
public partial class UserModelView:ObservableObject
{

    [ObservableProperty]
    public partial string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    
    [ObservableProperty]
    public partial string? UserName { get; set; } = "Unknown User";
    
    [ObservableProperty]
    public partial string? UserEmail { get; set; } = "Unknown Email";
    
    [ObservableProperty]
    public partial string? UserPassword { get; set; } = "Unknown Password";
    
    [ObservableProperty]
    public partial string? UserProfileImage { get; set; } = "user.png";
    
    [ObservableProperty]
    public partial string? UserBio { get; set; } = "Unknown Bio";
    
    [ObservableProperty]
    public partial string? UserCountry { get; set; } = "Unknown Country";
    
    [ObservableProperty]
    public partial string? UserLanguage { get; set; } = "Unknown Language";
    
    [ObservableProperty]
    public partial string? UserTheme { get; set; } = "Unknown Theme";
    
    [ObservableProperty]
    public partial string? UserDateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    
    [ObservableProperty]
    public partial string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    
    [ObservableProperty]
    public partial string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    
    [ObservableProperty]
    public partial string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    
    [ObservableProperty]
    public partial string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    
    [ObservableProperty]
    public partial string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;

}
