namespace Dimmer.Data.Models;
public partial class UserModel :RealmObject
{

    [PrimaryKey]
    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; } = "Unknown User";
    public string? UserEmail { get; set; } = "Unknown Email";
    public string? UserPassword { get; set; } = "Unknown Password";
    public string? UserProfileImage { get; set; } = "user.png";
    public string? UserBio { get; set; } = "Unknown Bio";
    public string? UserCountry { get; set; } = "Unknown Country";
    public string? UserLanguage { get; set; } = "Unknown Language";
    public string? UserTheme { get; set; } = "Unknown Theme";
    public string? UserDateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;

    
    public UserModel()
    {

    }
}
