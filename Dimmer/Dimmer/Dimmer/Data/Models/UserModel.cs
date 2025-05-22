namespace Dimmer.Data.Models;
public partial class UserModel :RealmObject, IRealmObjectWithObjectId
{

    [PrimaryKey]
    public ObjectId Id { get; set; }
    public string? UserName { get; set; } 
    public string? UserEmail { get; set; } 
    public string? UserPassword { get; set; } 
    public string? UserProfileImage { get; set; } 
    public string? UserBio { get; set; } 
    public string? UserCountry { get; set; } 
    public string? UserLanguage { get; set; } 
    public string? UserTheme { get; set; } 
    public string? SessionToken { get; set; } = string.Empty;   
    public DateTimeOffset? UserDateCreated { get; set; } = DateTimeOffset.UtcNow;
    public string? DeviceName { get; set; }
    public string? DeviceFormFactor { get; set; }
    public string? DeviceModel { get; set; }
    public string? DeviceManufacturer { get; set; }
    public string? DeviceVersion { get; set; }

    
    public UserModel()
    {

    }

    
}
