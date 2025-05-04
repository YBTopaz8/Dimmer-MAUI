
namespace Dimmer.Interfaces;
/// <summary>
/// 
/// </summary>
/// <seealso cref="System.IDisposable" />
public interface IDimmerStateService : IDisposable
{

    

    /// <summary>
    /// Fires immediately with the last value on subscription.
    /// </summary>
    /// <value>
    /// The current song.
    /// </value>
    IObservable<SongModelView> CurrentSong { get; }
    /// <summary>
    /// Gets the latest device log.
    /// </summary>
    /// <value>
    /// The latest device log.
    /// </value>
    IObservable<AppLogModel> LatestDeviceLog { get; }
    /// <summary>
    /// Gets the daily latest device logs.
    /// </summary>
    /// <value>
    /// The daily latest device logs.
    /// </value>
    IObservable<IList<AppLogModel>> DailyLatestDeviceLogs { get; }

    /// <summary>
    /// Fires immediately with the last snapshot on subscription.
    /// </summary>
    /// <value>
    /// All current songs.
    /// </value>
    IObservable<IReadOnlyList<SongModel>> AllCurrentSongs { get; }
    /// <summary>
    /// Gets the state of the current play back.
    /// </summary>
    /// <value>
    /// The state of the current play back.
    /// </value>
    IObservable<(DimmerPlaybackState State, object? ExtraParameter)> CurrentPlayBackState { get; }
    /// <summary>
    /// Gets the current playlist.
    /// </summary>
    /// <value>
    /// The current playlist.
    /// </value>
    IObservable<PlaylistModel> CurrentPlaylist { get; }
    /// <summary>
    /// Gets the currently open windows.
    /// </summary>
    /// <value>
    /// The currently open windows.
    /// </value>
    IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows { get; }
    /// <summary>
    /// Gets the current page.
    /// </summary>
    /// <value>
    /// The current page.
    /// </value>
    IObservable<CurrentPage> CurrentPage{ get; }
    /// <summary>
    /// Gets the second selected song.
    /// </summary>
    /// <value>
    /// The second selected song.
    /// </value>
    IObservable<SongModel> SecondSelectedSong { get; }
    /// <summary>
    /// Gets the current lyric.
    /// </summary>
    /// <value>
    /// The current lyric.
    /// </value>
    IObservable<LyricPhraseModel> CurrentLyric { get; }
    /// <summary>
    /// Gets the synchronize lyrics.
    /// </summary>
    /// <value>
    /// The synchronize lyrics.
    /// </value>
    IObservable<IReadOnlyList<LyricPhraseModel>> SyncLyrics { get; }
    /// <summary>
    /// Gets the is playing.
    /// </summary>
    /// <value>
    /// The is playing.
    /// </value>
    IObservable<bool> IsPlaying { get; }
    /// <summary>
    /// Gets the device volume.
    /// </summary>
    /// <value>
    /// The device volume.
    /// </value>
    IObservable<double> DeviceVolume { get; }

    #region Settings Methods

    /// <summary>
    /// Sets the current log MSG.
    /// </summary>
    /// <param name="logMessage">The log message.</param>
    void SetCurrentLogMsg(AppLogModel logMessage);

    #endregion


    /// <summary>
    /// Replace the master list of songs.
    /// </summary>
    /// <param name="songs">The songs.</param>
    void LoadAllSongs(IEnumerable<SongModel> songs, bool isShuffle=true);

    /// <summary>
    /// Change the “now playing” track.
    /// </summary>
    /// <param name="song">The song.</param>
    void SetCurrentSong(SongModel song);
    
    void SetCurrentState((DimmerPlaybackState State, object? ExtraParameter) state);
    /// <summary>
    /// Adds the window.
    /// </summary>
    /// <param name="window">The window.</param>
    void AddWindow(Window window);
    /// <summary>
    /// Removes the window.
    /// </summary>
    /// <param name="window">The window.</param>
    void RemoveWindow(Window window);
    /// <summary>
    /// Sets the current playlist.
    /// </summary>
    /// <param name="songs">The songs.</param>
    /// <param name="Playlist">The playlist.</param>
    void SetCurrentPlaylist(IEnumerable<SongModel> songs, PlaylistModel? Playlist = null);
    /// <summary>
    /// Sets the current page.
    /// </summary>
    /// <param name="page">The page.</param>
    void SetCurrentPage(CurrentPage page);
    /// <summary>
    /// Adds the songs to current playlist.
    /// </summary>
    /// <param name="p">The p.</param>
    /// <param name="songs">The songs.</param>
    void AddSongsToCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs);
    /// <summary>
    /// Adds the single song to current playlist.
    /// </summary>
    /// <param name="p">The p.</param>
    /// <param name="song">The song.</param>
    void AddSingleSongToCurrentPlaylist(PlaylistModel p, SongModel song);
    /// <summary>
    /// Removes the song from current playlist.
    /// </summary>
    /// <param name="p">The p.</param>
    /// <param name="song">The song.</param>
    void RemoveSongFromCurrentPlaylist(PlaylistModel p, SongModel song);
    /// <summary>
    /// Removes the song from current playlist.
    /// </summary>
    /// <param name="p">The p.</param>
    /// <param name="songs">The songs.</param>
    void RemoveSongFromCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs);
    /// <summary>
    /// Sets the second selectd song.
    /// </summary>
    /// <param name="song">The song.</param>
    void SetSecondSelectdSong(SongModel song);
    /// <summary>
    /// Sets the synchronize lyrics.
    /// </summary>
    /// <param name="lyric">The lyric.</param>
    void SetSyncLyrics(IEnumerable<LyricPhraseModel> lyric);
    /// <summary>
    /// Sets the current lyric.
    /// </summary>
    /// <param name="lyric">The lyric.</param>
    void SetCurrentLyric(LyricPhraseModel lyric);
    /// <summary>
    /// Sets the device volume.
    /// </summary>
    /// <param name="volume">The volume.</param>
    void SetDeviceVolume(double volume);
}
/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>

