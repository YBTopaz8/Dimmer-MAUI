
namespace Dimmer.Interfaces;
public interface IPlayerStateService : IDisposable
{
    /// <summary>
    /// Fires immediately with the last value on subscription.
    /// </summary>
    
    ///
    IObservable<SongModelView> CurrentSong { get; }
    IObservable<string> LatestDeviceLog { get; }
    IObservable<IList<string>> DailyLatestDeviceLogs { get; }

    /// <summary>
    /// Fires immediately with the last snapshot on subscription.
    /// </summary>
    IObservable<IReadOnlyList<SongModel>> AllCurrentSongs { get; }
    IObservable<DimmerPlaybackState> CurrentPlayBackState { get; }
    IObservable<PlaylistModel> CurrentPlaylist { get; }
    IObservable<IReadOnlyList<Window>> CurrentlyOpenWindows { get; }
    IObservable<CurrentPage> CurrentPage{ get; }
    IObservable<SongModel> SecondSelectedSong { get; }
    IObservable<LyricPhraseModel> CurrentLyric { get; }
    IObservable<IReadOnlyList<LyricPhraseModel>> SyncLyrics { get; }

    #region Settings Methods

    void SetCurrentLogMsg(string logMessage);

    #endregion


    /// <summary>
    /// Replace the master list of songs.
    /// </summary>
    void LoadAllSongs(IEnumerable<SongModel> songs);

    /// <summary>
    /// Change the “now playing” track.
    /// </summary>
    void SetCurrentSong(SongModel song);
    void SetCurrentState(DimmerPlaybackState state);
    void AddWindow(Window window);
    void RemoveWindow(Window window);
    void SetCurrentPlaylist(IEnumerable<SongModel> songs, PlaylistModel? Playlist = null);
    void SetCurrentPage(CurrentPage page);
    void AddSongsToCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs);
    void AddSingleSongToCurrentPlaylist(PlaylistModel p, SongModel song);
    void RemoveSongFromCurrentPlaylist(PlaylistModel p, SongModel song);
    void RemoveSongFromCurrentPlaylist(PlaylistModel p, IEnumerable<SongModel> songs);
    void SetSecondSelectdSong(SongModel song);
    void SetSyncLyrics(IEnumerable<LyricPhraseModel> lyric);
    void SetCurrentLyric(LyricPhraseModel lyric);
}
public interface IQueueManager<T>
{
    event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    event Action<int, T>? ItemDequeued;

    void Initialize(IEnumerable<T> items, int startIndex = 0);
    T? Next();
    T? Previous();
    void Clear();
    T? PeekPrevious();
    T? PeekNext();

    bool HasNext { get; }
    int Count { get; }
    T? Current { get; }
}
