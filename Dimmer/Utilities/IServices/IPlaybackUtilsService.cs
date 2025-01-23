namespace Dimmer_MAUI.Utilities.IServices;
public interface IPlaybackUtilsService
{

    IObservable<ObservableCollection<SongModelView>> NowPlayingSongs { get; } //to display songs in queue
    IObservable<ObservableCollection<SongModelView>> SecondaryQueue { get; } // This will be used to show songs from playlist
    IObservable<ObservableCollection<SongModelView>> TertiaryQueue { get; } //This will be used to show songs loaded externally
    bool PlaySong(SongModelView? song, PlaybackSource source, double positionInSec = 0);
    bool PlaySong(SongModelView song, bool isPreview=true);
    void PlayNextSong(bool isUserInitiated = true); //to play next song
    void PlayPreviousSong(bool isUserInitiated = true); //to play previous song
    bool StopSong(); //to stop song
    bool PauseResumeSong(double lastPosition, bool isPause=false); //to pause/resume song
    IObservable<MediaPlayerState> PlayerState { get; } //to update play/pause button
    void RemoveSongFromQueue(SongModelView song); //to remove song from queue
    void AddSongToQueue(SongModelView song); //to add song to queue
    Task<bool> LoadSongsFromFolder(List<string> folderPath); //to load songs from folder

    SongModelView? CurrentlyPlayingSong { get; }
    SongModelView PreviouslyPlayingSong { get; }
    SongModelView NextPlayingSong { get; }
    string TotalSongsSizes { get; }
    string TotalSongsDuration { get; }
    bool IsShuffleOn { get; set; }

    int CurrentRepeatMode { get; set; }
    int CurrentRepeatCount { get; set; }
    IObservable<PlaybackInfo> CurrentPosition { get; } //to read position and update slider
    void SeekTo(double positionInSec); // to set position from slider
    void ChangeVolume(double newVolumeValue);
    void SearchSong(string songTitleOrArtistName,List<string>? selectedFilters, int Rating); //to search song with title
    void DecreaseVolume();
    void IncreaseVolume();
    void ToggleShuffle(bool isShuffleOn);
    int ToggleRepeatMode();
    void UpdateSongToFavoritesPlayList(SongModelView song);
    int CurrentQueue { get; set; }
    void UpdateCurrentQueue(IList<SongModelView> songs, int QueueNumber = 1);
    bool PlaySelectedSongsOutsideApp(List<string> filePaths);
    void FullRefresh();
    void DeleteSongFromHomePage(SongModelView song);
    void MultiDeleteSongFromHomePage(ObservableCollection<SongModelView> songs);
    
    //Playlist Section
    ObservableCollection<PlaylistModelView>? AllPlaylists { get; }
    void AddSongToPlayListWithPlayListID(SongModelView song, PlaylistModelView playlistModel);    
    void RemoveSongFromPlayListWithPlayListID(SongModelView song, string playlistID);    
    ObservableCollection<PlaylistModelView> GetAllPlaylists();
    List<SongModelView> GetSongsFromPlaylistID(string playlistID);
    bool DeletePlaylistThroughID(string playlistID);
    string SelectedPlaylistName { get; }

    //Artist Section
    ObservableCollection<ArtistModelView> GetAllArtists();
    ObservableCollection<AlbumModelView> GetAllAlbums();
    void LoadSongsWithSorting(ObservableCollection<SongModelView>? songss = null, bool isFromSearch = false);
    void AddToImmediateNextInQueue(List<SongModelView> songs, bool playNext = true);
    void ReplaceAndPlayQueue(List<SongModelView> songs, bool playFirst = true);

    ObservableCollection<ArtistModelView>? AllArtists { get; }
}