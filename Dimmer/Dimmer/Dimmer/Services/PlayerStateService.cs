using System.Reactive.Disposables;

namespace Dimmer.Services;
public class PlayerStateService : IPlayerStateService
{
    private readonly BehaviorSubject<SongModel> _currentSongSubject;
    private readonly BehaviorSubject<DimmerPlaybackState> _currentPlaybackState;
    private readonly BehaviorSubject<IReadOnlyList<SongModel>> _allSongsSubject;

    public PlayerStateService()
    {
        // Seed with an “empty” SongModel and an empty list
        _currentSongSubject = new BehaviorSubject<SongModel>(new SongModel());
        _currentPlaybackState = new BehaviorSubject<DimmerPlaybackState>(DimmerPlaybackState.Stopped);
        _allSongsSubject    = new BehaviorSubject<IReadOnlyList<SongModel>>(Array.Empty<SongModel>());
    }

    /// <inheritdoc/>
    public IObservable<SongModel> CurrentSong => _currentSongSubject.AsObservable();
    public IObservable<DimmerPlaybackState> CurrentPlayBackState => _currentPlaybackState.AsObservable();

    /// <inheritdoc/>
    public IObservable<IReadOnlyList<SongModel>> AllSongs => _allSongsSubject.AsObservable();

    /// <inheritdoc/>
    public void LoadAllSongs(IEnumerable<SongModel> songs)
    {
        // Emit an immutable snapshot
        var snapshot = songs.ToList().AsReadOnly();
        _allSongsSubject.OnNext(snapshot);
    }

    /// <inheritdoc/>
    public void SetCurrentSong(SongModel song)
    {
        ArgumentNullException.ThrowIfNull(song);
        _currentSongSubject.OnNext(song);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Complete & free both subjects
        _currentSongSubject.OnCompleted();
        _allSongsSubject.OnCompleted();
        _currentSongSubject.Dispose();
        _allSongsSubject.Dispose();
    }
}
public class SubscriptionManager : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private bool _disposed;

    public void Add(IDisposable d)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SubscriptionManager));
        _disposables.Add(d);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Added to suppress finalization
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                _disposables.Dispose();
            }

            // Free unmanaged resources (if any)

            _disposed = true;
        }
    }
}
