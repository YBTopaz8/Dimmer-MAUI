using Dimmer.Charts;
using Dimmer.Charts.Services;

namespace Dimmer.ViewModel.StatsVMs;

public partial class ArtistStatsViewModel : ObservableObject, IDisposable
{
    private readonly ArtistStatsService _statsService;
    private readonly CompositeDisposable _disposables = new();

    [ObservableProperty] public partial bool IsLoading { get; set; }

    // --- 6 COMMON PROPERTIES ---
    [ObservableProperty] public partial TextStat TextTotalTime { get; set; }
    [ObservableProperty] public partial TextStat TextLifespan { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListPlaySkipRatio { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListTimeOfDayHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListDayOfWeekHeatmap { get; set; }
    [ObservableProperty] public partial IReadOnlyList<TrendStat> ListMonthlyTrend { get; set; }

    // --- 10 SPECIFIC PROPERTIES ---
    [ObservableProperty] public partial TextStat TextLoyaltyIndex { get; set; }
    [ObservableProperty] public partial TextStat TextCatalogCompletion { get; set; }
    [ObservableProperty] public partial TextStat TextOneHitWonderPct { get; set; }
    [ObservableProperty] public partial TextStat TextBingeRecord { get; set; }
    [ObservableProperty] public partial TextStat TextYoYGrowth { get; set; }
    [ObservableProperty] public partial TextStat TextParetoPrinciple { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopSongs { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListTopAlbums { get; set; }
    [ObservableProperty] public partial IReadOnlyList<LeaderboardItem> ListDeepCuts { get; set; }
    [ObservableProperty] public partial IReadOnlyList<ChartPoint> ListEraPreference { get; set; }

    public ArtistStatsViewModel(ArtistStatsService statsService)
    {
        _statsService = statsService;

        _statsService.IsLoading.Subscribe(v => IsLoading = v).DisposeWith(_disposables);

        // Common
        _statsService.TotalTime.Subscribe(v => TextTotalTime = v).DisposeWith(_disposables);
        _statsService.Lifespan.Subscribe(v => TextLifespan = v).DisposeWith(_disposables);
        _statsService.PlaySkipRatio.Subscribe(v => ListPlaySkipRatio = v).DisposeWith(_disposables);
        _statsService.TimeOfDayHeatmap.Subscribe(v => ListTimeOfDayHeatmap = v).DisposeWith(_disposables);
        _statsService.DayOfWeekHeatmap.Subscribe(v => ListDayOfWeekHeatmap = v).DisposeWith(_disposables);
        _statsService.MonthlyTrend.Subscribe(v => ListMonthlyTrend = v).DisposeWith(_disposables);

        // Specific
        _statsService.LoyaltyIndex.Subscribe(v => TextLoyaltyIndex = v).DisposeWith(_disposables);
        _statsService.CatalogCompletion.Subscribe(v => TextCatalogCompletion = v).DisposeWith(_disposables);
        _statsService.OneHitWonderPct.Subscribe(v => TextOneHitWonderPct = v).DisposeWith(_disposables);
        _statsService.BingeRecord.Subscribe(v => TextBingeRecord = v).DisposeWith(_disposables);
        _statsService.YoYGrowth.Subscribe(v => TextYoYGrowth = v).DisposeWith(_disposables);
        _statsService.ParetoPrinciple.Subscribe(v => TextParetoPrinciple = v).DisposeWith(_disposables);
        _statsService.TopSongs.Subscribe(v => ListTopSongs = v).DisposeWith(_disposables);
        _statsService.TopAlbums.Subscribe(v => ListTopAlbums = v).DisposeWith(_disposables);
        _statsService.DeepCuts.Subscribe(v => ListDeepCuts = v).DisposeWith(_disposables);
        _statsService.EraPreference.Subscribe(v => ListEraPreference = v).DisposeWith(_disposables);
    }

    public void LoadArtist(ObjectId artistId) => _statsService.SetArtistId(artistId);

    public void Dispose() => _disposables.Dispose();
}