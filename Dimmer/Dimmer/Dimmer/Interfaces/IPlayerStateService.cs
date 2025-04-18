
namespace Dimmer.Interfaces;
public interface IPlayerStateService : IDisposable
{
    /// <summary>
    /// Fires immediately with the last value on subscription.
    /// </summary>
    IObservable<SongModel> CurrentSong { get; }

    /// <summary>
    /// Fires immediately with the last snapshot on subscription.
    /// </summary>
    IObservable<IReadOnlyList<SongModel>> AllSongs { get; }
    IObservable<DimmerPlaybackState> CurrentPlayBackState { get; }

    /// <summary>
    /// Replace the master list of songs.
    /// </summary>
    void LoadAllSongs(IEnumerable<SongModel> songs);

    /// <summary>
    /// Change the “now playing” track.
    /// </summary>
    void SetCurrentSong(SongModel song);
}
public interface IQueueManager<T>
{
    event Action<int, IReadOnlyList<T>>? BatchEnqueued;
    event Action<int, T>? ItemDequeued;

    void Initialize(IEnumerable<T> items, int startIndex = 0);
    T? Next();
    bool HasNext { get; }
}
