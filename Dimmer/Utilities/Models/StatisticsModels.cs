namespace Dimmer_MAUI.Utilities.Models;

public partial class PlayCompletionStatusString:ObservableObject
{
    [ObservableProperty]
    public partial string PlayTypeDescription { get; set; }
    [ObservableProperty]
    public partial bool IsPlayedCompletely { get; set; }
}

public partial class PlayTypeSummaryCount : ObservableObject
{
    [ObservableProperty]
    public partial string PlayTypeDescription { get; set; }
    [ObservableProperty]
    public partial int Count { get; set; }
    [ObservableProperty]
    public partial int PlayTypeCode { get; set; }
}

