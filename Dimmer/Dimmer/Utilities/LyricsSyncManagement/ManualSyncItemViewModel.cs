namespace Dimmer.Utilities.LyricsSyncManagement;
public partial class ManualSyncItemViewModel : ObservableObject
{
    public string Text { get; set; }
    [ObservableProperty] private TimeSpan? _timestamp;
}