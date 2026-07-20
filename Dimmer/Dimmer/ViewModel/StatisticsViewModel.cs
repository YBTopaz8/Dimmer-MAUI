// Ensure your models/services namespaces are here (e.g., Dimmer.Data.ModelView, etc.)

using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel;



public partial class StatisticsViewModel : ObservableObject
{
    public BaseViewModel BaseVM => baseVM;
    private readonly BaseViewModel baseVM;
    private readonly GeneralStatsService _generalStats;
    private readonly SongStatsService _songStats;
    private readonly CompositeDisposable _disposables = new();

    // Bound to XAML
    [ObservableProperty] public partial TextStat LibraryTime { get; set; }
    [ObservableProperty] public partial TextStat SongSkipRate {get;set;}
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> SongRadarData {get;set;}
    [ObservableProperty] public partial IReadOnlyList<InsightStat> SongInsights { get;set;}
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> TopSongs { get;set;}


    public StatisticsViewModel(BaseViewModel vm, GeneralStatsService generalStats, SongStatsService songStats)
    {
        baseVM = vm;
        _generalStats = generalStats;
        _songStats = songStats;


        _generalStats.TopSongs
            .Subscribe(stat => TopSongs = stat)
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