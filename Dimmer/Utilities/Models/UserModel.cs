namespace Dimmer_MAUI.Utilities.Models;

public partial class ActionPending : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(ActionPending));    
    public required int Actionn { get; set; }=0; // 0 for add, 1 for update, 2 for delete
    public required int TargetType { get; set; }= 0; // 0 for song, 1 for artist, 2 for album, 3 for genre, 4 playlist, 5 user, 6 AAGS link,7 DatePlayedAndWasCompletedSongLink
    public SongModel? ActionSong { get; set; }
    public ArtistModel? ActionArtist { get; set; }
    public AlbumModel? ActionAlbum { get; set; }
    public GenreModel? ActionGenre { get; set; }
    public PlaylistModel? ActionPlaylist { get; set; }
    public UserModel? ActionUser { get; set; }
    public PlayDateAndCompletionStateSongLink? ActionPlayDateAndCompletionStateSongLink { get; set; }
    public AlbumArtistGenreSongLink? ActionListOfAlbumArtistGenreSongLink { get; set; }

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public DateTimeOffset DateRequested { get; set; } = DateTime.UtcNow;
    public bool IsRequestedByUser { get; set; }
    public bool IsCompleted { get; set; }=false;
    public required bool ApplyToAllThisDeviceOnly { get; set; } = true;
    public bool IsBatch { get; set; }
    public bool IsAuthenticated { get; set; } = false;
    public string? AdditionalNotes { get; set; } = string.Empty;
    
    public ActionPending()
    {
        
    }
}

public partial class UserModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(UserModelView));
   
    [ObservableProperty]
    bool isAuthenticated;
    [ObservableProperty]
    string? userIDOnline;
    [ObservableProperty]
    string? userName = "User";
    [ObservableProperty]
    string? userPassword;
    [ObservableProperty]
    string? userEmail;
    [ObservableProperty]
    bool? isLoggedInLastFM;
    [ObservableProperty]
    string? coverImage = string.Empty;    
    [ObservableProperty]
    DateTimeOffset lastSessionDate;

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public UserModelView(UserModel model)
    {
        
        LocalDeviceId = model.LocalDeviceId;
        UserName = model.UserName;
        userEmail = model.UserEmail;
        userPassword = model.UserPassword;
        coverImage = model.CoverImage;
        lastSessionDate = model.LastSessionDate;
    }
    public UserModelView()
    {
        
    }
    public UserModelView(ParseUser model)
    {
        UserEmail = model.Email;
        UserName = model.Username;
        UserIDOnline = model.ObjectId;
        IsAuthenticated = model.IsAuthenticated;
    }
}

public class UserModel : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(UserModel));

    public string? DateCreated { get; set; } = DateTime.UtcNow.ToString("o");
    public string? DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string? DeviceFormFactor { get; set; } = DeviceInfo.Current.Idiom.ToString();
    public string? DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string? DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string? DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;
    public string? UserName { get; set; } = "User";
    public string? UserPassword { get; set; }
    public string? UserEmail { get; set; }
    public string? UserIDOnline { get; set; }
    public string? CoverImage { get; set; } = string.Empty;
    public DateTimeOffset LastSessionDate { get; set; } = DateTimeOffset.UtcNow;
    public UserModel(UserModelView model)
    {
        
        LocalDeviceId = model.LocalDeviceId;
        UserName = model.UserName;
        UserPassword = model.UserPassword;
        UserEmail = model.UserEmail;
        CoverImage = model.CoverImage;
        LastSessionDate = model.LastSessionDate;
    }
    public UserModel()
    {
        
    }
    public async Task SyncOnlineAsync()
    {
        await Task.CompletedTask;
    }




}


// LAST FM SECTION

public class AuthData
{
    // Add your credentials for testing or use the command line args.
    string TEST_API_KEY = APIKeys.LASTFM_API_KEY;
    string TEST_API_SECRET = APIKeys.LASTFM_API_SECRET;

    public string ApiKey { get; set; }

    public string ApiSecret { get; set; }

    public string User { get; set; }

    public string Password { get; set; }

    public string SessionKey { get; set; }

    public void Print()
    {
        Console.WriteLine("API key    : {0}", ApiKey);

        if (!string.IsNullOrEmpty(ApiSecret))
        {
            Console.WriteLine("API secret : {0}", ApiSecret);
        }

        if (!string.IsNullOrEmpty(SessionKey))
        {
            Console.WriteLine("Session key: {0}", SessionKey);
        }

        if (!string.IsNullOrEmpty(User))
        {
            Console.WriteLine("User       : {0}", User);
        }

        if (!string.IsNullOrEmpty(User))
        {
            Console.WriteLine("Password   : {0}", Password);
        }
    }

    public static bool Validate(AuthData data, bool userAuth = false)
    {


        if (string.IsNullOrEmpty(data.ApiKey))
        {
            return false;
        }

        if (!userAuth)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(data.ApiSecret))
        {
            return true;
        }
        return !string.IsNullOrEmpty(data.User) && !string.IsNullOrEmpty(data.Password);
    }


    public static AuthData SetAPIData(string apiKey, string apiSecret)
    {
        return new AuthData()
        {
            ApiKey = apiKey,
            ApiSecret = apiSecret
        };
    }
    public static AuthData SetUNameAndUPass(string user, string password)
    {
        return new AuthData()
        {
            User = user,
            Password = password
        };
    }

    public static AuthData Create(string[] args)
    {
        var auth = new AuthData()
        {
            ApiKey = APIKeys.LASTFM_API_KEY,
            ApiSecret = APIKeys.LASTFM_API_SECRET
        };

        int length = args.Length;

        for (int i = 0; i < length; i++)
        {
            string s = args[i];

            if (s == "-u" || s == "--user")
            {
                if (i < length - 1)
                    auth.User = args[++i];
            }
            else if (s == "-p" || s == "--password")
            {
                if (i < length - 1)
                    auth.Password = args[++i];
            }
            else if (s == "-k" || s == "--api-key")
            {
                if (i < length - 1)
                    auth.ApiKey = args[++i];
            }
            else if (s == "-s" || s == "--api-secret")
            {
                if (i < length - 1)
                    auth.ApiSecret = args[++i];
            }
            else if (s == "-sk" || s == "--session-key")
            {
                if (i < length - 1)
                    auth.SessionKey = args[++i];
            }
        }

        return auth;
    }
}
