namespace Dimmer_MAUI.Utilities.Models;

public partial class UpdateNotification : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; }

}
