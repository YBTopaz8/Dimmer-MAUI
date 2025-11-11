using System.Reactive.Disposables;

namespace Dimmer.Interfaces.Services;
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
