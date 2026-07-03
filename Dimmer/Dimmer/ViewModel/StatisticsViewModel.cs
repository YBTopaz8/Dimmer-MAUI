// Ensure your models/services namespaces are here (e.g., Dimmer.Data.ModelView, etc.)

using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel;



public partial class StatisticsViewModel : ObservableObject
{

    private readonly GeneralStatsService _generalStats;
    private readonly SongStatsService _songStats;
    private readonly CompositeDisposable _disposables = new();

    // Bound to XAML
    [ObservableProperty] public partial TextStat LibraryTime { get; set; }
    [ObservableProperty] public partial TextStat SongSkipRate {get;set;}
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> SongRadarData {get;set;}

    // DynamicData Collection for UI ListView
    private readonly ReadOnlyObservableCollection<SongModelView> _topSongs;
    public ReadOnlyObservableCollection<SongModelView> TopSongs => _topSongs;

    public StatisticsViewModel(GeneralStatsService generalStats, SongStatsService songStats)
    {
        _generalStats = generalStats;
        _songStats = songStats;

        // 1. Bind General Text/Charts
        _generalStats.TotalListeningTime
            .Subscribe(stat => LibraryTime = stat)
            .DisposeWith(_disposables);

        // 2. Bind Leaderboard (DynamicData)
        _generalStats.TopSongsLeaderboard
            .Bind(out _topSongs) // Automatically populates the ObservableCollection!
            .Subscribe()
            .DisposeWith(_disposables);

        // 3. Bind Song Specific Stats
        _songStats.SkipRate
            .Subscribe(stat => SongSkipRate = stat)
            .DisposeWith(_disposables);

        _songStats.ActionRadarChart
            .Subscribe(chartData => SongRadarData = chartData)
            .DisposeWith(_disposables);
    }

    // Called from UI when user clicks a song
    [RelayCommand]
    public void SelectSong(ObjectId songId)
    {
        // THIS IS ALL YOU DO! The Service streams will automatically recalculate 
        // and push new data to the ViewModel subscriptions above.
        _songStats.SetSongId(songId);
    }

    public void Dispose()
    {
        _disposables.Dispose(); // Prevents memory leaks!
    }
}