using Parse;

namespace Dimmer_MAUI.Utilities.Models;

public partial class UserModel : RealmObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public string UserName { get; set; } = "User";
    public string? UserPassword { get; set; }
    public string? UserEmail { get; set; }
    public string? CoverImage { get; set; } = string.Empty;
    public IList<ActionsPending>? ActionsPending { get; }
    public IList<ActionsCompleted>? ActionsCompleted { get; }
    public UserDeviceModel? LastSessionDevice { get; set; }
    public DateTimeOffset LastSessionDate { get; set; }
    public IList<UserDeviceModel>? AllUserSessionDevices { get; }
    public required UserDeviceModel CurrentDevice { get; set; }

    public UserModel(UserModelView model)
    {
        UserName = model.UserName;
        UserPassword = model.UserPassword;
        UserEmail = model.UserEmail;
        CoverImage = model.CoverImage;
        LastSessionDevice = model.LastSessionDevice;
        LastSessionDate = model.LastSessionDate;
        CurrentDevice = model.CurrentDevice;        
    }

}

public partial class UserDeviceModel : RealmObject
{    
    public string DeviceName { get; set; } = DeviceInfo.Current.Name;
    public string DeviceModel { get; set; } = DeviceInfo.Current.Model;
    public string DeviceManufacturer { get; set; } = DeviceInfo.Current.Manufacturer;
    public string DeviceVersion { get; set; } = DeviceInfo.Current.VersionString;

}

public partial class ActionsCompleted : RealmObject
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public required int ActionType { get; set; } // 0 for add, 1 for update, 2 for delete
    public required int TargetType { get; set; } // 0 for song, 1 for artist, 2 for album, 3 for genre, 4 playlist, 5 user
    public SongModelView? ActionSong { get; set; }
    public ArtistModelView? ActionArtist { get; set; }
    public SongModelView? ActionAlbum { get; set; }
    public SongModelView? ActionGenre { get; set; }
    public SongModelView? ActionPlaylist { get; set; }
    public UserModelView? ActionUser { get; set; }
    public DateTimeOffset DateRequested { get; set; }
    public bool WasIsBatch { get; set; }
    public ObjectId UserId { get; set; }
    string? ActionDescription { get; set; }

}

public partial class ActionsPending: RealmObject
{
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
    public required int ActionType { get; set; } // 0 for add, 1 for update, 2 for delete
    public required int TargetType { get; set; } // 0 for song, 1 for artist, 2 for album, 3 for genre, 4 playlist, 5 user
    public SongsModel? ActionSong { get; set; }
    public ArtistModel? ActionArtist { get; set; }
    public AlbumModel? ActionAlbum { get; set; }
    public GenreModel? ActionGenre { get; set; }
    public PlaylistModel? ActionPlaylist { get; set; }
    public UserModelView? ActionUser { get; set; }
    public DateTimeOffset DateRequested{ get; set; }
    public bool IsRequestedByUser { get; set; }
    public bool IsBatch { get; set; }
    public ObjectId UserId { get; set; }
    public string? ActionDescription { get; set; }
}

public partial class UserModelView : ObservableObject
{
    [PrimaryKey]
    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    [ObservableProperty]
    public required string userName;

    [ObservableProperty]
    string? userPassword;

    [ObservableProperty]
    string? userEmail;

    [ObservableProperty]
    string? coverImage = string.Empty;
    [ObservableProperty]
    ObservableCollection<ActionsPending>? actionsPending;
    [ObservableProperty]
    ObservableCollection<ActionsCompleted>? actionsCompleted;
    [ObservableProperty]
    UserDeviceModel? lastSessionDevice;
    [ObservableProperty]
    public required UserDeviceModel currentDevice;    
    [ObservableProperty]
    DateTimeOffset lastSessionDate;
    [ObservableProperty]
    ObservableCollection<UserDeviceModel>? allUserSessionDevices;

    public UserModelView(UserModel model)
    {
        model.Id = Id;
        UserName = model.UserName;
        userEmail = model.UserEmail;
        userPassword = model.UserPassword;
        coverImage = model.CoverImage;
        lastSessionDevice = model.LastSessionDevice;
        lastSessionDate = model.LastSessionDate;
        currentDevice = model.CurrentDevice;
        allUserSessionDevices = model.AllUserSessionDevices?.ToObservableCollection();
        actionsPending = model.ActionsPending?.ToObservableCollection();
        actionsCompleted = model.ActionsCompleted?.ToObservableCollection();
    }
}

