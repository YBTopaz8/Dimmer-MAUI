namespace Dimmer.Data.ModelView;
public partial class DimmerStats : ObservableObject
{
    [ObservableProperty]
    public partial SongModel Song { get; set; }
    [ObservableProperty]
    public partial int? Count { get; set; }
    [ObservableProperty]
    public partial int NumberOfTracks { get; set; }
    [ObservableProperty]
    public partial string? TotalSeconds { get; set; }
}
