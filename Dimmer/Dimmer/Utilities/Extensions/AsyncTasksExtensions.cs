namespace Dimmer.Utilities.Extensions;
public static class TaskExtensions
{
    // A simple fire-and-forget extension that can optionally handle errors
    public static async void FireAndForget(this Task task, Action<Exception>? onError = null)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}