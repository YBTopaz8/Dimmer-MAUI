namespace Dimmer_MAUI.Utilities.IServices;
public interface IPlaybackUtilsService
{

    IObservable<ObservableCollection<SongsModelView>> NowPlayingSongs { get; } //to display songs in queue
    IObservable<ObservableCollection<SongsModelView>> SecondaryQueue { get; } // This will be used to show songs from playlist
    IObservable<ObservableCollection<SongsModelView>> TertiaryQueue { get; } //This will be used to show songs loaded externally
    Task<bool> PlaySongAsync(SongsModelView song,  int CurrentQueue = 0, 
        ObservableCollection<SongsModelView>? SecQueueSongs = null, double lastPosition = 0); //to play song
    Task<bool> PlayNextSongAsync(); //to play next song
    Task<bool> PlayPreviousSongAsync(); //to play previous song
    Task<bool> StopSongAsync(); //to stop song
    Task<bool> PauseResumeSongAsync(double lastPosition); //to pause/resume song

    void RemoveSongFromQueue(SongsModelView song); //to remove song from queue
    void AddSongToQueue(SongsModelView song); //to add song to queue

    int LoadingSongsProgressPercentage { get; }
    SongsModelView CurrentlyPlayingSong { get; }
    string TotalSongsSizes { get; }
    string TotalSongsDuration { get; }
    bool IsShuffleOn { get; set; }
    int CurrentRepeatMode { get; set; }
    IObservable<PlaybackInfo> CurrentPosition { get; } //to read position and update slider
    Task SetSongPosition(double positionFraction); // to set position from slider
    IObservable<MediaPlayerState> PlayerState { get; } //to update play/pause button
    Task<bool> LoadSongsFromFolder(List<string> folderPath);//to load songs from folder
    void ChangeVolume(double newVolumeValue);
    void SearchSong(string songTitleOrArtistName); //to search song with title
    void DecreaseVolume();
    void IncreaseVolume();
    void ToggleShuffle(bool isShuffleOn);
    int ToggleRepeatMode();
    void UpdateSongToFavoritesPlayList(SongsModelView song);
    int CurrentQueue { get; set; }
    void UpdateCurrentQueue(IList<SongsModelView> songs, int QueueNumber = 1);
    Task<bool> PlaySelectedSongsOutsideAppAsync(string[] filePaths);

    //Playlist Section

    ObservableCollection<PlaylistModelView> AllPlaylists { get; }
    void AddSongToPlayListWithPlayListName(SongsModelView song, string playlistName);
    void AddSongToPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    void RemoveSongFromPlayListWithPlayListID(SongsModelView song, ObjectId playlistID);
    void RemoveSongFromPlayListWithPlayListName(SongsModelView song, string playlistName);
    ObservableCollection<PlaylistModelView> GetAllPlaylists();
    void GetSongsFromPlaylistID(ObjectId playlistID);
    bool DeletePlaylistThroughID(ObjectId playlistID);
    string SelectedPlaylistName { get; }

    //Artist Section
    ObservableCollection<ArtistModelView> GetAllArtists();
    ObservableCollection<AlbumModelView> GetAllAlbums();
    ObservableCollection<ArtistModelView> AllArtists { get; }
    ObservableCollection<SongsModelView> GetallArtistsSongsByAlbumID(ObjectId albumID);
    ObservableCollection<SongsModelView> GetallArtistsSongsByArtistId(ObjectId artistID);
}
