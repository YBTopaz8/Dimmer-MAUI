
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;
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
        Dispatch(() => action(this, state));
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
                if (!cts.Token.IsCancellationRequested)
                    Dispatch(() => action(this, state));
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


    private static void Dispatch(Action action)
    {
        if (UiThreads.DispatchAction != null)
        {
            UiThreads.DispatchAction(action);
            return;
        }

        // Background-safe fallback
        Task.Run(action);
    }

}
