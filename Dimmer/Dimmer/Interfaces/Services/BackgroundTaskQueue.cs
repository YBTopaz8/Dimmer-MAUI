using System.Threading.Channels;

namespace Dimmer.Interfaces.Services;

public sealed class BackgroundTaskQueue : IDisposable
{
    private readonly Channel<Func<Task>> _queue;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    // Configurable retry settings
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public BackgroundTaskQueue(int capacity = 100, int maxRetries = 3, int retryDelayMs = 3000)
    {
        _queue = Channel.CreateBounded<Func<Task>>(capacity);
        _maxRetries = maxRetries;
        _retryDelay = TimeSpan.FromMilliseconds(retryDelayMs);
        _worker = Task.Run(ProcessQueueAsync);
    }

    public async Task EnqueueAsync(Func<Task> taskFactory)
    {
        await _queue.Writer.WriteAsync(taskFactory);
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var taskFactory in _queue.Reader.ReadAllAsync(_cts.Token))
        {
            int attempt = 0;
            while (attempt <= _maxRetries)
            {
                try
                {
                    await taskFactory();
                    break; // success
                }
                catch (IOException ioEx) when (IsFileLocked(ioEx) && attempt < _maxRetries)
                {
                    attempt++;
                    Debug.WriteLine($"[BGQueue] File locked, retrying {attempt}/{_maxRetries}...");
                    await Task.Delay(_retryDelay, _cts.Token);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[BGQueue] Task failed permanently: {ex}");
                    break;
                }
            }
        }
    }

    private static bool IsFileLocked(IOException ex)
    {
        // Common Win32 lock error codes
        return ex.HResult == -2147024864 || ex.HResult == -2147024865;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _queue.Writer.TryComplete();
        try { _worker.Wait(1000); } catch { }
    }
}
