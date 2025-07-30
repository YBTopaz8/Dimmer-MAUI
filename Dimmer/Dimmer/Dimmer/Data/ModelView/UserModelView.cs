
namespace Dimmer.Data.ModelView;
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
    public bool UserHasAccount
    {
        get
        {
            return string.IsNullOrEmpty(Email);
        }

        set;
    }


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

public partial class LastFMUserView : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the user name.
    /// </summary>
    [ObservableProperty]
    public partial string? Name { get; set; }

    /// <summary>
    /// Gets or sets the real name.
    [ObservableProperty]
    public partial string? RealName { get; set; }

    /// <summary>
    /// Gets or sets the url.
    [ObservableProperty]
    public partial string? Url { get; set; }

    /// <summary>
    /// Gets or sets the country.
    [ObservableProperty]
    public partial string? Country { get; set; }

    /// <summary>
    /// Gets or sets the age.
    [ObservableProperty]
    public partial int Age { get; set; }

    /// <summary>
    /// Gets or sets the gender.
    [ObservableProperty]
    public partial string? Gender { get; set; }

    /// <summary>
    /// Gets or sets the total playcount.
    [ObservableProperty]
    public partial int Playcount { get; set; }

    /// <summary>
    /// Gets or sets the number of playlists.
    [ObservableProperty]
    public partial int Playlists { get; set; }

    /// <summary>
    /// Gets or sets the date of registration.
    [ObservableProperty]
    public partial DateTimeOffset Registered { get; set; }

    /// <summary>
    /// Gets or sets the type.
    [ObservableProperty]
    public partial string? Type { get; set; }

    /// <summary>
    /// Gets or sets a list of images.
    [ObservableProperty]
    public partial LastImageView Image { get; set; }
    public  partial class LastImageView : ObservableObject
    {
        /// <summary>
        /// Gets or sets the image size.
        /// </summary>
        [ObservableProperty]
        public partial string? Size { get; set; }

        /// <summary>
        /// Gets or sets the image url.
        /// </summary>
        [ObservableProperty]
        public partial string? Url { get; set; }
    }

    #endregion



}