namespace Dimmer.Utilities.Extensions;

public static partial class UiThreads
{
    public static Action<Action>? DispatchAction { get; set; }

}