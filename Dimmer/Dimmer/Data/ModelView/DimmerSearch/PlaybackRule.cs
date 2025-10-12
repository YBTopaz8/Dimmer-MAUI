namespace Dimmer.Data.ModelView.DimmerSearch;
public partial class PlaybackRule : ObservableObject
{
    [ObservableProperty]
    public partial string Query { get; set; } // e.g., "len:<3:00"

    [ObservableProperty]
    public partial int Priority {get;set;}

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    public Guid Id { get; } = Guid.NewGuid();
}