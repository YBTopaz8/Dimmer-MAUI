
namespace Dimmer.Data.ModelView;

[Utils.Preserve(AllMembers = true)]
public partial class UserModelView : ObservableObject
{

    [ObservableProperty]
    public partial ObjectId Id { get; set; }

    [ObservableProperty]
    public partial string? Username { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Email { get; set; } = string.Empty;


    [ObservableProperty]
    public partial bool IsNew { get; set; }
 


    [ObservableProperty]
    public partial string? Password { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? UserProfileImage { get; set; } = "user.png";

    [ObservableProperty]
    public partial string? UserBio { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? UserCountry { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? UserLanguage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? UserTheme { get; set; } = string.Empty;

    [ObservableProperty]
    public partial DateTimeOffset? UserDateCreated { get; set; } = DateTimeOffset.UtcNow;

    [ObservableProperty]
    public partial string? DeviceName { get; set; }

    [ObservableProperty]
    public partial string? DeviceFormFactor { get; set; }

    [ObservableProperty]
    public partial string? DeviceModel { get; set; }

    [ObservableProperty]
    public partial string? DeviceManufacturer { get; set; }

    [ObservableProperty]
    public partial string? DeviceVersion { get; set; }

    [ObservableProperty]
    public partial LastFMUserView LastFMAccountInfo { get; set; } = new LastFMUserView();
    [ObservableProperty]
    public partial ParseFile? ProfileImageFile { get;  set; }
}

