using static Dimmer_MAUI.Utilities.Models.BaseEmbeddedView;

namespace Dimmer_MAUI.Utilities.Models;

public partial class ActionsCompleted : RealmObject
{

    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(ActionsCompleted));
    public BaseEmbedded? Instance { get; set; } // = new ();
    public int ActionType { get; set; } // 0 for add, 1 for update, 2 for delete
    public int TargetType { get; set; } // 0 for song, 1 for artist, 2 for album, 3 for genre, 4 playlist, 5 user
    public SongModel? ActionSong { get; set; }
    public ArtistModel? ActionArtist { get; set; }
    public AlbumModel? ActionAlbum { get; set; }
    public GenreModel? ActionGenre { get; set; }
    public PlaylistModel? ActionPlaylist { get; set; }
    public UserModel? ActionUser { get; set; }
    public DateTimeOffset DateRequested { get; set; }
    public bool WasIsBatch { get; set; }
    
    public string? ActionDescription { get; set; }

    
    
    public ActionsCompleted()
    {
        Instance = new BaseEmbedded();

    }
    //public ActionsCompleted(ActionsPending model)
    //{
    //    Instance = model.Instance;
    //    ActionType = model.ActionType;
    //    TargetType = model.TargetType;
    //    ActionSong = model.ActionSong;
    //    ActionArtist = model.ActionArtist;
    //    ActionAlbum = model.ActionAlbum;
    //    ActionGenre = model.ActionGenre;
    //    ActionPlaylist = model.ActionPlaylist;
    //    ActionUser = model.ActionUser;
    //    DateRequested = model.DateRequested;
    //    WasIsBatch = model.IsBatch;
    //    ActionDescription = model.ActionDescription;
    //}
}

public partial class ActionPending : RealmObject
{
    [PrimaryKey]
    public string? LocalDeviceId { get; set; } = GeneralStaticUtilities.GenerateRandomString(nameof(ActionPending));
    public BaseEmbedded? Instance { get; set; }
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


//public partial class ActionsPending: RealmObject
//{
//    public BaseEmbedded? Instance { get; set; }
//    public int ActionType { get; set; } // 0 for add, 1 for update, 2 for delete, 3 addRange, 4 updateRange, 5 deleteRange
//    public int TargetType { get; set; } // 0 for song, 1 for artist, 2 for album, 3 for genre, 4 playlist, 5 user, 6 AAGS link,7 AAS link, 8 DatePlayedAndWasCompletedSongLink
//    public SongModel? ActionSong { get; set; } // to be used to store the song that the action is to be performed on
//    public IList<SongModel>? ActionListOfSong { get; } // to be used to store the list of songs that the action is to be performed on
//    public ArtistModel? ActionArtist { get; set; } // to be used to store the artist that the action is to be performed on and so on down
//    public IList<ArtistModel>? ActionListOfArtist { get; }
//    public AlbumModel? ActionAlbum { get; set; }
//    public IList<AlbumModel>? ActionListOfAlbum { get; }
//    public GenreModel? ActionGenre { get; set; }
//    public IList<GenreModel>? ActionListOfGenre { get; }
//    public PlaylistModel? ActionPlaylist { get; set; }
//    public AlbumArtistGenreSongLink? ActionAlbumArtistGenreSongLink { get; set; }
//    public PlayDateAndCompletionStateSongLink? ActionPlayDateAndCompletionStateSongLink { get; set; }
//    public IList<AlbumArtistGenreSongLink>? ActionListOfAlbumArtistGenreSongLink { get; }
//    public AlbumArtistGenreSongLink? ActionAlbumArtistSongLink { get; set; }
//    public IList<AlbumArtistGenreSongLink>? ActionListOfAlbumArtistSongLink { get; }
//    public IList<PlayDateAndCompletionStateSongLink>? ActionListPlayDateAndCompletionStateSongLink { get; }
//    public IList<PlaylistModel>? ActionListOfPlaylist { get; }
//    public UserModel? ActionUser { get; set; }
//    public DateTimeOffset DateRequested{ get; set; }
//    public bool IsRequestedByUser { get; set; }
//     // to be used to det if the action is present in user local db, it's niche but can help
//    public bool IsBatch { get; set; } 
//     // to be used to identify the user that requested the action
//    public string? ActionDescription { get; set; } // to be used to describe the action
//    //to be used to identify the device that requested the action
//     // to be used to identify the device that requested the action as well
//    public bool IsActionCompleted { get; set; } // to be used to identify if the action has been completed it's mostly for server side in order to know if there was issues when performing action
//    //e.g. if the action was to delete a song and the song was not found in the db, the action would not be completed or network issues, the client will tell and we will retry

//    public ActionsPending(List<SongModel>? songs=null, List<ArtistModel>? artists = null)
//    {
//        if (songs is not null)
//        {
//            ActionListOfSong = songs.Select(x => x).ToList();
//        }

//        if (artists is not null)
//        {
//            ActionListOfArtist = artists.Select(x => x).ToList();
//        }
//    }

//    public ActionsPending()
//    {
//        Instance = new BaseEmbedded();

//    }
//    public ActionsPending(ActionsCompleted model)
//    {
//        ActionType = model.ActionType;
//        TargetType = model.TargetType;
//        ActionSong = model.ActionSong;
//        ActionArtist = model.ActionArtist;
//        ActionAlbum = model.ActionAlbum;
//        ActionGenre = model.ActionGenre;
//        ActionPlaylist = model.ActionPlaylist;
//        ActionUser = model.ActionUser;
//        DateRequested = model.DateRequested;
//        IsBatch = model.WasIsBatch;
//        ActionDescription = model.ActionDescription;
//        Instance = new BaseEmbedded();

//    }
//}

public partial class UserModelView : ObservableObject
{
    [ObservableProperty]
    string? localDeviceId = GeneralStaticUtilities.GenerateRandomString(nameof(UserModelView));
    [ObservableProperty]
    BaseEmbeddedView instance = new();
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
    
    public UserModelView(UserModel model)
    {
        Instance = new(model.Instance);
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
    public BaseEmbedded? Instance { get; set; } // = new ();
    public string? UserName { get; set; } = "User";
    public string? UserPassword { get; set; }
    public string? UserEmail { get; set; }
    public string? CoverImage { get; set; } = string.Empty;
    public DateTimeOffset LastSessionDate { get; set; } = DateTimeOffset.UtcNow;
    public UserModel(UserModelView model)
    {
        Instance = new(model.Instance);
        LocalDeviceId = model.LocalDeviceId;
        UserName = model.UserName;
        UserPassword = model.UserPassword;
        UserEmail = model.UserEmail;
        CoverImage = model.CoverImage;
        LastSessionDate = model.LastSessionDate;
    }
    public UserModel()
    {
        Instance = new BaseEmbedded();
    }
    public async Task SyncOnlineAsync()
    {
        await Task.CompletedTask;
    }




}
