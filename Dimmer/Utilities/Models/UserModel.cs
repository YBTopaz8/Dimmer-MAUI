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
    string? userIDOnline;
    [ObservableProperty]
    string? userName = "User";
    [ObservableProperty]
    string? userPassword;
    [ObservableProperty]
    string? userEmail;
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
