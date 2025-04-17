using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Services;
public class PlayerStateService : IPlayerStateService, IDisposable
{
    private bool _disposed = false;

    public BehaviorSubject<SongModel> CurrentSong { get; }
        = new(new SongModel());
    public BehaviorSubject<List<SongModel>> AllSongs { get; }
        = new(new List<SongModel>());

    public void LoadAllSongs(IEnumerable<SongModel> songs)
    {
        AllSongs.OnNext(songs.ToList());

    }

    public void Dispose()
    {
        Dispose(true);
        //GC.SuppressFinalize(this); // Added to suppress finalization
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                CurrentSong.Dispose();
                AllSongs.Dispose();
            }

            // Free unmanaged resources (if any)

            _disposed = true;
        }
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
