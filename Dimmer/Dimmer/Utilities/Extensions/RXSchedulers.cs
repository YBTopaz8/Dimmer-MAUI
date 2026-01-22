using System.Reactive.Concurrency;

namespace Dimmer.Utilities.Extensions;

public static class RxSchedulers
{

    public static readonly IScheduler UI = new MauiUiScheduler();

    public static readonly IScheduler Background = TaskPoolScheduler.Default;
}


public static class SchedulerExtensions
{
    public static IDisposable ScheduleTo(this IScheduler scheduler, Action action)
    {
        // We pass the Action as the "State" to the core Schedule method
        return scheduler.Schedule(action, (sc, state) =>
        {
            state(); // Execute the action
            return Disposable.Empty;
        });
    }
}