using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel.StatsVMs;


public partial class ArtistStatsViewModel : ObservableObject, IDisposable
{
    private readonly ArtistStatsService _artistService;
    private readonly CompositeDisposable _disposables = new();

    // The UI will bind to these!
    [ObservableProperty] public partial TextStat TextBingeScore { get; set; }
    [ObservableProperty] public partial TextStat TextLoyaltyIndex { get; set; }
    [ObservableProperty] public partial TextStat TextDiscoveryComparison { get; set; }


    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListDeepCuts { get; set; }

    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopAlbums { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopSongs { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }
    [ObservableProperty] public partial IReadOnlyList<InsightStat> ListInsights { get; set; }

    public ArtistStatsViewModel(ArtistStatsService artistService)
    {
        _artistService = artistService;

        _artistService.ListTopSongs.Subscribe(v => ListTopSongs = v).DisposeWith(_disposables);
        _artistService.ListDeepCuts.Subscribe(v => ListDeepCuts = v).DisposeWith(_disposables);

        _artistService.TextLoyaltyIndex.Subscribe(v => TextLoyaltyIndex = v).DisposeWith(_disposables);
        _artistService.TextDiscoveryComparison.Subscribe(v => TextDiscoveryComparison = v).DisposeWith(_disposables);
        _artistService.TextBingeScore.Subscribe(v => TextBingeScore = v).DisposeWith(_disposables);
      
        _artistService.ListTopAlbums.Subscribe(v => ListTopAlbums = v).DisposeWith(_disposables);

        _artistService.ListMonthlyTrend.Subscribe(v => ListMonthlyTrend = v).DisposeWith(_disposables);
        _artistService.ListInsights.Subscribe(v => ListInsights = v).DisposeWith(_disposables);
    }

    // Call this when the Page Navigates to an Artist!
    public void LoadArtist(ObjectId artistId)
    {
        _artistService.SetArtistId(artistId);
    }

    public void Dispose()
    {
        _disposables.Dispose(); // Cleans up Rx memory
    }
}