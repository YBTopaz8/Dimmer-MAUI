namespace Dimmer_MAUI.Utilities.IServices;
public interface IPlaybackUtilsService
{

    IObservable<ObservableCollection<SongsModelView>> NowPlayingSongs { get; } //to display songs in queue
    IObservable<ObservableCollection<SongsModelView>> BackEndShufflableSongsQueue { get; } 
    IObservable<ObservableCollection<SongsModelView>> SecondaryQueue { get; } // This will be used to show songs from playlist
    IObservable<ObservableCollection<SongsModelView>> TertiaryQueue { get; } //This will be used to show songs loaded externally
    Task<bool> PlaySongAsync(SongsModelView song, int CurrentQueue = 0, ObservableCollection<SongsModelView>? SecQueueSongs = null, 
        double lastPosition = 0, int repeatMode = 0, 
        int repeatMaxCount = 0, 
        bool IsFromPreviousOrNext = false, AppState CurrentAppState = AppState.OnForeGround); //to play song
    Task<bool> PlayNextSongAsync(); //to play next song
    Task<bool> PlayPreviousSongAsync(); //to play previous song
    Task<bool> StopSongAsync(); //to stop song
    Task<bool> PauseResumeSongAsync(double lastPosition, bool isPause=false); //to pause/resume song
    IObservable<MediaPlayerState> PlayerState { get; } //to update play/pause button
    void RemoveSongFromQueue(SongsModelView song); //to remove song from queue
    void AddSongToQueue(SongsModelView song); //to add song to queue

    int LoadingSongsProgressPercentage { get; }
    SongsModelView CurrentlyPlayingSong { get; }
    SongsModelView PreviouslyPlayingSong { get; }
    SongsModelView NextPlayingSong { get; }
    string TotalSongsSizes { get; }
    string TotalSongsDuration { get; }
    bool IsShuffleOn { get; set; }
    int CurrentRepeatMode { get; set; }
    int CurrentRepeatCount { get; set; }
    IObservable<PlaybackInfo> CurrentPosition { get; } //to read position and update slider
    Task SetSongPosition(double positionInSec); // to set position from slider
    Task<bool> LoadSongsFromFolderAsync(List<string> folderPath);//to load songs from folder
    void ChangeVolume(double newVolumeValue);
    void SearchSong(string songTitleOrArtistName,List<string>? selectedFilters, int Rating); //to search song with title
    void DecreaseVolume();
    void IncreaseVolume();
    void ToggleShuffle(bool isShuffleOn);
    int ToggleRepeatMode();
    void UpdateSongToFavoritesPlayList(SongsModelView song);
    int CurrentQueue { get; set; }
    void UpdateCurrentQueue(IList<SongsModelView> songs, int QueueNumber = 1);
    Task<bool> PlaySelectedSongsOutsideAppAsync(List<string> filePaths);

    Task DeleteSongFromHomePage(SongsModelView song);
    Task MultiDeleteSongFromHomePage(ObservableCollection<SongsModelView> songs);
    
    //Playlist Section

    ObservableCollection<PlaylistModelView> AllPlaylists { get; }
    void AddSongToPlayListWithPlayListID(SongsModelView song, PlaylistModelView playlistModel);
    
    void RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    
    ObservableCollection<PlaylistModelView> GetAllPlaylists();
    List<SongsModelView> GetSongsFromPlaylistID(ObjectId playlistID);
    bool DeletePlaylistThroughID(ObjectId playlistID);
    string SelectedPlaylistName { get; }

    //Artist Section
    ObservableCollection<ArtistModelView> GetAllArtists();
    ObservableCollection<AlbumModelView> GetAllAlbums();
    ObservableCollection<ArtistModelView> AllArtists { get; }
    ObservableCollection<SongsModelView> GetAllArtistsAlbumSongsAlbumID(ObjectId albumID);
    ObservableCollection<SongsModelView> GetallArtistsSongsByArtistId(ObjectId artistID);
}