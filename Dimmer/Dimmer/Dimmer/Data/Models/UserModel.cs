namespace Dimmer.Data.Models;
public partial class UserModel :RealmObject
{

    [PrimaryKey]
    public string LocalDeviceId { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; } 
    public string? UserEmail { get; set; } 
    public string? UserPassword { get; set; } 
    public string? UserProfileImage { get; set; } 
    public string? UserBio { get; set; } 
    public string? UserCountry { get; set; } 
    public string? UserLanguage { get; set; } 
    public string? UserTheme { get; set; } 
    public string? SessionToken { get; set; } = string.Empty;   
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
