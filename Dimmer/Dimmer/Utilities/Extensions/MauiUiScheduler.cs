using System.Reactive.Concurrency;
#if WINDOWS
using Microsoft.UI.Dispatching;
#endif

namespace Dimmer.Utilities.Extensions;

/// <summary>
/// Cross-platform Rx scheduler for dual-window MAUI + WinUI apps.
/// Ensures UI-bound work runs on the correct dispatcher.
/// </summary>
public sealed class MauiUiScheduler : IScheduler
{
    public DateTimeOffset Now => DateTimeOffset.Now;

    public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
    {
        UiThreads.DispatchAction?.Invoke(() =>
        {
            action(this, state);
        });

        return Disposable.Empty;
    }

    public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var cts = new CancellationTokenSource();

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(dueTime, cts.Token).ConfigureAwait(false);

                if(cts.IsCancellationRequested)
                    return;

                UiThreads.DispatchAction?.Invoke(() =>
                {
                    if (!cts.IsCancellationRequested)
                        action(this, state);
                });
            }
            catch (TaskCanceledException) { }
        });

        return Disposable.Create(() => cts.Cancel());
    }

    public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
    {
        var delay = dueTime - Now;
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;

        return Schedule(state, delay, action);
    }


}
